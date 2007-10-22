
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../bin/mautil.exe
COMPILE_TARGET = exe
ASSEMBLY_WRAPPER = mautil
ASSEMBLY_WRAPPER_IN = mautil.in
PROJECT_REFERENCES =  \
	../bin/Mono.Addins.dll \
	../bin/Mono.Addins.Setup.dll
BUILD_DIR = ../bin

	
all: $(ASSEMBLY)

FILES =  \
	AssemblyInfo.cs \
	Main.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES = -r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
