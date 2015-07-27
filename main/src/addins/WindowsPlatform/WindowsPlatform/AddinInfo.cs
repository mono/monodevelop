
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("WindowsPlatform", 
    Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Flags = AddinFlags.Hidden,
	Category = "Platform Support")]

[assembly:AddinName ("MonoDevelop Windows Platform Support")]
[assembly:AddinDescription ("Windows Platform Support for MonoDevelop")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
