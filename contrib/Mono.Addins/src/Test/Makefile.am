
SUBDIRS =  UnitTests CommandExtension FileContentExtension HelloWorldExtension SystemInfoExtension FileExtender

NUNIT_CONSOLE = `pkg-config --variable=libdir mono-nunit`/mono/1.0/nunit-console.exe

test: all
	mono --debug $(NUNIT_CONSOLE) UnitTests/bin/Debug/UnitTests.dll