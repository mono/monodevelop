#! /bin/bash -e

echo ""
echo "MonoDevelop Makefile configuration script"
echo "-----------------------------------------"
echo "This will generate Makefile with correct paths for you. You may need to provide path for some components if they cannot be found automatically. If you're using default path but it wasn't found automatically, please report it, so that it can be added. Contact: Tomas Petricek (tomas@tomasp.net)"
echo ""
echo "Usage: ./configure.sh [-e:<mono>] [-f:<fsc>] [-c:<gmcs>] [-n]"
echo ""
echo "  -n        If specified, the 'mono' executable is not used"
echo "            (Use this on Windows or when 'exe' files can be executed)"
echo ""
echo "  -e <mono> Specify 'mono' executable with parameters"
echo "            Default value: mono"
echo ""
echo "  -f <fsc>  Path/name of the F# compiler executable or script"
echo "            ('mono' is NOT automatically added to the front) "
echo "            Default value: fsc"
echo ""
echo "  -c <gmcs> Path/name of the C# compiler executable or script"
echo "            ('mono' is NOT automatically added to the front) "
echo "            Default value: gmcs"
echo ""
echo "Press enter to continue..."

read a

# ------------------------------------------------------------------------------
# Parse command line arguments and specify default values

GMCS=gmcs
MONO=mono
FSC=fsc


if [[ `which $FSC` == "" ]]; then FSC=fsc; else FSC=`which $FSC`; fi 
if [[ `which $GMCS` == "" ]]; then GMCS=gmcs; else GMCS=`which $GMCS`; fi
 
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

PATHS=( /usr/lib/monodevelop /Applications/MonoDevelop.app/Contents/MacOS/lib/monodevelop /opt/mono/lib/monodevelop )
searchpaths "MonoDevelop" bin/MonoDevelop.Core.dll PATHS[@]
MDDIR=$RESULT
echo "Successfully found MonoDevelop root directory." $MDDIR

PATHS=( /usr/lib/fsharp /usr/local/lib/fsharp /opt/mono/lib/mono/4.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.0 /usr/lib/mono/4.0 /usr/lib64/mono/4.0)
searchpaths "F#" FSharp.Core.dll PATHS[@]
FSDIR=$RESULT
echo "Successfully found F# root directory." $FSDIR

PATHS=( /usr/lib/mono/4.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.0 /opt/mono/lib/mono/4.0 /usr/lib64/mono)
searchpaths "Mono" mscorlib.dll PATHS[@]
MONODIR=$RESULT
echo "Successfully found Mono root directory." $MONODIR

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/gtk-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gtk-sharp-2.0 /opt/mono/lib/mono/gtk-sharp-2.0 /usr/lib64/mono/gtk-sharp-2.0)
searchpaths "Gtk#" gtk-sharp.dll PATHS[@]
GTKDIR=$RESULT
echo "Successfully found Gtk# root directory." $GTKDIR

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/glib-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gtk-sharp-2.0 /opt/mono/lib/mono/gtk-sharp-2.0 /usr/lib64/mono/gtk-sharp-2.0)
searchpaths "Glib" glib-sharp.dll PATHS[@]
GLIBDIR=$RESULT
echo "Successfully found Glib# root directory." $GLIBDIR

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/atk-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gtk-sharp-2.0 /opt/mono/lib/mono/gtk-sharp-2.0 /usr/lib64/mono/gtk-sharp-2.0)
searchpaths "Atk#" atk-sharp.dll PATHS[@]
ATKDIR=$RESULT
echo "Successfully found Atk# root directory." $ATKDIR

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/gdk-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gtk-sharp-2.0 /opt/mono/lib/mono/gtk-sharp-2.0 /usr/lib64/mono/gtk-sharp-2.0)
searchpaths "Gdk#" gdk-sharp.dll PATHS[@]
GDKDIR=$RESULT
echo "Successfully found Gdk# root directory." $GDKDIR

PATHS=( /usr/lib/mono/gtk-sharp-2.0 /usr/lib/cli/pango-sharp-2.0 /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/gtk-sharp-2.0 /opt/mono/lib/mono/gtk-sharp-2.0 /usr/lib64/mono/gtk-sharp-2.0)
searchpaths "Pango#" pango-sharp.dll PATHS[@]
PANGODIR=$RESULT
echo "Successfully found Pango root directory." $PANGODIR

PATHS=( /usr/lib/mono/mono-addins /usr/lib/cli/mono-addins /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/mono-addins /opt/mono/lib/mono/mono-addins /usr/lib/cli/Mono.Addins-0.2 /usr/lib64/mono/mono-addins)
searchpaths "Mono.Addins" Mono.Addins.dll PATHS[@]
MADIR=$RESULT
echo "Successfully found Mono.Addins directory." $MADIR
echo "Using F# compiler : " $FSC
echo "Using C# compiler : " $GMCS

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
sed "s,INSERT_MA_DIR,$MADIR,g" Makefile.2 > Makefile.1
rm Makefile.2
mv Makefile.1 Makefile
