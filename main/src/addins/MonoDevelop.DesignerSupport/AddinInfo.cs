
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("DesignerSupport", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "MonoDevelop Core")]

[assembly:AddinName ("Visual Designer Support")]
[assembly:AddinDescription ("Supporting services and pads for visual design tools")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]

[assembly: ImportAddinAssembly ("Xamarin.PropertyEditing.dll")]

#if MAC
[assembly: ImportAddinAssembly ("Xamarin.PropertyEditing.Mac.dll")]
#endif
