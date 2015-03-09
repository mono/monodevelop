using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin("ValaBinding",
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Language bindings")]
[assembly:AddinName("Vala Language Binding")]
[assembly:AddinDescription("Vala Language Binding")]
[assembly:AddinDependency("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency("Deployment", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency("Deployment.Linux", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency("Refactoring", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency("SourceEditor2", MonoDevelop.BuildInfo.Version)]
