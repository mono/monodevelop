
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Refactoring", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "IDE extensions")]

[assembly:AddinName ("Refactoring Support")]
[assembly:AddinDescription ("Provides refactoring support to MonoDevelop")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("RegexToolkit", MonoDevelop.BuildInfo.Version)]

#if DEBUG
[assembly: ImportAddinAssembly ("ClrHeapAllocationAnalyzer.dll", Scan = false)]
#endif