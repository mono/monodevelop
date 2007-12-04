MCS = mcs
RUNTIME = mono

all:
	$(MCS) -g test.cs -o ../../build/bin/VfsTest.exe \
	/r:../../build/bin/MonoDevelop.Gui.Utils.dll

run:
	$(RUNTIME) --debug ../../build/bin/VfsTest.exe

clean:
	rm ../../build/bin/VfsTest.exe
