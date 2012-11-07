#! /bin/bash -e

# ------------------------------------------------------------------------------
# Parse command line arguments and specify default values

MONO=mono

# Annoyingly, Mono 2.10 originally installed 'fsc' on Mac, but 
# later versions install 'fsharpc'. Fresh builds on Linux
# and future versions of Mono will install 'fsharpc'. Even more annoyingly,
# there can be old, crappy versions of 'fsharpc' hanging around on Mac
# machines - these versions aren't suitable for use with MonoDevelop.
if [ ! -e "/usr/bin/fsharpc" ] && [ -e "/usr/bin/fsc" ];
then FSC=fsc; 
else FSC=fsharpc; 
fi


if [[ `which $FSC` == "" ]]; then FSC=fsc; else FSC=`which $FSC`; fi 
 
while getopts e:f:c:n OPT; do
  case "$OPT" in
    e) MONO=$OPTARG
       ;;
    n) MONO=""
       ;;
    f) FSC=$OPTARG
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

PATHS=( /usr/lib/fsharp /usr/local/lib/fsharp /usr/local/lib/mono/4.0 /opt/mono/lib/mono/4.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.0 /usr/lib/mono/4.0 /usr/lib64/mono/4.0)
searchpaths "F#" FSharp.Core.dll PATHS[@]
FSDIR=$RESULT
echo "Successfully found F# root directory." $FSDIR

PATHS=( /usr/lib/mono/4.0 /usr/local/lib/mono/4.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.0 /opt/mono/lib/mono/4.0 /usr/lib64/mono)
searchpaths "Mono" mscorlib.dll PATHS[@]
MONODIR=$RESULT
echo "Successfully found Mono root directory." $MONODIR

echo "Using F# compiler : " $FSC

# ------------------------------------------------------------------------------
# Write Makefile


sed -e "s,INSERT_MONO_BIN,$MONODIR,g" \
    -e "s,INSERT_FSHARP_BIN,$FSDIR,g" \
    -e "s,INSERT_FSHARP_COMPILER,$FSC,g" \
    Makefile.orig > Makefile

