#r "Microsoft.Build.Engine"
#r "Microsoft.Build.Framework"
#r "Microsoft.Build.Tasks.v4.0"
open Microsoft.Build.BuildEngine
open Microsoft.Build.Framework
open Microsoft.Build.Tasks

let gfp = GetFrameworkPath()
printfn "%s" gfp.FrameworkVersion11Path
printfn "%s" gfp.FrameworkVersion20Path
printfn "%s" gfp.FrameworkVersion35Path
printfn "%s" gfp.FrameworkVersion40Path
printfn "%s" gfp.FrameworkVersion45Path



let p = new Project()
let cl = new ConsoleLogger(LoggerVerbosity.Diagnostic)
p.ParentEngine.RegisterLogger(cl)

p.Load("FSharp.AutoComplete.fsproj")
//p.Load("test/Test1/Test1.fsproj")

p.Build([|"GetFrameworkPath"|])
p.Build([|"ResolveAssemblyReferences"|])

[for i in p.GetEvaluatedItemsByName("ReferencePath") do yield i.FinalItemSpec ]

let ts = new Toolset("4.5",
                     "/home/scratch/local_mono/lib/mono/4.5",
                     null)

p.ParentEngine.Toolsets.Add(ts)

let be = new Engine()
//be.BinPath <- "/home/scratch/local_mono/lib/mono/4.5"
//be.DefaultToolsVersion <- 4.5

be.RegisterLogger(cl)


