
ADDIN_BUILD = $(top_builddir)/build/AddIns/NUnit
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.NUnit.dll

DLLS = /r:nunit.framework.dll \
	/r:nunit.core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Base.dll \
	/r:$(top_builddir)/build/bin/MonoDevelop.Gui.Widgets.dll \
	$(GTK_SHARP_LIBS) \
	$(GLADE_SHARP_LIBS)

FILES = \
AssemblyInfo.cs \
Commands/NUnitCommands.cs \
Gui/CircleImage.cs \
Gui/TestPad.cs \
Gui/TestResultsPad.cs \
Gui/TestNodeBuilder.cs \
Gui/TestChart.cs \
Gui/NUnitOptionsPanel.cs \
Gui/UnitTestOptionsDialog.cs \
Gui/NUnitAssemblyGroupNodeBuilder.cs \
Gui/NUnitAssemblyGroupConfigurationNodeBuilder.cs \
Gui/TestAssemblyNodeBuilder.cs \
Project/NUnitAssemblyGroupFileFormat.cs \
Project/NUnitAssemblyGroupProject.cs \
Project/TestAssembly.cs \
Project/TestAssemblyCollection.cs \
Services/CombineTestGroup.cs \
Services/ITestProvider.cs \
Services/NUnitService.cs \
Services/SystemTestProvider.cs \
Services/ITestProgressMonitor.cs \
Services/TestContext.cs \
Services/UnitTestCollection.cs \
Services/UnitTest.cs \
Services/UnitTestGroup.cs \
Services/UnitTestResult.cs \
Services/UnitTestStatus.cs \
Services/ExternalTestRunner.cs \
Services/NUnitAssemblyTestSuite.cs \
Services/NUnitProjectTestSuite.cs \
Services/NUnitTestCase.cs \
Services/NUnitTestSuite.cs \
Services/GeneralTestOptions.cs \
Services/NUnitOptions.cs \
Services/IResultsStore.cs \
Services/UnitTestResultsStore.cs \
Services/XmlResultsStore.cs

RES = \
nunit.glade \
templates/NUnitAssemblyGroup.xpt.xml \
Gui/NUnit.Running.png \
Gui/NUnit.Failed.png \
Gui/NUnit.SuccessAndFailed.png \
Gui/NUnit.None.png \
Gui/NUnit.NotRun.png \
Gui/NUnit.Loading.png \
Gui/NUnit.Success.png

ADDIN = MonoDevelopNUnit.addin.xml

if ENABLE_NUNIT
all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)
else
all:
endif

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ASSEMBLY): $(FILES) $(RES)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(DLLS) $(RES:%=/resource:%) $(build_sources) -out:$@ -target:library

if ENABLE_NUNIT
assemblydir = $(MD_ADDIN_DIR)/NUnit
assembly_DATA = $(ASSEMBLY) $(ADDIN)

endif

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include
