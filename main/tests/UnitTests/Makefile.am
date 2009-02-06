RUNTIME = mono
ASSEMBLY = $(top_builddir)/build/bin/UnitTests.dll

RES = 

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)
NUNIT_CONSOLE = `pkg-config --variable=libdir mono-nunit`/mono/2.0/nunit-console.exe
FIXTURE = /fixture=$(fixture)

REFS =  \
	$(GTK_SHARP_LIBS) \
	$(MONO_NUNIT_LIBS) \
	-r:../../build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	-r:../../build/AddIns/MonoDevelop.AspNet/MonoDevelop.AspNet.dll \
	-r:../../build/AddIns/MonoDevelop.Autotools/MonoDevelop.Autotools.dll \
	-r:../../build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:../../build/AddIns/MonoDevelop.XmlEditor/MonoDevelop.XmlEditor.dll \
	-r:../../build/bin/Mono.TextEditor.dll \
	-r:../../build/bin/MonoDevelop.Components.dll \
	-r:../../build/bin/MonoDevelop.Core.dll \
	-r:../../build/bin/MonoDevelop.Core.Gui.dll \
	-r:../../build/bin/MonoDevelop.Ide.dll \
	-r:../../build/bin/MonoDevelop.Projects.dll \
	-r:../../build/bin/MonoDevelop.Projects.Gui.dll \
	-r:../../build/bin/NRefactory.dll \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	Mono.TextEditor.Tests.DefaultEditActions/CaretMoveTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/InsertNewLineTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/InsertTabTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/RemoveTabTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/SelectionSelectAllTests.cs \
	Mono.TextEditor.Tests/DocumentTests.cs \
	Mono.TextEditor.Tests/FoldingTests.cs \
	Mono.TextEditor.Tests/LineSplitterTests.cs \
	Mono.TextEditor.Tests/SearchTests.cs \
	Mono.TextEditor.Tests/SelectionTests.cs \
	Mono.TextEditor.Tests/SyntaxHighlightingTests.cs \
	Mono.TextEditor.Tests/UndoRedoTests.cs \
	MonoDevelop.CSharpBinding/CodeCompletionAccessibleTests.cs \
	MonoDevelop.CSharpBinding/CodeCompletionBugTests.cs \
	MonoDevelop.CSharpBinding/CodeCompletionCSharp3Tests.cs \
	MonoDevelop.CSharpBinding/CodeCompletionCSharpTests.cs \
	MonoDevelop.CSharpBinding/CodeCompletionOperatorTests.cs \
	MonoDevelop.CSharpBinding/FindMemberVisitorTests.cs \
	MonoDevelop.CSharpBinding/MemberTests.cs \
	MonoDevelop.CSharpBinding/ParameterCompletionTests.cs \
	MonoDevelop.CSharpBinding/TestViewContent.cs \
	MonoDevelop.CSharpBinding/TestWorkbenchWindow.cs \
	MonoDevelop.CSharpBinding/TopLevelTests.cs \
	MonoDevelop.Projects/CompletionDatabaseTests.cs \
	MonoDevelop.Projects/DomCompilationUnitTests.cs \
	MonoDevelop.Projects/DomPersistenceTests.cs \
	MonoDevelop.Projects/LocalCopyTests.cs \
	MonoDevelop.Projects/MakefileTests.cs \
	MonoDevelop.Projects/MdsTests.cs \
	MonoDevelop.Projects/MonoDeveloperFormatTests.cs \
	MonoDevelop.Projects/MSBuildTests.cs \
	MonoDevelop.Projects/PackagingTests.cs \
	MonoDevelop.Projects/ProjectTests.cs \
	MonoDevelop.Projects/SolutionTests.cs \
	MonoDevelop.Projects/TestProjectsChecks.cs \
	MonoDevelop.Projects/TextFormatterTests.cs \
	MonoDevelop.Projects/WorkspaceTests.cs \
	MonoDevelop.Xml.StateEngine/ParsingTests.cs \
	MonoDevelop.Xml.StateEngine/TestParser.cs \
	TestBase.cs \
	Util.cs

all: $(ASSEMBLY)

test: all
	rm -rf ../tmp
	if test -n "$(fixture)"; then \
		fp="/fixture=$(fixture)"; \
	fi; \
	$(MD_LAUNCH_SETUP) MONO_PATH=$(top_builddir)/build/AddIns/BackendBindings:$(top_builddir)/build/AddIns/MonoDevelop.XmlEditor:$(top_builddir)/build/AddIns/MonoDevelop.AspNet mono --debug $(NUNIT_CONSOLE) -noshadow $(top_builddir)/build/bin/UnitTests.dll $$fp

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin/
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(build_resources:%=/resource:%) $(REFS)

include $(top_srcdir)/Makefile.include

