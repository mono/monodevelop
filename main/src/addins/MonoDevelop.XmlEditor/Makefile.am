
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.XmlEditor
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.XmlEditor.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	-r:$(top_builddir)/build/bin/Mono.TextEditor.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.XmlEditor.Gui.XmlEditorOptionsPanelWidget.cs \
	gtk-gui/MonoDevelop.XmlEditor.Gui.XmlSchemasPanelWidget.cs \
	MonoDevelop.XmlEditor.Gui/XmlEditorOptionsPanel.cs \
	MonoDevelop.XmlEditor.Gui/XmlEditorOptionsPanelWidget.cs \
	MonoDevelop.XmlEditor.Gui/XmlSchemasPanel.cs \
	MonoDevelop.XmlEditor.Gui/XmlSchemasPanelWidget.cs \
	MonoDevelop.XmlEditor/ClosingBracketCompletionDataProvider.cs \
	MonoDevelop.XmlEditor/Commands.cs \
	MonoDevelop.XmlEditor/EncodedStringWriter.cs \
	MonoDevelop.XmlEditor/QualifiedName.cs \
	MonoDevelop.XmlEditor/QualifiedNameCollection.cs \
	MonoDevelop.XmlEditor/XmlCompletionData.cs \
	MonoDevelop.XmlEditor/XmlCompletionDataCollection.cs \
	MonoDevelop.XmlEditor/XmlCompletionDataProvider.cs \
	MonoDevelop.XmlEditor/XmlEditorAddInOptions.cs \
	MonoDevelop.XmlEditor/XmlEditorService.cs \
	MonoDevelop.XmlEditor/XmlElementPath.cs \
	MonoDevelop.XmlEditor/XmlFileExtensionNode.cs \
	MonoDevelop.XmlEditor/XmlFileExtensions.cs \
	MonoDevelop.XmlEditor/XmlNamespace.cs \
	MonoDevelop.XmlEditor/XmlParser.cs \
	MonoDevelop.XmlEditor/XmlSchemaAssociation.cs \
	MonoDevelop.XmlEditor/XmlSchemaCompletionData.cs \
	MonoDevelop.XmlEditor/XmlSchemaCompletionDataCollection.cs \
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

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/schemas
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.XmlEditor
assembly_DATA = $(ASSEMBLY)

schemadir = $(MD_ADDIN_DIR)/MonoDevelop.XmlEditor/schemas
schema_DATA = $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include



