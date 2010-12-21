This addin requires a couple of env vars to point to the location of the MonoDroid SDK.
In addition, you will probably need to configure the Android SDK location in MD Preferences.

MSBuildExtensionsPath=/Users/michael/Mono/mondroid/tools/msbuild/build
MONODROID_PATH=/Users/michael/Mono/mondroid/ make run

Also, the shared runtime APK must be copied to $MONODROID_PATH/bin/
