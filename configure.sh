#! /bin/bash -e

# ------------------------------------------------------------------------------
# Parse command line arguments and specify default values

MONO=mono

while getopts e:f:c:n OPT; do
  case "$OPT" in
    e) MONO=$OPTARG
       ;;
    n) MONO=""
       ;;
  esac
done

# ------------------------------------------------------------------------------
# Utility function that searches specified directories for a specified file
# and if the file is not found, it asks user to provide a directory

RESULT=""


# On OSX use Mono's private copy of pkg-config if it exists, see https://github.com/fsharp/fsharp/issues/107
osx_pkg_config=/Library/Frameworks/Mono.framework/Versions/Current/bin/pkg-config
if test -e $osx_pkg_config; then
    PKG_CONFIG=$osx_pkg_config
elif test "x$PKG_CONFIG" = "xno"; then
    PKG_CONFIG=`which pkg-config`
fi

MONODIR=`$PKG_CONFIG --variable=libdir mono`/mono/4.0

echo "Assuming Mono root directory." $MONODIR

sed -e "s,INSERT_MONO_BIN,$MONODIR,g" Makefile.orig > Makefile

echo "Getting nuget packages..."
mozroots --import --sync --quiet
(cd monodevelop/MonoDevelop.FSharpBinding && mono ../../lib/nuget/nuget.exe install -OutputDirectory packages)
