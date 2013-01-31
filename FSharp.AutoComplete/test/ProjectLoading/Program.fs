open Microsoft.Build.Tasks
open Microsoft.Build.BuildEngine
open Microsoft.Build.Framework

[<EntryPoint>]
let main args =
  let gfp = GetFrameworkPath()
  printfn "%s" gfp.FrameworkVersion11Path
  printfn "%s" gfp.FrameworkVersion20Path
  printfn "%s" gfp.FrameworkVersion35Path
  printfn "%s" gfp.FrameworkVersion40Path
  printfn "%s" gfp.FrameworkVersion45Path

  if args.Length <> 1 then
    printfn "One project file please"
  else
    let p = new Project()
    let cl = new ConsoleLogger(LoggerVerbosity.Diagnostic)
    p.ParentEngine.RegisterLogger(cl)

    p.Load(args.[0])

    ignore <| p.Build([|"ResolveAssemblyReferences"|])

    [for i in p.GetEvaluatedItemsByName("ReferencePath")
       do printfn "%s" i.FinalItemSpec ]
    |> ignore

  0
