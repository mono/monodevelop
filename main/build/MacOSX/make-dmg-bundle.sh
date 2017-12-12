#!/usr/bin/env bash

# Shamelessly lifted from Banshee's build process
 	
pushd $(dirname $0) &>/dev/null

DMG_APP=$1
RENDER_OP=$2

if [ -z "$DMG_APP" ]; then
	DMG_APP=MonoDevelop.app
fi

if test ! -e "$DMG_APP" ; then
	echo "Missing $DMG_APP"
	exit 1
fi

NAME=`grep -A1 CFBundleName "$DMG_APP/Contents/Info.plist"  | grep string | sed -e 's/.*<string>//' -e 's,</string>,,'`
VERSION=`grep -A1 CFBundleVersion "$DMG_APP/Contents/Info.plist"  | grep string | sed -e 's/.*<string>//' -e 's,</string>,,'`

#if we use the version in the volume name, Finder can't find the background image
#because the DS_Store depends on the volume name, and we aren't currently able
#to alter it programmatically
VOLUME_NAME="$NAME"

echo "Building bundle for $NAME $VERSION..."

DMG_FILE="$NAME-$VERSION.dmg"
MOUNT_POINT="$VOLUME_NAME.mounted"

rm -f "$DMG_FILE"
rm -f "$DMG_FILE.master"
 	
# Compute an approximated image size in MB, and bloat by double
# codesign adds a unknown amount of extra size requirements and there are some
# files where the additional size required is even more "unknown". doubling
# is a brute force approach, but doesn't really impact final distribution size
# because the empty space is compressed to nothing.
image_size=$(du -ck "$DMG_APP" | tail -n1 | cut -f1)
image_size=$((($image_size *2) / 1000))

echo "Creating disk image (${image_size}MB)..."
hdiutil create "$DMG_FILE" -megabytes $image_size -volname "$VOLUME_NAME" -fs HFS+ -quiet || exit $?

echo "Attaching to disk image..."
hdiutil attach "$DMG_FILE" -readwrite -noautoopen -mountpoint "$MOUNT_POINT" -quiet || exit $?

echo "Populating image..."

# this used to be mv, but we need to preserve the bundle directory to do more checks on the contents
# such as compatibility-check
ditto "$DMG_APP" "$MOUNT_POINT/$DMG_APP"

# This won't result in any deletions 
#find "$MOUNT_POINT" -type d -iregex '.*\.svn$' &>/dev/null | xargs rm -rf

pushd "$MOUNT_POINT" &>/dev/null
ln -s /Applications Applications
popd &>/dev/null

mkdir -p "$MOUNT_POINT/.background"
if [ "$RENDER_OP" == "norender" ]; then
	cp dmg-bg.png "$MOUNT_POINT/.background/dmg-bg.png"
else
	DYLD_FALLBACK_LIBRARY_PATH="/Library/Frameworks/Mono.framework/Versions/Current/lib:/lib:/usr/lib" mono render.exe "$NAME $VERSION"
	mv dmg-bg-with-version.png "$MOUNT_POINT/.background/dmg-bg.png"
fi

cp DS_Store "$MOUNT_POINT/.DS_Store"
if [ -e VolumeIcon.icns ] ; then
	cp VolumeIcon.icns "$MOUNT_POINT/.VolumeIcon.icns"
	SetFile -c icnC "$MOUNT_POINT/.VolumeIcon.icns"
fi
SetFile -a C "$MOUNT_POINT"

echo "Detaching from disk image..."
hdiutil detach "$MOUNT_POINT" -quiet || exit $?

mv "$DMG_FILE" "$DMG_FILE.master"

echo "Creating distributable image..."
hdiutil convert -quiet -format UDBZ -o "$DMG_FILE" "$DMG_FILE.master" || exit $?

echo "Built disk image $DMG_FILE"

if [ ! "x$1" = "x-m" ]; then
rm "$DMG_FILE.master"
fi

rm -rf "$MOUNT_POINT"

echo "Done."

popd &>/dev/null 
