FILES= \
Properties/CharacterEncodings.cs \
Properties/DefaultProperties.cs \
Properties/UnknownPropertyNodeException.cs \
Properties/PropertyEventArgs.cs \
Properties/IProperties.cs \
Properties/PropertyFileLoadException.cs \
Properties/IXmlConvertable.cs \
Services/FileUtilityService.cs \
Services/AbstractService.cs \
Services/IMessageService.cs \
Services/StringParserService.cs \
Services/PropertyService.cs \
Services/ServiceManager.cs \
Services/IService.cs \
Services/IStringTagProvider.cs \
AddIns/Conditions/DuplicateConditionException.cs \
AddIns/Conditions/ConditionCollection.cs \
AddIns/Conditions/ICondition.cs \
AddIns/Conditions/CompareCondition.cs \
AddIns/Conditions/ConditionWithoutRequiredAttributesException.cs \
AddIns/Conditions/ConditionBuilder.cs \
AddIns/Conditions/ConditionNotFoundException.cs \
AddIns/Conditions/ConditionAttribute.cs \
AddIns/Conditions/ConditionBuilderCollection.cs \
AddIns/Conditions/ConditionFactory.cs \
AddIns/Conditions/AbstractCondition.cs \
AddIns/Conditions/ComplexConditions.cs \
AddIns/PathAttribute.cs \
AddIns/Codons/CodonBuilder.cs \
AddIns/Codons/CodonNameAttribute.cs \
AddIns/Codons/ClassCodon.cs \
AddIns/Codons/CodonNotFoundException.cs \
AddIns/Codons/AbstractCodon.cs \
AddIns/Codons/CodonFactory.cs \
AddIns/Codons/IconCodon.cs \
AddIns/Codons/AbstractCommand.cs \
AddIns/Codons/DuplicateCodonException.cs \
AddIns/Codons/ICommand.cs \
AddIns/Codons/ICodon.cs \
AddIns/AddInLoadException.cs \
AddIns/TreePathNotFoundException.cs \
AddIns/DefaultAddInTreeNode.cs \
AddIns/AddIn.cs \
AddIns/IAddInTree.cs \
AddIns/DefaultAddInTree.cs \
AddIns/IAddInTreeNode.cs \
AddIns/AddInTreeSingleton.cs \
AddIns/XmlMemberAttributeAttribute.cs \
AddIns/XmlMemberArraryAttribute.cs \
AddIns/AddInTreeFormatException.cs \
AddIns/TypeNotFoundException.cs \
AddIns/AddInInitializeException.cs \
AddIns/AddInCollection.cs \
AddIns/AssemblyLoader.cs \
AddIns/AddInSignatureException.cs

build_sources = $(addprefix $(srcdir)/, $(FILES)) Services/GettextCatalog.cs AssemblyInfo.cs

DLL = $(top_builddir)/build/bin/MonoDevelop.Core.dll

REFS = -r:Mono.Posix.dll \
	-r:$(top_builddir)/build/bin/Mono.Cecil.dll

all: $(DLL)

Services/GettextCatalog.cs: $(srcdir)/Services/GettextCatalog.cs.in
	mkdir -p Services
	sed -e 's,@''prefix@,$(prefix),g' $(srcdir)/$@.in > $@.tmp
	mv $@.tmp $@

$(DLL): $(build_sources) $(build_resources)
	$(CSC) $(CSC_FLAGS) -out:$@ -target:library $(REFS) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb
DISTCLEANFILES = Services/GettextCatalog.cs

EXTRA_DIST = $(FILES) Services/GettextCatalog.cs.in

