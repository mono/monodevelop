ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../lib/CommandExtension.dll
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/Mono.Addins.dll \
	../UnitTests/bin/Debug/UnitTests.dll
BUILD_DIR = ../lib

	
if ENABLE_TESTS
all: $(ASSEMBLY)
endif

FILES = CommandExtensionNode.cs 

DATA_FILES = CommandExtension.addin.xml 

RESOURCES = 

EXTRAS = 

REFERENCES = -r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

if ENABLE_TESTS
include $(top_srcdir)/makefile-sample.include
endif
