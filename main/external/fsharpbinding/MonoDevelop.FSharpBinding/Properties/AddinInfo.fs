namespace MonoDevelop.FSharp
open Mono.Addins

[<Addin ("FSharpBinding", 
  Namespace = "MonoDevelop",
  Version = MonoDevelop.BuildInfo.Version,
  Url = "http://github.com/fsharp/xamarin-monodevelop-fsharp-addin/",
  Category = "Language bindings")>]

[<AddinName ("F# Language Binding")>]
[<AddinDescription ("F# Language Binding (for Xamarin Studio/MonoDevelop " + MonoDevelop.BuildInfo.Version + "). Install F# before using, see http://fsharp.org")>]
[<AddinAuthor ("F# Software Foundation (fsharp.org)")>]

[<AddinDependency ("Core", MonoDevelop.BuildInfo.Version)>]
[<AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)>]
[<AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)>]
[<AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)>]
[<AddinDependency ("UnitTesting", MonoDevelop.BuildInfo.Version)>]
()