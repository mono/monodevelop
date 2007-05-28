
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Core.dll

FILES =  \
	MonoDevelop.Core.AddIns/ApplicationExtensionNode.cs \
	MonoDevelop.Core.AddIns/AssemblyExtensionNode.cs \
	MonoDevelop.Core.AddIns/ExecutionHandlerCodon.cs \
	MonoDevelop.Core.AddIns/IconCodon.cs \
	MonoDevelop.Core.AddIns/PackageExtensionNode.cs \
	MonoDevelop.Core.AddIns/ServiceExtensionNode.cs \
	MonoDevelop.Core.AddIns/SetupApp.cs \
	MonoDevelop.Core.Execution/AddinDependencyAttribute.cs \
	MonoDevelop.Core.Execution/BreakpointEventHandler.cs \
	MonoDevelop.Core.Execution/DefaultExecutionHandlerFactory.cs \
	MonoDevelop.Core.Execution/ExternalConsoleFactory.cs \
	MonoDevelop.Core.Execution/IBreakpoint.cs \
	MonoDevelop.Core.Execution/IConsole.cs \
	MonoDevelop.Core.Execution/IConsoleFactory.cs \
	MonoDevelop.Core.Execution/IDebuggerService.cs \
	MonoDevelop.Core.Execution/IExecutionHandler.cs \
	MonoDevelop.Core.Execution/IExecutionHandlerFactory.cs \
	MonoDevelop.Core.Execution/IProcessAsyncOperation.cs \
	MonoDevelop.Core.Execution/IProcessHost.cs \
	MonoDevelop.Core.Execution/IProcessHostController.cs \
	MonoDevelop.Core.Execution/MintPlatformExecutionHandler.cs \
	MonoDevelop.Core.Execution/MonoPlatformExecutionHandler.cs \
	MonoDevelop.Core.Execution/NativePlatformExecutionHandler.cs \
	MonoDevelop.Core.Execution/ProcessHostController.cs \
	MonoDevelop.Core.Execution/ProcessService.cs \
	MonoDevelop.Core.Execution/ProcessWrapper.cs \
	MonoDevelop.Core.Execution/RemoteProcessObject.cs \
	MonoDevelop.Core.FileSystem/DefaultFileSystemExtension.cs \
	MonoDevelop.Core.FileSystem/FileSystemExtension.cs \
	MonoDevelop.Core.ProgressMonitoring/AggregatedOperationMonitor.cs \
	MonoDevelop.Core.ProgressMonitoring/AggregatedProgressMonitor.cs \
	MonoDevelop.Core.ProgressMonitoring/ConsoleProgressMonitor.cs \
	MonoDevelop.Core.ProgressMonitoring/ConsoleProgressStatus.cs \
	MonoDevelop.Core.ProgressMonitoring/FilteredProgressMonitor.cs \
	MonoDevelop.Core.ProgressMonitoring/LogTextWriter.cs \
	MonoDevelop.Core.ProgressMonitoring/NullAsyncOperation.cs \
	MonoDevelop.Core.ProgressMonitoring/NullProgressMonitor.cs \
	MonoDevelop.Core.ProgressMonitoring/ProgressTracker.cs \
	MonoDevelop.Core.ProgressMonitoring/SynchronizedProgressMonitor.cs \
	MonoDevelop.Core.Properties/CharacterEncodings.cs \
	MonoDevelop.Core.Properties/DefaultProperties.cs \
	MonoDevelop.Core.Properties/IProperties.cs \
	MonoDevelop.Core.Properties/IXmlConvertable.cs \
	MonoDevelop.Core.Properties/PropertyEventArgs.cs \
	MonoDevelop.Core.Properties/PropertyFileLoadException.cs \
	MonoDevelop.Core.Properties/UnknownPropertyNodeException.cs \
	MonoDevelop.Core.Utils.DirectoryArchive/Bz2Support.cs \
	MonoDevelop.Core.Utils.DirectoryArchive/Decompressor.cs \
	MonoDevelop.Core.Utils.DirectoryArchive/GzSupport.cs \
	MonoDevelop.Core.Utils.DirectoryArchive/TarDecompressor.cs \
	MonoDevelop.Core.Utils.DirectoryArchive/ZipDecompressor.cs \
	MonoDevelop.Core.Utils.ReportingStream/ReportingStream.cs \
	MonoDevelop.Core/AbstractService.cs \
	MonoDevelop.Core/ClrVersion.cs \
	MonoDevelop.Core/DefaultLoggingService.cs \
	MonoDevelop.Core/FileEventArgs.cs \
	MonoDevelop.Core/FileService.cs \
	MonoDevelop.Core/IAsyncOperation.cs \
	MonoDevelop.Core/ILoggingService.cs \
	MonoDevelop.Core/IProgressMonitor.cs \
	MonoDevelop.Core/IService.cs \
	MonoDevelop.Core/IStringTagProvider.cs \
	MonoDevelop.Core/PropertyService.cs \
	MonoDevelop.Core/Runtime.cs \
	MonoDevelop.Core/ServiceManager.cs \
	MonoDevelop.Core/StringParserService.cs \
	MonoDevelop.Core/SystemAssemblyService.cs \
	MonoDevelop.Core/SystemPackage.cs \
	MonoDevelop.Core/UserException.cs

GENERATED_FILES = MonoDevelop.Core/GettextCatalog.cs \
		  AssemblyInfo.cs

RES =                                                          

REFS =  \
	-r:$(top_srcdir)/contrib/log4net.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.Setup.dll \
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix \
	-r:System \
	-r:System.Runtime.Remoting \
	-r:System.Xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

MonoDevelop.Core/GettextCatalog.cs: $(srcdir)/MonoDevelop.Core/GettextCatalog.cs.in
	mkdir -p MonoDevelop.Core
	sed -e 's,@''prefix@,$(prefix),g' $(srcdir)/$@.in > $@.tmp
	mv $@.tmp $@

ADDIN = MonoDevelop.Core.addin.xml

ADDIN_BUILD = $(top_builddir)/build/AddIns

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(build_sources) $(RES)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(RES:%=/resource:%) $(build_sources) 

addindir = $(MD_ADDIN_DIR)
addin_DATA = $(ADDIN)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = MonoDevelop.Core/GettextCatalog.cs

EXTRA_DIST = $(FILES) $(ADDIN) MonoDevelop.Core/GettextCatalog.cs.in

include $(top_srcdir)/Makefile.include
