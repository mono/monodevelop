SUBDIRS=`ls .`
TARBALL_PATH=../tarballs

NGIT_SLN=ngit/ngit.sln
NGIT=$(NGIT_SLN) /t:NGit

CECIL_SLN=cecil/Mono.Cecil.sln
CECIL=/property:Configuration=net_4_0_Debug $(CECIL_SLN)
CECIL_OUTPUT=cecil/bin/net_4_0_Debug

OUTPUT=../main/build/bin

all:
	mkdir -p $(OUTPUT)
	xbuild $(NGIT)
	cp ngit/bin/* $(OUTPUT)
	xbuild $(CECIL)
	cp $(CECIL_OUTPUT)/Mono.Cecil.dll $(OUTPUT)
	cp $(CECIL_OUTPUT)/Mono.Cecil.dll.mdb $(OUTPUT)
	cp $(CECIL_OUTPUT)/Mono.Cecil.Mdb.dll $(OUTPUT)
	cp $(CECIL_OUTPUT)/Mono.Cecil.Mdb.dll.mdb $(OUTPUT)

clean:
	@if test -f $(NGIT_SLN); then xbuild $(NGIT) /t:Clean; fi
	@if test -f $(CECIL_SLN); then xbuild $(CECIL) /t:Clean; fi

install:

uninstall:

distdir: dist

dist:
	mkdir -p $(TARBALL_PATH)
	@for dir in $(SUBDIRS); do \
		if test -d $$dir; then \
			echo Creating tarball for $$dir; \
			cd $$dir && git archive --prefix=external/$$dir/ HEAD | bzip2 > ../$(TARBALL_PATH)/$$dir.tar.bz2 && cd ..; \
		fi \
	done	

dist-clean:
	@for dir in $(SUBDIRS); do \
		if test -d $$dir; then \
			rm -f $(TARBALL_PATH)/$$dir.tar.bz2; \
		fi \
	done

