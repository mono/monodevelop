TARBALL_PATH=../../tarballs/external

NGIT_SLN=ngit/ngit.sln
NGIT=$(NGIT_SLN) /t:NGit
NGIT_OUTPUT = ngit/bin/
NGIT_ASSEMBLIES = $(addprefix $(NGIT_OUTPUT), NGit.dll NSch.dll Sharpen.dll)
NGIT_BINARIES = $(NGIT_ASSEMBLIES) $(addsuffix .mdb, $(NGIT_ASSEMBLIES))

CECIL_SLN=cecil/Mono.Cecil.sln
CECIL=/property:Configuration=net_4_0_Debug $(CECIL_SLN)
CECIL_OUTPUT=cecil/bin/net_4_0_Debug/
CECIL_ASSEMBLIES = $(addprefix $(CECIL_OUTPUT), Mono.Cecil.dll Mono.Cecil.Mdb.dll)
CECIL_BINARIES = $(CECIL_ASSEMBLIES) $(addsuffix .mdb, $(CECIL_ASSEMBLIES))

BINARIES = $(CECIL_BINARIES) $(NGIT_BINARIES)
OUTPUT=$(top_builddir)/build/bin

all:
	mkdir -p $(OUTPUT)
	xbuild $(NGIT)
	xbuild $(CECIL)
	for binary in $(BINARIES); do \
		cp $$binary $(OUTPUT)/; \
	done

clean:
	@if test -f $(NGIT_SLN); then xbuild $(NGIT) /t:Clean; fi
	@if test -f $(CECIL_SLN); then xbuild $(CECIL) /t:Clean; fi
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

