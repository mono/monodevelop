
ADDIN_BUILD = $(top_builddir)/build/AddIns/DebuggerAddIn
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Debugger.dll

REFS =	$(GTK_SHARP_LIBS) \
	$(MONO_DEBUGGER_LIBS) \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
        -r:$(top_builddir)/build/AddIns/ICSharpCode.SharpRefactory.dll

FILES = \
DebuggingService.cs \
DebugExecutionHandlerFactory.cs \
EvaluationContext.cs \
Expression.cs \
DebuggerASTVisitor.cs \
DebugAttributeHandler.cs \
Services.cs \
Gui/Pads/LocalsPad.cs \
Gui/Pads/StackTracePad.cs \
Gui/Pads/ThreadPad.cs

RES = MonoDevelopDebugger.addin.xml

GENERATED_FILES = DebuggerPaths.cs
EXTRA_DIST = $(FILES) $(ADDIN) DebuggerPaths.cs.in $(RES)

if ENABLE_DEBUGGER

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

DebuggerPaths.cs: $(srcdir)/DebuggerPaths.cs.in
	sed -e 's,@''pkglibdir@,$(pkglibdir),g' $(srcdir)/DebuggerPaths.cs.in > $@.tmp
	mv $@.tmp $@

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(build_resources:%=/resource:%) $(build_sources) $(REFS)

assemblydir = $(MD_ADDIN_DIR)/DebuggerAddIn
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = DebuggerPaths.cs

else
all:

endif
