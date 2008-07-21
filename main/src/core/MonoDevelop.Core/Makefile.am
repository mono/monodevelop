
ASSEMBLY = $(top_builddir)/build/bin/MonoDevelop.Core.dll

FILES =  \
	MonoDevelop.Core.AddIns/ApplicationExtensionNode.cs \
	MonoDevelop.Core.AddIns/AssemblyExtensionNode.cs \
	MonoDevelop.Core.AddIns/ExecutionHandlerCodon.cs \
	MonoDevelop.Core.AddIns/ExecutionModeNode.cs \
	MonoDevelop.Core.AddIns/PackageExtensionNode.cs \
	MonoDevelop.Core.AddIns/ServiceExtensionNode.cs \
	MonoDevelop.Core.Execution/AddinDependencyAttribute.cs \
	MonoDevelop.Core.Execution/BreakpointEventHandler.cs \
	MonoDevelop.Core.Execution/DefaultExecutionHandlerFactory.cs \
	MonoDevelop.Core.Execution/ExecutionPlatform.cs \
	MonoDevelop.Core.Execution/ExternalConsoleFactory.cs \
	MonoDevelop.Core.Execution/ExternalConsoleLocator.cs \
	MonoDevelop.Core.Execution/IBreakpoint.cs \
	MonoDevelop.Core.Execution/IConsole.cs \
	MonoDevelop.Core.Execution/IConsoleFactory.cs \
	MonoDevelop.Core.Execution/IExecutionHandler.cs \
	MonoDevelop.Core.Execution/IExecutionHandlerFactory.cs \
	MonoDevelop.Core.Execution/IExecutionMode.cs \
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
	MonoDevelop.Core.Logging/ConsoleCrayon.cs \
	MonoDevelop.Core.Logging/ConsoleLogger.cs \
	MonoDevelop.Core.Logging/FileLogger.cs \
	MonoDevelop.Core.Logging/ILogger.cs \
	MonoDevelop.Core.Logging/LogLevel.cs \
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
	MonoDevelop.Core.Serialization/ArrayHandler.cs \
	MonoDevelop.Core.Serialization/ArrayListHandler.cs \
	MonoDevelop.Core.Serialization/ClassDataType.cs \
	MonoDevelop.Core.Serialization/CollectionDataType.cs \
	MonoDevelop.Core.Serialization/DataCollection.cs \
	MonoDevelop.Core.Serialization/DataContext.cs \
	MonoDevelop.Core.Serialization/DataIncludeAttribute.cs \
	MonoDevelop.Core.Serialization/DataItem.cs \
	MonoDevelop.Core.Serialization/DataItemAttribute.cs \
	MonoDevelop.Core.Serialization/DataNode.cs \
	MonoDevelop.Core.Serialization/DataSerializer.cs \
	MonoDevelop.Core.Serialization/DataType.cs \
	MonoDevelop.Core.Serialization/DataValue.cs \
	MonoDevelop.Core.Serialization/DictionaryDataType.cs \
	MonoDevelop.Core.Serialization/EnumDataType.cs \
	MonoDevelop.Core.Serialization/ExpandedCollectionAttribute.cs \
	MonoDevelop.Core.Serialization/GenericCollectionHandler.cs \
	MonoDevelop.Core.Serialization/ICollectionHandler.cs \
	MonoDevelop.Core.Serialization/ICustomDataItemHandler.cs \
	MonoDevelop.Core.Serialization/IExtendedDataItem.cs \
	MonoDevelop.Core.Serialization/ILoadController.cs \
	MonoDevelop.Core.Serialization/IPropertyFilter.cs \
	MonoDevelop.Core.Serialization/ISerializationAttributeProvider.cs \
	MonoDevelop.Core.Serialization/ItemProperty.cs \
	MonoDevelop.Core.Serialization/ItemPropertyAttribute.cs \
	MonoDevelop.Core.Serialization/PrimitiveDataType.cs \
	MonoDevelop.Core.Serialization/SerializationContext.cs \
	MonoDevelop.Core.Serialization/TypeAttributeProvider.cs \
	MonoDevelop.Core.Serialization/XmlDataSerializer.cs \
	MonoDevelop.Core.Serialization/XmlElementDataType.cs \
	MonoDevelop.Core.Serialization/XmlMapAttributeProvider.cs \
	MonoDevelop.Core/AbstractService.cs \
	MonoDevelop.Core/ClrVersion.cs \
	MonoDevelop.Core/DefaultAddinLocalizer.cs \
	MonoDevelop.Core/FileEventArgs.cs \
	MonoDevelop.Core/FileService.cs \
	MonoDevelop.Core/GettextCatalog.cs \
	MonoDevelop.Core/IAsyncOperation.cs \
	MonoDevelop.Core/ICustomXmlSerializer.cs \
	MonoDevelop.Core/IProgressMonitor.cs \
	MonoDevelop.Core/IService.cs \
	MonoDevelop.Core/LoggingService.cs \
	MonoDevelop.Core/Properties.cs \
	MonoDevelop.Core/PropertyChangedEventArgs.cs \
	MonoDevelop.Core/PropertyService.cs \
	MonoDevelop.Core/Runtime.cs \
	MonoDevelop.Core/ServiceManager.cs \
	MonoDevelop.Core/StringParserService.cs \
	MonoDevelop.Core/SystemAssemblyService.cs \
	MonoDevelop.Core/SystemPackage.cs \
	MonoDevelop.Core/UserException.cs \
	MonoDevelop.Core/XmlReadHelper.cs

GENERATED_FILES =  \
		  AssemblyInfo.cs

RES = MonoDevelop.Core.addin.xml                                                          

REFS =  \
	$(MONO_ADDINS_LIBS) \
	$(MONO_ADDINS_SETUP_LIBS) \
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix \
	-r:System \
	-r:System.Runtime.Remoting \
	-r:System.Xml

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_resources:%=/resource:%) $(build_sources) 

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
