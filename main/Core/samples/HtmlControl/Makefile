CSC = mcs /debug
EXE = ../../build/bin/HtmlTest.exe

DLLS = -r gtk-sharp -r ../../build/bin/MonoDevelop.Base.dll

all:
	$(CSC) Test.cs $(DLLS) /out:$(EXE)

run:
	mono --debug $(EXE)

clean:
	rm -f $(EXE)
