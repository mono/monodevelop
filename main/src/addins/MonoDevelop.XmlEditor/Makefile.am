
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.XmlEditor
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.XmlEditor.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Data \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.XmlEditor.Gui.XmlEditorOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.XmlEditor.Gui.XmlSchemasPanelWidget.cs \
	MonoDevelop.Xml.StateEngine/Parser.cs \
	MonoDevelop.Xml.StateEngine/State.cs \
	MonoDevelop.Xml.StateEngine/XDom.cs \
	MonoDevelop.Xml.StateEngine/XmlAttributeState.cs \
	MonoDevelop.Xml.StateEngine/XmlAttributeValueState.cs \
	MonoDevelop.Xml.StateEngine/XmlCDataState.cs \
	MonoDevelop.Xml.StateEngine/XmlChar.cs \
	MonoDevelop.Xml.StateEngine/XmlClosingTagState.cs \
	MonoDevelop.Xml.StateEngine/XmlCommentState.cs \
	MonoDevelop.Xml.StateEngine/XmlDocTypeState.cs \
	MonoDevelop.Xml.StateEngine/XmlFreeState.cs \
	MonoDevelop.Xml.StateEngine/XmlMalformedTagState.cs \
	MonoDevelop.Xml.StateEngine/XmlNameState.cs \
	MonoDevelop.Xml.StateEngine/XmlParsedDocument.cs \
	MonoDevelop.Xml.StateEngine/XmlProcessingInstructionState.cs \
	MonoDevelop.Xml.StateEngine/XmlTagState.cs \
	MonoDevelop.XmlEditor.Completion/EmptyXmlCompletionProvider.cs \
	MonoDevelop.XmlEditor.Completion/ILazilyLoadedProvider.cs \
	MonoDevelop.XmlEditor.Completion/IXmlCompletionProvider.cs \
	MonoDevelop.XmlEditor.Completion/XmlCompletionData.cs \
	MonoDevelop.XmlEditor.Completion/XmlCompletionDataCollection.cs \
	MonoDevelop.XmlEditor.Completion/XmlMultipleClosingTagCompletionData.cs \
	MonoDevelop.XmlEditor.Completion/XmlSchemaCompletionData.cs \
	MonoDevelop.XmlEditor.Completion/XmlSchemaCompletionDataCollection.cs \
	MonoDevelop.XmlEditor.Completion/XmlTagCompletionData.cs \
	MonoDevelop.XmlEditor.Gui/BaseXmlEditorExtension.cs \
	MonoDevelop.XmlEditor.Gui/XmlEditorOptionsPanel.cs \
	MonoDevelop.XmlEditor.Gui/XmlEditorOptionsPanelWidget.cs \
	MonoDevelop.XmlEditor.Gui/XmlSchemasPanel.cs \
	MonoDevelop.XmlEditor.Gui/XmlSchemasPanelWidget.cs \
	MonoDevelop.XmlEditor/Commands.cs \
	MonoDevelop.XmlEditor/EncodedStringWriter.cs \
	MonoDevelop.XmlEditor/QualifiedName.cs \
	MonoDevelop.XmlEditor/QualifiedNameCollection.cs \
	MonoDevelop.XmlEditor/XmlDocumentParser.cs \
	MonoDevelop.XmlEditor/XmlEditorAddInOptions.cs \
	MonoDevelop.XmlEditor/XmlEditorService.cs \
	MonoDevelop.XmlEditor/XmlElementPath.cs \
	MonoDevelop.XmlEditor/XmlFileExtensionNode.cs \
	MonoDevelop.XmlEditor/XmlFileExtensions.cs \
	MonoDevelop.XmlEditor/XmlNamespace.cs \
	MonoDevelop.XmlEditor/XmlParser.cs \
	MonoDevelop.XmlEditor/XmlSchemaAssociation.cs \
	MonoDevelop.XmlEditor/XmlSchemaManager.cs \
	MonoDevelop.XmlEditor/XmlTextEditorExtension.cs

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
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	Icons/XPathQueryPad.png \
	MonoDevelop.XmlEditor.addin.xml \
	XmlEditor.glade


DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/schemas/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/schemas
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library -define:DEBUG

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.XmlEditor
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

schemadir = $(MD_ADDIN_DIR)/MonoDevelop.XmlEditor/schemas
schema_DATA = $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include



