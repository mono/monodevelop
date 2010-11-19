#! /bin/sh -e

echo ""
echo "MonoDevelop Makefile configuration script"
echo "-----------------------------------------"
echo "This will generate Makefile with correct paths for you. You may need to provide path for some components if they cannot be found automatically. If you're using default path but it wasn't find automatically, please report it, so that it can be added. Contact: Tomas Petricek (tomas@tomasp.net)"
echo ""
echo "Usage: ./configure.sh [-e:<mono>] [-f:<fsc>] [-c:<gmcs>] [-n]"
echo ""
echo "  -n        If specified, the the 'mono' executable is not used"
echo "            (Use this on Windows or when 'exe' files can be executed)"
echo ""
echo "  -e <mono> Specify 'mono' executable with parameters"
echo "            Default value: mono"
echo ""
echo "  -f <fsc>  Path/name of the F# compiler executable or script"
echo "            ('mono' is NOT automatically added to the front) "
echo "            Default value: fsharpc"
echo ""
echo "  -c <gmcs> Path/name of the C# compiler executable or script"
echo "            ('mono' is NOT automatically added to the front) "
echo "            Default value: gmcs"
echo ""
echo "Pres enter to continue..."

read a

# ------------------------------------------------------------------------------
# Parse command line arguments and specify default values

GMCS=gmcs
MONO=mono
FSC=fsharpc
 
while getopts e:f:c:n OPT; do
  case "$OPT" in
    e) MONO=$OPTARG
       ;;
    n) MONO=""
       ;;
    f) FSC=$OPTARG
       ;;
    c) GMCS=$OPTARG
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
# Find all paths that we need in order to generate the make file

PATHS=( /usr/lib/monodevelop /Applications/MonoDevelop.app/Contents/MacOS/lib/monodevelop )
searchpaths "MonoDevelop" bin/MonoDevelop.Core.dll PATHS[@]
MDDIR=$RESULT
echo "Successfully found MonoDevelop root directory."

PATHS=( /usr/lib/fsharp /usr/local/lib/fsharp )
searchpaths "F#" FSharp.Core.dll PATHS[@]
FSDIR=$RESULT
echo "Successfully found F# root directory."

PATHS=( /usr/lib/mono/2.0 /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono/2.0 )
searchpaths "Mono" mscorlib.dll PATHS[@]
MONODIR=$RESULT
echo "Successfully found Mono root directory."

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/gtk-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono/gtk-sharp-2.0 )
searchpaths "Gtk#" gtk-sharp.dll PATHS[@]
GTKDIR=$RESULT
echo "Successfully found Gtk# root directory."

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/glib-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono/gtk-sharp-2.0 )
searchpaths "Glib" glib-sharp.dll PATHS[@]
GLIBDIR=$RESULT
echo "Successfully found Glib# root directory."

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/atk-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono/gtk-sharp-2.0 )
searchpaths "Atk#" atk-sharp.dll PATHS[@]
ATKDIR=$RESULT
echo "Successfully found Atk# root directory."

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/gdk-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono/gtk-sharp-2.0 )
searchpaths "Gdk#" gdk-sharp.dll PATHS[@]
GDKDIR=$RESULT
echo "Successfully found Gdk# root directory."

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/pango-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/2.8/lib/mono/gtk-sharp-2.0 )
searchpaths "Pango#" pango-sharp.dll PATHS[@]
PANGODIR=$RESULT
echo "Successfully found Pango root directory."

# ------------------------------------------------------------------------------
# Write Makefile

cp Makefile.orig Makefile.1
sed "s,INSERT_MD_ROOT,$MDDIR,g" Makefile.1 > Makefile.2
sed "s,INSERT_MONO_BIN,$MONODIR,g" Makefile.2 > Makefile.1
sed "s,INSERT_GTK_DIR,$GTKDIR,g" Makefile.1 > Makefile.2
sed "s,INSERT_ATK_DIR,$ATKDIR,g" Makefile.2 > Makefile.1
sed "s,INSERT_GLIB_DIR,$GLIBDIR,g" Makefile.1 > Makefile.2
sed "s,INSERT_GDK_DIR,$GDKDIR,g" Makefile.2 > Makefile.1
sed "s,INSERT_FSHARP_BIN,$FSDIR,g" Makefile.1 > Makefile.2
sed "s,INSERT_PANGO_DIR,$PANGODIR,g" Makefile.2 > Makefile.1
sed "s,INSERT_MONO,$MONO,g" Makefile.1 > Makefile.2
sed "s,INSERT_FSHARP_COMPILER,$FSC,g" Makefile.2 > Makefile.1
sed "s,INSERT_CSHARP_COMPILER,$GMCS,g" Makefile.1 > Makefile.2
rm Makefile.1
mv Makefile.2 Makefile