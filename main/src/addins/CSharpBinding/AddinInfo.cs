
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("CSharpBinding", 
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "Language bindings")]

[assembly:AddinName ("CSharp Language Binding")]
[assembly:AddinDescription ("CSharp Language Binding")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]

// Submodules
[assembly:AddinModule ("MonoDevelop.CSharpBinding.Autotools.dll")]
[assembly:AddinModule ("MonoDevelop.CSharpBinding.AspNet.dll")]
