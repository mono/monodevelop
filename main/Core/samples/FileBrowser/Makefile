ASSEMBLY = ../../build/bin/FileBrowserTest.exe
MCS = mcs -g
RUNTIME = mono --debug

DLLS = /r:gtk-sharp.dll /r:../../build/bin/MonoDevelop.Gui.Widgets.dll

all:
	$(MCS) test.cs -o $(ASSEMBLY) $(DLLS)

run:
	$(RUNTIME) $(ASSEMBLY)

clean:
	rm -f $(ASSEMBLY)
