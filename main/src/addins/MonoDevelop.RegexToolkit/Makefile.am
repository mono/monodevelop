
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.RegexToolkit
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.RegexToolkit.dll

DEPS =	\
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
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
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Data \
	-r:System.Web \
	-r:System.Web.Services \
	-r:System.Xml
	
FILES =  \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.RegexToolkit.RegexLibraryWindow.cs \
	gtk-gui/MonoDevelop.RegexToolkit.RegexToolkitWindow.cs \
	gtk-gui/MonoDevelop.RegexToolkit.UpdateInProgressDialog.cs \
	MonoDevelop.RegexToolkit/Commands.cs \
	MonoDevelop.RegexToolkit/RegexLibraryWindow.cs \
	MonoDevelop.RegexToolkit/RegexToolkitWindow.cs \
	MonoDevelop.RegexToolkit/UpdateInProgressDialog.cs \
	MonoDevelop.RegexToolkit/Webservices.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.RegexToolkit.addin.xml \
	RegexElements.xml 



all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.RegexToolkit
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

