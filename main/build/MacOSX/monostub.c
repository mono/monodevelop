#include <stdlib.h>
#include <stdio.h>
#include <dlfcn.h>

typedef int (*MonoMain) (int argc, char* argv[]);

int main (int argc, char *argv[])
{
	void *libmono = dlopen ("libmono-2.0.dylib", RTLD_LAZY);
	if (libmono == NULL) {
		libmono = dlopen ("libmono-0.dylib", RTLD_LAZY);
		if(libmono == NULL) {
			printf ("Could not load libmono\n");
			exit (1);
		}
	}

	MonoMain mono_main = (MonoMain) dlsym (libmono, "mono_main");
	if (mono_main == NULL) {
		printf ("Could not load mono_main\n");
		exit (2);
	}

	return mono_main (argc, argv);
}
