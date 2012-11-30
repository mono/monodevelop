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


MONODIR=`pkg-config --variable=libdir mono`/mono/4.0

echo "Assuming Mono root directory." $MONODIR

sed -e "s,INSERT_MONO_BIN,$MONODIR,g" Makefile.orig > Makefile

