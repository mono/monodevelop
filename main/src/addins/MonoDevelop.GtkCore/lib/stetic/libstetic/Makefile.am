ASSEMBLY_DIR = $(top_builddir)/bin

ASSEMBLY = $(ASSEMBLY_DIR)/libstetic.dll

ASSEMBLY_FILES = \
	$(ASSEMBLY) \
	$(ASSEMBLY_DIR)/libstetic.dll.mdb \
	$(DATA_FILE_BUILD)

pkglib_DATA = $(ASSEMBLY_FILES)
libstetic_dll_sources =  \
	ClassDescriptor.cs \
	Clipboard.cs \
	CommandDescriptor.cs \
	CustomWidget.cs \
	DND.cs \
	editor/Accelerator.cs \
	editor/ActionGroupEditor.cs \
	editor/ActionItem.cs \
	editor/ActionMenu.cs \
	editor/ActionMenuBar.cs \
	editor/ActionMenuItem.cs \
	editor/ActionToolbar.cs \
	editor/ActionToolItem.cs \
	editor/Boolean.cs \
	editor/CellRendererComboBox.cs \
	editor/Char.cs \
	editor/Color.cs \
	editor/DateTimeEditor.cs \
	editor/EditIconDialog.cs \
	editor/EditIconFactoryDialog.cs \
	editor/Enumeration.cs \
	editor/Flags.cs \
	editor/FlagsSelectorDialog.cs \
	editor/FloatRange.cs \
	editor/GroupPicker.cs \
	editor/IconList.cs \
	editor/IconSelectorItem.cs \
	editor/IconSelectorMenu.cs \
	editor/IconSelectorMenuItem.cs \
	editor/Identifier.cs \
	editor/Image.cs \
	editor/ImageFile.cs \
	editor/ImageSelector.cs \
	editor/IntRange.cs \
	editor/NonContainerWarningDialog.cs \
	editor/OptIntRange.cs \
	editor/ProjectIconList.cs \
	editor/ProjectIconSelectorItem.cs \
	editor/ResponseId.cs \
	editor/SelectIconDialog.cs \
	editor/SelectImageDialog.cs \
	editor/StockIconList.cs \
	editor/StockIconSelectorItem.cs \
	editor/StockItem.cs \
	editor/String.cs \
	editor/StringArray.cs \
	editor/Text.cs \
	editor/TextBox.cs \
	editor/TextEditor.cs \
	editor/TextEditorDialog.cs \
	editor/ThemedIcon.cs \
	editor/ThemedIconList.cs \
	editor/TimeSpanEditor.cs \
	editor/Translatable.cs \
	editor/TreeViewCellContainer.cs \
	editor/WidgetSelector.cs \
	EnumDescriptor.cs \
	ErrorWidget.cs \
	GeneratorContext.cs \
	GladeException.cs \
	GladeUtils.cs \
	IDesignArea.cs \
	IEditableObject.cs \
	ImageInfo.cs \
	IProject.cs \
	IPropertyEditor.cs \
	IRadioGroupManager.cs \
	IResourceProvider.cs \
	ItemDescriptor.cs \
	ItemGroup.cs \
	ItemGroupCollection.cs \
	NoGuiDispatchAttribute.cs \
	ObjectReader.cs \
	ObjectWrapper.cs \
	ObjectWrapperEventHandler.cs \
	ObjectWriter.cs \
	ParamSpec.cs \
	Placeholder.cs \
	ProjectIconFactory.cs \
	PropertyDescriptor.cs \
	PropertyEditorAttribute.cs \
	PropertyEditorCell.cs \
	RadioGroupManager.cs \
	Registry.cs \
	Set.cs \
	SignalDescriptor.cs \
	TranslatableAttribute.cs \
	TypedClassDescriptor.cs \
	TypedPropertyDescriptor.cs \
	TypedSignalDescriptor.cs \
	undo/ActionDiffAdaptor.cs \
	undo/DiffGenerator.cs \
	undo/IDiffAdaptor.cs \
	undo/UndoManager.cs \
	undo/XmlDiffAdaptor.cs \
	WidgetLibrary.cs \
	WidgetUtils.cs \
	wrapper/Action.cs \
	wrapper/ActionGroup.cs \
	wrapper/ActionToolbarWrapper.cs \
	wrapper/ActionTree.cs \
	wrapper/Bin.cs \
	wrapper/Box.cs \
	wrapper/Button.cs \
	wrapper/ButtonBox.cs \
	wrapper/CheckButton.cs \
	wrapper/ColorButton.cs \
	wrapper/ComboBox.cs \
	wrapper/ComboBoxEntry.cs \
	wrapper/Container.cs \
	wrapper/Custom.cs \
	wrapper/Dialog.cs \
	wrapper/Entry.cs \
	wrapper/Expander.cs \
	wrapper/Fixed.cs \
	wrapper/FontButton.cs \
	wrapper/FontSelectionDialog.cs \
	wrapper/Frame.cs \
	wrapper/HScale.cs \
	wrapper/HScrollbar.cs \
	wrapper/IconView.cs \
	wrapper/Image.cs \
	wrapper/ImageMenuItem.cs \
	wrapper/Label.cs \
	wrapper/MenuBar.cs \
	wrapper/MenuItem.cs \
	wrapper/MessageDialog.cs \
	wrapper/Misc.cs \
	wrapper/Notebook.cs \
	wrapper/Object.cs \
	wrapper/OptionMenu.cs \
	wrapper/Paned.cs \
	wrapper/RadioActionGroupManager.cs \
	wrapper/RadioButton.cs \
	wrapper/RadioMenuItem.cs \
	wrapper/RadioToolButton.cs \
	wrapper/Range.cs \
	wrapper/Scale.cs \
	wrapper/ScrolledWindow.cs \
	wrapper/Signal.cs \
	wrapper/SignalChangedEventHandler.cs \
	wrapper/SignalCollection.cs \
	wrapper/SignalEventHandler.cs \
	wrapper/SpinButton.cs \
	wrapper/Table.cs \
	wrapper/TextView.cs \
	wrapper/ToggleToolButton.cs \
	wrapper/Toolbar.cs \
	wrapper/ToolButton.cs \
	wrapper/TreeView.cs \
	wrapper/Viewport.cs \
	wrapper/VScale.cs \
	wrapper/VScrollbar.cs \
	wrapper/Widget.cs \
	wrapper/WidgetEventHandler.cs \
	wrapper/WidgetNameChangedHandler.cs \
	wrapper/Window.cs

libstetic_dll_resources =  \
	stetic.glade \
	wrapper/objects.xml \
	wrapper/pixmaps/accellabel.png \
	wrapper/pixmaps/actiongroup.png \
	wrapper/pixmaps/add-check-label.png \
	wrapper/pixmaps/add-menu.png \
	wrapper/pixmaps/alignment.png \
	wrapper/pixmaps/arrow.png \
	wrapper/pixmaps/box-expand.png \
	wrapper/pixmaps/box-fill.png \
	wrapper/pixmaps/button.png \
	wrapper/pixmaps/calendar.png \
	wrapper/pixmaps/cell-expand-h.png \
	wrapper/pixmaps/cell-expand-v.png \
	wrapper/pixmaps/cell-fill-h.png \
	wrapper/pixmaps/cell-fill-v.png \
	wrapper/pixmaps/checkbutton.png \
	wrapper/pixmaps/colorbutton.png \
	wrapper/pixmaps/colorselection.png \
	wrapper/pixmaps/colorselectiondialog.png \
	wrapper/pixmaps/combo.png \
	wrapper/pixmaps/comboentry.png \
	wrapper/pixmaps/custom.png \
	wrapper/pixmaps/dec-border.png \
	wrapper/pixmaps/dialog.png \
	wrapper/pixmaps/drawingarea.png \
	wrapper/pixmaps/entry.png \
	wrapper/pixmaps/eventbox.png \
	wrapper/pixmaps/expander.png \
	wrapper/pixmaps/fileselection.png \
	wrapper/pixmaps/fixed.png \
	wrapper/pixmaps/fontbutton.png \
	wrapper/pixmaps/fontselection.png \
	wrapper/pixmaps/fontselectiondialog.png \
	wrapper/pixmaps/frame.png \
	wrapper/pixmaps/globe.png \
	wrapper/pixmaps/globe-not.png \
	wrapper/pixmaps/handlebox.png \
	wrapper/pixmaps/hbox.png \
	wrapper/pixmaps/hbuttonbox.png \
	wrapper/pixmaps/hpaned.png \
	wrapper/pixmaps/hscale.png \
	wrapper/pixmaps/hscrollbar.png \
	wrapper/pixmaps/hseparator.png \
	wrapper/pixmaps/iconview.png \
	wrapper/pixmaps/image.png \
	wrapper/pixmaps/inc-border.png \
	wrapper/pixmaps/label.png \
	wrapper/pixmaps/menu.png \
	wrapper/pixmaps/menubar.png \
	wrapper/pixmaps/messagedialog.png \
	wrapper/pixmaps/missing.png \
	wrapper/pixmaps/notebook.png \
	wrapper/pixmaps/optionmenu.png \
	wrapper/pixmaps/progressbar.png \
	wrapper/pixmaps/radiobutton.png \
	wrapper/pixmaps/remove-check-label.png \
	wrapper/pixmaps/remove-menu.png \
	wrapper/pixmaps/scrolledwindow.png \
	wrapper/pixmaps/spinbutton.png \
	wrapper/pixmaps/statusbar.png \
	wrapper/pixmaps/table.png \
	wrapper/pixmaps/textview.png \
	wrapper/pixmaps/togglebutton.png \
	wrapper/pixmaps/toolbar.png \
	wrapper/pixmaps/treeview.png \
	wrapper/pixmaps/vbox.png \
	wrapper/pixmaps/vbuttonbox.png \
	wrapper/pixmaps/viewport.png \
	wrapper/pixmaps/vpaned.png \
	wrapper/pixmaps/vscale.png \
	wrapper/pixmaps/vscrollbar.png \
	wrapper/pixmaps/vseparator.png \
	wrapper/pixmaps/widget.png \
	wrapper/pixmaps/window.png

libstetic_dll_datafiles = libstetic.dll.config

	
libstetic_dll_references =  \
	-pkg:glade-sharp-2.0 \
	-pkg:gnome-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-r:Mono.Posix \
	-r:System


all: $(ASSEMBLY) $(DATA_FILE_BUILD)

RESOURCE_FILES = $(addprefix $(srcdir)/, $(libstetic_dll_resources))

$(ASSEMBLY): $(libstetic_dll_sources) $(RESOURCE_FILES) $(DATA_FILE_BUILD)
	mkdir -p $(ASSEMBLY_DIR)
	@rm -f sources.rsp
	@echo $(libstetic_dll_sources:%=$(srcdir)/%) > sources.rsp
	@for resource in $(RESOURCE_FILES); do echo -resource:$$resource,`basename $$resource` >> sources.rsp; done
	$(MCS) -debug $(libstetic_dll_references) -target:library -out:$@ @sources.rsp

DATA_FILE_BUILD = $(addprefix $(ASSEMBLY_DIR)/, $(libstetic_dll_datafiles))

$(DATA_FILE_BUILD): $(srcdir)/$(notdir $@)
	mkdir -p $(ASSEMBLY_DIR)
	cp $(srcdir)/$(notdir $@) $(ASSEMBLY_DIR)/.

CLEANFILES = $(ASSEMBLY_FILES) sources.rsp

## Don't remove the 'COPIED' file below without fixing 'dist-hook'.
EXTRA_DIST =				\
	$(libstetic_dll_sources)	\
	$(RESOURCE_FILES)		\
	$(libstetic_dll_datafiles)		\
	wrapper/pixmaps/COPIED
