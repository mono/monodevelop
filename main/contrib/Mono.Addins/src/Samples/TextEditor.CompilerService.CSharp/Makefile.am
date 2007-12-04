
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../bin/TextEditor.CompilerService.CSharp.dll
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/Mono.Addins.dll \
	../bin/TextEditor.CompilerService.dll
BUILD_DIR = ../bin

	
if ENABLE_GUI
all: $(ASSEMBLY)
endif

FILES =  \
	AssemblyInfo.cs \
	CSharpCompiler.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES = -r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

if ENABLE_GUI
include $(top_srcdir)/makefile-sample.include
endif
