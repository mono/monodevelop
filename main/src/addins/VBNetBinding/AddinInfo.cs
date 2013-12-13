
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("VBBinding", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Language bindings")]

[assembly:AddinName ("VB.NET Language Binding")]
[assembly:AddinDescription ("VB.NET Language Binding")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
