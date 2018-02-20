AC_INIT([monodevelop], m4_esyscmd_s([grep ^Version ../version.config|cut -d '=' -f 2|tr -d '\n']), [monodevelop-list@lists.ximian.com])
AC_PREREQ(2.53)
AM_INIT_AUTOMAKE([1.10 tar-ustar])
AM_MAINTAINER_MODE

#capture aclocal flags for autoreconf
AC_SUBST(ACLOCAL_FLAGS)

ASSEMBLY_VERSION=4.0.0.0

# This is parsed in BuildVariables.cs. Keep the format consistent to avoid breaking
# the C# side of things. It should be one of the following two formats:
# 1) "VERSION_NUMBER"					"2.0"
# 2) "VERSION_NUMBER BUILD_TYPE BUILD_NUMBER"		"2.0 Alpha 1"
PACKAGE_VERSION_LABEL="m4_esyscmd_s([grep ^Label ../version.config|cut -d "=" -f 2|tr -d '\n'])"

COMPAT_ADDIN_VERSION=m4_esyscmd_s([grep ^CompatVersion ../version.config|cut -d "=" -f 2|tr -d '\n'])

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, mcs)
AC_PATH_PROG(FSHARPC, fsharpc)

if test "x$MONO" = "x" ; then
	AC_MSG_ERROR([Can't find "mono" in your PATH])
fi

if test "x$MCS" = "x" ; then
  AC_MSG_ERROR([Can't find "mcs" in your PATH])
fi

if test "x$FSHARPC" = "x" ; then
  AC_MSG_ERROR([Can't find "fsharpc" in your PATH])
fi

dnl Find pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

MONO_REQUIRED_VERSION=4.0

if ! $PKG_CONFIG --atleast-version=$MONO_REQUIRED_VERSION mono; then
	AC_MSG_ERROR([You need mono $MONO_REQUIRED_VERSION or newer])
fi

###
# Disable the environment capturing as it breaks on El Capitan and we end up storing empty env vars
# which means we break things like `DYLD_FALLBACK_LIBRARY_PATH` because that env has a default meaning
# when it is *not set*, but this code causes it to be set to an empty value so we can't load libc.
###
##ensure we have the same env as when configured
#AC_SUBST(PATH)
#AC_SUBST(PKG_CONFIG_PATH)
#AC_SUBST(LD_LIBRARY_PATH)
#AC_SUBST(DYLD_FALLBACK_LIBRARY_PATH)
#AC_SUBST(MONO_GAC_PREFIX)

default_gnomeplatform=no
default_windowsplatform=no
default_macplatform=no
default_macarch=i386

case `uname` in
    Darwin) default_macplatform=yes ;;
    CYGWIN*) default_windowsplatform=yes ;;
    *) default_gnomeplatform=yes ;;
esac

dnl Add MD's mimetypes to mime and desktop databases
AC_ARG_ENABLE(update-mimedb,
   AC_HELP_STRING([--disable-update-mimedb],
                   [disable the update-mime-database after install [default=no]]),,
    enable_update_mimedb=$default_gnomeplatform)
 AM_CONDITIONAL(ENABLE_UPDATE_MIMEDB, test x$enable_update_mimedb = xyes)
if test "x$enable_update_mimedb" = "xyes"; then
  AC_PATH_PROG(UPDATE_MIME_DB, update-mime-database, no)
  if test "x$UPDATE_MIME_DB" = "xno"; then
    AC_MSG_ERROR([You need to install update-mime-database])
  fi
fi
AC_SUBST(UPDATE_MIME_DB)

AC_ARG_ENABLE(update-desktopdb,
   AC_HELP_STRING([--disable-update-desktopdb],
                  [disable the update-desktop-database after install [default=no]]),,
                  enable_update_desktopdb=default_gnomeplatform)
   AM_CONDITIONAL(ENABLE_UPDATE_DESKTOPDB, test x$enable_update_desktopdb = xyes)

if test "x$enable_update_desktopdb" = "xyes"; then
  AC_PATH_PROG(UPDATE_DESKTOP_DB, update-desktop-database, no)
  if test "x$UPDATE_DESKTOP_DB" = "xno"; then
    AC_MSG_ERROR([You need to install update-desktop-database])
  fi
fi
AC_SUBST(UPDATE_DESKTOP_DB)

dnl Find msgfmt for translations
# Find msgfmt and msgmerge for translations
AC_PATH_PROG(MSGFMT, msgfmt, no)
if test "x$MSGFMT" = "xno"; then
        AC_MSG_ERROR([You need to install msgfmt from intltool])
fi
AC_PATH_PROG(MSGMERGE, msgmerge, no)
if test "x$MSGMERGE" = "xno"; then
        AC_MSG_ERROR([You need to install msgmerge from intltool])
fi

PKG_CHECK_MODULES(UNMANAGED_DEPENDENCIES_MONO,mono >= $MONO_REQUIRED_VERSION, has_mono=true, has_mono=false)

if test "x$has_mono" = "xfalse"; then
      AC_MSG_ERROR([Please install mono version $MONO_REQUIRED_VERSION or later to install MonoDevelop.
Please see http://www.mono-project.org/ to download latest mono sources or packages])
fi

if test "x$has_mono" = "xtrue"; then
    AC_PATH_PROG(RUNTIME, mono, no)
    AC_PATH_PROG(CSC, dmcs, no)
    if test `uname -s` = "Darwin"; then
	LIB_PREFIX=
	LIB_SUFFIX=.dylib
    else
	LIB_PREFIX=.so
	LIB_SUFFIX=
    fi
fi


dnl hard dependencies
MONOADDINS_REQUIRED_VERSION=0.6
GTKSHARP_REQUIRED_VERSION=2.12.8
MONODOC_REQUIRED_VERSION=1.0

PKG_CHECK_MODULES(GLIB_SHARP, glib-sharp-2.0 >= $GTKSHARP_REQUIRED_VERSION)
AC_SUBST(GLIB_SHARP_LIBS)
PKG_CHECK_MODULES(GTK_SHARP, gtk-sharp-2.0 >= $GTKSHARP_REQUIRED_VERSION)
AC_SUBST(GTK_SHARP_LIBS)
PKG_CHECK_MODULES(GLADE_SHARP, glade-sharp-2.0 >= $GTKSHARP_REQUIRED_VERSION)
AC_SUBST(GLADE_SHARP_LIBS)

PKG_CHECK_MODULES(MONODOC, monodoc >= $MONODOC_REQUIRED_VERSION)
AC_SUBST(MONODOC_LIBS)

gtksharp_prefix="`$PKG_CONFIG --variable=prefix gtk-sharp-2.0`"
AC_SUBST(gtksharp_prefix)

## nunit tests

AC_ARG_ENABLE(tests,
	AC_HELP_STRING([--enable-tests],
		[build unit tests [default=no]]),
		enable_tests=${enableval}, enable_tests=no)

AM_CONDITIONAL(ENABLE_TESTS, test x$enable_tests = xyes)

if test "x$enable_tests" = "xyes"; then
	PKG_CHECK_MODULES([MONO_NUNIT], [mono-nunit])
fi

AC_SUBST(MONO_NUNIT_LIBS)
AM_CONDITIONAL(ENABLE_TESTS, test x$enable_tests = xyes)

AC_ARG_ENABLE(monoextensions,
	AC_HELP_STRING([--enable-monoextensions],
		[enable extensions for Mono development [default=yes]]),
	enable_monoextensions=${enableval}, enable_monoextensions=yes)

AM_CONDITIONAL(ENABLE_MONOEXTENSIONS, test x$enable_monoextensions = xyes)

AC_ARG_ENABLE(subversion,
	AC_HELP_STRING([--enable-subversion],
		[enable Subversion support [default=yes]]),
		enable_subversion=${enableval}, enable_subversion=yes)

AM_CONDITIONAL(ENABLE_SUBVERSION, test x$enable_subversion = xyes)

AC_ARG_ENABLE(git,
	AC_HELP_STRING([--enable-git],
		[enable Git support [default=no]]),
		enable_git=${enableval}, enable_git=yes)

AM_CONDITIONAL(ENABLE_GIT, test x$enable_git = xyes)

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[build release mode [default=no]]),
		enable_release=${enableval}, enable_release=no)

AM_CONDITIONAL(RELEASE_BUILDS, [test x$enable_release = xyes])

platform_bindings=""

# Gnome platform addin
AC_ARG_ENABLE(gnomeplatform,
	AC_HELP_STRING([--enable-gnomeplatform],
		[enable Gnome platform support [default=no]]),
		enable_gnomeplatform=${enableval}, enable_gnomeplatform=$default_gnomeplatform)

if test x$enable_gnomeplatform = xyes; then
	platform_bindings="${platform_bindings}GNOME "
fi

AM_CONDITIONAL(ENABLE_GNOMEPLATFORM, [test x$enable_gnomeplatform = xyes])

# Mac platform addin
AC_ARG_ENABLE(macplatform,
	AC_HELP_STRING([--enable-macplatform],
		[enable Mac platform support [default=no]]),
		enable_macplatform=${enableval}, enable_macplatform=$default_macplatform)

if test x$enable_macplatform = xyes; then
	platform_bindings="${platform_bindings}Mac ${with_macarch}"
fi

AM_CONDITIONAL(ENABLE_MACPLATFORM, [test x$enable_macplatform = xyes])

# What architecture for Mac
AC_ARG_WITH(macarch,
	AC_HELP_STRING([--with-macarch],
		[which architecture for mac: i386, x86_64 [default=i386]]),
	with_macarch=${withval}, with_macarch=$default_macarch)

SGEN_SUFFIX=
MONOSTUB_ARCH=32
if test x$with_macarch = xx86_64; then
	SGEN_SUFFIX=64
	MONOSTUB_ARCH=64
fi
AC_SUBST(SGEN_SUFFIX)
AC_SUBST(MONOSTUB_ARCH)

MAC_ARCHITECTURE=${with_macarch}
AC_SUBST(MAC_ARCHITECTURE)

# Windows platform addin
AC_ARG_ENABLE(windowsplatform,
	AC_HELP_STRING([--enable-windowsplatform],
		[enable Windows platform support [default=no]]),
		enable_windowsplatform=${enableval}, enable_windowsplatform=$default_windowsplatform)

if test x$enable_windowsplatform = xyes; then
	platform_bindings="${platform_bindings}Windows "
fi

AM_CONDITIONAL(ENABLE_WINDOWSPLATFORM, [test x$enable_windowsplatform = xyes])

if test x$enable_gnomeplatform = xyes && test x$enable_git = xyes; then
	AC_PATH_PROG(CMAKE, cmake, no)
	if test "x$CMAKE" = "xno"; then
	        AC_MSG_ERROR([You need to install cmake for Git support to be built])
	fi

	PKG_CHECK_MODULES(LIBSSH2, libssh2)
	AC_SUBST(LIBSSH2_LIBS)
fi

dnl Intl
ALL_LINGUAS="cs da de es fr ja pt_BR tr pl it zh_TW zh_CN ru sl ca gl pt sv nl id hu nb ko uk"
AC_SUBST(ALL_LINGUAS)
GETTEXT_PACKAGE=monodevelop
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])

CSC_FLAGS="-debug -codepage:utf8"

AC_SUBST(LIB_SUFFIX)
AC_SUBST(LIB_PREFIX)
AC_SUBST(ASSEMBLY_VERSION)
AC_SUBST(PACKAGE_VERSION_LABEL)
AC_SUBST(COMPAT_ADDIN_VERSION)
AC_SUBST(SQLITE_XML)
AC_SUBST(CSC_FLAGS)


MD_DIR='$(prefix)/lib/monodevelop'
MD_ASSEMBLY_DIR="$MD_DIR/bin"
MD_ADDIN_DIR="$MD_DIR/AddIns"

AC_SUBST(MD_ASSEMBLY_DIR)
AC_SUBST(MD_ADDIN_DIR)
AC_SUBST(MD_DIR)

if test -z "$platform_bindings"; then
	AC_MSG_ERROR([You must enable one of the platform bindings])
fi

echo PACKAGE_VERSION=$PACKAGE_VERSION > monodevelop_version

AC_OUTPUT([
build/Makefile
build/MacOSX/Makefile
contrib/Makefile
contrib/ICSharpCode.Decompiler/Makefile
external/Makefile
msbuild/Makefile
src/Makefile
src/core/Makefile
src/core/Mono.Texteditor/Makefile
src/core/Mono.TextEditor.Platform/Makefile
src/core/Mono.TextEditor.Shared/Makefile
src/core/MonoDevelop.Core/Makefile
src/core/MonoDevelop.Projects.Formats.MSBuild/Makefile
src/core/MonoDevelop.Projects.Formats.MSBuild/MonoDevelop.Projects.MSBuild.Shared/Makefile
src/core/MonoDevelop.Ide/Makefile
src/core/MonoDevelop.Ide/MonoDevelop.Ide.Editor.Highlighting/Makefile
src/core/MonoDevelop.Startup/Makefile
src/core/MonoDevelop.TextEditor.Tests/Makefile
src/tools/Makefile
src/tools/mdhost/Makefile
src/tools/mdtool/Makefile
src/tools/mdmonitor/Makefile
src/addins/Makefile
src/addins/MonoDeveloperExtensions/Makefile
src/addins/MonoDeveloperExtensions/NUnit/Makefile
src/addins/MonoDevelop.UnitTesting/Makefile
src/addins/MonoDevelop.UnitTesting.NUnit/Makefile
src/addins/MonoDevelop.UnitTesting.NUnit/NUnitRunner/Makefile
src/addins/MonoDevelop.UnitTesting.NUnit/NUnit3Runner/Makefile
src/addins/PerformanceDiagnostics/Makefile
src/addins/PerformanceDiagnostics/PerformanceDiagnostics/Makefile
src/addins/PerformanceDiagnostics/UIThreadMonitorDaemon/Makefile
src/addins/VersionControl/MonoDevelop.VersionControl/Makefile
src/addins/VersionControl/MonoDevelop.VersionControl.Subversion/Makefile
src/addins/VersionControl/MonoDevelop.VersionControl.Subversion.Unix/Makefile
src/addins/VersionControl/MonoDevelop.VersionControl.Subversion.Tests/Makefile
src/addins/VersionControl/MonoDevelop.VersionControl.Git/Makefile
src/addins/VersionControl/MonoDevelop.VersionControl.Git.Tests/Makefile
src/addins/VersionControl/Subversion.Win32.Tests/Makefile
src/addins/VersionControl/Subversion.Win32/Makefile
src/addins/VersionControl/Makefile
src/addins/CSharpBinding/Makefile
src/addins/CSharpBinding/Autotools/Makefile
src/addins/CSharpBinding/AspNet/Makefile
src/addins/ILAsmBinding/Makefile
src/addins/VBNetBinding/Makefile
src/addins/ChangeLogAddIn/Makefile
src/addins/MonoDevelop.GtkCore/Makefile
src/addins/MonoDevelop.GtkCore/libstetic/Makefile
src/addins/MonoDevelop.GtkCore/libsteticui/Makefile
src/addins/TextTemplating/Makefile
src/addins/TextTemplating/MonoDevelop.TextTemplating/Makefile
src/addins/AspNet/Makefile
src/addins/MonoDevelop.Autotools/Makefile
src/addins/MonoDevelop.DesignerSupport/Makefile
src/addins/MonoDevelop.WebReferences/Makefile
src/addins/Deployment/Makefile
src/addins/Deployment/MonoDevelop.Deployment/Makefile
src/addins/Deployment/MonoDevelop.Deployment.Linux/Makefile
src/addins/MonoDevelop.Gettext/Makefile
src/addins/MonoDevelop.RegexToolkit/Makefile
src/addins/GnomePlatform/Makefile
src/addins/MacPlatform/Makefile
src/addins/WindowsPlatform/Makefile
src/addins/WindowsPlatform/WindowsAPICodePack/Makefile
src/addins/WindowsPlatform/WindowsAPICodePack/Core/Makefile
src/addins/WindowsPlatform/WindowsAPICodePack/Shell/Makefile
src/addins/WindowsPlatform/WindowsPlatform/Makefile
src/addins/MonoDevelop.AssemblyBrowser/Makefile
src/addins/MonoDevelop.SourceEditor2/Makefile
src/addins/Xml/Makefile
src/addins/MonoDevelop.Refactoring/Makefile
src/addins/MonoDevelop.AspNetCore/Makefile
src/addins/MonoDevelop.AzureFunctions/Makefile
src/addins/MonoDevelop.ConnectedServices/Makefile
src/addins/MonoDevelop.DotNetCore/Makefile
src/addins/MonoDevelop.DotNetCore/MonoDevelop.DotNetCore.Tests/Makefile
src/addins/MonoDevelop.Debugger/Makefile
src/addins/MonoDevelop.Debugger.Gdb/Makefile
src/addins/MonoDevelop.Debugger.Soft/Makefile
src/addins/MonoDevelop.Debugger.Soft/MonoDevelop.Debugger.Soft/Makefile
src/addins/MonoDevelop.Debugger.Soft/MonoDevelop.Debugger.Soft.AspNet/Makefile
src/addins/MonoDevelop.Debugger.VSCodeDebugProtocol/Makefile
src/addins/MonoDevelop.Debugger.VSCodeDebugProtocol/MonoDevelop.Debugger.VsCodeDebugProtocol/Makefile
src/addins/MonoDevelop.Debugger.Win32/Makefile
src/addins/MonoDevelop.Debugger.Win32/MonoDevelop.Debugger.Win32/Makefile
src/addins/MonoDevelop.HexEditor/Makefile
src/addins/MonoDevelop.DocFood/Makefile
src/addins/MonoDevelop.Packaging/Makefile
src/addins/MonoDevelop.Packaging/MonoDevelop.Packaging.Tests/Makefile
src/addins/MonoDevelop.PackageManagement/Makefile
src/addins/MonoDevelop.PackageManagement/MonoDevelop.PackageManagement.Tests/Makefile
tests/Makefile
tests/UnitTests/Makefile
tests/UserInterfaceTests/Makefile
tests/TestRunner/Makefile
tests/Ide.Tests/Makefile
tests/MacPlatform.Tests/Makefile
tests/MonoDevelop.Core.Tests/Makefile
tests/MonoDevelop.CSharpBinding.Tests/Makefile
tests/MonoDevelop.Refactoring.Tests/Makefile
tests/WindowsPlatform.Tests/Makefile
Makefile
monodevelop
mdtool
monodevelop.pc
monodevelop-core-addins.pc
monodevelop-core-mac-addins.pc
monodevelop.spec
po/Makefile
man/Makefile
theme-icons/Makefile
])

echo ""
echo "Configuration summary"
echo ""
echo "   * Installation prefix = $prefix"
echo "   * Version = $VERSION"
echo "   * Version Label = $PACKAGE_VERSION_LABEL"
echo "   * Compat Version = $COMPAT_ADDIN_VERSION"
echo "   * C# compiler = $CSC"
echo "   * Mono class library development extensions: $enable_monoextensions"
echo "   * Version control providers:"
echo "   *     Subversion (Unix): $enable_subversion"
echo "   *     Git: $enable_git"
echo "   * Platform bindings: $platform_bindings"
echo "   * Unit tests: $enable_tests"
echo "   * Release builds: $enable_release"
echo ""
