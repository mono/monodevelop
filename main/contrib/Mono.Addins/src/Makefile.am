
SUBDIRS =  Mono.Addins Mono.Addins.Setup Mono.Addins.Gui mautil Test Samples

DOCFILES = \
	$(top_builddir)/bin/Mono.Addins.dll \
	$(top_builddir)/bin/Mono.Addins.Setup.dll \
	$(top_builddir)/bin/Mono.Addins.Gui.dll

apidocs:
	mkdir -p docs
	monodocer --delete --pretty $(DOCFILES:%=--assembly:%) --path docs

EXTRA_DIST = mono-addins.snk
