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

PATHS=( /usr/lib/monodevelop /usr/local/lib/monodevelop /Applications/MonoDevelop.app/Contents/MacOS/lib/monodevelop /opt/mono/lib/monodevelop )
searchpaths "MonoDevelop" bin/MonoDevelop.Core.dll PATHS[@]
MDDIR=$RESULT
echo "Successfully found MonoDevelop root directory." $MDDIR


# ------------------------------------------------------------------------------
# Write Makefile

sed "s,INSERT_MD_ROOT,$MDDIR,g" Makefile.orig > Makefile
