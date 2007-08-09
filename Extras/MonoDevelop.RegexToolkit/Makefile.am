
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.RegexToolkit
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.RegexToolkit.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	../../build/AddIns/MonoDevelop.Deployment.dll \
	../../build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	../../build/bin/MonoDevelop.Components.dll \
	../../build/bin/MonoDevelop.Core.dll \
	../../build/bin/MonoDevelop.Core.Gui.dll \
	../../build/bin/MonoDevelop.Ide.dll \
	../../build/bin/MonoDevelop.Projects.dll \
	../../build/bin/MonoDevelop.Projects.Gui.dll \
	../../contrib/Mono.Addins.dll \
	Mono.Posix \
	System \
	System.Data \
	System.Web \
	System.Web.Services \
	System.Xml
	
FILES =  \
	AssemblyInfo.cs \
	ChangeLog \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.RegexToolkit.RegexLibrary.cs \
	gtk-gui/MonoDevelop.RegexToolkit.RegexToolkit.cs \
	MonoDevelop.RegexToolkit/Commands.cs \
	MonoDevelop.RegexToolkit/RegexLibrary.cs \
	MonoDevelop.RegexToolkit/RegexToolkit.cs \
	MonoDevelop.RegexToolkit/Webservices.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.RegexToolkit.addin.xml \
	RegexElements.xml 



all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.RegexToolkit
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

