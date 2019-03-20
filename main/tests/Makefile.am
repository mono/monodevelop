include $(top_srcdir)/xbuild.include

MONO=mono$(SGEN_SUFFIX)
RUN_TEST=$(MDTOOL_RUN) run-md-tests
TEST_DIR=$(top_builddir)/build/tests
TEST_SRC_DIR=$(top_srcdir)/tests
EXTERNAL=$(top_builddir)/external
MACOS_10_10=$(shell (test `sw_vers -productVersion | cut -f1 -d.` = 10 && test `sw_vers -productVersion | cut -f2 -d.` -ge 10) && echo yes)
TEST_DEPLOY_DIR=$(top_builddir)/build/test-deploy

TEST_PROJECTS_MAC = \
	MacPlatform.Tests/MacPlatform.Tests.csproj

TEST_PROJECTS_COMMON = \
	IdeUnitTests/IdeUnitTests.csproj \
	UnitTests/UnitTests.csproj \
	$(top_srcdir)/src/addins/MonoDevelop.Debugger/MonoDevelop.Debugger.Tests/MonoDevelop.Debugger.Tests.csproj

TEST_ASSEMBLIES_MAC = \
	$(TEST_DIR)/MonoDevelop.VersionControl.Subversion.Tests.dll \
	$(TEST_DIR)/MacPlatform.Tests.dll

TEST_ASSEMBLIES_WINDOWS = \
	$(TEST_DIR)/VersionControl.Subversion.Win32.Tests.dll

TEST_ASSEMBLIES_COMMON = \
    $(TEST_DIR)/MonoDevelop.Core.Tests.dll \
	$(TEST_DIR)/MonoDevelop.Ide.Tests.dll \
	$(TEST_DIR)/MonoDevelop.VersionControl.Git.Tests.dll \
	$(TEST_DIR)/MonoDevelop.Debugger.Tests.dll \
	$(TEST_DIR)/Mono.TextTemplating.Tests.dll \
	$(TEST_DIR)/MonoDevelop.PackageManagement.Tests.dll \
	$(TEST_DIR)/MonoDevelop.Packaging.Tests.dll \
	$(TEST_DIR)/MonoDevelop.TextEditor.Tests.dll \
	$(TEST_DIR)/MonoDevelop.Xml.Tests.dll \
	$(TEST_DIR)/MonoDevelop.AspNet.Tests.dll \
	$(TEST_DIR)/MonoDevelop.CSharpBinding.Tests.dll \
	$(TEST_DIR)/MonoDevelop.DotNetCore.Tests.dll \
	$(TEST_DIR)/MonoDevelop.AspNetCore.Tests.dll \
	$(TEST_DIR)/MonoDevelop.UnitTesting.Tests.dll \
	$(TEST_DIR)/MonoDevelop.Refactoring.Tests.dll \
	$(TEST_DIR)/MonoDevelop.Ide.PerfTests.dll \
	$(TEST_DIR)/PerfTool.exe

if RELEASE_BUILDS
TEST_ASSEMBLIES_COMMON += $(EXTERNAL)/nrefactory/bin/Release/ICSharpCode.NRefactory.Tests.dll
TEST_ASSEMBLIES_COMMON += $(EXTERNAL)/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Release/MonoDevelop.FSharp.Tests.dll
else
TEST_ASSEMBLIES_COMMON += $(EXTERNAL)/nrefactory/bin/Debug/ICSharpCode.NRefactory.Tests.dll
TEST_ASSEMBLIES_COMMON += $(EXTERNAL)/fsharpbinding/MonoDevelop.FSharp.Tests/bin/Debug/MonoDevelop.FSharp.Tests.dll
endif

TEST_ASSEMBLIES = $(TEST_ASSEMBLIES_COMMON)

if ENABLE_MACPLATFORM
TEST_ASSEMBLIES += $(TEST_ASSEMBLIES_MAC)
endif

if ENABLE_WINDOWSPLATFORM
TEST_ASSEMBLIES += $(TEST_ASSEMBLIES_WINDOWS)
endif

test:
	@if test -n "$(assembly)"; then \
		for asm in $(TEST_ASSEMBLIES); do \
			if test `basename $$asm` = $(assembly); then \
				($(RUN_TEST) -xml=TestResult_`basename $$asm`.xml -labels $$asm) || exit $?; \
			fi; \
		done; \
	fi

	@if ! test -n "$(assembly)"; then \
		fail=0; \
		for asm in $(TEST_ASSEMBLIES); do \
			echo Testing `basename $$asm`...; \
			($(RUN_TEST) -xml=TestResult_`basename $$asm`.xml -labels $$asm); \
			if [ $$? != 0 ]; then \
				fail=1; \
			fi; \
		done; \
		if [ $${fail} = 1 ]; then \
			exit 1; \
		fi; \
	fi

uitest:
	rm -rf $(TEST_DIR)/TestResults
	@if test -n "$(assembly)"; then \
		for asm in $(wildcard $(TEST_DIR)/MonoDevelop.UITests*.dll); do \
			if test `basename $$asm` = $(assembly); then \
				if test -n "$(categories)"; then \
					($(RUN_TEST) -xml=TestResult_`basename $$asm`.xml -labels -include=$(categories) $$asm) || exit $?; \
				fi; \
				if test -n "$(tests)"; then \
					($(RUN_TEST) -xml=TestResult_`basename $$asm`.xml -labels -run=$(tests) $$asm) || exit $?; \
				fi; \
				if ! test -n "$(categories)" && ! test -n "$(tests)"; then \
					($(RUN_TEST) -xml=TestResult_`basename $$asm`.xml -labels $$asm) || exit $?; \
				fi; \
			fi; \
		done; \
	fi

	@if ! test -n "$(assembly)"; then \
		echo "You must pass 'assembly' argument"; \
		exit 1; \
	fi

profile:
	@if test -n "$(assembly)"; then \
		for asm in $(TEST_ASSEMBLIES); do \
			if test `basename $$asm` = $(assembly); then \
				echo Generating profiling for `basename $$asm`...; \
				($(MD_LAUNCH_SETUP) exec -a "mdtool" $(RUNTIME) --debug --profile=log:heapshot,alloc,calls,calldepth=100,zip,sample=cycles/1000,counters,output=ProfileResult_`basename $$asm`.mlpd "$(MD_BIN_PATH)/mdtool.exe" run-md-tests $$asm); \
				mprof-report ProfileResult_`basename $$asm`.mlpd > ProfileAnalysis_`basename $$asm`.txt ; \
			fi; \
		done; \
	fi

coverage:
	if ! test -n "$(assembly)"; then \
		for asm in $(TEST_ASSEMBLIES); do \
			echo Generating coverage for `basename $$asm`...; \
			($(MD_LAUNCH_SETUP) exec -a "mdtool" $(RUNTIME) --debug --profile=log:coverage,onlycoverage,output=CoverageResult_`basename $$asm`.mlpd,covfilter-file=`basename $$asm`.filter "$(MD_BIN_PATH)/mdtool.exe" run-md-tests $$asm); \
			mprof-report --coverage-out=CoverageResult_`basename $$asm`.xml CoverageResult_`basename $$asm`.mlpd > /dev/null; \
		done; \
	fi

deploy-tests:
	mkdir -p "$(TEST_DEPLOY_DIR)/tests/test-projects"
	cp -r $(TEST_DIR)/ "$(TEST_DEPLOY_DIR)/tests"; \
	cp -r $(top_srcdir)/tests/test-projects "$(TEST_DEPLOY_DIR)/tests"; \
	for asm in $(TEST_ASSEMBLIES); do \
		if ! [ -e $(TEST_DIR)/`basename $$asm` ]; then \
			cp -r `dirname $$asm`/* "$(TEST_DEPLOY_DIR)/tests"; \
		fi \
	done;

performance-tests:
	rm -rf $(TEST_DIR)/TestSolutions/ExampleFormsSolution
	cd $(TEST_DIR)/TestSolutions; unzip ExampleFormsSolution.zip
	$(RUN_TEST) -xml=TestResult_PerfTest.xml -labels $(TEST_DIR)/MonoDevelop.Ide.PerfTests.dll || exit $?;
	mono $(TEST_DIR)/PerfTool.exe generate-results $(TEST_DIR)/Baseline_MonoDevelop.Ide.PerfTests.xml $(TEST_SRC_DIR)/TestResult_PerfTest.xml $(TEST_DIR)/PerformanceTestReport.xml

include $(top_srcdir)/Makefile.include