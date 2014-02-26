
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("TextTemplating", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "IDE extensions")]

[assembly:AddinName ("Text Templating")]
[assembly:AddinDescription ("Support for editing and running T4 text templates")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
