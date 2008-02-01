srcdir=.
top_srcdir=.

include $(top_srcdir)/Makefile.include
include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG\;TRACE
ASSEMBLY = build/AspNetEdit.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = build/AspNetEdit.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = build


endif

extension_name = aspdesigner
extension_jar = $(shell pwd)/$(BUILD_DIR)/$(extension_name).jar

FILES =  \
	AspNetEdit.Editor.ComponentModel/DesignContainer.cs \
	AspNetEdit.Editor.ComponentModel/DesignerHost.cs \
	AspNetEdit.Editor.ComponentModel/Document.cs \
	AspNetEdit.Editor.ComponentModel/DocumentDirective.cs \
	AspNetEdit.Editor.ComponentModel/EventBindingService.cs \
	AspNetEdit.Editor.ComponentModel/ExtenderListService.cs \
	AspNetEdit.Editor.ComponentModel/MenuCommandService.cs \
	AspNetEdit.Editor.ComponentModel/NameCreationService.cs \
	AspNetEdit.Editor.ComponentModel/RootDesigner.cs \
	AspNetEdit.Editor.ComponentModel/SelectionService.cs \
	AspNetEdit.Editor.ComponentModel/TextToolboxItem.cs \
	AspNetEdit.Editor.ComponentModel/ToolboxService.cs \
	AspNetEdit.Editor.ComponentModel/Transaction.cs \
	AspNetEdit.Editor.ComponentModel/TypeDescriptorFilterService.cs \
	AspNetEdit.Editor.ComponentModel/TypeResolutionService.cs \
	AspNetEdit.Editor.ComponentModel/WebFormPage.cs \
	AspNetEdit.Editor.ComponentModel/WebFormReferenceManager.cs \
	AspNetEdit.Editor.Persistence/ControlPersister.cs \
	AspNetEdit.Editor.Persistence/DesignTimeParser.cs \
	AspNetEdit.Editor.Persistence/HtmlParsingObject.cs \
	AspNetEdit.Editor.Persistence/ParsingObject.cs \
	AspNetEdit.Editor.Persistence/RootParsingObject.cs \
	AspNetEdit.Editor.Persistence/ServerControlParsingObject.cs \
	AspNetEdit.Editor.UI/PropertyGrid.cs \
	AspNetEdit.Editor.UI/RootDesignerView.cs \
	AspNetEdit.Editor/EditorHost.cs \
	AspNetEdit.Integration/AspNetEditDisplayBinding.cs \
	AspNetEdit.Integration/AspNetEditViewContent.cs \
	AspNetEdit.Integration/EditorProcess.cs \
	AspNetEdit.Integration/GeckoWebBrowser.cs \
	AspNetEdit.Integration/MonoDevelopProxy.cs \
	AspNetEdit.Integration/ToolboxProvider.cs \
	AspNetEdit.JSCall/CommandManager.cs \
	AspNetEdit.JSCall/InvalidJSArgumentException.cs \
	AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = AspNetEdit.addin.xml 

EXTRAS = \
	Makefile.am \
	chrome/install.rdf \
	chrome/Makefile.am \
	chrome/README \
	chrome/content/aspdesigner/aspdesigner.xul \
	chrome/content/aspdesigner/clipboard.js \
	chrome/content/aspdesigner/constants.js \
	chrome/content/aspdesigner/contents.rdf \
	chrome/content/aspdesigner/editor.js \
	chrome/content/aspdesigner/editorContent.css \
	chrome/content/aspdesigner/JSCall.js \
	chrome/content/aspdesigner/xpcom.js \
	chrome/locale/en-US/aspdesigner/contents.rdf \
	ChangeLog \
	chrome/aspdesigner.manifest.in 

REFERENCES =  \
	-pkg:gecko-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:mono-addins \
	-pkg:monodevelop \
	-pkg:monodevelop-core-addins \
	System \
	System.Design \
	System.Drawing \
	System.Drawing.Design \
	System.Web \
	System.Xml

DLL_REFERENCES = 

CLEANFILES += 

INSTALL_DIR = $(prefix)/lib/monodevelop/AddIns/AspNetEdit

## actually all in $(srcdir)/chrome, but we handle this later
## need to handle paths this way to get gorrect paths in the zip
jar_files = \
	content/aspdesigner/contents.rdf \
	content/aspdesigner/aspdesigner.xul \
	content/aspdesigner/editorContent.css \
	content/aspdesigner/editor.js \
	content/aspdesigner/clipboard.js \
	content/aspdesigner/constants.js \
	content/aspdesigner/xpcom.js \
	content/aspdesigner/JSCall.js \
	locale/en-US/aspdesigner/contents.rdf


#Targets

all-local: $(ASSEMBLY) $(extension_jar) $(top_srcdir)/config.make

LOCAL_PKGCONFIG=PKG_CONFIG_PATH=../../local-config:$$PKG_CONFIG_PATH

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(dir $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(LOCAL_PKGCONFIG) $(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)


$(extension_jar): $(foreach f, $(jar_files), $(srcdir)/chrome/$f)
	pushd $(srcdir)/chrome; zip -q9 $@ $(jar_files); popd

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(extension_jar)
	make pre-install-local-hook prefix=$(prefix)
	mkdir -p $(INSTALL_DIR)
	cp $(ASSEMBLY) $(ASSEMBLY_MDB) $(extension_jar) $(INSTALL_DIR)
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB) $(extension_jar)
	make pre-uninstall-local-hook prefix=$(prefix)
	rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY))
	test -z '$(ASSEMBLY_MDB)' || rm -f $(INSTALL_DIR)/$(notdir $(ASSEMBLY_MDB))
	make post-uninstall-local-hook prefix=$(prefix)
