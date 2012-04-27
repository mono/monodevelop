SUBDIRS=`ls .`
TARBALL_PATH=../tarballs
NGIT=ngit/ngit.sln /t:NGit
CECIL=/property:Configuration=net_4_0_Debug cecil/Mono.Cecil.sln
OUTPUT=../main/build/bin
CECIL_OUTPUT=cecil/bin/net_4_0_Debug

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
	xbuild $(NGIT) /t:Clean
	xbuild $(CECIL) /t:Clean

install:

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

