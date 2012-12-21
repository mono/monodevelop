
open System
open Microsoft.Build.Engine

module ProjectParser =

  let load (uri: string) : Project =
    (new Project()).Load(uri)

  let getFiles (p: Project) : string[] =
    let fs = p.GetEvaluatedItemsByName("Compile")
    [| for f in fs do yield f.FinalItemSpec |]


  let getReferences (p: Project) : string[] =
    let refs = project.GetEvaluatedItemsByName("Reference")
    let resolve r =
      if r.HasMetadata("HintPath") then
        r.GetEvaluatedMetadata("HintPath") + r.FinalItemSpec
      else
        r.FinalItemSpec
    //[ for r in refs do yield resolve r ]
    [|  |]

  let getOptions (p: Project) : string[] =
    let getprop s = p.GetEvaluatedProperty(s)
    let optimize     = getprop "Optimize" |> Convert.ToBoolean
    let tailcalls    = getprop "Tailcalls" |> Convert.ToBoolean
    let debugsymbols = getprop "DebugSymbols" |> Convert.ToBoolean
    let defines = (getprop "DefineConstants").Split([|';';',';' '|],
                                                    StringSplitOptions.RemoveEmptyEntries)
    let otherflags = (getprop "OtherFlags").Split([|' '|],
                                                  StringSplitOptions.RemoveEmptyEntries)
    
    [| yield "--noframework"
       for symbol in defines do yield "--define:" + symbol
       yield if debugsymbols then  "--debug+" else  "--debug-"
       yield if optimize then "--optimize+" else "--optimize-"
       yield if tailcalls then "--tailcalls+" else "--tailcalls-"
       yield! otherflags |]
    
