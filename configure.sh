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

searchpaths()
{
  declare -a DIRS=("${!3}")
  FILE=$2
  DIR=${DIRS[0]}
  for TRYDIR in ${DIRS[@]}
  do
    if [ -f $TRYDIR/$FILE ] 
    then
      DIR=$TRYDIR
    fi
  done

  while [ ! -f $DIR/$FILE ]
  do 
    echo "File '$FILE' was not found in any of ${DIRS[@]}. Please enter $1 installation directory:"
    read DIR
  done
  RESULT=$DIR
}

# ------------------------------------------------------------------------------
# Find all paths that we need in order to generate the make file. Paths
# later in the list are preferred.

PATHS=( /usr/lib/mono/4.0 /usr/local/lib/mono/4.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.0 /opt/mono/lib/mono/4.0 /usr/lib64/mono)
searchpaths "Mono" mscorlib.dll PATHS[@]
MONODIR=$RESULT
echo "Successfully found Mono root directory." $MONODIR

# ------------------------------------------------------------------------------
# Write Makefile


sed -e "s,INSERT_MONO_BIN,$MONODIR,g" \
    Makefile.orig > Makefile

