AC_INIT([monodevelop], m4_esyscmd_s([grep ^Version ../version.config|cut -d '=' -f 2|tr -d '\n']), [monodevelop-list@lists.ximian.com])
AC_PREREQ(2.53)
AM_INIT_AUTOMAKE([1.10 tar-ustar])
AM_MAINTAINER_MODE

#capture aclocal flags for autoreconf
AC_SUBST(ACLOCAL_FLAGS)

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
	platform_bindings="${platform_bindings}Mac"
fi

AM_CONDITIONAL(ENABLE_MACPLATFORM, [test x$enable_macplatform = xyes])

# Windows platform addin
AC_ARG_ENABLE(windowsplatform,
	AC_HELP_STRING([--enable-windowsplatform],
		[enable Windows platform support [default=no]]),
		enable_windowsplatform=${enableval}, enable_windowsplatform=$default_windowsplatform)

if test x$enable_windowsplatform = xyes; then
	platform_bindings="${platform_bindings}Windows "
fi

AM_CONDITIONAL(ENABLE_WINDOWSPLATFORM, [test x$enable_windowsplatform = xyes])

dnl Intl
ALL_LINGUAS="cs da de es fr ja pt_BR tr pl it zh_TW zh_CN ru sl ca gl pt sv nl id hu nb ko uk"
AC_SUBST(ALL_LINGUAS)
GETTEXT_PACKAGE=monodevelop
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])

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
monodevelop
mdtool
theme-icons/Makefile
tests/Makefile
Makefile
po/Makefile
build/MacOSX/Makefile
external/Makefile
])

echo ""
echo "Configuration summary"
echo ""
echo "   * Installation prefix = $prefix"
echo "   * Version = $VERSION"
echo "   * Compat Version = $COMPAT_ADDIN_VERSION"
echo "   * Release builds: $enable_release"
echo ""
