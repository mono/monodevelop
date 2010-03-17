
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Projects.dll
CONFIG = MonoDevelop.Projects.dll.config
  
DEPS =  \
	$(top_builddir)/build/bin/Mono.Cecil.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Formats.MSBuild.exe

REFS =  \
	$(MONO_ADDINS_LIBS) \
	$(MONODOC_LIBS) \
	-r:Microsoft.Build.Engine \
	-r:Microsoft.Build.Framework \
	-r:Microsoft.Build.Utilities \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Data \
	-r:System.Drawing \
	-r:System.Runtime.Remoting \
	-r:System.Xml

FILES =

RES = 

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	$(CSC) $(CSC_FLAGS) -warnaserror -debug -out:$@ -target:library $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) \
	&& cp $(srcdir)/$(CONFIG) $(top_builddir)/build/bin
	
$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(CONFIG)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES) $(CONFIG)

include $(top_srcdir)/Makefile.include
