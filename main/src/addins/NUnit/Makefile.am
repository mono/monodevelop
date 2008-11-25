
ADDIN_BUILD = $(top_builddir)/build/AddIns/NUnit
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.NUnit.dll

DEPS =  \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll

REFS =  \
	$(GLADE_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:$(top_srcdir)/src/addins/NUnit/lib/nunit.core.dll \
	-r:$(top_srcdir)/src/addins/NUnit/lib/nunit.framework.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	Commands/NUnitCommands.cs \
	gtk-gui/generated.cs \
	Gui/CircleImage.cs \
	Gui/NUnitAssemblyGroupConfigurationNodeBuilder.cs \
	Gui/NUnitAssemblyGroupNodeBuilder.cs \
	Gui/NUnitOptionsPanel.cs \
	Gui/TestAssemblyNodeBuilder.cs \
	Gui/TestChart.cs \
	Gui/TestNodeBuilder.cs \
	Gui/TestPad.cs \
	Gui/TestResultsPad.cs \
	Gui/UnitTestOptionsDialog.cs \
	Project/NUnitAssemblyGroupFileFormat.cs \
	Project/NUnitAssemblyGroupProject.cs \
	Project/TestAssembly.cs \
	Project/TestAssemblyCollection.cs \
	Services/ExternalTestRunner.cs \
	Services/GeneralTestOptions.cs \
	Services/IResultsStore.cs \
	Services/ITestProgressMonitor.cs \
	Services/ITestProvider.cs \
	Services/NUnitAssemblyTestSuite.cs \
	Services/NUnitOptions.cs \
	Services/NUnitProjectTestSuite.cs \
	Services/NUnitService.cs \
	Services/NUnitTestCase.cs \
	Services/NUnitTestSuite.cs \
	Services/SolutionFolderTestGroup.cs \
	Services/SystemTestProvider.cs \
	Services/TestContext.cs \
	Services/UnitTest.cs \
	Services/UnitTestCollection.cs \
	Services/UnitTestGroup.cs \
	Services/UnitTestResult.cs \
	Services/UnitTestResultsStore.cs \
	Services/UnitTestStatus.cs \
	Services/WorkspaceTestGroup.cs \
	Services/XmlResultsStore.cs

RES =  \
	gtk-gui/gui.stetic \
	Gui/NUnit.Failed.png \
	Gui/NUnit.Loading.png \
	Gui/NUnit.None.png \
	Gui/NUnit.NotRun.png \
	Gui/nunit.png \
	Gui/NUnit.Running.png \
	Gui/NUnit.Success.png \
	Gui/NUnit.SuccessAndFailed.png \
	Gui/nunit-overlay-32.png \
	md1format.xml \
	MonoDevelopNUnit.addin.xml \
	nunit.glade \
	templates/NUnitAssemblyGroup.xpt.xml \
	templates/NUnitProject.xpt.xml \
	templates/NUnitTestClass.xft.xml


NUNIT_FILES = \
nunit.core.dll \
nunit.framework.dll

SRC_NUNIT_FILES = $(addprefix $(srcdir)/lib/, $(NUNIT_FILES))
BUILD_NUNIT_FILES = $(addprefix $(ADDIN_BUILD)/, $(NUNIT_FILES))

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(BUILD_NUNIT_FILES)

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/NUnit
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(SRC_NUNIT_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(BUILD_NUNIT_FILES)
EXTRA_DIST = $(FILES) $(RES) $(SRC_NUNIT_FILES)

include $(top_srcdir)/Makefile.include
