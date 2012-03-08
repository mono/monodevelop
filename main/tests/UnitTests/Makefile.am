RUNTIME = mono
ASSEMBLY = $(top_builddir)/build/bin/UnitTests.dll

RES = 

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)
NUNIT_CONSOLE = `pkg-config --variable=libdir mono-nunit`/mono/2.0/nunit-console.exe
FIXTURE = /fixture=$(fixture)

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:../../build/AddIns/BackendBindings/MonoDevelop.CSharpBinding.dll \
	-r:../../build/AddIns/MonoDevelop.AspNet/MonoDevelop.AspNet.dll \
	-r:../../build/AddIns/MonoDevelop.Autotools/MonoDevelop.Autotools.dll \
	-r:../../build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	-r:../../build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:../../build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	-r:../../build/AddIns/MonoDevelop.XmlEditor/MonoDevelop.XmlEditor.dll \
	-r:../../build/AddIns/VersionControl/MonoDevelop.VersionControl.dll \
	-r:../../build/AddIns/VersionControl/MonoDevelop.VersionControl.Git.dll \
	-r:../../build/bin/ICSharpCode.NRefactory.CSharp.dll \
	-r:../../build/bin/ICSharpCode.NRefactory.dll \
	-r:../../build/bin/Mono.TextEditor.dll \
	-r:../../build/bin/MonoDevelop.Core.dll \
	-r:../../build/bin/MonoDevelop.Ide.dll \
	-r:../../build/bin/OldNRefactory.dll \
	-r:../../contrib/bin/NGit.dll \
	-r:../../contrib/bin/Sharpen.dll \
	-r:../../src/addins/NUnit/lib/nunit.core.dll \
	-r:../../src/addins/NUnit/lib/nunit.core.interfaces.dll \
	-r:../../src/addins/NUnit/lib/nunit.framework.dll \
	-r:../../src/addins/NUnit/lib/nunit.util.dll \
	-r:Mono.Cairo \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	Mono.TextEditor.Tests.DefaultEditActions/BookmarkTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/CaretMoveActionTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/DeleteActionTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/InsertNewLineTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/InsertTabTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/RemoveTabTests.cs \
	Mono.TextEditor.Tests.DefaultEditActions/SelectionActionTests.cs \
	Mono.TextEditor.Tests/BufferTests.cs \
	Mono.TextEditor.Tests/DocumentTests.cs \
	Mono.TextEditor.Tests/EditActionsTest.cs \
	Mono.TextEditor.Tests/FoldingTests.cs \
	Mono.TextEditor.Tests/HeightTreeTests.cs \
	Mono.TextEditor.Tests/InsertionModeTests.cs \
	Mono.TextEditor.Tests/LineSplitterTests.cs \
	Mono.TextEditor.Tests/RedBlackTreeTests.cs \
	Mono.TextEditor.Tests/SearchTests.cs \
	Mono.TextEditor.Tests/SegmentTreeTests.cs \
	Mono.TextEditor.Tests/SelectionTests.cs \
	Mono.TextEditor.Tests/SmartIndentModeTests.cs \
	Mono.TextEditor.Tests/SyntaxHighlightingTests.cs \
	Mono.TextEditor.Tests/TextBreakerTests.cs \
	Mono.TextEditor.Tests/UndoRedoTests.cs \
	Mono.TextEditor.Tests/VirtualIndentModeTests.cs \
	Mono.TextEditor.Tests/VirtualSpacesTests.cs \
	Mono.TextEditor.Tests/ViTests.cs \
	MonoDevelop.CSharpBinding/CSharpTextEditorIndentationTests.cs \
	MonoDevelop.CSharpBinding/FindMemberVisitorTests.cs \
	MonoDevelop.CSharpBinding/FoldingParserTests.cs \
	MonoDevelop.CSharpBinding/MemberTests.cs \
	MonoDevelop.CSharpBinding/ProjectDomTests.cs \
	MonoDevelop.CSharpBinding/TestDocument.cs \
	MonoDevelop.CSharpBinding/TestViewContent.cs \
	MonoDevelop.CSharpBinding/TestWorkbenchWindow.cs \
	MonoDevelop.CSharpBinding/TopLevelTests.cs \
	MonoDevelop.Projects.Gui/CompletionListWindowTests.cs \
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
	MonoDevelop.Refactoring/ImplementInterfaceTests.cs \
	MonoDevelop.VersionControl.Git/GitUtilsTests.cs \
	MonoDevelop.VersionControl.Views/EditorCompareWidgetBaseTest.cs \
	TestBase.cs \
	Util.cs

all: $(ASSEMBLY)

test: all
	rm -rf ../tmp
	if test -n "$(fixture)"; then \
		fp="-fixture=$(fixture)"; \
	fi; \
	$(MD_LAUNCH_SETUP) MONO_PATH=$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport:$(top_builddir)/build/AddIns/MonoDevelop.Refactoring:$(top_builddir)/build/AddIns/BackendBindings:$(top_builddir)/build/AddIns/MonoDevelop.XmlEditor:$(top_builddir)/build/AddIns/MonoDevelop.AspNet mono --debug $(NUNIT_CONSOLE) -noshadow $(top_builddir)/build/bin/UnitTests.dll $$fp

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(top_builddir)/build/bin/
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources) $(build_resources:%=/resource:%) $(REFS)

include $(top_srcdir)/Makefile.include

