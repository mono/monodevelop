#! /bin/bash -e

echo ""
echo "MonoDevelop Makefile configuration script"
echo "-----------------------------------------"
echo "This will generate Makefile with correct paths for you."
echo ""
echo "Usage: ./configure.sh"
echo ""


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

PATHS=( /usr/lib/monodevelop /usr/local/monodevelop/lib/monodevelop /usr/local/lib/monodevelop /Applications/MonoDevelop.app/Contents/MacOS/lib/monodevelop /opt/mono/lib/monodevelop )
searchpaths "MonoDevelop" bin/MonoDevelop.Core.dll PATHS[@]
MDDIR=$RESULT
echo "Successfully found MonoDevelop root directory." $MDDIR

echo "Running $MDDIR/../../MonoDevelop to determine MonoDevelop version"

# e.g. 3.0.4.7
MDVERSION4=`$MDDIR/../../MonoDevelop /? | head -n 1 | grep -o "[0-9]\+.[0-9]\+.[0-9]\+\(.[0-9]\+\)\?"`
# e.g. 3.0.4
MDVERSION3=`$MDDIR/../../MonoDevelop /? | head -n 1 | grep -o "[0-9]\+.[0-9]\+.[0-9]\+"`

echo "Detected MonoDevelop version " $MDVERSION4

# ------------------------------------------------------------------------------
# Write Makefile

sed -e "s,INSERT_MDROOT,$MDDIR,g" -e "s,INSERT_MDVERSION3,$MDVERSION3,g" -e "s,INSERT_MDVERSION4,$MDVERSION4,g" Makefile.orig > Makefile
