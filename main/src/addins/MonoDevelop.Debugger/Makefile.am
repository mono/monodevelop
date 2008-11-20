
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Debugger
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Debugger.dll

DEPS =  \
	$(top_builddir)/build/bin/Mono.Debugging.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml 

FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	MonoDevelop.Debugger/BreakpointPad.cs \
	MonoDevelop.Debugger/DebugTextMarker.cs \
	MonoDevelop.Debugger/DisasemblyView.cs \
	MonoDevelop.Debugger/Initializer.cs \
	MonoDevelop.Debugger/LocalsPad.cs \
	MonoDevelop.Debugger/ObjectValuePad.cs \
	MonoDevelop.Debugger/ObjectValueTreeView.cs \
	MonoDevelop.Debugger/StackTracePad.cs \
	MonoDevelop.Debugger/ThreadsPad.cs \
	MonoDevelop.Debugger/WatchPad.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Debugger.addin.xml

DATA_FILES =

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/templates$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Debugger
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
