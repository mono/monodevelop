//gcc -m32 monostub.m -o monostub -framework AppKit

#include <stdio.h>
#include <string.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <unistd.h>
#include <dlfcn.h>
#include <errno.h>
#include <ctype.h>
#include <time.h>

#if XM_SYSTEM
#include <main.h>
#include <launch.h>
#include <runtime.h>
#else
typedef int (* mono_main) (int argc, char **argv);
typedef void (* mono_free) (void *ptr);
typedef char * (* mono_get_runtime_build_info) (void);
#endif
typedef void (* gobject_tracker_init) (void *libmono);

#include "monostub-utils.h"

#import <Foundation/Foundation.h>

#if XM_REGISTRAR
extern
#if EXTERN_C
"C"
#endif
int xamarin_create_classes_Xamarin_Mac ();
#endif

#if STATIC_REGISTRAR
extern
#if EXTERN_C
"C"
#endif
void xamarin_create_classes ();
#endif

void *libmono;

static void
exit_with_message (const char *reason, char *argv0)
{
	fprintf (stderr, "Failed to launch: %s", reason);

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
		NSString *mono_download_url = @"https://go.microsoft.com/fwlink/?linkid=835346";
		CFURLRef url = CFURLCreateWithString (NULL, (CFStringRef) mono_download_url, NULL);
		LSOpenCFURLRef (url, NULL);
		CFRelease (url);
	}
	exit (1);
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


static void
run_md_bundle (NSString *appDir, NSArray *arguments)
{
	NSURL *bundleURL = [NSURL fileURLWithPath: appDir];
	pid_t myPID = getpid ();
	NSRunningApplication *mdApp = nil;

	NSArray *runningApplications = [[NSWorkspace sharedWorkspace] runningApplications];
	for (NSRunningApplication *app in runningApplications) {
		if ([[[app bundleURL] path] isEqual:[bundleURL path]] && myPID != [app processIdentifier])
		{
			mdApp = app;
			break;
		}
	}

	if (mdApp != nil) {
		for (int i = 0; i < 10; i++) {
			if ([mdApp isTerminated])
				break;
			[NSThread sleepForTimeInterval:0.5f];
		}
	}

	NSError *error = nil;
	mdApp = [[NSWorkspace sharedWorkspace] launchApplicationAtURL:bundleURL options:NSWorkspaceLaunchAsync configuration:[NSDictionary dictionaryWithObject:arguments forKey:NSWorkspaceLaunchConfigurationArguments] error:&error];

	if (mdApp == nil)
	{
		NSLog(@"Failed to start bundle %@: %@", appDir, error);
		exit (1);
	}
	exit (0);
}

static void
correct_locale(void)
{
	NSString *preferredLanguage;

	preferredLanguage = [[NSLocale preferredLanguages] objectAtIndex: 0];
	// Apply fixups such as zh_HANS/HANT -> zh_CN/TW
	// Strip other languages of remainder so we choose a generic culture.
	if ([preferredLanguage caseInsensitiveCompare:@"zh-hans"] == NSOrderedSame)
		preferredLanguage = @"zh_CN";
	else if ([preferredLanguage caseInsensitiveCompare:@"zh-hant"] == NSOrderedSame)
		preferredLanguage = @"zh_TW";
	else
		preferredLanguage = [[preferredLanguage componentsSeparatedByString:@"-"] objectAtIndex:0];

	setenv("MONODEVELOP_STUB_LANGUAGE", [preferredLanguage UTF8String], 1);
	setenv("LANGUAGE", [preferredLanguage UTF8String], 1);
	setenv("LC_CTYPE", [preferredLanguage UTF8String], 1);
}

static void
try_load_gobject_tracker (void *libmono, char *entry_executable)
{
	void *gobject_tracker;
	NSString *entryExecutable = [[NSString alloc] initWithUTF8String: entry_executable];
	NSString *binDir = [entryExecutable stringByDeletingLastPathComponent];
	NSString *libgobjectPath = [binDir stringByAppendingPathComponent: @"libgobject-tracker.dylib"];
	gobject_tracker = dlopen ((char *)[libgobjectPath UTF8String], RTLD_GLOBAL);
	if (gobject_tracker) {
		gobject_tracker_init _gobject_tracker_init = (gobject_tracker_init) dlsym (gobject_tracker, "gobject_tracker_init");
		if (_gobject_tracker_init) {
			_gobject_tracker_init (libmono);
			printf ("Loaded gobject tracker\n");
			return;
		}
	}
}

static void
run_md_bundle_if_needed(NSString *appDir, int argc, char **argv)
{
    // if we are running inside an app bundle and --start-app-bundle has been passed
    // run the actual bundle and exit.
    if (![appDir isEqualToString:@"."] && argc > 1 && !strcmp(argv[1], "--start-app-bundle")) {
        NSArray *arguments = [NSArray array];
        if (argc > 2) {
            NSString *strings[argc-2];
            for (int i = 0; i < argc-2; i++)
                strings [i] = [[NSString alloc] initWithUTF8String:argv[i+2]];
            arguments = [NSArray arrayWithObjects:strings count:argc-2];
        }
        run_md_bundle (appDir, arguments);
    }
}

static void
init_registrar()
{
#if XM_REGISTRAR
    xamarin_create_classes_Xamarin_Mac ();
#elif STATIC_REGISTRAR
    xamarin_create_classes ();
#endif
}

#ifdef XM_SYSTEM
extern "C"
void xamarin_app_initialize(xamarin_initialize_data *data)
{
	setenv ("MONO_GC_PARAMS", "major=marksweep-conc,nursery-size=8m", 0);

    run_md_bundle_if_needed(data->app_dir, data->argc, data->argv);

    data->requires_relaunch = update_environment ([[data->app_dir stringByAppendingPathComponent:@"Contents"] UTF8String], true);

    if (data->requires_relaunch)
        return;

    correct_locale();
}

extern "C" int
xammac_setup ()
{
    init_registrar();
	return 0;
}
#else
int main (int argc, char **argv)
{
	//clock_t start = clock();
	NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];
	NSString *binDir = [[NSString alloc] initWithUTF8String: "Contents/Resources/lib/monodevelop/bin"];

	// Check if we are running inside an actual app bundle. If we are not, then assume we're being run
	// as part of `make run` and then binDir should be '.'
	NSString *entryExecutable = [[NSString alloc] initWithUTF8String: argv[0]];
	NSArray *components = [NSArray arrayWithObjects:[entryExecutable stringByDeletingLastPathComponent], @"..", @"..", binDir, nil];
	NSString *binDirFullPath = [NSString pathWithComponents:components];
	BOOL isDir = NO;
	if (![[NSFileManager defaultManager] fileExistsAtPath: binDirFullPath isDirectory: &isDir] || !isDir)
		binDir = [[NSString alloc] initWithUTF8String: "."];

	NSString *appDir = [[NSBundle mainBundle] bundlePath];

	run_md_bundle_if_needed(appDir, argc, argv);

	// can be overridden with plist string MonoMinVersion
	NSString *req_mono_version = @"5.2.0.171";
	// can be overridden with either plist bool MonoUseSGen or MONODEVELOP_USE_SGEN env
	bool use_sgen = YES;
	bool need64Bit = false;

	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist) {
		NSNumber *sgen_obj = (NSNumber *) [plist objectForKey:@"MonoUseSGen"];
		if (sgen_obj)
			use_sgen = [sgen_obj boolValue];

		NSString *version_obj = [plist objectForKey:@"MonoMinVersion"];
		if (version_obj && [version_obj length] > 0)
			req_mono_version = version_obj;

		NSNumber *need_64bit_obj = (NSNumber *) [plist objectForKey:@"Mono64Bit"];
		if (need_64bit_obj) {
			need64Bit = [need_64bit_obj boolValue];
		}
	}

	setenv ("MONO_GC_PARAMS", "major=marksweep-conc,nursery-size=8m", 0);

  NSString *exePath;
  char **extra_argv;
  int extra_argc;
#if USE_SIMPLE_PATH
  exePath = [[appDir stringByAppendingPathComponent: binDir] stringByAppendingPathComponent: SIMPLE_PATH];
#else
  NSString *exeName;
	const char *basename;
	struct rlimit limit;

	if (!(basename = strrchr (argv[0], '/')))
		basename = argv[0];
	else
		basename++;

	if (update_environment ([[appDir stringByAppendingPathComponent:@"Contents"] UTF8String], need64Bit)) {
		//printf ("Updated the environment.\n");
		[pool drain];

		return execv (argv[0], argv);
	}

	correct_locale();
	//printf ("Running main app.\n");

	if (getrlimit (RLIMIT_NOFILE, &limit) == 0 && limit.rlim_cur < 1024) {
		limit.rlim_cur = MIN (limit.rlim_max, 1024);
		setrlimit (RLIMIT_NOFILE, &limit);
	}

	exeName = [NSString stringWithFormat:@"%s.exe", basename];
	exePath = [[appDir stringByAppendingPathComponent: binDir] stringByAppendingPathComponent: exeName];
#endif

	// allow the MONODEVELOP_USE_SGEN environment variable to override the plist value
	use_sgen = env2bool ("MONODEVELOP_USE_SGEN", use_sgen);

	libmono = dlopen (use_sgen ? MONO_LIB_PATH ("libmonosgen-2.0.dylib") : MONO_LIB_PATH ("libmono-2.0.dylib"), RTLD_LAZY);

	if (libmono == NULL) {
		fprintf (stderr, "Failed to load libmono%s-2.0.dylib: %s\n", use_sgen ? "sgen" : "", dlerror ());
		NSString *msg = [NSString stringWithFormat:@"This application requires Mono %s or newer.", [req_mono_version UTF8String]];
		exit_with_message ((char *)[msg UTF8String], argv[0]);
	}

	init_registrar();

	try_load_gobject_tracker (libmono, argv [0]);

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
	if (!check_mono_version (mono_version, [req_mono_version UTF8String])) {
		NSString *msg = [NSString stringWithFormat:@"This application requires a newer version (%s+) of the Mono framework.", [req_mono_version UTF8String]];
		exit_with_message ((char *)[msg UTF8String], argv[0]);
	}

	extra_argv = get_mono_env_options (&extra_argc);

	const int injected = 2; /* --debug and exe path */
	char **new_argv = (char **) malloc (sizeof (char *) * (argc + extra_argc + injected + 1));
	int i, n = 0;

	new_argv[n++] = argv[0];
	for (i = 0; i < extra_argc; i++)
		new_argv[n++] = extra_argv[i];

	// enable --debug so that we can get useful stack traces
	new_argv[n++] = (char *) "--debug";

	new_argv[n++] = strdup ([exePath UTF8String]);

	for (i = 1; i < argc; i++)
		new_argv[n++] = argv[i];
	new_argv[n] = NULL;

	free (extra_argv);
	[pool drain];

	//clock_t end = clock();
	//printf("%f seconds to start\n", (float)(end - start) / CLOCKS_PER_SEC);

	return _mono_main (argc + extra_argc + injected, new_argv);
}
#endif
