ADDIN_BUILD = $(top_builddir)/build/AddIns/BackendBindings
ASSEMBLY = $(ADDIN_BUILD)/CSharpBinding.dll

PARSE = parse.exe

DLLS = /r:System.Drawing.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.SourceEditor.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/ICSharpCode.SharpRefactory.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll  \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
Gui/ChooseRuntimePanel.cs \
Gui/CodeGenerationPanel.cs \
Parser/SharpDevelopTree/Destructor.cs \
Parser/SharpDevelopTree/Event.cs \
Parser/SharpDevelopTree/Indexer.cs \
Parser/SharpDevelopTree/Constructor.cs \
Parser/SharpDevelopTree/Method.cs \
Parser/SharpDevelopTree/Class.cs \
Parser/SharpDevelopTree/Parameter.cs \
Parser/SharpDevelopTree/AttributeSection.cs \
Parser/SharpDevelopTree/CompilationUnit.cs \
Parser/SharpDevelopTree/Property.cs \
Parser/SharpDevelopTree/Field.cs \
Parser/SharpDevelopTree/ReturnType.cs \
Parser/TypeVisitor.cs \
Parser/CSharpVisitor.cs \
Parser/Resolver.cs \
Parser/Parser.cs \
Parser/ExpressionFinder.cs \
CSharpAmbience.cs \
Project/CSharpCompilerParameters.cs \
CSharpLanguageBinding.cs \
FormattingStrategy/CSharpFormattingStrategy.cs \
CSharpBindingCompilerManager.cs

RES = \
CSharp.glade \
templates/AppConfigFile.xft.xml \
templates/BlankCombine.xpt.xml \
templates/ConsoleProject.xpt.xml \
templates/CSharpSessionStateWebService.xft.xml \
templates/CSharpWebService.xft.xml \
templates/EmptyCSharpFile.xft.xml \
templates/EmptyHTMLFile.xft.xml \
templates/EmptyProject.xpt.xml \
templates/EmptyResourceFile.xft.xml \
templates/EmptyTextFile.xft.xml \
templates/EmptyXMLFile.xft.xml \
templates/GladeSharpProject.xpt.xml \
templates/GladeSharp2Project.xpt.xml \
templates/GnomeSharpProgram.xft.xml \
templates/GnomeSharpProject.xpt.xml \
templates/GnomeSharp2Project.xpt.xml \
templates/GtkSharpProject.xpt.xml \
templates/GtkSharp2Project.xpt.xml \
templates/GtkSharpWindow.xft.xml \
templates/Library.xpt.xml \
icons/C\#.File.EmptyFile \
icons/C\#.File.Form \
icons/C\#.File.FullFile \
icons/C\#.FileIcon \
icons/C\#.File.NewClass \
icons/C\#.File.WebFile \
icons/C\#.Project.DOSProject \
icons/C\#.Project.EmptyProject \
icons/C\#.Project.Form \
icons/C\#.Project.FullProject \
icons/C\#.ProjectIcon \
icons/C\#.Project.Library \
icons/C\#.Project.ServiceProject \
icons/C\#.Project.UserControl \
icons/C\#.Project.WebProject

ADDIN = CSharpBinding.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(DLLS) \
		$(RES:%=/resource:%) $(build_sources)

parse: $(PARSE)

$(PARSE): parse.cs $(ASSEMBLY)
	$(CSC) -out:$@ parse.cs $(DLLS) -r:$(ASSEMBLY)

run-parse-test: $(PARSE)
	MONO_PATH=../../../../../build/AddIns/BackendBindings/:../../../../../build/bin/ mono $(PARSE) .

csharpbindinglibdir = $(MD_ADDIN_DIR)/BackendBindings
csharpbindinglib_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(PARSE) $(PARSE).mdb

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include

