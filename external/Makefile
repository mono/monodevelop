SUBDIRS=`ls .`
NGIT=ngit/ngit.sln
TARBALL_PATH=../tarballs

all:
	xbuild $(NGIT) /t:NGit

clean:
	xbuild $(NGIT) /t:NGit /t:Clean

install:

dist:
	mkdir -p $(TARBALL_PATH)
	@for dir in $(SUBDIRS); do \
		if test -d $$dir; then \
			echo Creating tarball for $$dir; \
			cd $$dir && git archive --prefix=$$dir/ HEAD | bzip2 > ../$(TARBALL_PATH)/$$dir.tar.bz2 && cd ..; \
		fi \
	done	

dist-clean:
	@for dir in $(SUBDIRS); do \
		if test -d $$dir; then \
			rm -f $(TARBALL_PATH)/$$dir.tar.bz2; \
		fi \
	done

