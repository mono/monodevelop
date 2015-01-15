#import <Cocoa/Cocoa.h>

#include <xamarin/xamarin.h>
#include <xamarin/launch.h>

static int
push_env (const char *variable, const char *value)
{
	size_t len = strlen (value);
	const char *current;
	int rv;
	
	if ((current = getenv (variable)) && *current) {
		char *buf = malloc (len + strlen (current) + 2);
		memcpy (buf, value, len);
		buf[len] = ':';
		strcpy (buf + len + 1, current);
		rv = setenv (variable, buf, 1);
		free (buf);
	} else {
		rv = setenv (variable, value, 1);
	}
	
	return rv;
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

void
xamarin_app_initialize (xamarin_initialize_data *data)
{
	if (data->size != sizeof (xamarin_initialize_data)) {
		fprintf (stderr, "Failed size initialization check.\n");
		data->exit_code = 1;
		data->exit = true;
		return;
	}

	if (!data->is_relaunch) {
		push_env ("DYLD_FALLBACK_LIBRARY_PATH", "/lib:/usr/lib:/Library/Developer/CommandLineTools/usr/lib:/usr/local/lib");
		/* CommandLineTools are needed for OSX 10.9+ */
		push_env ("DYLD_FALLBACK_LIBRARY_PATH", [[data->app_dir stringByAppendingPathComponent:@"Contents/MacOS/lib/monodevelop/bin/"] UTF8String]);
		data->requires_relaunch = true;
		return;
	}
	
	// Use SGen or Boehm?
	// can be overridden with either plist bool MonoUseSGen or MONODEVELOP_USE_SGEN env
	bool use_sgen = YES;
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist) {
		NSNumber *sgen_obj = (NSNumber *) [plist objectForKey:@"MonoUseSGen"];
		if (sgen_obj)
			use_sgen = [sgen_obj boolValue];
	}
	// allow the MONODEVELOP_USE_SGEN environment variable to override the plist value
	use_sgen = env2bool ("MONODEVELOP_USE_SGEN", use_sgen);

	xamarin_set_use_sgen (use_sgen);
	xamarin_set_is_unified (true);

	xamarin_set_bundle_path ([[data->app_dir stringByAppendingPathComponent: @"Contents/Resources/lib/monodevelop/bin"] UTF8String]);
}
