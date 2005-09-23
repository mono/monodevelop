FILES= \
src/SharpAssembly/PE/SectionTable.cs \
src/SharpAssembly/PE/IAT.cs \
src/SharpAssembly/PE/NameTable.cs \
src/SharpAssembly/PE/CLIHeader.cs \
src/SharpAssembly/PE/StandardFields.cs \
src/SharpAssembly/PE/DataDirectories.cs \
src/SharpAssembly/PE/ImportTable.cs \
src/SharpAssembly/PE/StreamHeader.cs \
src/SharpAssembly/PE/PEFileHeader.cs \
src/SharpAssembly/PE/NTSpecificFields.cs \
src/SharpAssembly/AssemblyReader.cs \
src/SharpAssembly/SharpAssemblyName.cs \
src/SharpAssembly/AssemblyMetadataTables.cs \
src/SharpAssembly/FusionNative.cs \
src/SharpAssembly/SharpAssembly.cs \
src/SharpAssembly/Metadata/Rows/Param.cs \
src/SharpAssembly/Metadata/Rows/TypeDef.cs \
src/SharpAssembly/Metadata/Rows/ModuleRef.cs \
src/SharpAssembly/Metadata/Rows/ClassLayout.cs \
src/SharpAssembly/Metadata/Rows/DeclSecurity.cs \
src/SharpAssembly/Metadata/Rows/TypeRef.cs \
src/SharpAssembly/Metadata/Rows/AssemblyProcessor.cs \
src/SharpAssembly/Metadata/Rows/FieldLayout.cs \
src/SharpAssembly/Metadata/Rows/Module.cs \
src/SharpAssembly/Metadata/Rows/Event.cs \
src/SharpAssembly/Metadata/Rows/AssemblyRefProcessor.cs \
src/SharpAssembly/Metadata/Rows/MethodSemantics.cs \
src/SharpAssembly/Metadata/Rows/AbstractRow.cs \
src/SharpAssembly/Metadata/Rows/AssemblyOS.cs \
src/SharpAssembly/Metadata/Rows/FieldMarshal.cs \
src/SharpAssembly/Metadata/Rows/ImplMap.cs \
src/SharpAssembly/Metadata/Rows/StandAloneSig.cs \
src/SharpAssembly/Metadata/Rows/Method.cs \
src/SharpAssembly/Metadata/Rows/ENCMap.cs \
src/SharpAssembly/Metadata/Rows/ENCLog.cs \
src/SharpAssembly/Metadata/Rows/FieldRVA.cs \
src/SharpAssembly/Metadata/Rows/AssemblyRefOS.cs \
src/SharpAssembly/Metadata/Rows/FieldPtr.cs \
src/SharpAssembly/Metadata/Rows/EventMap.cs \
src/SharpAssembly/Metadata/Rows/ManifestResource.cs \
src/SharpAssembly/Metadata/Rows/EventPtr.cs \
src/SharpAssembly/Metadata/Rows/PropertyMap.cs \
src/SharpAssembly/Metadata/Rows/Constant.cs \
src/SharpAssembly/Metadata/Rows/PropertyPtr.cs \
src/SharpAssembly/Metadata/Rows/File.cs \
src/SharpAssembly/Metadata/Rows/MemberRef.cs \
src/SharpAssembly/Metadata/Rows/Assembly.cs \
src/SharpAssembly/Metadata/Rows/Property.cs \
src/SharpAssembly/Metadata/Rows/ExportedType.cs \
src/SharpAssembly/Metadata/Rows/TypeSpec.cs \
src/SharpAssembly/Metadata/Rows/InterfaceImpl.cs \
src/SharpAssembly/Metadata/Rows/NestedClass.cs \
src/SharpAssembly/Metadata/Rows/AssemblyRef.cs \
src/SharpAssembly/Metadata/Rows/MethodPtr.cs \
src/SharpAssembly/Metadata/Rows/CodedIndex.cs \
src/SharpAssembly/Metadata/Rows/CustomAttribute.cs \
src/SharpAssembly/Metadata/Rows/ParamPtr.cs \
src/SharpAssembly/Metadata/Rows/Field.cs \
src/SharpAssembly/Metadata/Rows/MethodImpl.cs \
src/SharpAssembly/Metadata/AssemblyMetaData.cs \
src/SharpAssembly/Metadata/MetadataTable.cs \
src/SharpAssembly/Metadata/Signatures/DataTypes.cs \
src/SharpAssembly/Metadata/Signatures/CallingConventions.cs \
src/SharpAssembly/Metadata/MethodBody.cs \
src/SharpAssembly/AssemblyInfo.cs \
src/SharpAssembly/AssemblyNameNotFoundException.cs 

DLL = $(top_builddir)/build/bin/ICSharpCode.SharpAssembly.dll

all: $(DLL)

$(DLL): $(build_sources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DLL)
  
CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include

