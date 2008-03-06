
ADDIN_BUILD = $(top_builddir)/build/AddIns/XmlEditor
DATA_DIR = $(ADDIN_BUILD)/schemas
ASSEMBLY = MonoDevelop.XmlEditor.dll

DLLS = -r:System \
	-r:System.Data \
	-r:System.Xml \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.Components.dll \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Core.dll \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.Core.Gui.dll \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.Ide.dll \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.Projects.dll \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.Projects.Gui.dll \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.SourceEditor.dll \
	$(GTK_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS)

FILES = \
	AssemblyInfo.cs \
	AssignStylesheetCommand.cs \
	Commands.cs \
	CreateSchemaCommand.cs \
	EncodedStringWriter.cs \
	FormatXmlCommand.cs \
	OpenStylesheetCommand.cs \
	QualifiedNameCollection.cs \
	QualifiedName.cs \
	RunXslTransformCommand.cs \
	SelectXmlSchemaDialog.cs \
	SourceViewTextIterator.cs \
	ValidateXmlCommand.cs \
	XmlCompletionDataCollection.cs \
	XmlCompletionData.cs \
	XmlCompletionDataProvider.cs \
	XmlElementPath.cs \
	XmlParser.cs \
	XmlSchemaCompletionDataCollection.cs \
	XmlSchemaCompletionData.cs \
	XmlSchemaManager.cs \
	XmlSchemaAssociation.cs \
	XmlEditorAddInOptions.cs \
	XmlDisplayBinding.cs \
	XmlEditorView.cs \
	XmlEditorWindow.cs \
	XmlCompletionListWindow.cs \
	XmlEditorListWindow.cs \
	XmlEditorViewContent.cs \
	XmlEditorOptionsPanel.cs \
	XmlEditorOptionsPanelWidget.cs \
	XmlSchemasPanel.cs \
	XmlSchemasPanelWidget.cs \
	XmlFileExtensions.cs \
	XmlEditorService.cs \
	XmlFileExtensionCodon.cs \
	XslOutputViewContent.cs

DATA_FILES = \
	schemas/appconfig.xsd \
	schemas/MonoDevelopAddIn.xsd \
	schemas/manifest.xsd \
	schemas/nant.xsd \
	schemas/readme.txt \
	schemas/W3C-License.html \
	schemas/XMLSchema.xsd \
	schemas/xslt.xsd

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

RES = XmlEditor.glade

ADDIN = MonoDevelop.XmlEditor.addin.xml

MD_XML_ADDIN_DIR = $(MD_ADDIN_DIR)/XmlEditor
MD_XML_ADDIN_DATA_DIR = $(MD_XML_ADDIN_DIR)/schemas

all: $(ADDIN_BUILD)/$(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)

install: $(ADDIN_BUILD)/$(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)
	mkdir -p $(MD_XML_ADDIN_DIR)
	mkdir -p $(MD_XML_ADDIN_DATA_DIR)
	cp $(ADDIN_BUILD)/$(ASSEMBLY) $(MD_XML_ADDIN_DIR)
	cp $(ADDIN_BUILD)/$(ADDIN) $(MD_XML_ADDIN_DIR)
	cp $(DATA_DIR)/* $(MD_XML_ADDIN_DATA_DIR)

uninstall:
	-rm $(MD_XML_ADDIN_DIR)/$(ASSEMBLY) 
	-rm $(MD_XML_ADDIN_DIR)/$(ADDIN) 

build_sources = $(addprefix $(srcdir)/, $(FILES))

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(DATA_FILE_BUILD): $(srcdir)/schemas/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	mkdir -p $(DATA_DIR)
	cp $(srcdir)/schemas/$(notdir $@) $(DATA_DIR)/. 

$(ADDIN_BUILD)/$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(DLLS) $(RES:%=/resource:%) $(build_sources) -out:$@ -target:library

CLEANFILES = $(ADDIN_BUILD)/$(ASSEMBLY) $(ADDIN_BUILD)/$(ASSEMBLY).mdb $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD) 
EXTRA_DIST = $(FILES) $(ADDIN) $(RES) $(DATA_FILES)



