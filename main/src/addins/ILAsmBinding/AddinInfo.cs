
using Mono.Addins;

[assembly:Addin ("ILAsmBinding", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "Language bindings")]

[assembly:AddinName ("ILAsm Language Binding")]
[assembly:AddinDescription ("ILAsm Language Binding")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
