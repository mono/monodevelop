//gcc -m32 monostub.m -o monostub -framework AppKit

#include <mach-o/dyld.h>
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
#include <limits.h>
#include <libgen.h>

typedef int (* mono_main) (int argc, char **argv);
typedef char * (* mono_get_runtime_build_info) (void);
typedef char * (* mono_parse_options_from) (const char *, int *, char **[]);
typedef void (* gobject_tracker_init) (void *libmono);

#include "monostub-utils.h"

#if STATIC_REGISTRAR
// We have full static registrar enabled.
# define XM_FULL_STATIC_REGISTRAR 1
# define XAMARIN_CREATE_CLASSES xamarin_create_classes
#else
// This means we only have xammac's framework registrar
# define XAMARIN_CREATE_CLASSES xamarin_create_classes_Xamarin_Mac
#endif

extern "C" void XAMARIN_CREATE_CLASSES ();

#if NOGUI
static void
show_alert (NSString *msg, NSString *appName, NSString *mono_download_url)
{
	fprintf(stderr, "Could not launch: %s\n", [appName UTF8String]);
	fprintf(stderr, "%s\n", [msg UTF8String]);
	fprintf(stderr, "%s\n", [mono_download_url UTF8String]);
}
#else
static void
show_alert (NSString *msg, NSString *appName, NSString *mono_download_url)
{
	NSAlert *alert = [[NSAlert alloc] init];
	[alert setMessageText:[NSString stringWithFormat:@"Could not launch %@", appName]];
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
}
#endif

static void
exit_with_message (NSString *reason, NSString *entryExecutableName)
{
	NSString *appName = nil;
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist) {
		appName = (NSString *) [plist objectForKey:(NSString *)kCFBundleNameKey];
	}
	if (!appName) {
		appName = entryExecutableName;
	}

	NSString *fmt = @"%@\n\nPlease download and install the latest version of Mono.";
	NSString *msg = [NSString stringWithFormat:fmt, reason];
	NSString *mono_download_url = @"https://go.microsoft.com/fwlink/?linkid=835346";

	show_alert(msg, appName, mono_download_url);
	exit (1);
}

void *
load_symbol(const char *symbol_type, void *lib, const char *framework_name, NSString *app_name)
{
	void *symbol = dlsym (lib, symbol_type);
	if (!symbol) {
		fprintf (stderr, "Could not load %s(): %s\n", symbol_type, dlerror ());
		NSString *msg = [NSString stringWithFormat:@"Failed to load the %s framework.", framework_name];
		exit_with_message (msg, app_name);
	}

	return symbol;
}
#define LOAD_MONO_SYMBOL(symbol_type, libmono, app_name) (symbol_type)load_symbol(#symbol_type, libmono, "Mono", app_name);

static void
get_mono_env_options (int *ref_argc, char **ref_argv [], void *libmono, NSString *app_name)
{
	const char *env = getenv ("MONO_ENV_OPTIONS");

	mono_parse_options_from _mono_parse_options_from = LOAD_MONO_SYMBOL(mono_parse_options_from, libmono, app_name);

	char *ret = _mono_parse_options_from (env, ref_argc, ref_argv);
	if (ret)
		fprintf(stderr, "%s", ret);
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
	NSString *preferredLanguage = [[[NSBundle mainBundle] preferredLocalizations] firstObject];

	// Apply fixups such as zh_HANS/HANT -> zh_CN/TW
	// Strip other languages of remainder so we choose a generic culture.
	if ([preferredLanguage hasPrefix:@"zh-Hans"])
		preferredLanguage = @"zh_CN";
	else if ([preferredLanguage hasPrefix:@"zh-Hant"])
		preferredLanguage = @"zh_TW";

	preferredLanguage = [preferredLanguage stringByReplacingOccurrencesOfString:@"-" withString:@"_"];

	const char *value = [preferredLanguage UTF8String];
	setenv("MONODEVELOP_STUB_LANGUAGE", value, 1);
	setenv("LANGUAGE", value, 1);
	setenv("LC_CTYPE", value, 1);
}

static void
try_load_gobject_tracker (void *libmono, NSString *entryExecutable)
{
	void *gobject_tracker;
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

#define LOAD_DYLIB(name) \
	name = dlopen ("@loader_path/" #name ".dylib", RTLD_LAZY); \
	if (!name) { \
		name = dlopen ("@loader_path/../Resources/lib/monodevelop/bin/" #name ".dylib", RTLD_LAZY); \
	}

bool
should_load_xammac_registrar(NSString *app_name)
{
	void *libxammac;

	LOAD_DYLIB(libxammac);
	if (!libxammac) {
		fprintf (stderr, "Failed to load libxammac.dylib: %s\n", dlerror ());
		NSString *msg = @"This application requires Xamarin.Mac native library side-by-side.";
		exit_with_message (msg, app_name);
	}

#if XM_FULL_STATIC_REGISTRAR
	char *registrar_toggle = getenv("MD_DISABLE_STATIC_REGISTRAR");
	void *libvsmregistrar = NULL;
	if (!registrar_toggle) {
		LOAD_DYLIB(libvsmregistrar);
	}

	return libvsmregistrar != NULL;
#else
	return true;
#endif
}

int main (int argc, char **argv)
{
	//clock_t start = clock();
	NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];
	NSString *binDir = [[NSString alloc] initWithUTF8String: "Contents/Resources/lib/monodevelop/bin"];

	// Check if we are running inside an actual app bundle. If we are not, then assume we're being run
	// as part of `make run` and then binDir should be '.'
	NSString *entryExecutable = [[NSString alloc] initWithUTF8String: argv[0]];
	NSString *entryExecutableName = [entryExecutable lastPathComponent];
	NSArray *components = [NSArray arrayWithObjects:[entryExecutable stringByDeletingLastPathComponent], @"..", @"..", binDir, nil];
	NSString *binDirFullPath = [NSString pathWithComponents:components];
	BOOL isDir = NO;
	if (![[NSFileManager defaultManager] fileExistsAtPath: binDirFullPath isDirectory: &isDir] || !isDir)
		binDir = [[NSString alloc] initWithUTF8String: "."];

	NSString *appDir = [[NSBundle mainBundle] bundlePath];

	run_md_bundle_if_needed(appDir, argc, argv);

	// can be overridden with plist string MonoMinVersion
	NSString *req_mono_version = @"5.18.1.24";

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
#if HYBRID_SUSPEND_ABORT
	setenv ("MONO_SLEEP_ABORT_LIMIT", "5000", 0);
#endif

	// To be removed: https://github.com/mono/monodevelop/issues/6326
	setenv ("MONO_THREADS_SUSPEND", "preemptive", 0);


	if (update_environment ([[appDir stringByAppendingPathComponent:@"Contents"] UTF8String], need64Bit)) {
		//printf ("Updated the environment.\n");
		[pool drain];

		return execv (argv[0], argv);
	}

	correct_locale();
	//printf ("Running main app.\n");

	struct rlimit limit;
	if (getrlimit (RLIMIT_NOFILE, &limit) == 0 && limit.rlim_cur < 1024) {
		limit.rlim_cur = MIN (limit.rlim_max, 1024);
		setrlimit (RLIMIT_NOFILE, &limit);
	}

	NSString *exeName = [NSString stringWithFormat:@"%@.exe", entryExecutableName];
	NSString *exePath = [[appDir stringByAppendingPathComponent: binDir] stringByAppendingPathComponent: exeName];

	// allow the MONODEVELOP_USE_SGEN environment variable to override the plist value
	use_sgen = env2bool ("MONODEVELOP_USE_SGEN", use_sgen);

	void *libmono = dlopen (use_sgen ? MONO_LIB_PATH ("libmonosgen-2.0.dylib") : MONO_LIB_PATH ("libmono-2.0.dylib"), RTLD_LAZY);

	if (libmono == NULL) {
		fprintf (stderr, "Failed to load libmono%s-2.0.dylib: %s\n", use_sgen ? "sgen" : "", dlerror ());
		NSString *msg = [NSString stringWithFormat:@"This application requires Mono %@ or newer.", req_mono_version];
		exit_with_message (msg, entryExecutableName);
	}

	if (should_load_xammac_registrar (entryExecutableName))
		XAMARIN_CREATE_CLASSES ();

	try_load_gobject_tracker (libmono, entryExecutableName);

	mono_main _mono_main = LOAD_MONO_SYMBOL(mono_main, libmono, entryExecutableName);
	mono_get_runtime_build_info _mono_get_runtime_build_info = LOAD_MONO_SYMBOL(mono_get_runtime_build_info, libmono,
 entryExecutableName);

	char *mono_version = _mono_get_runtime_build_info ();

	if (!check_mono_version (mono_version, [req_mono_version UTF8String])) {
		NSString *msg = [NSString stringWithFormat:@"This application requires a newer version (%@+) of the Mono framework.", req_mono_version];
		exit_with_message (msg, entryExecutableName);
	}

	// enable --debug so that we can get useful stack traces and add mono env options
	int mono_argc = 1;
	char **mono_argv = (char **) malloc (sizeof (char *) * mono_argc);

	mono_argv[0] = (char *) "--debug";
	get_mono_env_options (&mono_argc, &mono_argv, libmono, entryExecutableName);

	// append original arguments
	int new_argc = mono_argc + argc;
	char **new_argv = (char **) malloc (sizeof (char *) * new_argc);
	int n = 0;

	new_argv[0] = argv[0];
	for (int i = 0; i < mono_argc; i++)
		new_argv[n++] = mono_argv[i];

	// append old arguments
	new_argv[n++] = strdup ([exePath UTF8String]);
	for (int i = 1; i < argc; i++)
		new_argv[n++] = argv[i];

	[pool drain];

	//clock_t end = clock();
	//printf("%f seconds to start\n", (float)(end - start) / CLOCKS_PER_SEC);

	return _mono_main (new_argc, new_argv);
}

