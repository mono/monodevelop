#include <stdlib.h>
#include <string.h>

#import <Cocoa/Cocoa.h>

#define MONO_LIB_PATH(lib) "/Library/Frameworks/Mono.framework/Libraries/"lib

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
	result = str_append (value, "/Library/Frameworks/Mono.framework/Libraries:/lib:/usr/lib:/Library/Developer/CommandLineTools/usr/lib:/usr/local/lib");

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
push_env (const char *variable, const char *value)
{
	const char *current;
	size_t len;
	char *buf;
	BOOL updated = YES;
	
	if ((current = getenv (variable)) && *current) {
		char *token, *copy, *tofree;

		tofree = copy = strdup (current);
		len = strlen (value);
		while ((token = strsep(&copy, ":"))) {
			if (!strncmp (token, value, len)) {
				while ((strsep(&copy, ":")))
					continue;

				updated = NO;
				goto done;
			}
		}

		if (!(buf = malloc (len + strlen (current) + 2)))
			return NO;
		
		memcpy (buf, value, len);
		buf[len] = ':';
		strcpy (buf + len + 1, current);
		setenv (variable, buf, 1);
		free (buf);
done:
		free (tofree);
	} else {
		setenv (variable, value, 1);
	}

	//if (updated)
	//	printf ("Updated the %s environment variable with '%s'.\n", variable, value);

	return updated;
}

static bool
update_environment (const char *contentsDir)
{
	bool updated = NO;
	char *value;
	
	if ((value = generate_fallback_path (contentsDir))) {
		char *token;

		while ((token = strsep(&value, ":"))) {
			if (push_env ("DYLD_FALLBACK_LIBRARY_PATH", token))
				updated = YES;
		}

		free (value);
	}
	
	if (push_env ("PKG_CONFIG_PATH", "/Library/Frameworks/Mono.framework/External/pkgconfig"))
		updated = YES;

	/* Enable the use of stuff bundled into the app bundle and the Mono "External" directory */
	if ((value = str_append (contentsDir, "/Resources/lib/pkgconfig"))) {
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
		if (push_env ("PATH", value))
			updated = YES;

		free (value);
	}

	// Note: older versions of Xamarin Studio incorrectly set the PATH to the Resources dir instead of the MacOS dir
	// and older versions of mtouch relied on this broken behavior.
	if ((value = str_append (contentsDir, "/Resources"))) {
		if (push_env ("PATH", value))
			updated = YES;

		free (value);
	}

	if (push_env ("PATH", "/Library/Frameworks/Mono.framework/Commands"))
		updated = YES;

	return updated;
}

