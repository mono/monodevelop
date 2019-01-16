#!/bin/sh
# Run this to generate all the initial makefiles, etc.
# Ripped off from GNOME macros version

DIE=0

PKG_NAME=MonoDevelop
WANT_AUTOCONF="2.5"
srcdir=`dirname $0`
test -z "$srcdir" && srcdir=.

(autoconf --version) < /dev/null > /dev/null 2>&1 || {
  echo
  echo "**Error**: You must have \`autoconf' installed to compile MonoDevelop."
  echo "Download the appropriate package for your distribution, or get the "
  echo "source at ftp://ftp.gnu.org/pub/gnu/autoconf/autoconf-2.61.tar.bz2 "
  echo "(or a newer version if it is available)"
  DIE=1
}

(automake --version) < /dev/null > /dev/null 2>&1 || {
  echo
  echo "**Error**: You must have \`automake' installed to compile MonoDevelop."
  echo "Download the appropriate package for your distribution, or get the "
  echo "source at ftp://ftp.gnu.org/pub/gnu/automake/automake-1.10.1.tar.bz2 "
  echo "(or a newer version if it is available)"
  DIE=1
  NO_AUTOMAKE=yes
}

# if no automake, don't bother testing for aclocal
test -n "$NO_AUTOMAKE" || (aclocal --version) < /dev/null > /dev/null 2>&1 || {
  echo
  echo "**Error**: Missing \`aclocal'."
  DIE=1
}

if test "$DIE" -eq 1; then
  exit 1
fi

if test -z "$*"; then
  echo "**Warning**: I am going to run \`configure' with no arguments."
  echo "If you wish to pass any to it, please specify them on the"
  echo \`$0\'" command line."
  echo
fi

case $CC in
xlc )
  am_opt=--include-deps;;
esac

echo "Running aclocal $ACLOCAL_FLAGS ..."
aclocal $ACLOCAL_FLAGS || {
  echo
  echo "**Error**: aclocal failed. This may mean that you have not"
  echo "installed all of the packages you need, or you may need to"
  echo "set ACLOCAL_FLAGS to include \"-I \$prefix/share/aclocal\""
  echo "for the prefix where you installed the packages whose"
  echo "macros were not found"
  exit 1
}

echo "Running automake --foreign $am_opt ..."
automake --add-missing --foreign -Wno-portability -Wno-syntax $am_opt ||
  { echo "**Error**: automake failed."; exit 1; }

echo "Running autoconf ..."
WANT_AUTOCONF="2.5" autoconf || { echo "**Error**: autoconf failed."; exit 1; }


conf_flags="--enable-maintainer-mode"

if test x$NOCONFIGURE = x; then
  echo Running $srcdir/configure $conf_flags "$@" ...
  $srcdir/configure $conf_flags "$@" \
  && echo Now type \`make\' to compile $PKG_NAME || exit 1
else
  echo Skipping configure process.
fi
