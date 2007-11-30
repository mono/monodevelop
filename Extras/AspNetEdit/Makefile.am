if ENABLE_ASPNETEDIT
SUBDIRS=chrome
endif

ADDIN_BUILD = $(top_builddir)/build/AddIns/AspNetEdit
ASSEMBLY = $(ADDIN_BUILD)/AspNetEdit.dll

REFS =  \
	$(GECKO_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/AspNetAddIn/AspNetAddIn.dll \
	-r:$(top_builddir)/build/AddIns/GeckoWebBrowser/GeckoWebBrowser.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/bin/Mono.Addins.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:System \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Xml

FILES =	\
	AssemblyInfo.cs \
	\
	AspNetEdit.Editor/EditorHost.cs \
	\
	AspNetEdit.Editor.ComponentModel/ExtenderListService.cs \
	AspNetEdit.Editor.ComponentModel/Transaction.cs \
	AspNetEdit.Editor.ComponentModel/MenuCommandService.cs \
	AspNetEdit.Editor.ComponentModel/TypeDescriptorFilterService.cs \
	AspNetEdit.Editor.ComponentModel/DesignContainer.cs \
	AspNetEdit.Editor.ComponentModel/NameCreationService.cs \
	AspNetEdit.Editor.ComponentModel/TypeResolutionService.cs \
	AspNetEdit.Editor.ComponentModel/DesignerHost.cs \
	AspNetEdit.Editor.ComponentModel/RootDesigner.cs \
	AspNetEdit.Editor.ComponentModel/Document.cs \
	AspNetEdit.Editor.ComponentModel/SelectionService.cs \
	AspNetEdit.Editor.ComponentModel/WebFormPage.cs \
	AspNetEdit.Editor.ComponentModel/DocumentDirective.cs \
	AspNetEdit.Editor.ComponentModel/TextToolboxItem.cs \
	AspNetEdit.Editor.ComponentModel/WebFormReferenceManager.cs \
	AspNetEdit.Editor.ComponentModel/EventBindingService.cs \
	\
	AspNetEdit.Editor.Persistence/HtmlParsingObject.cs \
	AspNetEdit.Editor.Persistence/ServerControlParsingObject.cs \
	AspNetEdit.Editor.Persistence/ControlPersister.cs \
	AspNetEdit.Editor.Persistence/DesignTimeParser.cs \
	AspNetEdit.Editor.Persistence/ParsingObject.cs \
	AspNetEdit.Editor.Persistence/RootParsingObject.cs \
	\
	AspNetEdit.Editor.UI/PropertyGrid.cs \
	AspNetEdit.Editor.UI/RootDesignerView.cs \
	\
	AspNetEdit.Integration/AspNetEditDisplayBinding.cs \
	AspNetEdit.Integration/AspNetEditViewContent.cs \
	AspNetEdit.Integration/MonoDevelopProxy.cs \
	AspNetEdit.Integration/ToolboxProvider.cs \
	AspNetEdit.Integration/EditorProcess.cs \
	\
	AspNetEdit.JSCall/CommandManager.cs \
	AspNetEdit.JSCall/InvalidJSArgumentException.cs


RES = AspNetEdit.addin.xml


DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(notdir $(DATA_FILES)))

if ENABLE_ASPNETEDIT
  all: $(ASSEMBLY) $(DATA_FILE_BUILD)
else
  all:
endif

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -d:TRACE -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

$(DATA_FILE_BUILD): $(srcdir)/data/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/data/$(notdir $@) $(ADDIN_BUILD)/.

if ENABLE_ASPNETEDIT
  aspneteditlibdir = $(MD_ADDIN_DIR)/AspNetEdit
  aspneteditlib_DATA = $(ASSEMBLY) $(DATA_FILES)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

