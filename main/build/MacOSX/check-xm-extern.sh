#/usr/bin/env bash

# If we find a symbol that matches `_xamarin_create_classes_Xamarin_Mac` it means we found a non-mangled version of the
# symbol. The space before the underscore is mandatory, because that's the delimiter that's used by nm between symbol
# offset and the function name.
(nm ../../external/Xamarin.Mac.registrar.full.a | grep " _xamarin_create_classes_Xamarin_Mac" >/dev/null) && echo "-DEXTERN_C"
