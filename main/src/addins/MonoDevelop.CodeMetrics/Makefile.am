
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.CodeMetrics
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.CodeMetrics.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/NRefactory.dll

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
	MonoDevelop.CodeMetrics/AssociationBetweenClasses.cs \
	MonoDevelop.CodeMetrics/ASTVisitor.cs \
	MonoDevelop.CodeMetrics/ClassProperties.cs \
	MonoDevelop.CodeMetrics/CodeMetricsNodeExtension.cs \
	MonoDevelop.CodeMetrics/CodeMetricsServices.cs \
	MonoDevelop.CodeMetrics/CodeMetricsView.cs \
	MonoDevelop.CodeMetrics/CodeMetricsWidget.cs \
	MonoDevelop.CodeMetrics/Cohesion.cs \
	MonoDevelop.CodeMetrics/Commands.cs \
	MonoDevelop.CodeMetrics/ComplexityMetrics.cs \
	MonoDevelop.CodeMetrics/Coupling.cs \
	MonoDevelop.CodeMetrics/DelegateProperties.cs \
	MonoDevelop.CodeMetrics/EnumProperties.cs \
	MonoDevelop.CodeMetrics/FieldProperties.cs \
	MonoDevelop.CodeMetrics/InheritanceTree.cs \
	MonoDevelop.CodeMetrics/InterfaceProperties.cs \
	MonoDevelop.CodeMetrics/IProperties.cs \
	MonoDevelop.CodeMetrics/LOCEvaluate.cs \
	MonoDevelop.CodeMetrics/MethodProperties.cs \
	MonoDevelop.CodeMetrics/MetricsContext.cs \
	MonoDevelop.CodeMetrics/NamespaceProperties.cs \
	MonoDevelop.CodeMetrics/ObjectOrientedMetrics.cs \
	MonoDevelop.CodeMetrics/ProjectProperties.cs \
	MonoDevelop.CodeMetrics/StructProperties.cs

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

