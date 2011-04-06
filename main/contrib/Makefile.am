SUBDIRS = Mono.Cecil Mono.Debugger.Soft NRefactory Sharpen NSch NGit ICSharpCode.NRefactory ICSharpCode.Decompiler

EXTRA_DIST =  $(srcdir)/extract_makefile_variable.sh

update-ngit:
	rm -rf ngit-source
	git clone --depth=1 git://github.com/slluis/ngit.git ngit-source
	cp NGit/Makefile Makefile.tmp
	cp NGit/Makefile.in Makefile.tmp.in
	cp NGit/Makefile.am Makefile.tmp.am
	rm -rf NGit
	cp -r ngit-source/NGit .
	cp Makefile.tmp NGit/Makefile
	cp Makefile.tmp.in NGit/Makefile.in
	cp Makefile.tmp.am NGit/Makefile.am
	cp NSch/Makefile Makefile.tmp
	cp NSch/Makefile.in Makefile.tmp.in
	cp NSch/Makefile.am Makefile.tmp.am
	rm -rf NSch
	cp -r ngit-source/NSch .
	cp Makefile.tmp NSch/Makefile
	cp Makefile.tmp.in NSch/Makefile.in
	cp Makefile.tmp.am NSch/Makefile.am
	cp Sharpen/Makefile Makefile.tmp
	cp Sharpen/Makefile.in Makefile.tmp.in
	cp Sharpen/Makefile.am Makefile.tmp.am
	rm -rf Sharpen
	cp -r ngit-source/Sharpen .
	cp Makefile.tmp Sharpen/Makefile
	cp Makefile.tmp.in Sharpen/Makefile.in
	cp Makefile.tmp.am Sharpen/Makefile.am
	rm -rf ngit-source
