
ADDIN = MonoDevelop.RegexToolkit.addin.xml
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.RegexToolkit
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.RegexToolkit.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	../../build/AddIns/MonoDevelop.Components.dll \
	../../build/AddIns/MonoDevelop.Core.Gui.dll \
	../../build/AddIns/MonoDevelop.Deployment.dll \
	../../build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	../../build/AddIns/MonoDevelop.Ide.dll \
	../../build/AddIns/MonoDevelop.Projects.dll \
	../../build/AddIns/MonoDevelop.Projects.Gui.dll \
	../../build/bin/MonoDevelop.Core.dll \
	../../contrib/Mono.Addins.dll \
	Mono.Posix \
	System \
	System.Xml
	
FILES =  \
	AssemblyInfo.cs \
	ChangeLog \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.RegexToolkit.RegexToolkit.cs \
	MonoDevelop.RegexToolkit/Commands.cs \
	MonoDevelop.RegexToolkit/RegexToolkit.cs

RES = gtk-gui/gui.stetic 



all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/$(ADDIN)

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.RegexToolkit
assembly_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include

