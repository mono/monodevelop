dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([mono-addins], [0.3])
AM_INIT_AUTOMAKE([foreign])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

AC_PATH_PROG(MCS, mcs, no)
if test "x$MCS" = "xno"; then
        AC_MSG_ERROR([mcs Not found])
fi

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno" ; then
	AC_MSG_ERROR([No gacutil tool found])
fi

GACUTIL_FLAGS='/package mono-addins /root $(DESTDIR)$(prefix)/lib'

AC_SUBST(GACUTIL)
AC_SUBST(GACUTIL_FLAGS)

AC_ARG_ENABLE(gui,
	AC_HELP_STRING([--enable-gui],
		[enable support for GTK dependent libraries [default=yes]]),
		enable_gui=${enableval}, enable_gui=yes)

AM_CONDITIONAL(ENABLE_GUI, test x$enable_gui = xyes)

AC_ARG_ENABLE(tests,
	AC_HELP_STRING([--enable-tests],
		[build unit tests [default=yes]]),
		enable_tests=${enableval}, enable_tests=yes)

AM_CONDITIONAL(ENABLE_TESTS, test x$enable_tests = xyes)

if test "x$enable_gui" = "xyes"; then
	PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
fi

if test "x$enable_tests" = "xyes"; then
	PKG_CHECK_MODULES([MONO_NUNIT], [mono-nunit])
fi


AC_SUBST(MONO_NUNIT_LIBS)

MONO_ADDINS_DEFAULT_LIB='Mono.Addins.dll'
MONO_ADDINS_SETUP_DEFAULT_LIB='Mono.Addins.Setup.dll'
MONO_ADDINS_GUI_DEFAULT_LIB='Mono.Addins.Gui.dll'
AC_SUBST(MONO_ADDINS_DEFAULT_LIB)
AC_SUBST(MONO_ADDINS_GUI_DEFAULT_LIB)
AC_SUBST(MONO_ADDINS_SETUP_DEFAULT_LIB)

AC_CONFIG_FILES([
./Mono.Addins/mono-addins.pc
./Mono.Addins.Gui/mono-addins-gui.pc
./Mono.Addins.Setup/mono-addins-setup.pc
./Makefile
./Mono.Addins.Gui/Makefile
./Mono.Addins.Setup/Makefile
./Mono.Addins/Makefile
./mautil/Makefile
./mautil/mautil
./Test/HelloWorldExtension/Makefile
./Test/FileContentExtension/Makefile
./Test/UnitTests/Makefile
./Test/CommandExtension/Makefile
./Test/SystemInfoExtension/Makefile
./Test/FileExtender/Makefile
./Test/Makefile
./Samples/TextEditor.Xml/Makefile
./Samples/TextEditor/Makefile
./Samples/TextEditor.CompilerService/Makefile
./Samples/WriterServiceHost/Makefile
./Samples/TextEditor.CompilerService.CSharp/Makefile
./Samples/WriterService.SampleExtender/Makefile
./Samples/Makefile
./Samples/TextEditorLib/Makefile
./Samples/WriterService/Makefile

])

AC_OUTPUT

echo ""
echo "Configuration summary"
echo ""
echo "   * Installation prefix = $prefix"
echo "   * gui support: $enable_gui"
echo "   * unit tests: $enable_tests"
echo ""


