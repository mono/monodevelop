
ADDIN = MonoDevelopDebugger.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/DebuggerAddIn
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Debugger.dll

DLLS = $(GTK_SHARP_LIBS) \
	$(MONO_DEBUGGER_LIBS) \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
        /r:$(top_builddir)/build/bin/ICSharpCode.SharpRefactory.dll

FILES = \
DebuggingService.cs \
DebugExecutionHandlerFactory.cs \
EvaluationContext.cs \
Expression.cs \
DebuggerASTVisitor.cs \
DebugAttributeHandler.cs \
Gui/Pads/LocalsPad.cs \
Gui/Pads/StackTracePad.cs \
Gui/Pads/ThreadPad.cs


EXTRA_DIST = $(FILES) $(ADDIN) DebuggerPaths.cs.in

if ENABLE_DEBUGGER

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

build_sources = $(addprefix $(srcdir)/, $(FILES)) DebuggerPaths.cs

DebuggerPaths.cs: $(srcdir)/DebuggerPaths.cs.in
	sed -e 's,@''pkglibdir@,$(pkglibdir),g' $(srcdir)/DebuggerPaths.cs.in > $@.tmp
	mv $@.tmp $@

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $@

$(ASSEMBLY): $(build_sources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library \
	$(build_sources) $(DLLS)

assemblydir = $(MD_ADDIN_DIR)/DebuggerAddIn
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = DebuggerPaths.cs

else
all:

endif
