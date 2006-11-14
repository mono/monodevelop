if ENABLE_ASPNETEDIT
SUBDIRS=chrome
endif

ADDIN_BUILD = $(top_builddir)/build/AddIns/AspNetEdit
ASSEMBLY = $(ADDIN_BUILD)/AspNetEdit.dll

REFS = -r:System \
       -r:System.Xml \
       -r:System.Web \
       -r:System.Design \
       -r:System.Drawing \
       -r:System.Drawing.Design \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll  \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/AspNetAddIn/AspNetAddIn.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GECKO_SHARP_LIBS) \
	$(JSCALL_LIBS)

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
	AspNetEdit.Editor.Persistence/AspParser.cs \
	AspNetEdit.Editor.Persistence/HtmlParsingObject.cs \
	AspNetEdit.Editor.Persistence/ServerControlParsingObject.cs \
	AspNetEdit.Editor.Persistence/AspTokenizer.cs \
	AspNetEdit.Editor.Persistence/ILocation.cs \
	AspNetEdit.Editor.Persistence/StrUtils.cs \
	AspNetEdit.Editor.Persistence/Location.cs \
	AspNetEdit.Editor.Persistence/TagAttributes.cs \
	AspNetEdit.Editor.Persistence/ControlPersister.cs \
	AspNetEdit.Editor.Persistence/ParseException.cs \
	AspNetEdit.Editor.Persistence/TagType.cs \
	AspNetEdit.Editor.Persistence/DesignTimeParser.cs \
	AspNetEdit.Editor.Persistence/ParsingObject.cs \
	AspNetEdit.Editor.Persistence/Directive.cs \
	AspNetEdit.Editor.Persistence/RootParsingObject.cs \
	\
	AspNetEdit.Editor.UI/PropertyGrid.cs \
	AspNetEdit.Editor.UI/RootDesignerView.cs \
	\
	AspNetEdit.Integration/AspNetEditDisplayBinding.cs \
	AspNetEdit.Integration/AspNetEditViewContent.cs \
	AspNetEdit.Integration/MonoDevelopProxy.cs \
	AspNetEdit.Integration/ToolboxProvider.cs \
	AspNetEdit.Integration/EditorProcess.cs


RES = \
	#

DATA_FILES = \
	data/WebControlsToolbox.xml

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD), $(DATA_FILES))

ADDIN = AspNetEdit.addin.xml

if ENABLE_ASPNETEDIT
  all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)
else
  all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -d:TRACE -out:$@ -target:library $(REFS) \
		$(RES:%=/resource:%) $(build_sources)

$(DATA_FILE_BUILD): $(srcdir)/data/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)/data
	cp $(srcdir)/data/$(notdir $@) $(ADDIN_BUILD)/data/.

if ENABLE_ASPNETEDIT
  aspneteditlibdir = $(MD_ADDIN_DIR)/AspNetEdit
  aspneteditlib_DATA = $(ASSEMBLY) $(ADDIN) $(DATA_FILES)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

