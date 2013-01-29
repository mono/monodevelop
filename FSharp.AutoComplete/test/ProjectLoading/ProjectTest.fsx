#r "Microsoft.Build.Engine"
open Microsoft.Build.BuildEngine

let p = new Project()

let d = new System.Collections.Hashtable()

//p.Load("FSharp.AutoComplete.fsproj")
p.Load("test/Test1/Test1.fsproj")

p.Build([|"ResolveAssemblyReferences"|])

p.GetEvaluatedItemsByName("ReferencePath")
