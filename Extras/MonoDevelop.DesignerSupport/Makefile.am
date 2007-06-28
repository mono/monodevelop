ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.DesignerSupport.dll
REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:System \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Web \
	-r:System.Windows.Forms \
	-r:System.Xml

FILES =  \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindClassBuilder.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindDisplayBinding.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindProjectFileExtension.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindViewContent.cs \
	MonoDevelop.DesignerSupport.CodeBehind/ICodeBehindProvider.cs \
	MonoDevelop.DesignerSupport.Projects/ComponentNodeBuilder.cs \
	MonoDevelop.DesignerSupport.Projects/ProjectsPropertyProvider.cs \
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
	MonoDevelop.DesignerSupport.Toolbox/BaseToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/CategoryToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/CompactToolboxView.cs \
	MonoDevelop.DesignerSupport.Toolbox/ItemToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxConsumer.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxProvider.cs \
	MonoDevelop.DesignerSupport.Toolbox/TextToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/Toolbox.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxList.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxStore.cs \
	MonoDevelop.DesignerSupport.Toolbox/TypeReference.cs \
	MonoDevelop.DesignerSupport.Toolbox/TypeToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/UnknownToolboxNode.cs \
	MonoDevelop.DesignerSupport/BindingService.cs \
	MonoDevelop.DesignerSupport/CodeBehindService.cs \
	MonoDevelop.DesignerSupport/CustomDescriptor.cs \
	MonoDevelop.DesignerSupport/DesignerSupportService.cs \
	MonoDevelop.DesignerSupport/IPropertyProvider.cs \
	MonoDevelop.DesignerSupport/MemberExistsException.cs \
	MonoDevelop.DesignerSupport/PropertyPad.cs \
	MonoDevelop.DesignerSupport/RemoteDesignerProcess.cs \
	MonoDevelop.DesignerSupport/ToolboxPad.cs \
	MonoDevelop.DesignerSupport/ToolboxService.cs \
	MonoDevelop.DesignerSupport/WrapperDesignView.cs

RES =  \
	icons/group-by-category.png \
	icons/md-design-listboxtoggle.png \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.DefaultPropertyTab.bmp \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.EventPropertyTab.bmp \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.SortByCat.png


ADDIN = MonoDevelop.DesignerSupport.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.DesignerSupport
assembly_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include

