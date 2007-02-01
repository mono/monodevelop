ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.DesignerSupport.dll

REFS = -r:System \
	-r:System.Xml \
	-r:System.Design \
	-r:System.Drawing \
	-r:System.Drawing.Design \
	-r:System.Windows.Forms \
	-r:System.Web \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll  \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
	MonoDevelop.DesignerSupport/PropertyPad.cs \
	MonoDevelop.DesignerSupport/DesignerSupportService.cs \
	MonoDevelop.DesignerSupport/ToolboxPad.cs \
	MonoDevelop.DesignerSupport/WrapperDesignView.cs \
	MonoDevelop.DesignerSupport/ToolboxService.cs \
	MonoDevelop.DesignerSupport/CodeBehindService.cs \
	MonoDevelop.DesignerSupport/BindingService.cs \
	MonoDevelop.DesignerSupport/MemberExistsException.cs \
	MonoDevelop.DesignerSupport/RemoteDesignerProcess.cs \
	 \
	MonoDevelop.DesignerSupport.CodeBehind/ICodeBehindProvider.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindDisplayBinding.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindViewContent.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindClassBuilder.cs \
	MonoDevelop.DesignerSupport.CodeBehind/CodeBehindProjectFileExtension.cs \
	\
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyGrid.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/DefaultPropertyTab.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/EditorManager.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyValueChangedEventArgs.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/EventPropertyTab.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyValueChangedEventHandler.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/GridRow.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/SurrogateUITypeEditorAttribute.cs \
	MonoDevelop.DesignerSupport.PropertyGrid/PropertyEditorTypeAttribute.cs \
	 \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/BaseEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/DefaultEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/ExpandableObjectEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/BoolEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/CollectionEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/EnumEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/NumberEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/ColorEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/EventEditor.cs \
	MonoDevelop.DesignerSupport.PropertyGrid.Editors/StringEditor.cs \
	 \
	MonoDevelop.DesignerSupport.Toolbox/BaseToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/ItemToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/CategoryToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/TextToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/UnknownToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/TypeToolboxNode.cs \
	MonoDevelop.DesignerSupport.Toolbox/Toolbox.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxStore.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxConsumer.cs \
	MonoDevelop.DesignerSupport.Toolbox/IToolboxProvider.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxList.cs \
	MonoDevelop.DesignerSupport.Toolbox/ToolboxItemToolboxLoader.cs \
	MonoDevelop.DesignerSupport.Toolbox/TypeReference.cs

RES = \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.DefaultPropertyTab.bmp \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.SortByCat.png \
	icons/MonoDevelop.DesignerSupport.PropertyGrid.EventPropertyTab.bmp \
	icons/group-by-category.png

ADDIN = MonoDevelop.DesignerSupport.addin.xml

if ENABLE_DESIGNERSUPP
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(RES:%=/resource:%) $(build_sources)

if ENABLE_DESIGNERSUPP
csharpbindinglibdir = $(MD_ADDIN_DIR)/MonoDevelop.DesignerSupport
csharpbindinglib_DATA = $(ASSEMBLY) $(ADDIN)
endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include

