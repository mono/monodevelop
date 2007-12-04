ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../lib/extras/FileExtender.dll
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/Mono.Addins.dll \
	../lib/FileContentExtension.dll
BUILD_DIR = ../lib/extras

	
if ENABLE_TESTS
all: $(ASSEMBLY)
endif

FILES = FileExtender.cs 

DATA_FILES = ExtraExtender.addin.xml 

RESOURCES = FileExtender.addin.xml 

EXTRAS = 

REFERENCES = -r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

if ENABLE_TESTS
include $(top_srcdir)/makefile-sample.include
endif
