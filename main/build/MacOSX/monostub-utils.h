#include <stdlib.h>
#include <dlfcn.h>
#include <string.h>

#import <Cocoa/Cocoa.h>

#define MONO_LIB_PATH(lib) "/Library/Frameworks/Mono.framework/Libraries/"lib

static int
check_mono_version (const char *version, const char *req_version)
{
	char *req_end, *end;
	long req_val, val;

	while (*req_version && *version) {
		req_val = strtol (req_version, &req_end, 10);
		if (req_version == req_end || (*req_end && *req_end != '.')) {
			NSLog (@"Bad version requirement string '%s'", req_end);
			return FALSE;
		}

		val = strtol (version, &end, 10);
		if (version == end || val < req_val)
			return FALSE;

		if (val > req_val) {
			return TRUE;
		}

		if (*req_end == '.' && *end != '.')
			return FALSE;

		req_version = req_end;
		if (*req_version)
			req_version++;

		version = end + 1;
	}

	return TRUE;
}

static char *
str_append (const char *base, const char *append)
{
	size_t baselen = strlen (base);
	size_t len = strlen (append);
	char *buf;

	if (!(buf = (char *)malloc (baselen + len + 1)))
		abort();

	memcpy (buf, base, baselen);
	strcpy (buf + baselen, append);

	return buf;
}

static NSString *
xcode_get_dev_path ()
{
	NSString *xcode_link = [[NSFileManager defaultManager]
		destinationOfSymbolicLinkAtPath:@"/var/db/xcode_select_link"
		error:nil
	];

	return xcode_link ? xcode_link : @"/Applications/Xcode.app/Contents/Developer";
}

static NSArray<NSString *> *
generate_fallback_paths (NSString *contentsDir)
{
	return @[
		/* Inject our Resources/lib dir */
		[contentsDir stringByAppendingPathComponent:@"Resources/lib"],
		/* Inject our Resources/lib/monodevelop/bin dir so we can load libxammac.dylib */
		[contentsDir stringByAppendingPathComponent:@"Resources/lib/monodevelop/bin"],
		/* Add Xcode's CommandLineTools dev lib dir before Xcode's Developer dir */
		@"/Library/Developer/CommandLineTools/usr/lib",
		/* Add Xcode's dev lib dir into the DYLD_FALLBACK_LIBRARY_PATH */
		xcode_get_dev_path(),
		/* Add Mono's lib dir */
		@"/Library/Frameworks/Mono.framework/Libraries",
		@"/usr/lib",
		@"/usr/local/lib",
	];
}

static bool
push_env (const char *variable, const char *value, BOOL push_to_end)
{
	const char *current;
	size_t len;
	char *buf;
	BOOL updated = YES;
	
	if ((current = getenv (variable)) && *current) {
		char *token, *copy, *tofree;
		size_t current_length;

		tofree = copy = strdup (current);
		current_length = strlen (current);
		len = strlen (value);
		while ((token = strsep(&copy, ":"))) {
			if (!strncmp (token, value, len)) {
				while ((strsep(&copy, ":")))
					continue;

				updated = NO;
				goto done;
			}
		}

		if (!(buf = (char *)malloc (len + current_length + 2)))
			return NO;

		if (push_to_end) {
			memcpy (buf, current, current_length);
			buf[current_length] = ':';
			strcpy (buf + current_length + 1, value);
		} else {
			memcpy (buf, value, len);
			buf[len] = ':';
			strcpy (buf + len + 1, current);
		}
		setenv (variable, buf, 1);
		free (buf);
done:
		free (tofree);
	} else {
		setenv (variable, value, 1);
	}

	return updated;
}

static bool
push_env_to_start (const char *variable, const char *value)
{
	return push_env (variable, value, NO);
}

static bool
push_env_to_end (const char *variable, const char *value)
{
	return push_env (variable, value, YES);
}

static bool
replace_env (const char *variable, const char *value)
{
	const char *old = getenv (variable);

	if (old && !strcmp (old, value))
		return false;

	setenv (variable, value, true);
	return true;
}

static bool
update_environment (NSString *contentsDir)
{
	NSArray<NSString *> *array;
	bool updated = NO;
	char *value;

	if ((array = generate_fallback_paths (contentsDir))) {
			for (NSString *token in array) {
				if (push_env_to_end ("DYLD_FALLBACK_LIBRARY_PATH", [token UTF8String]))
					updated = YES;
			}
	}

	if (push_env_to_start ("PKG_CONFIG_PATH", "/Library/Frameworks/Mono.framework/External/pkgconfig"))
		updated = YES;

	/* Enable the use of stuff bundled into the app bundle and the Mono "External" directory */
	const char *ccontentsDir = [contentsDir UTF8String];
	if ((value = str_append (ccontentsDir, "/Resources/lib/pkgconfig"))) {
		if (push_env_to_start ("PKG_CONFIG_PATH", value))
			updated = YES;

		free (value);
	}

	if ((value = str_append (ccontentsDir, "/Resources"))) {
		if (push_env_to_start ("MONO_GAC_PREFIX", value))
			updated = YES;

		free (value);
	}

	if ((value = str_append (ccontentsDir, "/MacOS"))) {
		if (push_env_to_start ("PATH", value))
			updated = YES;

		free (value);
	}

	// Note: older versions of Xamarin Studio incorrectly set the PATH to the Resources dir instead of the MacOS dir
	// and older versions of mtouch relied on this broken behavior.
	if ((value = str_append (ccontentsDir, "/Resources"))) {
		if (push_env_to_start ("PATH", value))
			updated = YES;

		free (value);
	}

	if (push_env_to_start ("PATH", "/Library/Frameworks/Mono.framework/Commands"))
		updated = YES;

	if (push_env_to_end ("PATH", "/usr/local/bin"))
		updated = YES;

	if (push_env_to_end ("PATH", "~/.dotnet/tools"))
		updated = YES;

	if (replace_env ("MONODEVELOP_64BIT_SAFE", "yes"))
		updated = YES;

	if (replace_env ("LC_NUMERIC", "C"))
		updated = YES;

	return updated;
}
