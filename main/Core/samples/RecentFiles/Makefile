
CSC = mcs /debug
EXE = ../../build/bin/RecentFilesTest.exe

DLLS = /r:../../build/bin/MonoDevelop.Gui.Utils.dll

FILES = test.cs \
../../src/Main/Base/Services/File/FdoRecentFiles.cs \
../../src/Main/Base/Services/File/RecentItem.cs

all: $(FILES)
	$(CSC) $(DLLS) $(FILES) /out:$(EXE)

clean:
	rm -f $(EXE)

run:
	mono --debug $(EXE)
