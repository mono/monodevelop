
ADDIN_BUILD = $(top_builddir)/build/AddIns/XmlEditor
DATA_DIR = $(ADDIN_BUILD)/schemas
ASSEMBLY = MonoDevelop.XmlEditor.dll

DLLS = -r:System \
	-r:System.Data \
	-r:System.Xml \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Components.dll \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Core.dll \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Core.Gui.dll \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Ide.dll \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Projects.dll \
	-r:$(MD_ASSEMBLY_DIR)/MonoDevelop.Projects.Gui.dll \
	-r:$(MD_ADDIN_DIR)/MonoDevelop.SourceEditor.dll \
	$(MONO_ADDINS_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS)

FILES = \
	AssemblyInfo.cs \
	AssignStylesheetCommand.cs \
	Commands.cs \
	CreateSchemaCommand.cs \
	DeclarationViewWindow.cs \
	EncodedStringWriter.cs \
	FormatXmlCommand.cs \
	GoToSchemaDefinitionCommand.cs \
	OpenStylesheetCommand.cs \
	OpenXPathQueryPadCommand.cs \
	QualifiedNameCollection.cs \
	QualifiedName.cs \
	RemoveXPathHighlightingCommand.cs \
	RunXslTransformCommand.cs \
	SelectXmlSchemaDialog.cs \
	SourceViewTextIterator.cs \
	ValidateXmlCommand.cs \
	XmlCompletionDataCollection.cs \
	XmlCompletionData.cs \
	XmlCompletionDataProvider.cs \
	XmlElementPath.cs \
	XmlNamespace.cs \
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
	XmlFileExtensionNode.cs \
	XPathHistoryList.cs \
	XPathNamespaceList.cs \
	XPathNodeMatch.cs \
	XPathNodeTextMarker.cs \
	XPathQueryPad.cs \
	XPathQueryPadOptions.cs \
	XPathQueryWidget.cs \
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

RES = \
	XmlEditor.glade \
	MonoDevelop.XmlEditor.addin.xml \
	XPathQueryPad.png

MD_XML_ADDIN_DIR = $(MD_ADDIN_DIR)/XmlEditor
MD_XML_ADDIN_DATA_DIR = $(MD_XML_ADDIN_DIR)/schemas

all: $(ADDIN_BUILD)/$(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN) $(DATA_FILE_BUILD)

install: $(ADDIN_BUILD)/$(ASSEMBLY) $(DATA_FILE_BUILD)
	mkdir -p $(MD_XML_ADDIN_DIR)
	mkdir -p $(MD_XML_ADDIN_DATA_DIR)
	cp $(ADDIN_BUILD)/$(ASSEMBLY) $(MD_XML_ADDIN_DIR)
	cp $(DATA_DIR)/* $(MD_XML_ADDIN_DATA_DIR)

uninstall:
	-rm $(MD_XML_ADDIN_DIR)/$(ASSEMBLY) 

build_sources = $(addprefix $(srcdir)/, $(FILES))

$(DATA_FILE_BUILD): $(srcdir)/schemas/$(notdir $@)
	mkdir -p $(ADDIN_BUILD)
	mkdir -p $(DATA_DIR)
	cp $(srcdir)/schemas/$(notdir $@) $(DATA_DIR)/. 

$(ADDIN_BUILD)/$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(DLLS) $(RES:%=/resource:%) $(build_sources) -out:$@ -target:library

CLEANFILES = $(ADDIN_BUILD)/$(ASSEMBLY) $(ADDIN_BUILD)/$(ASSEMBLY).mdb $(DATA_FILE_BUILD) 
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)



