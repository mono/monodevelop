
KEY_FILE=$(top_srcdir)/mono-addins.snk
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS = -keyfile:$(KEY_FILE) -langversion:ISO-1 -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../bin/Mono.Addins.Setup.dll
ASSEMBLY_NAME=Mono.Addins.Setup
COMPILE_TARGET = library
PC_FILES_IN = mono-addins-setup.pc.in
PROJECT_REFERENCES = ../bin/Mono.Addins.dll
BUILD_DIR = ../bin

	
all: $(ASSEMBLY)

FILES =  \
	AssemblyInfo.cs \
	Mono.Addins.Setup.ProgressMonitoring/ConsoleProgressMonitor.cs \
	Mono.Addins.Setup.ProgressMonitoring/LogTextWriter.cs \
	Mono.Addins.Setup.ProgressMonitoring/NullProgressMonitor.cs \
	Mono.Addins.Setup.ProgressMonitoring/ProgressStatusMonitor.cs \
	Mono.Addins.Setup.ProgressMonitoring/ProgressTracker.cs \
	Mono.Addins.Setup/AddinInfo.cs \
	Mono.Addins.Setup/AddinInfoCollection.cs \
	Mono.Addins.Setup/AddinPackage.cs \
	Mono.Addins.Setup/AddinRepositoryEntry.cs \
	Mono.Addins.Setup/AddinStore.cs \
	Mono.Addins.Setup/AddinSystemConfiguration.cs \
	Mono.Addins.Setup/AddinSystemConfigurationReaderWriter.cs \
	Mono.Addins.Setup/AddinSystemConfigurationSerializer.cs \
	Mono.Addins.Setup/ConsoleAddinInstaller.cs \
	Mono.Addins.Setup/InstallException.cs \
	Mono.Addins.Setup/IProgressMonitor.cs \
	Mono.Addins.Setup/Package.cs \
	Mono.Addins.Setup/PackageCollection.cs \
	Mono.Addins.Setup/ReferenceRepositoryEntry.cs \
	Mono.Addins.Setup/Repository.cs \
	Mono.Addins.Setup/RepositoryEntry.cs \
	Mono.Addins.Setup/RepositoryEntryCollection.cs \
	Mono.Addins.Setup/RepositoryReaderWriter.cs \
	Mono.Addins.Setup/RepositoryRecord.cs \
	Mono.Addins.Setup/RepositoryRegistry.cs \
	Mono.Addins.Setup/RepositorySerializer.cs \
	Mono.Addins.Setup/SetupService.cs \
	Mono.Addins.Setup/SetupTool.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	./Makefile.am 

REFERENCES =  \
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

install-data-local:
	echo "$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)";  \
	$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS) || exit 1;

uninstall-local:
	echo "$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS)"; \
	$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS) || exit 1;

include $(top_srcdir)/Makefile.include
