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

#include "monostub-utils.h"

typedef int (* mono_main) (int argc, char **argv);
typedef char * (* mono_get_runtime_build_info) (void);
typedef char * (* mono_parse_options_from) (const char *, int *, char **[]);
typedef void (* monoeg_g_free) (void *ptr);
typedef void (* gobject_tracker_init) (void *libmono);

static NSString *appName;

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
show_alert (NSString *msg, NSString *bundle_name, NSString *mono_download_url)
{
	NSLog (@"Could not launch: %@\n%@\n%@", bundle_name, msg, mono_download_url);
}
#else
static void
show_alert (NSString *msg, NSString *bundle_name, NSString *mono_download_url)
{
	NSAlert *alert = [[NSAlert alloc] init];
	[alert setMessageText:[NSString stringWithFormat:@"Could not launch %@", bundle_name]];
	[alert setInformativeText:msg];
	[alert addButtonWithTitle:@"Download Mono Framework"];
	[alert addButtonWithTitle:@"Cancel"];
	NSInteger answer = [alert runModal];
	[alert release];

	if (answer == NSAlertFirstButtonReturn) {
		[[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:mono_download_url]];
	}
}
#endif

static void
exit_with_message (NSString *reason)
{
	NSString *entryExecutableName = appName;
	NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
	if (plist) {
		entryExecutableName = (NSString *) [plist objectForKey:(NSString *)kCFBundleNameKey];
	}

	NSString *fmt = @"%@\n\nPlease download and install the latest version of Mono.";
	NSString *msg = [NSString stringWithFormat:fmt, reason];
	NSString *mono_download_url = @"https://go.microsoft.com/fwlink/?linkid=835346";

	show_alert(msg, entryExecutableName, mono_download_url);
	exit (1);
}

static void *
load_symbol(const char *symbol_type, void *lib, const char *framework_name)
{
	void *symbol = dlsym (lib, symbol_type);
	if (!symbol) {
		NSLog (@"Could not load %s(): %s", symbol_type, dlerror ());
		NSString *msg = [NSString stringWithFormat:@"Failed to load the %s framework.", framework_name];
		exit_with_message (msg);
	}

	return symbol;
}
#define LOAD_MONO_SYMBOL(symbol_type, libmono) \
	(symbol_type)load_symbol(#symbol_type, libmono, "Mono");

static void
get_mono_env_options (int *ref_argc, char **ref_argv [], void *libmono)
{
	const char *env = getenv ("MONO_ENV_OPTIONS");

	mono_parse_options_from _mono_parse_options_from = LOAD_MONO_SYMBOL(mono_parse_options_from, libmono);

	char *ret = _mono_parse_options_from (env, ref_argc, ref_argv);
	if (ret) {
		exit_with_message ([NSString stringWithUTF8String:ret]);
	}
}

static void
run_md_bundle (NSArray *arguments)
{
	NSURL *bundleURL = [[NSBundle mainBundle] bundleURL];
	int myPID = [[NSProcessInfo processInfo] processIdentifier];
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
		NSLog(@"Failed to start bundle %@: %@", [[NSBundle mainBundle] bundlePath], error);
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
try_load_gobject_tracker (void *libmono)
{
	void *gobject_tracker = dlopen("libgobject-tracker.dylib", RTLD_LAZY);
	if (!gobject_tracker)
		return;

	gobject_tracker_init _gobject_tracker_init = (gobject_tracker_init) dlsym (gobject_tracker, "gobject_tracker_init");
	if (!_gobject_tracker_init)
		return;

	_gobject_tracker_init (libmono);
	printf ("Loaded gobject tracker\n");
}

static void
run_md_bundle_if_needed(int argc, char **argv)
{
	NSString *appDir = [[NSBundle mainBundle] bundlePath];

	// if we are running inside an app bundle and --start-app-bundle has been passed
	// run the actual bundle and exit.
	if (![appDir isEqualToString:@"."] && argc > 1 && !strcmp(argv[1], "--start-app-bundle")) {
		NSArray *arguments = [NSArray array];
		if (argc > 2) {
			int new_argc = argc - 2;
			NSMutableArray *array = [NSMutableArray arrayWithCapacity:new_argc];
			for (int i = 2; i < new_argc; i++)
				[array addObject:[NSString stringWithUTF8String:argv[i]]];
			arguments = array;
		}
		run_md_bundle (arguments);
	}
}

static bool
should_load_xammac_registrar()
{
#if XM_FULL_STATIC_REGISTRAR
	char *registrar_toggle = getenv("MD_DISABLE_STATIC_REGISTRAR");
	if (registrar_toggle)
		return false;

	return dlopen("libvsmregistrar.dylib", RTLD_LAZY);
#else
	return true;
#endif
}

static void
load_xammac()
{
	void *libxammac = dlopen("libxammac.dylib", RTLD_LAZY);
	if (!libxammac) {
		NSLog (@"Failed to load libxammac.dylib %s", dlerror ());
		exit_with_message (@"This application requires Xamarin.Mac native library side-by-side.");
	}

	if (should_load_xammac_registrar()) {
		NSLog (@"loading vsmregistrar");
		XAMARIN_CREATE_CLASSES ();
	}
}

int
main (int argc, char **argv)
{
	int new_argc;
	char **new_argv;
	mono_main _mono_main;

	@autoreleasepool {
		//clock_t start = clock();
		// Check if we are running inside an actual app bundle. If we are not, then assume we're being run
		// as part of `make run` and then binDir should be '.'
		NSString *entryExecutable = [NSString stringWithUTF8String: argv[0]];
		appName = [entryExecutable lastPathComponent];
		NSString *entryExecutableDir = [entryExecutable stringByDeletingLastPathComponent];
		NSString *binDirFullPath = [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"lib/monodevelop/bin"];
		BOOL isDir = NO;
		if (![[NSFileManager defaultManager] fileExistsAtPath: binDirFullPath isDirectory: &isDir] || !isDir)
			binDirFullPath = entryExecutableDir;

		run_md_bundle_if_needed(argc, argv);

		if (update_environment (binDirFullPath)) {
			//printf ("Updated the environment.\n");

			return execv (argv[0], argv);
		}

		correct_locale();
		//printf ("Running main app.\n");

		struct rlimit limit;
		if (getrlimit (RLIMIT_NOFILE, &limit) == 0 && limit.rlim_cur < 1024) {
			limit.rlim_cur = MIN (limit.rlim_max, 1024);
			setrlimit (RLIMIT_NOFILE, &limit);
		}

		// can be overridden with plist string MonoMinVersion
		NSString *req_mono_version = @"5.18.1.24";

		NSDictionary *plist = [[NSBundle mainBundle] infoDictionary];
		if (plist) {
			NSString *version_obj = [plist objectForKey:@"MonoMinVersion"];
			if (version_obj && [version_obj length] > 0)
				req_mono_version = version_obj;
		}

#if HYBRID_SUSPEND_ABORT
		setenv ("MONO_SLEEP_ABORT_LIMIT", "5000", 0);
#endif

		// To be removed: https://github.com/mono/monodevelop/issues/6326
		setenv ("MONO_THREADS_SUSPEND", "preemptive", 0);

		setenv ("MONO_GC_PARAMS", "major=marksweep-conc,nursery-size=8m", 0);

		void *libmono = dlopen ("libmonosgen-2.0.dylib", RTLD_LAZY);

		if (libmono == NULL) {
			NSLog (@"Failed to load libmonosgen-2.0.dylib: %s", dlerror ());
			NSString *msg = [NSString stringWithFormat:@"This application requires Mono %@ or newer.", req_mono_version];
			exit_with_message (msg);
		}

		load_xammac();

		try_load_gobject_tracker (libmono);

		monoeg_g_free _g_free = LOAD_MONO_SYMBOL(monoeg_g_free, libmono);

		_mono_main = LOAD_MONO_SYMBOL(mono_main, libmono);
		mono_get_runtime_build_info _mono_get_runtime_build_info = LOAD_MONO_SYMBOL(mono_get_runtime_build_info, libmono);

		char *mono_version = _mono_get_runtime_build_info ();

		if (!check_mono_version (mono_version, [req_mono_version UTF8String])) {
			NSString *msg = [NSString stringWithFormat:@"This application requires a newer version (%@+) of the Mono framework.", req_mono_version];
			exit_with_message (msg);
		}

		_g_free (mono_version);

		// enable --debug so that we can get useful stack traces and add mono env options
		int mono_argc = 1;
		char **mono_argv = (char **) malloc (sizeof (char *) * mono_argc);

		mono_argv[0] = (char *) "--debug";
		get_mono_env_options (&mono_argc, &mono_argv, libmono);

		// append original arguments
		new_argc = mono_argc + argc + 1;
		new_argv = (char **) malloc (sizeof (char *) * new_argc);
		int n = 0;

		new_argv[n++] = argv[0];
		for (int i = 0; i < mono_argc; i++) {
			new_argv[n++] = strdup (mono_argv[i]);
			if (i > 0)
				_g_free (mono_argv [i]);
		}

		_g_free(mono_argv);

		// append old arguments
		NSString *exePath = [NSString stringWithFormat:@"%@/%@.exe", binDirFullPath, appName];
		new_argv[n++] = strdup ([exePath UTF8String]);
		for (int i = 1; i < argc; i++)
			new_argv[n++] = argv[i];

		//clock_t end = clock();
		//printf("%f seconds to start\n", (float)(end - start) / CLOCKS_PER_SEC);
	}

	return _mono_main (new_argc, new_argv);
}

