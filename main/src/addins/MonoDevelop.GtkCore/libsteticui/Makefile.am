ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.GtkCore
ASSEMBLY = $(ADDIN_BUILD)/libsteticui.dll
assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.GtkCore
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(build_dll_config)

FILES =  \
	ActionComponent.cs \
	ActionGroupComponent.cs \
	ActionGroupDesigner.cs \
	ActionGroupDesignerBackend.cs \
	ActionGroupEditSession.cs \
	ActionGroupToolbar.cs \
	Application.cs \
	ApplicationBackend.cs \
	ApplicationBackendController.cs \
	AssemblyResolver.cs \
	AssemblyWidgetLibrary.cs \
	CecilClassDescriptor.cs \
	CecilPropertyDescriptor.cs \
	CecilSignalDescriptor.cs \
	CecilWidgetLibrary.cs \
	CodeGenerationResult.cs \
	CodeGenerator.cs \
	CodeGeneratorInternalClass.cs \
	CodeGeneratorPartialClass.cs \
	Component.cs \
	ComponentEventHandler.cs \
	ComponentSignalEventHandler.cs \
	ComponentType.cs \
	ContainerUndoRedoManager.cs \
	ContextMenu.cs \
	Designer.cs \
	EmbedWindow.cs \
	Glade.cs \
	Grid.cs \
	GuiDispatchServerSink.cs \
	GuiDispatchServerSinkProvider.cs \
	LibraryCache.cs \
	Metacity/ButtonFunction.cs \
	Metacity/ButtonLayout.cs \
	Metacity/FrameFlags.cs \
	Metacity/FrameType.cs \
	Metacity/ObjectManager.cs \
	Metacity/Preview.cs \
	Metacity/Theme.cs \
	Palette.cs \
	PaletteBackend.cs \
	PluggableWidget.cs \
	Project.cs \
	ProjectBackend.cs \
	ProjectViewBackend.cs \
	PropertyEditor.cs \
	PropertyGrid.cs \
	PropertyTree.cs \
	Shadow.cs \
	SignalsEditor.cs \
	SignalsEditorBackend.cs \
	UndoQueue.cs \
	UserInterface.cs \
	WidgetActionBar.cs \
	WidgetComponent.cs \
	WidgetDesigner.cs \
	WidgetDesignerBackend.cs \
	WidgetEditSession.cs \
	WidgetFactory.cs \
	WidgetInfoEventHandler.cs \
	WidgetPropertyTree.cs \
	WidgetPropertyTreeBackend.cs \
	WidgetTree.cs \
	WidgetTreeCombo.cs

RES =  \
	action.png \
	missing.png

DEPS =  \
	../../../../build/AddIns/MonoDevelop.GtkCore/libstetic.dll \
	../../../../build/bin/Mono.Cecil.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Runtime.Remoting \
	-r:System.Xml

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -unsafe -main:Stetic.ApplicationBackend -out:$@ \
	$(build_resources:%=/resource:%) $(build_sources) $(REFS) $(build_deps)

dll_config = libsteticui.dll.config
build_dll_config = $(ADDIN_BUILD)/$(dll_config)

$(build_dll_config): $(srcdir)/$(dll_config)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(dll_config) $@

EXTRA_DIST = $(FILES) $(RES) $(dll_config)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(top_srcdir)/Makefile.include

