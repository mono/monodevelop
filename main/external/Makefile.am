TARBALL_PATH=../../tarballs/external

CECIL_SLN=cecil/Mono.Cecil.sln
CECIL=/property:Configuration=net_4_0_Debug $(CECIL_SLN)
CECIL_OUTPUT=cecil/bin/net_4_0_Debug/
CECIL_ASSEMBLIES = $(addprefix $(CECIL_OUTPUT), Mono.Cecil.dll Mono.Cecil.Mdb.dll)
CECIL_BINARIES = $(CECIL_ASSEMBLIES) $(addsuffix .mdb, $(CECIL_ASSEMBLIES))

BINARIES = $(CECIL_BINARIES)
OUTPUT=$(top_builddir)/build/bin

all:
	$(MAKE) -C monomac/src
	mkdir -p $(OUTPUT)
	xbuild /property:CodePage=65001 $(CECIL)
	for binary in $(BINARIES); do \
		rsync -u $$binary $(OUTPUT)/ || exit 1; \
	done

clean:
	@if test -f $(CECIL_SLN); then xbuild /property:CodePage=65001 $(CECIL) /t:Clean; fi
	for binary in $(BINARIES); do \
		rm -f $(OUTPUT)/`basename $$binary`; \
	done
install:

uninstall:

distdir: dist

dist:
	mkdir -p $(TARBALL_PATH)
	@for dir in `ls .`; do \
		if test -d $$dir; then \
			echo Creating tarball for $$dir; \
			cd $$dir && git archive --prefix=monodevelop-@PACKAGE_VERSION@/external/$$dir/ HEAD | bzip2 > ../$(TARBALL_PATH)/$$dir-@PACKAGE_VERSION@.tar.bz2 && cd ..; \
		fi \
	done	

dist-clean:
	@for dir in `ls .`; do \
		if test -d $$dir; then \
			rm -f $(TARBALL_PATH)/$$dir.tar.bz2; \
		fi \
	done

