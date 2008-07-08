
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Debugger
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Debugger.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:../../../build/bin/Mono.Debugging.dll \
	-r:../../../build/bin/Mono.TextEditor.dll \
	-r:../../../build/bin/MonoDevelop.Components.dll \
	-r:../../../build/bin/MonoDevelop.Core.dll \
	-r:../../../build/bin/MonoDevelop.Core.Gui.dll \
	-r:../../../build/bin/MonoDevelop.Ide.dll \
	-r:../../../build/bin/MonoDevelop.Projects.dll \
	-r:../../../build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Cairo \
	-r:System 

FILES =  \
	AssemblyInfo.cs \
	MonoDevelop.Debugger/DebugTextMarker.cs \
	MonoDevelop.Debugger/DisasemblyView.cs \
	MonoDevelop.Debugger/Initializer.cs \
	MonoDevelop.Debugger/ObjectValueTreeView.cs \
	MonoDevelop.Debugger/StackTracePad.cs \
	MonoDevelop.Debugger/WatchPad.cs

RES = MonoDevelop.Debugger.addin.xml

DATA_FILES =

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/templates$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Debugger
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include
