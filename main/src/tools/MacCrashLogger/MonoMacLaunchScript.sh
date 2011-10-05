#!/bin/sh

# Determine locations

MACOS_DIR=$(cd "$(dirname "$0")"; pwd)
APP_ROOT=${MACOS_DIR%%/Contents/MacOS}
 
CONTENTS_DIR="$APP_ROOT/Contents"
RESOURCES_PATH="$CONTENTS_DIR/Resources"

APP_NAME=MonoDevelopLogAgent
ASSEMBLY=MonoDevelopLogAgent.exe

MONO_FRAMEWORK_PATH=/Library/Frameworks/Mono.framework/Versions/Current

#Environment setup
export DYLD_FALLBACK_LIBRARY_PATH="$MONO_FRAMEWORK_PATH/lib:$DYLD_FALLBACK_LIBRARY_PATH"
export PATH="$MONO_FRAMEWORK_PATH/bin:$PATH"
export DYLD_LIBRARY_PATH="$RESOURCES_PATH:$DYLD_LIBRARY_PATH"

# Pass the executable name as the last parameter of MONO_ENV_OPTIONS
# since some NSApplication APIs will poke at the startup arguments and do not
# like the .exe there
export MONO_ENV_OPTIONS="$MONO_OPTIONS $RESOURCES_PATH/$ASSEMBLY"

#run the app
exec "$APP_ROOT/$APP_NAME" $@