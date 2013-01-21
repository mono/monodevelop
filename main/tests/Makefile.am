include $(top_srcdir)/xbuild.include

SUBDIRS=UnitTests MacPlatform.Tests UserInterfaceTests

MONO=mono
HARNESS=$(top_srcdir)/external/mdtestharness/nunit-console-x86.exe -domain=multiple
RUN_TEST=$(MD_LAUNCH_SETUP) $(MONO) $(HARNESS)
TEST_DIR=$(top_builddir)/build/tests
EXTERNAL=$(top_builddir)/external

TEST_PROJECTS_MAC = \
	MacPlatform.Tests/MacPlatform.Tests.csproj

TEST_PROJECTS_COMMON = \
	UnitTests/UnitTests.csproj \
	$(top_srcdir)/src/addins/MonoDevelop.Debugger/MonoDevelop.Debugger.Tests/MonoDevelop.Debugger.Tests.csproj \
	$(EXTERNAL)/ngit/NGit.Test/NGit.Test.csproj

TEST_ASSEMBLIES_MAC = \
	$(TEST_DIR)/MacPlatform.Tests.dll

TEST_ASSEMBLIES_COMMON = \
	$(TEST_DIR)/UnitTests.dll \
	$(TEST_DIR)/MonoDevelop.Debugger.Tests.dll \
	$(EXTERNAL)/ngit/bin/NGit.Test.dll


ALL_CSPROJ = $(TEST_PROJECTS_COMMON)
TEST_ASSEMBLIES = $(TEST_ASSEMBLIES_COMMON)

if ENABLE_MACPLATFORM
ALL_CSPROJ += $(TEST_PROJECTS_MAC)
TEST_ASSEMBLIES += $(TEST_ASSEMBLIES_MAC)
endif

test:
	@if test -n "$(assembly)"; then \
		for asm in $(TEST_ASSEMBLIES); do \
			if test `basename $$asm` = $(assembly); then \
				$(RUN_TEST) $$asm || exit $?; \
			fi; \
		done; \
	fi

	@if ! test -n "$(assembly)"; then \
		for asm in $(TEST_ASSEMBLIES); do \
			$(RUN_TEST) $$asm || exit $?; \
		done; \
	fi

EXTRA_DIST = \
	test-projects/resources-tester/ResourcesTester/Subfolder/FormFile2.cs \
	test-projects/resources-tester/ResourcesTester/Subfolder/FormFile2.Designer.cs \
	test-projects/resources-tester/ResourcesTester/Subfolder/Normal2.Designer.cs \
	test-projects/resources-tester/ResourcesTester/Subfolder/Normal2.resx \
	test-projects/resources-tester/ResourcesTester/Subfolder/Cultured2.ca.resx \
	test-projects/resources-tester/ResourcesTester/Subfolder/BitmapCultured2.ca.bmp \
	test-projects/resources-tester/ResourcesTester/Subfolder/Bitmap2.bmp \
	test-projects/resources-tester/ResourcesTester/Subfolder/FormFile2.ca.resx \
	test-projects/resources-tester/ResourcesTester/Subfolder/FormFile2.resx \
	test-projects/resources-tester/ResourcesTester/Bitmap1.bmp \
	test-projects/resources-tester/ResourcesTester/Normal.resx \
	test-projects/resources-tester/ResourcesTester/Normal.Designer.cs \
	test-projects/resources-tester/ResourcesTester/FormFile.Designer.cs \
	test-projects/resources-tester/ResourcesTester/Cultured.ca.resx \
	test-projects/resources-tester/ResourcesTester/Properties/Settings.Designer.cs \
	test-projects/resources-tester/ResourcesTester/Properties/Settings.settings \
	test-projects/resources-tester/ResourcesTester/Properties/Resources.resx \
	test-projects/resources-tester/ResourcesTester/Properties/AssemblyInfo.cs \
	test-projects/resources-tester/ResourcesTester/Properties/Resources.Designer.cs \
	test-projects/resources-tester/ResourcesTester/ResourcesTester.csproj \
	test-projects/resources-tester/ResourcesTester/FormFile.ca.resx \
	test-projects/resources-tester/ResourcesTester/BitmapCultured.ca.bmp \
	test-projects/resources-tester/ResourcesTester/FormFile.cs \
	test-projects/resources-tester/ResourcesTester/FormFile.resx \
	test-projects/resources-tester/ResourcesTester/Program.cs \
	test-projects/resources-tester/ResourcesTester.sln \
	test-projects/nunit-assembly-group/nunit-assembly-group.mds \
	test-projects/nunit-assembly-group/nunit-assembly-group.md-nunit \
	test-projects/test-build-configs/Lib3/MyClass.cs \
	test-projects/test-build-configs/Lib3/Lib3.mdp \
	test-projects/test-build-configs/Lib3/Properties/AssemblyInfo.cs \
	test-projects/test-build-configs/Lib4/Lib4.mdp \
	test-projects/test-build-configs/Lib4/MyClass.cs \
	test-projects/test-build-configs/Lib4/Properties/AssemblyInfo.cs \
	test-projects/test-build-configs/Lib2/MyClass.cs \
	test-projects/test-build-configs/Lib2/Lib2.mdp \
	test-projects/test-build-configs/Lib2/Properties/AssemblyInfo.cs \
	test-projects/test-build-configs/test-build-configs.mds \
	test-projects/test-build-configs/Lib1/Lib1.mdp \
	test-projects/test-build-configs/Lib1/MyClass.cs \
	test-projects/test-build-configs/Lib1/Properties/AssemblyInfo.cs \
	test-projects/generated-console-project/TestProject.csproj \
	test-projects/generated-console-project/TestProject.mdp \
	test-projects/generated-console-project/TestSolution.sln \
	test-projects/generated-console-project/TestSolution.mds \
	test-projects/console-with-libs-mdp/library1/library1.mdp \
	test-projects/console-with-libs-mdp/library1/MyClass.cs \
	test-projects/console-with-libs-mdp/library1/Properties/AssemblyInfo.cs \
	test-projects/console-with-libs-mdp/console-with-libs-mdp \
	test-projects/console-with-libs-mdp/console-with-libs-mdp/Program.cs \
	test-projects/console-with-libs-mdp/console-with-libs-mdp/console-with-libs-mdp.mdp \
	test-projects/console-with-libs-mdp/console-with-libs-mdp/Properties/AssemblyInfo.cs \
	test-projects/console-with-libs-mdp/console-with-libs-mdp.mds \
	test-projects/console-with-libs-mdp/library2/MyClass.cs \
	test-projects/console-with-libs-mdp/library2/Properties/AssemblyInfo.cs \
	test-projects/console-with-libs-mdp/library2/library2.mdp \
	test-projects/nested-solutions-mdp/nested-solutions-mdp.mds \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/nested-solution3.mds \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/library3/library3.mdp \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/library3/MyClass.cs \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/library3/Properties/AssemblyInfo.cs \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/library4/library4.mdp \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/library4/MyClass.cs \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution3/library4/Properties/AssemblyInfo.cs \
	test-projects/nested-solutions-mdp/nested-solution2/console-project2/Program.cs \
	test-projects/nested-solutions-mdp/nested-solution2/console-project2/Properties/AssemblyInfo.cs \
	test-projects/nested-solutions-mdp/nested-solution2/console-project2/console-project2.mdp \
	test-projects/nested-solutions-mdp/nested-solution2/nested-solution2.mds \
	test-projects/nested-solutions-mdp/console-project/Program.cs \
	test-projects/nested-solutions-mdp/console-project/console-project.mdp \
	test-projects/nested-solutions-mdp/console-project/Properties/AssemblyInfo.cs \
	test-projects/nested-solutions-mdp/nested-solution1/library1/library1.mdp \
	test-projects/nested-solutions-mdp/nested-solution1/library1/MyClass.cs \
	test-projects/nested-solutions-mdp/nested-solution1/library1/Properties/AssemblyInfo.cs \
	test-projects/nested-solutions-mdp/nested-solution1/nested-solution1.mds \
	test-projects/nested-solutions-mdp/nested-solution1/library2/MyClass.cs \
	test-projects/nested-solutions-mdp/nested-solution1/library2/Properties/AssemblyInfo.cs \
	test-projects/nested-solutions-mdp/nested-solution1/library2/library2.mdp \
	test-projects/console-project-with-makefile/Makefile.include \
	test-projects/console-project-with-makefile/ConsoleProject/ConsoleProject.csproj \
	test-projects/console-project-with-makefile/ConsoleProject/Makefile \
	test-projects/console-project-with-makefile/ConsoleProject/consoleproject.in \
	test-projects/console-project-with-makefile/ConsoleProject/Properties/AssemblyInfo.cs \
	test-projects/console-project-with-makefile/ConsoleProject/Program.cs \
	test-projects/console-project-with-makefile/Makefile \
	test-projects/console-project-with-makefile/rules.make \
	test-projects/console-project-with-makefile/configure \
	test-projects/console-project-with-makefile/ConsoleProject.sln \
	test-projects/csharp-console-mdp/csharp-console-mdp/csharp-console-mdp.mdp \
	test-projects/csharp-console-mdp/csharp-console-mdp/Program.cs \
	test-projects/csharp-console-mdp/csharp-console-mdp/Properties/AssemblyInfo.cs \
	test-projects/csharp-console-mdp/csharp-console-mdp.mds \
	test-projects/ChangeLog \
	test-projects/console-project/ConsoleProject/ConsoleProject.csproj \
	test-projects/console-project/ConsoleProject/Properties/AssemblyInfo.cs \
	test-projects/console-project/ConsoleProject/Program.cs \
	test-projects/console-project/ConsoleProject.sln

