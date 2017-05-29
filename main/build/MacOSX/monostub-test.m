#include <stdio.h>

#include "monostub-utils.h"

void fail(void)
{
	NSLog(@"%@", [NSThread callStackSymbols]);
	exit(1);
}

void check_string_equal(const char *expected, const char *actual)
{
	if (strcmp(expected, actual)) {
		printf("Expected '%s'\nActual   '%s'\n", expected, actual);
		fail();
	}
}

void check_bool_equal(int expected, int actual)
{
	if (expected != actual) {
		printf("Expected '%d'\nActual   '%d'\n", expected, actual);
		fail();
	}
}

void test_mono_lib_path(void)
{
	char *expected = "/Library/Frameworks/Mono.framework/Libraries/test";
	char *actual = MONO_LIB_PATH("test");

	check_string_equal(expected, actual);
}

void test_check_mono_version(void)
{
	typedef struct {
		char *mono_version, *req_mono_version;
		int expected;
	} version_check;

	version_check versions[] = {
		// Lower than requirement
		{ "3.0", "3.1", FALSE },

		// Higher than requirement
		{ "3.1", "3.0", TRUE },

		// Release lower than requirement.
		{ "3.1", "3.1.1", FALSE },

		// Release higher than requirement.
		{ "3.1.1", "3.1", TRUE },

		{ "5.2.0.138", "5.2.0.130", TRUE },

		{ "5.2.0.138 (2017-04/f1196da)", "5.2.0.138", TRUE },

		// Bogus requirement value.
		{ "3.1", "BOGUS STRING", FALSE },
	};

	version_check *version;
	int i;
	for (i = 0; i < sizeof(versions) / sizeof(version_check); ++i) {
		version = &versions[i];
		check_bool_equal(version->expected, check_mono_version(version->mono_version, version->req_mono_version));
	}
}

void test_str_append(void)
{
	char *str = "asdf";
	char *conc = str_append(str, str);

	check_string_equal("asdfasdf", conc);
}

void test_env2bool(void)
{
	typedef struct {
		bool expected, defaultValue;
		const char *var, *value;
	} bool_check;

	bool_check bools[] = {
		// If variable does not exist, return default.
		{ TRUE, TRUE, "WILL_NOT_EXIST", NULL },
		{ FALSE, FALSE, "WILL_NOT_EXIST", NULL },

		// Check that truth-y values are true.
		{ TRUE, FALSE, "WILL_EXIST", "TRUE" },
		{ TRUE, FALSE, "WILL_EXIST", "YES" },
		{ TRUE, FALSE, "WILL_EXIST", "1" },

		// Check that false-y values are false.
		{ FALSE, TRUE, "WILL_EXIST", "BOGUS" },
		{ FALSE, TRUE, "WILL_EXIST", "0" },
	};

	bool_check *current;
	int i;
	for (i = 0; i < sizeof(bools) / sizeof(bool_check); ++i) {
		current = &bools[i];
		if (current->value)
			setenv(current->var, current->value, 1);

		check_bool_equal(current->expected, env2bool(current->var, current->defaultValue));
	}
}

void test_push_env(void)
{
	typedef struct {
		bool expected;
		const char *var, *initial, *to_find, *updated;
	} push_env_check;

	const char *three_part = "/usr/lib:/lib:/etc";
	push_env_check checks[] = {
		// We don't have an initial value.
		{ TRUE, "WILL_NOT_EXIST", NULL, "/usr/lib", "/usr/lib" },

		// First component matches.
		{ FALSE, "WILL_EXIST", three_part, "/usr/lib", three_part },

		// Middle component matches.
		{ FALSE, "WILL_EXIST", three_part, "/lib", three_part },

		// End component matches.
		{ FALSE, "WILL_EXIST", three_part, "/etc", three_part },

		// Add a non existing component.
		{ TRUE, "WILL_EXIST", three_part, "/Library", "/Library:/usr/lib:/lib:/etc" },
	};

	push_env_check *current;
	int i;
	for (i = 0; i < sizeof(checks) / sizeof(push_env_check); ++i) {
		current = &checks[i];
		if (current->initial)
			setenv(current->var, current->initial, 1);

		check_bool_equal(current->expected, push_env_to_start(current->var, current->to_find));
		check_string_equal(current->updated, getenv(current->var));
	}
}

void check_path_has_components(char *path, const char **components, int count)
{
	char *token, *tofree, *copy;

	for (int i = 0; i < count; ++i) {
		BOOL found = FALSE;
		tofree = copy = strdup(path);

		while ((token = strsep(&copy, ":"))) {
			if (!strncmp(token, components[i], strlen(components[i])))
				found = TRUE;
		}

		if (!found) {
			printf("Expected '%s'\nIn       '%s'", components[i], tofree);
			fail();
		}
		free(tofree);
	}
}

void test_update_environment(void)
{
	const char *path_components[] = {
		"/Library/Frameworks/Mono.framework/Commands",
		"./Resources",
		"./MacOS",
	};
	const char *dyld_components[] = {
		"/usr/local/lib",
		"/usr/lib",
		"/lib",
		"/Library/Frameworks/Mono.framework/Libraries",
		"./Resources/lib/monodevelop/bin",
		"./Resources/lib",
	};
	const char *pkg_components[] = {
		"./Resources/lib/pkgconfig",
		"/Library/Frameworks/Mono.framework/External/pkgconfig",
	};
	const char *gac_components[] = {
		"./Resources",
	};
	const char *safe_components[] = {
		"yes",
	};
	const char *numeric_components[] = {
		"C",
	};

	// Check that we only get updates one time, that's how monostub works.
	check_bool_equal(TRUE, update_environment(".", true));
	check_bool_equal(FALSE, update_environment(".", true));


	check_path_has_components(getenv("DYLD_FALLBACK_LIBRARY_PATH"), dyld_components, sizeof(dyld_components) / sizeof(char *));
	check_path_has_components(getenv("PATH"), path_components, sizeof(path_components) / sizeof(char *));
	check_path_has_components(getenv("PKG_CONFIG_PATH"), pkg_components, sizeof(pkg_components) / sizeof(char *));
	check_path_has_components(getenv("MONO_GAC_PREFIX"), gac_components, sizeof(gac_components) / sizeof(char *));
	check_path_has_components(getenv("MONODEVELOP_64BIT_SAFE"), safe_components, sizeof(safe_components) / sizeof (char *));
	check_path_has_components(getenv("LC_NUMERIC"), numeric_components, sizeof(numeric_components) / sizeof(char *));
}

void (*tests[])(void) = {
	test_mono_lib_path,
	test_check_mono_version,
	test_str_append,
	test_env2bool,
	test_push_env,
	test_update_environment,
};

int main(int argc, char **argv)
{
	for (int i = 0; i < sizeof(tests) / sizeof(void *); ++i)
		tests[i]();
	return 0;
}
