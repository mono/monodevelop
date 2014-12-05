//gcc -m32 monostub.m -o monostub -framework AppKit

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <unistd.h>
#include <dlfcn.h>
#include <errno.h>
#include <ctype.h>

#import <Cocoa/Cocoa.h>

#define MONO_LIB_PATH(lib) "/Library/Frameworks/Mono.framework/Versions/Current/lib/"lib

typedef int (* mono_main) (int argc, char **argv);
typedef void (* mono_free) (void *ptr);
typedef char * (* mono_get_runtime_build_info) (void);

void *libmono;

static void
exit_with_message (char *reason, char *argv0)
{
	NSString *appName = nil;
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist) {
		appName = (NSString *) [plist objectForKey:(NSString *)kCFBundleNameKey];
	}
	if (!appName) {
		appName = [[NSString stringWithUTF8String: argv0] lastPathComponent];
	}

	NSAlert *alert = [[NSAlert alloc] init];
	[alert setMessageText:[NSString stringWithFormat:@"Could not launch %@", appName]];
	NSString *fmt = @"%s\n\nPlease download and install the latest version of Mono.";
	NSString *msg = [NSString stringWithFormat:fmt, reason]; 
	[alert setInformativeText:msg];
	[alert addButtonWithTitle:@"Download Mono Framework"];
	[alert addButtonWithTitle:@"Cancel"];
	NSInteger answer = [alert runModal];
	[alert release];
	
	if (answer == NSAlertFirstButtonReturn) {
		NSString *mono_download_url = @"http://www.go-mono.com/mono-downloads/download.html";
		CFURLRef url = CFURLCreateWithString (NULL, (CFStringRef) mono_download_url, NULL);
		LSOpenCFURLRef (url, NULL);
		CFRelease (url);
	}
	exit (1);
}

static int
check_mono_version (const char *version, const char *req_version)
{
	char *req_end, *end;
	long req_val, val;
	
	while (*req_version) {
		req_val = strtol (req_version, &req_end, 10);
		if (req_version == req_end || (*req_end && *req_end != '.')) {
			fprintf (stderr, "Bad version requirement string '%s'\n", req_end);
			return FALSE;
		}
		
		req_version = req_end;
		if (*req_version)
			req_version++;
		
		val = strtol (version, &end, 10);
		if (version == end || val < req_val)
			return FALSE;
		
		if (val > req_val)
			return TRUE;
		
		if (*req_version == '.' && *end != '.')
			return FALSE;
		
		version = end + 1;
	}
	
	return TRUE;
}

typedef struct _ListNode {
	struct _ListNode *next;
	char *value;
} ListNode;

static char *
decode_qstring (unsigned char **in, unsigned char qchar)
{
	unsigned char *inptr = *in;
	unsigned char *start = *in;
	char *value, *v;
	size_t len = 0;
	
	while (*inptr) {
		if (*inptr == qchar)
			break;
		
		if (*inptr == '\\') {
			if (inptr[1] == '\0')
				break;
			
			inptr++;
		}
		
		inptr++;
		len++;
	}
	
	v = value = (char *) malloc (len + 1);
	while (start < inptr) {
		if (*start == '\\')
			start++;
		
		*v++ = (char) *start++;
	}
	
	*v = '\0';
	
	if (*inptr)
		inptr++;
	
	*in = inptr;
	
	return value;
}

static char **
get_mono_env_options (int *count)
{
	const char *env = getenv ("MONO_ENV_OPTIONS");
	ListNode *list = NULL, *node, *tail = NULL;
	unsigned char *start, *inptr;
	char *value, **argv;
	int i, n = 0;
	size_t size;
	
	if (env == NULL) {
		*count = 0;
		return NULL;
	}
	
	inptr = (unsigned char *) env;
	
	while (*inptr) {
		while (isblank ((int) *inptr))
			inptr++;
		
		if (*inptr == '\0')
			break;
		
		start = inptr++;
		switch (*start) {
		case '\'':
		case '"':
			value = decode_qstring (&inptr, *start);
			break;
		default:
			while (*inptr && !isblank ((int) *inptr))
				inptr++;
			
			// Note: Mac OS X <= 10.6.8 do not have strndup()
			//value = strndup ((char *) start, (size_t) (inptr - start));
			size = (size_t) (inptr - start);
			value = (char *) malloc (size + 1);
			memcpy (value, start, size);
			value[size] = '\0';
			break;
		}
		
		node = (ListNode *) malloc (sizeof (ListNode));
		node->value = value;
		node->next = NULL;
		n++;
		
		if (tail != NULL)
			tail->next = node;
		else
			list = node;
		
		tail = node;
	}
	
	*count = n;
	
	if (n == 0)
		return NULL;
	
	argv = (char **) malloc (sizeof (char *) * (n + 1));
	i = 0;
	
	while (list != NULL) {
		node = list->next;
		argv[i++] = list->value;
		free (list);
		list = node;
	}
	
	argv[i] = NULL;
	
	return argv;
}

static bool
push_env (const char *variable, const char *value)
{
	const char *current;
	size_t len;
	char *buf;
	
	if ((current = getenv (variable)) && *current) {
		len = strlen (value);
		
		if (!strncmp (current, value, len) && (current[len] == ':' || current[len] == '\0'))
			return NO;
		
		if (!(buf = malloc (len + strlen (current) + 2)))
			return NO;
		
		memcpy (buf, value, len);
		buf[len] = ':';
		strcpy (buf + len + 1, current);
		setenv (variable, buf, 1);
		free (buf);
	} else {
		setenv (variable, value, 1);
	}

	//printf ("Updated the %s environment variable.\n", variable);
	
	return YES;
}

static char *
str_append (const char *base, const char *append)
{
	size_t baselen = strlen (base);
	size_t len = strlen (append);
	char *buf;
	
	if (!(buf = malloc (baselen + len + 1)))
		return NULL;
	
	memcpy (buf, base, baselen);
	strcpy (buf + baselen, append);
	
	return buf;
}

static char *
generate_fallback_path (const char *contentsDir)
{
	char *lib_dir;
	char *monodevelop_bin_dir;
	char *value;
	char *result;

	/* Inject our Resources/lib dir */
	lib_dir = str_append (contentsDir, "/Resources/lib:");

	/* Inject our Resources/lib/monodevelop/bin dir so we can load libxammac.dylib */
	monodevelop_bin_dir = str_append (contentsDir, "/Resources/lib/monodevelop/bin:");

	if (lib_dir == NULL || monodevelop_bin_dir == NULL)
		abort ();

	value = str_append (lib_dir, monodevelop_bin_dir);
	if (value == NULL)
		abort ();

	/* Mono's lib dir, and CommandLineTool's lib dir into the DYLD_FALLBACK_LIBRARY_PATH */
	result = str_append (value, "/Library/Frameworks/Mono.framework/Versions/Current/lib:/lib:/usr/lib:/Library/Developer/CommandLineTools/usr/lib:/usr/local/lib");

	free (lib_dir);
	free (monodevelop_bin_dir);
	free (value);
	return result;
}

static bool
update_environment (const char *contentsDir)
{
	bool updated = NO;
	char *value;
	
	if ((value = generate_fallback_path (contentsDir))) {
		if (push_env ("DYLD_FALLBACK_LIBRARY_PATH", value))
			updated = YES;
		
		free (value);
	}
	
	/* Enable the use of stuff bundled into the app bundle and the Mono "External" directory */
	if ((value = str_append (contentsDir, "/Resources/lib/pkgconfig:/Library/Frameworks/Mono.framework/External/pkgconfig"))) {
		if (push_env ("PKG_CONFIG_PATH", value))
			updated = YES;
		
		free (value);
	}

	if ((value = str_append (contentsDir, "/Resources"))) {
		if (push_env ("MONO_GAC_PREFIX", value))
			updated = YES;
		
		free (value);
	}
	
	if ((value = str_append (contentsDir, "/MacOS"))) {
		char *compat;
		
		// Note: older versions of Xamarin Studio incorrectly set the PATH to the Resources dir instead of the MacOS dir
		// and older versions of mtouch relied on this broken behavior.
		if ((compat = str_append (contentsDir, "/Resources"))) {
			size_t compatlen = strlen (compat);
			size_t valuelen = strlen (value);
			char *combined;
			
			if ((combined = malloc (compatlen + valuelen + 2))) {
				memcpy (combined, compat, compatlen);
				combined[compatlen] = ':';
				strcpy (combined + compatlen + 1, value);

				if (push_env ("PATH", combined))
					updated = YES;
				
				free (combined);
			} else {
				// if we can't combine them, set the old (incorrect) compat value
				if (push_env ("PATH", compat))
					updated = YES;
			}
			
			free (compat);
		} else {
			if (push_env ("PATH", value))
				updated = YES;
		}
		
		free (value);
	}

	return updated;
}

static bool
env2bool (const char *env, bool defaultValue)
{
	const char *value;
	bool nz = NO;
	int i;
	
	if (!(value = getenv (env)))
		return defaultValue;
	
	if (!strcasecmp (value, "true"))
		return YES;
	
	if (!strcasecmp (value, "yes"))
		return YES;
	
	/* check to see if the value is numeric. All numeric values evaluate to true *except* zero */
	for (i = 0; value[i]; i++) {
		if (!isdigit ((int) ((unsigned char) value[i])))
			return NO;
		
		if (value[i] != '0')
			nz = YES;
	}
	
	return nz;
}

int main (int argc, char **argv)
{
	NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];
	NSString *binDir = [[NSString alloc] initWithUTF8String: "Contents/Resources/lib/monodevelop/bin"];
	NSString *appDir = [[NSBundle mainBundle] bundlePath];
	// can be overridden with plist string MonoMinVersion
	NSString *req_mono_version = @"3.10";
	// can be overridden with either plist bool MonoUseSGen or MONODEVELOP_USE_SGEN env
	bool use_sgen = YES;

	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist) {
		NSNumber *sgen_obj = (NSNumber *) [plist objectForKey:@"MonoUseSGen"];
		if (sgen_obj)
			use_sgen = [sgen_obj boolValue];
		
		NSString *version_obj = [plist objectForKey:@"MonoMinVersion"];
		if (version_obj && [version_obj length] > 0)
			req_mono_version = version_obj;
	}

	NSString *exePath, *exeName;
	const char *basename;
	struct rlimit limit;
	char **extra_argv;
	int extra_argc;
	
	if (!(basename = strrchr (argv[0], '/')))
		basename = argv[0];
	else
		basename++;
	
	if (update_environment ([[appDir stringByAppendingPathComponent:@"Contents"] UTF8String])) {
		//printf ("Updated the environment.\n");
		[pool drain];
		
		return execv (argv[0], argv);
	}

	//printf ("Running main app.\n");
	
	if (getrlimit (RLIMIT_NOFILE, &limit) == 0 && limit.rlim_cur < 1024) {
		limit.rlim_cur = MIN (limit.rlim_max, 1024);
		setrlimit (RLIMIT_NOFILE, &limit);
	}
	
	exeName = [NSString stringWithFormat:@"%s.exe", basename];
	exePath = [[appDir stringByAppendingPathComponent: binDir] stringByAppendingPathComponent: exeName];
	
	// allow the MONODEVELOP_USE_SGEN environment variable to override the plist value
	use_sgen = env2bool ("MONODEVELOP_USE_SGEN", use_sgen);
	
	libmono = dlopen (use_sgen ? MONO_LIB_PATH ("libmonosgen-2.0.dylib") : MONO_LIB_PATH ("libmono-2.0.dylib"), RTLD_LAZY);
	
	if (libmono == NULL) {
		fprintf (stderr, "Failed to load libmono%s-2.0.dylib: %s\n", use_sgen ? "sgen" : "", dlerror ());
		exit_with_message ("This application requires the Mono framework.", argv[0]);
	}
	
	mono_main _mono_main = (mono_main) dlsym (libmono, "mono_main");
	if (!_mono_main) {
		fprintf (stderr, "Could not load mono_main(): %s\n", dlerror ());
		exit_with_message ("Failed to load the Mono framework.", argv[0]);
	}
	
	mono_free _mono_free = (mono_free) dlsym (libmono, "mono_free");
	if (!_mono_free) {
		fprintf (stderr, "Could not load mono_free(): %s\n", dlerror ());
		exit_with_message ("Failed to load the Mono framework.", argv[0]);
	}
	
	mono_get_runtime_build_info _mono_get_runtime_build_info = (mono_get_runtime_build_info) dlsym (libmono, "mono_get_runtime_build_info");
	if (!_mono_get_runtime_build_info) {
		fprintf (stderr, "Could not load mono_get_runtime_build_info(): %s\n", dlerror ());
		exit_with_message ("Failed to load the Mono framework.", argv[0]);
	}
	
	char *mono_version = _mono_get_runtime_build_info ();
	if (!check_mono_version (mono_version, [req_mono_version UTF8String]))
		exit_with_message ("This application requires a newer version of the Mono framework.", argv[0]);
	
	extra_argv = get_mono_env_options (&extra_argc);
	
	const int injected = 2; /* --debug and exe path */
	char **new_argv = (char **) malloc (sizeof (char *) * (argc + extra_argc + injected + 1));
	int i, n = 0;
	
	new_argv[n++] = argv[0];
	for (i = 0; i < extra_argc; i++)
		new_argv[n++] = extra_argv[i];
	
	// enable --debug so that we can get useful stack traces
	new_argv[n++] = "--debug";
	
	new_argv[n++] = strdup ([exePath UTF8String]);
	
	for (i = 1; i < argc; i++)
		new_argv[n++] = argv[i];
	new_argv[n] = NULL;
	
	free (extra_argv);
	[pool drain];
	
	return _mono_main (argc + extra_argc + injected, new_argv);
}
