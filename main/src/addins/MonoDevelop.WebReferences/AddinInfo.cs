
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("WebReferences", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "Web Development")]

[assembly:AddinName ("Project Web References")]
[assembly:AddinDescription ("Provides support for adding and maintaining Web References for CSharp and VB projects")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
