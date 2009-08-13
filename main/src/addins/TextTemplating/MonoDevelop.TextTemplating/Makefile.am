ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.TextTemplating
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.TextTemplating.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.TextTemplating/Mono.TextTemplating.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:System

FILES =  \
	AssemblyInfo.cs \
	Gui/T4EditorExtension.cs \
	Parser/T4ParsedDocument.cs \
	Parser/T4Parser.cs \
	TextTemplatingService.cs

RES =  \
	MonoDevelop.TextTemplating.addin.xml \
	T4SyntaxMode.xml

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.TextTemplating
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

