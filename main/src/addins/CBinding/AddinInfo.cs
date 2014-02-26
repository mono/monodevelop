
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("CBinding", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Language bindings")]

[assembly:AddinName ("C/C++ Language Binding")]
[assembly:AddinDescription ("C/C++ Language binding")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Deployment", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Deployment.Linux", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
