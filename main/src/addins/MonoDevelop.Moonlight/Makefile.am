ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Moonlight
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Moonlight.dll

REFS =  \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.AspNet/MonoDevelop.AspNet.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	gtk-gui/generated.cs \
	MonoDevelop.Moonlight/MoonlightProject.cs \
	MonoDevelop.Moonlight/MoonlightProjectBinding.cs \
	MonoDevelop.Moonlight/MoonlightProjectConfiguration.cs

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.Moonlight.addin.xml \
	Templates/Moonlight-Application.xpt.xml \
	Templates/Moonlight-Library.xpt.xml \
	Templates/Moonlight-Xaml.xft.xml \
	Templates/Moonlight-XamlWithCodeBehind.xft.xml

DATA_FILES = \
	Templates/Silverlight.js

DATA_FILE_BUILD = $(addprefix $(ADDIN_BUILD)/, $(notdir $(DATA_FILES)))

all: $(ASSEMBLY) $(DATA_FILE_BUILD)

$(DATA_FILE_BUILD): $(srcdir)/$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/Templates$(subst $(ADDIN_BUILD),,$@) $@

$(ASSEMBLY): $(build_sources) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) \
		$(build_resources:%=/resource:%) $(build_sources)

check: all

mdmoonlightlibdir = $(MD_ADDIN_DIR)/MonoDevelop.Moonlight
mdmoonlightlib_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

EXTRA_DIST = $(FILES) $(RES) $(DATA_FILES)

include $(top_srcdir)/Makefile.include

