ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../lib/SystemInfoExtension.dll
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/Mono.Addins.dll \
	../UnitTests/bin/Debug/UnitTests.dll
BUILD_DIR = ../lib

	
if ENABLE_TESTS
all: $(ASSEMBLY) $(SATELLITE_ASSEMBLY)
endif

FILES =  \
	ModuleExtensionNode.cs \
	MyClass.cs \
	SystemInfoWriter.cs

DATA_FILES = SystemInfoExtension.addin.xml 

RESOURCES =
	
SATELLITE_ASSEMBLY = ../lib/ca/SystemInfoExtension.resources.dll
MAIN_RESOURCE = Resources.resources

EXTRAS = Resources.resx Resources.ca.resx

REFERENCES = -r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) $(MAIN_RESOURCE)
	mkdir -p $(dir $(ASSEMBLY))		
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) /res:$(MAIN_RESOURCE) $(build_sources) $(build_resources_embed) $(build_references_ref)

$(MAIN_RESOURCE): Resources.resx
	resgen /compile Resources.resx

$(SATELLITE_ASSEMBLY): Resources.ca.resx
	resgen /compile Resources.ca.resx
	mkdir -p $(dir $(ASSEMBLY))/ca
	al /t:lib /embed:Resources.ca.resources,Resources.ca.resources /out:$(SATELLITE_ASSEMBLY) /culture:ca
# Workaround to mono bug #345627 - Satellite resource assemblies not loaded from the main assembly directory
	mkdir -p ../UnitTests/bin/Debug/ca
	cp $(SATELLITE_ASSEMBLY) ../UnitTests/bin/Debug/ca

if ENABLE_TESTS

include $(top_srcdir)/makefile-sample.include

CLEANFILES += $(SATELLITE_ASSEMBLY) $(MAIN_RESOURCE)

endif

