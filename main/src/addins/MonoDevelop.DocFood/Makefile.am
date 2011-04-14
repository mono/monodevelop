DLLNAME = MonoDevelop.DocFood.dll
ASSEMBLY = $(top_builddir)/build/AddIns/$(DLLNAME)

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.DocFood.Options.AcronymsPanelWidget.cs \
	gtk-gui/MonoDevelop.DocFood.Options.ExpansionsPanelWidget.cs \
	gtk-gui/MonoDevelop.DocFood.Options.NoTheReorderingWidget.cs \
	gtk-gui/MonoDevelop.DocFood.Options.OfTheReorderingWidget.cs \
	gtk-gui/MonoDevelop.DocFood.Options.OptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.DocFood.Options.RulesPanelWidget.cs \
	Main.cs \
	MonoDevelop.DocFood.Options/AcronymsPanelWidget.cs \
	MonoDevelop.DocFood.Options/ExpansionsPanelWidget.cs \
	MonoDevelop.DocFood.Options/NoTheReorderingWidget.cs \
	MonoDevelop.DocFood.Options/OfTheReorderingWidget.cs \
	MonoDevelop.DocFood.Options/OptionsPanelWidget.cs \
	MonoDevelop.DocFood.Options/RulesPanelWidget.cs \
	MonoDevelop.DocFood/Commands.cs \
	MonoDevelop.DocFood/DocConfig.cs \
	MonoDevelop.DocFood/DocFoodTextEditorExtension.cs \
	MonoDevelop.DocFood/DocGenerator.cs \
	MonoDevelop.DocFood/IfNotStatement.cs \
	MonoDevelop.DocFood/IfStatement.cs \
	MonoDevelop.DocFood/MemberVisitor.cs \
	MonoDevelop.DocFood/Node.cs \
	MonoDevelop.DocFood/Section.cs \
	MonoDevelop.DocFood/SwitchStatement.cs 

RES =  \
	DocFood.addin.xml \
	DocFood.config.xml \
	gtk-gui/gui.stetic  

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.AssemblyBrowser.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll 

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../build/AddIns/MonoDevelop.Refactoring/MonoDevelop.Refactoring.dll \
	-r:../../../build/bin/ICSharpCode.NRefactory.dll \
	-r:../../../build/bin/Mono.TextEditor.dll \
	-r:../../../build/bin/MonoDevelop.Core.dll \
	-r:../../../build/bin/MonoDevelop.Ide.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY) : $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(top_builddir)/build/bin
	$(CSC) $(CSC_FLAGS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -t:library $(REFS) $(build_deps)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

