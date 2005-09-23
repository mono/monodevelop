SOURCES=Main.cs
EXE=../../build/bin/PropertyGridTest.exe

all: $(EXE)

$(EXE): $(SOURCES)
	@ mcs /debug /target:exe /out:$(EXE) /r:gtk-sharp.dll /r:pango-sharp.dll /r:../../build/bin/MonoDevelop.Gui.Widgets.dll /r:System.Drawing.dll $(SOURCES)

run: all
	@ mono --debug ../../build/bin/PropertyGridTest.exe
