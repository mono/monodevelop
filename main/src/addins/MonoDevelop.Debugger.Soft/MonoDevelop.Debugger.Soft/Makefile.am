ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Debugger.Soft
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Debugger.Soft.dll

DBG_ASM = $(ADDIN_BUILD)/Mono.Debugger.Soft.dll
DBG_ASM_SRC = $(top_srcdir)/src/addins/MonoDevelop.Debugger.Soft/MonoDevelop.Debugger.Soft/Mono.Debugger.Soft.dll
DBG_MDB = $(DBG_ASM).mdb
DBG_MDB_SRC = $(DBG_ASM_SRC).mdb

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Debugger/MonoDevelop.Debugger.dll \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
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
	-r:$(top_srcdir)/src/addins/MonoDevelop.Debugger.Soft/MonoDevelop.Debugger.Soft/Mono.Debugger.Soft.dll \
	-r:System \
	-r:System.Core

FILES =  \
	ArrayAdaptor.cs \
	AssemblyInfo.cs \
	FieldValueReference.cs \
	PropertyValueReference.cs \
	RemoteSoftDebuggerSession.cs \
	SoftDebuggerAdaptor.cs \
	SoftDebuggerBacktrace.cs \
	SoftDebuggerEngine.cs \
	SoftDebuggerSession.cs \
	SoftEvaluationContext.cs \
	VariableValueReference.cs

RES = Manifest.addin.xml

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS) $(DBG_ASM) $(DBG_MDB)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(DBG_ASM): $(DBG_ASM_SRC)
	mkdir -p $(ADDIN_BUILD)
	cp $^ $@ 

$(DBG_MDB): $(DBG_MDB_SRC)
	mkdir -p $(ADDIN_BUILD)
	cp $^ $@

$(ASSEMBLY).mdb: $(ASSEMBLY)

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Debugger.Soft
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DBG_ASM) $(DBG_MDB)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DBG_ASM) $(DBG_MDB)
EXTRA_DIST = $(FILES) $(RES) $(DBG_ASM_SRC) $(DBG_MDB_SRC)

include $(top_srcdir)/Makefile.include

