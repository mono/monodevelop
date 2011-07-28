
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.MacDev
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.MacDev.dll

DEPS =  \
	../../../build/AddIns/MacPlatform.dll \
	../../../build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	../../../build/bin/Mono.TextEditor.dll \
	../../../build/bin/MonoDevelop.Core.dll \
	../../../build/bin/MonoDevelop.Ide.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:../../../../extras/MonoDevelop.MonoMac/MonoDevelop.MonoMac/MonoMac.dll \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml \
	-r:System.Xml.Linq

FILES =  \
	AppleSdkSettings.cs \
	AppleSdkSettingsPanel.cs \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.MacDev.PlistEditor.CustomPropertiesWidget.cs \
	MacBuildUtilities.cs \
	MonoDevelop.MacDev.InterfaceBuilder/Collections.cs \
	MonoDevelop.MacDev.InterfaceBuilder/IBConnectionRecord.cs \
	MonoDevelop.MacDev.InterfaceBuilder/IBDocument.cs \
	MonoDevelop.MacDev.InterfaceBuilder/IBObject.cs \
	MonoDevelop.MacDev.InterfaceBuilder/IBObjectRecord.cs \
	MonoDevelop.MacDev.InterfaceBuilder/IBReference.cs \
	MonoDevelop.MacDev.InterfaceBuilder/UnknownIBObject.cs \
	MonoDevelop.MacDev.Plist/PlistArray.cs \
	MonoDevelop.MacDev.Plist/PlistBoolean.cs \
	MonoDevelop.MacDev.Plist/PlistData.cs \
	MonoDevelop.MacDev.Plist/PlistDate.cs \
	MonoDevelop.MacDev.Plist/PlistDictionary.cs \
	MonoDevelop.MacDev.Plist/PlistDocument.cs \
	MonoDevelop.MacDev.Plist/PlistInteger.cs \
	MonoDevelop.MacDev.Plist/PlistObject.cs \
	MonoDevelop.MacDev.Plist/PlistObjectBase.cs \
	MonoDevelop.MacDev.Plist/PlistReal.cs \
	MonoDevelop.MacDev.Plist/PlistString.cs \
	MonoDevelop.MacDev.PlistEditor/CloseableExpander.cs \
	MonoDevelop.MacDev.PlistEditor/CustomPropertiesWidget.cs \
	MonoDevelop.MacDev.PlistEditor/ExpanderList.cs \
	MonoDevelop.MacDev.PlistEditor/ImageChooser.cs \
	MonoDevelop.MacDev.PlistEditor/IPlistEditingHandler.cs \
	MonoDevelop.MacDev.PlistEditor/MacExpander.cs \
	MonoDevelop.MacDev.PlistEditor/PListEditorDisplayBinding.cs \
	MonoDevelop.MacDev.PlistEditor/PListEditorViewContent.cs \
	MonoDevelop.MacDev.PlistEditor/PListEditorWidget.cs \
	MonoDevelop.MacDev.PlistEditor/PListObject.cs \
	MonoDevelop.MacDev.PlistEditor/PListScheme.cs \
	NativeReferences/NativeReference.cs \
	NativeReferences/NativeReferenceCommandHandler.cs \
	NativeReferences/NativeReferenceFolderNodeBuilder.cs \
	NativeReferences/NativeReferenceNodeBuilder.cs \
	NativeReferences/NativeReferencePropertyProvider.cs \
	NativeReferences/ProjectNodeBuilder.cs \
	ObjCIntegration/CodebehindTemplateBase.cs \
	ObjCIntegration/CSharpCodeCodebehind.cs \
	ObjCIntegration/IBAction.cs \
	ObjCIntegration/IBActionParameter.cs \
	ObjCIntegration/IBOutlet.cs \
	ObjCIntegration/NSObjectInfoService.cs \
	ObjCIntegration/NSObjectProjectInfo.cs \
	ObjCIntegration/NSObjectTypeInfo.cs \
	SigningIdentityCombo.cs \
	XcodeIntegration/PBXBuildFile.cs \
	XcodeIntegration/PBXFileReference.cs \
	XcodeIntegration/PBXFrameworksBuildPhase.cs \
	XcodeIntegration/PBXGroup.cs \
	XcodeIntegration/PBXNativeTarget.cs \
	XcodeIntegration/PBXProject.cs \
	XcodeIntegration/PBXResourcesBuildPhase.cs \
	XcodeIntegration/PBXSourcesBuildPhase.cs \
	XcodeIntegration/PBXVariantGroup.cs \
	XcodeIntegration/XCBuildConfiguration.cs \
	XcodeIntegration/XCConfigurationList.cs \
	XcodeIntegration/XcodeObject.cs \
	XcodeIntegration/XcodeProject.cs \
	XcodeIntegration/XcodeType.cs \
	XcodeInterfaceBuilderDisplayBinding.cs \
	XcodeSyncing/XcodeMonitor.cs \
	XcodeSyncing/XcodeProjectTracker.cs \
	XcodeSyncing/XcodeSyncBackContext.cs \
	XcodeSyncing/XcodeSyncContext.cs \
	XcodeSyncing/XcodeSyncedContent.cs \
	XcodeSyncing/XcodeSyncedItem.cs \
	XcodeSyncing/XcodeSyncedResource.cs \
	XcodeSyncing/XcodeSyncedType.cs 

RES =  \
	gtk-gui/gui.stetic \
	MonoDevelop.MacDev.addin.xml

ICON_FILES =  \
	icons/application-x-mono-develop-xib-48.png \
	icons/application-x-mono-develop-xib-32.png \
	icons/application-x-mono-develop-xib-24.png \
	icons/application-x-mono-develop-xib-22.png \
	icons/application-x-mono-develop-xib-16.png \
	icons/xib-template-application.png \
	icons/xib-template-empty.png \
	icons/xib-template-view.png \
	icons/xib-template-window.png

ICON_BUILD = $(addprefix $(ADDIN_BUILD)/icons/, $(notdir $(ICON_FILES)))

$(ICON_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/icons
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

if ENABLE_MACPLATFORM
all: $(ASSEMBLY) $(ASSEMBLY).mdb $(ICON_BUILD)
else
all:
endif

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -unsafe $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

if ENABLE_MACPLATFORM
assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.MacDev
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb
iconlibdir = $(assemblydir)/icons
iconlib_DATA = $(ICON_BUILD)
endif


CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(ICON_BUILD)
EXTRA_DIST = $(FILES) $(RES) $(ICON_FILES)

include $(top_srcdir)/Makefile.include
