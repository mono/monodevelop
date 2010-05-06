ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.TextTemplating
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.TextTemplating.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.SourceEditor2.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.TextTemplating/Mono.TextTemplating.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:System \
	-r:System.Core

FILES =  \
	AssemblyInfo.cs \
	Gui/T4EditorExtension.cs \
	Parser/T4ParsedDocument.cs \
	Parser/T4Parser.cs \
	TextTemplatingService.cs \
	TextTemplatingTool.cs

RES =  \
	MonoDevelop.TextTemplating.addin.xml \
	T4SyntaxMode.xml

DATA_FILES = \
	Templates/T4TemplateCSharp.xft.xml

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(DATA_FILES))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(DATA_FILE_BUILD): $(ADDIN_BUILD)/% : $(srcdir)/%
	mkdir -p $(dir $@)
	cp $^ $@

$(ASSEMBLY).mdb: $(ASSEMBLY)

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.TextTemplating
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

install-data-hook:
	for F in $(DATA_FILES); do \
		mkdir -p "$(DESTDIR)$(assemblydir)/$$(dirname $$F)/"; \
		$(INSTALL) -c -m 0644 $(srcdir)/$$F "$(DESTDIR)$(assemblydir)/$$(dirname $$F)/"; \
	done

uninstall-hook:
	for F in $(DATA_FILES); do \
		rm -f "$(DESTDIR)$(assemblydir)/$$F"; \
	done

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

