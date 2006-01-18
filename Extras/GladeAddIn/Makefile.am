
ADDIN = GladeAddIn.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/GladeAddIn
ASSEMBLY = $(ADDIN_BUILD)/GladeAddIn.dll

DLLS = $(GTK_SHARP_LIBS) \
	$(GLADEUI_LIBS) \
	-pkg:gconf-sharp-2.0 \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	/r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll

FILES = \
Metacity/ButtonFunction.cs \
Metacity/ButtonLayout.cs \
Metacity/FrameFlags.cs \
Metacity/FrameType.cs \
Metacity/ObjectManager.cs \
Metacity/Preview.cs \
Metacity/Theme.cs \
GladeAddIn.Commands/GladeCommands.cs \
GladeAddIn.Gui/ProjectFolderNodeBuilderExtension.cs \
GladeAddIn.Gui/GladeFileTemplate.cs \
GladeAddIn.Gui/GladePalettePad.cs \
GladeAddIn.Gui/GladeProjectNodeBuilder.cs \
GladeAddIn.Gui/GladeProjectPad.cs \
GladeAddIn.Gui/GladePropertiesPad.cs \
GladeAddIn.Gui/GladeService.cs \
GladeAddIn.Gui/GuiBuilderProject.cs \
GladeAddIn.Gui/GuiBuilderProjectNodeBuilder.cs \
GladeAddIn.Gui/GuiBuilderWindow.cs \
GladeAddIn.Gui/GuiBuilderWindowNodeBuilder.cs \
GladeAddIn.Gui/GladeWidgetTreePad.cs \
GladeAddIn.Gui/WindowsFolder.cs \
GladeAddIn.Gui/EmbedWindow.cs \
GladeAddIn.Gui/PreviewBox.cs \
GladeAddIn.Gui/GuiBuilderView.cs \
GladeAddIn.Gui/GuiBuilderDisplayBinding.cs \
GladeAddIn.Gui/GuiBuilderEditSession.cs \
GladeAddIn.Gui/ClassUtils.cs \
GladeAddIn.Gui/GladeFileDisplayBinding.cs \
GladeAddIn.Gui/WindowsFolderNodeBuilder.cs

RES = \
templates/GladeWindow.xft.xml \
templates/GladeDialog.xft.xml

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

if ENABLE_GLADE

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

build_sources = $(addprefix $(srcdir)/, $(FILES))

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ASSEMBLY): $(build_sources) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(RES:%=/resource:%) $(build_sources) $(DLLS)

assemblydir = $(MD_ADDIN_DIR)/GladeAddIn
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

else
all:

endif
