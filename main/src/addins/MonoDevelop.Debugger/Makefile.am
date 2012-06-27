include $(top_srcdir)/xbuild.include

NUNIT_CONSOLE = `pkg-config --variable=libdir mono-nunit`/mono/2.0/nunit-console.exe
FIXTURE = /fixture=$(fixture)

test: all
	if test -n "$(fixture)"; then \
		fp="-fixture=$(fixture)"; \
	fi; \
	$(MD_LAUNCH_SETUP) MONO_PATH=../AddIns/MonoDevelop.Debugger mono --debug $(NUNIT_CONSOLE) -noshadow $(top_builddir)/build/bin/MonoDevelop.Debugger.Tests.dll $$fp
