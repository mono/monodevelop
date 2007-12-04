SOURCES=Main.cs
EXE=../../build/bin/MonoPad.exe

all: $(EXE)

$(EXE): $(SOURCES)
	@ mcs /debug /target:exe /out:$(EXE) /define:GTK /r:gtk-sharp.dll /r:pango-sharp.dll /r:../../build/bin/ICSharpCode.TextEditor.dll /r:System.Drawing.dll $(SOURCES)
