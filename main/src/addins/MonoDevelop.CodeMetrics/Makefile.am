
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.CodeMetrics
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CodeMetrics.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
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
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Web \
	-r:System.Web.Services \
	-r:System.Xml
	
FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.CodeMetrics.CodeMetricsWidget.cs \
	MonoDevelop.CodeMetrics/CodeMetricsNodeExtension.cs \
	MonoDevelop.CodeMetrics/CodeMetricsView.cs \
	MonoDevelop.CodeMetrics/CodeMetricsWidget.cs \
	MonoDevelop.CodeMetrics/Commands.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.CodeMetrics.addin.xml 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.CodeMetrics
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

