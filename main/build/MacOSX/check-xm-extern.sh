#/usr/bin/env bash
(nm ../../external/Xamarin.Mac.registrar.full.a | grep " _xamarin_create_classes_Xamarin_Mac" >/dev/null) && echo "-DEXTERN_C"
