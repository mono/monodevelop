SUBDIRS = \
	MonoDevelop.CodeAnalysis \
	MonoDevelop.CodeAnalysis.Gendarme \
	MonoDevelop.CodeAnalysis.Smokey

all:

pkgconfig_in_files = monodevelop-codeanalysis.pc.in

pkgconfigdir= $(prefix)/lib/pkgconfig
pkgconfig_DATA = $(pkgconfig_in_files:.pc.in=.pc)

PACKAGES = 

mpacks: $(PACKAGES)
	test -z "packages" || mkdir -p packages
	mdtool setup p $(PACKAGES:%=%) -d:packages
	mdtool setup rb packages

### SYNCH BLOCK: any changes to this line should be kept in sync with the corresponding block in Makefile.am
MD_PKG_CONFIG_PATH=$(gtksharp_prefix)/lib/pkgconfig/:/usr/lib/pkgconfig/:/usr/local/lib/pkgconfig/:/usr/share/pkgconfig/:/usr/local/share/pkgconfig/$(shell if test -d /usr/lib64; then echo ":/usr/lib64/pkgconfig/:/usr/local/lib64/pkgconfig/"; fi)
### END BLOCK

# we can't use the variables that are conditionally defined
EXTRA_DIST = $(pkgconfig_in_files)

DISTCLEANFILES = $(pkgconfig_DATA) 
