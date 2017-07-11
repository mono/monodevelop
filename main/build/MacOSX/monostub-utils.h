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
			fprintf (stderr, "Bad version requirement string '%s'\n", req_end);
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
		return NULL;

	memcpy (buf, base, baselen);
	strcpy (buf + baselen, append);

	return buf;
}

static char *
xcode_get_dev_path ()
{
	int len;
	char buf[PATH_MAX];
	if ((len = readlink ("/var/db/xcode_select_link", (char*) &buf, PATH_MAX)) > 0) {
		return strndup (buf, len);
	}

	return strdup("/Applications/Xcode.app/Contents/Developer");
}

static char *
generate_fallback_path (const char *contentsDir)
{
	char *lib_dir;
	char *monodevelop_bin_dir;
	char *value;
	char *result;
	char *xcode_dev_path;
	char *xcode_dev_lib_path;
	char *tmp;

	/* Inject our Resources/lib dir */
	lib_dir = str_append (contentsDir, "/Resources/lib:");

	/* Inject our Resources/lib/monodevelop/bin dir so we can load libxammac.dylib */
	monodevelop_bin_dir = str_append (contentsDir, "/Resources/lib/monodevelop/bin:");

	if (lib_dir == NULL || monodevelop_bin_dir == NULL)
		abort ();

	value = str_append (lib_dir, monodevelop_bin_dir);
	if (value == NULL)
		abort ();

	/* Add Mono's lib dirs and Xcode's dev lib dir into the DYLD_FALLBACK_LIBRARY_PATH */
	if ((xcode_dev_path = xcode_get_dev_path ()) != NULL) {
		xcode_dev_lib_path = str_append (xcode_dev_path, "/usr/lib:");
		tmp = value;
		value = str_append (value, xcode_dev_lib_path);
		free (tmp);
		free (xcode_dev_path);
		free (xcode_dev_lib_path);
	}

	result = str_append (value, "/Library/Frameworks/Mono.framework/Libraries:/lib:/usr/lib:/usr/local/lib");

	free (lib_dir);
	free (monodevelop_bin_dir);
	free (value);
	return result;
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
update_environment (const char *contentsDir, bool need64Bit)
{
	bool updated = NO;
	char *value;
	
	if ((value = generate_fallback_path (contentsDir))) {
		char *token;

		while ((token = strsep(&value, ":"))) {
			if (push_env_to_end ("DYLD_FALLBACK_LIBRARY_PATH", token))
				updated = YES;
		}

		free (value);
	}
	
	if (push_env_to_start ("PKG_CONFIG_PATH", "/Library/Frameworks/Mono.framework/External/pkgconfig"))
		updated = YES;

	/* Enable the use of stuff bundled into the app bundle and the Mono "External" directory */
	if ((value = str_append (contentsDir, "/Resources/lib/pkgconfig"))) {
		if (push_env_to_start ("PKG_CONFIG_PATH", value))
			updated = YES;

		free (value);
	}

	if ((value = str_append (contentsDir, "/Resources"))) {
		if (push_env_to_start ("MONO_GAC_PREFIX", value))
			updated = YES;
		
		free (value);
	}
	
	if ((value = str_append (contentsDir, "/MacOS"))) {
		if (push_env_to_start ("PATH", value))
			updated = YES;

		free (value);
	}

	// Note: older versions of Xamarin Studio incorrectly set the PATH to the Resources dir instead of the MacOS dir
	// and older versions of mtouch relied on this broken behavior.
	if ((value = str_append (contentsDir, "/Resources"))) {
		if (push_env_to_start ("PATH", value))
			updated = YES;

		free (value);
	}

	if (push_env_to_start ("PATH", "/Library/Frameworks/Mono.framework/Commands"))
		updated = YES;

	if (push_env_to_end ("PATH", "/usr/local/bin"))
		updated = YES;

	if (need64Bit) {
		if (push_env_to_start ("MONODEVELOP_64BIT_SAFE", "yes")) {
			updated = YES;
		}
	}

	if (replace_env ("LC_NUMERIC", "C"))
		updated = YES;

	return updated;
}
