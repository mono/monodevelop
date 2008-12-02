ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.DesignerSupport.dll

DEPS =  \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Windows.Forms \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.DesignerSupport.Toolbox.ComponentSelectorDialog.cs \
	MonoDevelop.DesignerSupport.Projects/ComponentNodeBuilder.cs \
	MonoDevelop.DesignerSupport.Projects/ProjectFileDescriptor.cs \
	MonoDevelop.DesignerSupport.Projects/ProjectItemPropertyProvider.cs \
	MonoDevelop.DesignerSupport.Projects/ProjectReferenceDescriptor.cs \
	MonoDevelop.DesignerSupport.Projects/PropertyPadTextEditorExtension.cs \
	MonoDevelop.DesignerSupport.Projects/SolutionItemDescriptor.cs \
	MonoDevelop.DesignerSupport.Projects/SolutionItemPropertyProvider.cs \
	MonoDevelop.DesignerSupport.Projects/WorkspaceItemDescriptor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/BooleanEditorCell.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/CharPropertyEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/CollectionEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/ColorEditorCell.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/DateTimeEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/DefaultEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/EnumerationEditorCell.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/EventEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/ExpandableObjectEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/FlagsEditorCell.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/FlagsSelectorDialog.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/FloatRange.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/IntRange.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/TextEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/TextEditorDialog.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/TimeSpanEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/DefaultPropertyTab.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/EditorManager.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/EventPropertyTab.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/IPropertyPadProvider.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyEditorCell.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyEditorTypeAttribute.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyGrid.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyGridTree.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyPadVisitor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyValueChangedEventArgs.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyValueChangedEventHandler.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/SurrogateUITypeEditorAttribute.cs \
	MonoDevelop.DesignerSupport.Toolbox/CecilToolboxItemLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/CecilTypeResolver.cs \
	MonoDevelop.DesignerSupport.Toolbox/CodeTemplateToolboxProvider.cs \
	MonoDevelop.DesignerSupport.Toolbox/ComponentSelectorDialog.cs \
	MonoDevelop.DesignerSupport.Toolbox/ItemToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/ITextToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxConsumer.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxProvider.cs \
	MonoDevelop.DesignerSupport.Toolbox/TextToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/Toolbox.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxConfiguration.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxList.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxWidget.cs \
	MonoDevelop.DesignerSupport.Toolbox/TypeReference.cs \
	MonoDevelop.DesignerSupport.Toolbox/TypeToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/UnknownToolboxNode.cs \
	MonoDevelop.DesignerSupport/BindingService.cs \
	MonoDevelop.DesignerSupport/ClassOutlineTextEditorExtension.cs \
	MonoDevelop.DesignerSupport/CodeBehind.cs \
	MonoDevelop.DesignerSupport/Commands.cs \
	MonoDevelop.DesignerSupport/CustomDescriptor.cs \
	MonoDevelop.DesignerSupport/DesignerSupportService.cs \
	MonoDevelop.DesignerSupport/DocumentOutlinePad.cs \
	MonoDevelop.DesignerSupport/ErrorInFileException.cs \
	MonoDevelop.DesignerSupport/IOutlinedDocument.cs \
	MonoDevelop.DesignerSupport/IPropertyProvider.cs \
	MonoDevelop.DesignerSupport/MemberExistsException.cs \
	MonoDevelop.DesignerSupport/OpenDocumentFileProvider.cs \
	MonoDevelop.DesignerSupport/PropertyPad.cs \
	MonoDevelop.DesignerSupport/RemoteDesignerProcess.cs \
	MonoDevelop.DesignerSupport/ToolboxPad.cs \
	MonoDevelop.DesignerSupport/ToolboxService.cs \
	MonoDevelop.DesignerSupport/TypeNotFoundException.cs \
	MonoDevelop.DesignerSupport/WrapperDesignView.cs

RES =  \
	gtk-gui/gui.stetic \
	icons/group-by-category.png \
	icons/md-design-listboxtoggle.png \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.DefaultPropertyTab.bmp \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.EventPropertyTab.bmp \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.SortByCat.png \
	icons/pad-document-outline-16.png \
	icons/properties-pad-16.png \
	icons/toolbox-pad-16.png \
	MonoDevelop.DesignerSupport.addin.xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.DesignerSupport
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

