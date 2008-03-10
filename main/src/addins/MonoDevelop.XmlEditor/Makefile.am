
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.XmlEditor
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.XmlEditor.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW2_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:$(top_srcdir)/build/AddIns/MonoDevelop.SourceEditor.dll \
	-r:System \
	-r:System.Data \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	MonoDevelop.XmlEditor.XPath/XPathHistoryList.cs \
	MonoDevelop.XmlEditor.XPath/XPathNamespaceList.cs \
	MonoDevelop.XmlEditor.XPath/XPathNodeMatch.cs \
	MonoDevelop.XmlEditor.XPath/XPathNodeTextMarker.cs \
	MonoDevelop.XmlEditor.XPath/XPathQueryPad.cs \
	MonoDevelop.XmlEditor.XPath/XPathQueryPadOptions.cs \
	MonoDevelop.XmlEditor.XPath/XPathQueryWidget.cs \
	MonoDevelop.XmlEditor/AssignStylesheetCommand.cs \
	MonoDevelop.XmlEditor/Commands.cs \
	MonoDevelop.XmlEditor/CreateSchemaCommand.cs \
	MonoDevelop.XmlEditor/DeclarationViewWindow.cs \
	MonoDevelop.XmlEditor/EncodedStringWriter.cs \
	MonoDevelop.XmlEditor/FormatXmlCommand.cs \
	MonoDevelop.XmlEditor/GoToSchemaDefinitionCommand.cs \
	MonoDevelop.XmlEditor/OpenStylesheetCommand.cs \
	MonoDevelop.XmlEditor/OpenXPathQueryPadCommand.cs \
	MonoDevelop.XmlEditor/QualifiedName.cs \
	MonoDevelop.XmlEditor/QualifiedNameCollection.cs \
	MonoDevelop.XmlEditor/RemoveXPathHighlightingCommand.cs \
	MonoDevelop.XmlEditor/RunXslTransformCommand.cs \
	MonoDevelop.XmlEditor/SelectXmlSchemaDialog.cs \
	MonoDevelop.XmlEditor/SourceViewTextIterator.cs \
	MonoDevelop.XmlEditor/ValidateXmlCommand.cs \
	MonoDevelop.XmlEditor/XmlCompletionData.cs \
	MonoDevelop.XmlEditor/XmlCompletionDataCollection.cs \
	MonoDevelop.XmlEditor/XmlCompletionDataProvider.cs \
	MonoDevelop.XmlEditor/XmlCompletionListWindow.cs \
	MonoDevelop.XmlEditor/XmlDisplayBinding.cs \
	MonoDevelop.XmlEditor/XmlEditorAddInOptions.cs \
	MonoDevelop.XmlEditor/XmlEditorListWindow.cs \
	MonoDevelop.XmlEditor/XmlEditorOptionsPanel.cs \
	MonoDevelop.XmlEditor/XmlEditorOptionsPanelWidget.cs \
	MonoDevelop.XmlEditor/XmlEditorService.cs \
	MonoDevelop.XmlEditor/XmlEditorView.cs \
	MonoDevelop.XmlEditor/XmlEditorViewContent.cs \
	MonoDevelop.XmlEditor/XmlEditorWindow.cs \
	MonoDevelop.XmlEditor/XmlElementPath.cs \
	MonoDevelop.XmlEditor/XmlFileExtensionNode.cs \
	MonoDevelop.XmlEditor/XmlFileExtensions.cs \
	MonoDevelop.XmlEditor/XmlNamespace.cs \
	MonoDevelop.XmlEditor/XmlParser.cs \
	MonoDevelop.XmlEditor/XmlSchemaAssociation.cs \
	MonoDevelop.XmlEditor/XmlSchemaCompletionData.cs \
	MonoDevelop.XmlEditor/XmlSchemaCompletionDataCollection.cs \
	MonoDevelop.XmlEditor/XmlSchemaManager.cs \
	MonoDevelop.XmlEditor/XmlSchemasPanel.cs \
	MonoDevelop.XmlEditor/XmlSchemasPanelWidget.cs \
	MonoDevelop.XmlEditor/XslOutputViewContent.cs

DATA_FILES =  \
	schemas/appconfig.xsd \
	schemas/manifest.xsd \
	schemas/MonoDevelopAddIn.xsd \
	schemas/nant.xsd \
	schemas/readme.txt \
	schemas/W3C-License.html \
	schemas/XMLSchema.xsd \
	schemas/xslt.xsd

RES =  \
	Icons/XPathQueryPad.png \
	MonoDevelop.XmlEditor.addin.xml \
	XmlEditor.glade


DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/schemas/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/schemas
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.XmlEditor
assembly_DATA = $(ASSEMBLY) $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include



