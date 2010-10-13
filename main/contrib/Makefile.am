SUBDIRS = Mono.Cecil NRefactory Sharpen NSch NGit

EXTRA_DIST =  $(srcdir)/extract_makefile_variable.sh

update-ngit:
	rm -rf ngit-source
	git clone --depth=1 git://github.com/slluis/ngit.git ngit-source
	cp NGit/Makefile.am tmp.am
	rm -rf NGit
	cp -r ngit-source/NGit .
	cp tmp.am NGit/Makefile.am
	cp NSch/Makefile.am tmp.am
	rm -rf NSch
	cp -r ngit-source/NSch .
	cp tmp.am NSch/Makefile.am
	cp Sharpen/Makefile.am tmp.am
	rm -rf Sharpen
	cp -r ngit-source/Sharpen .
	cp tmp.am Sharpen/Makefile.am
	rm -rf ngit-source
