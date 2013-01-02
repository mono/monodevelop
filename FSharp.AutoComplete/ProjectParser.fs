// --------------------------------------------------------------------------------------
// (c) Robin Neatherway
// --------------------------------------------------------------------------------------
namespace FSharp.InteractiveAutocomplete

open System
open Microsoft.Build.BuildEngine
open Microsoft.Build.Framework
open Microsoft.Build.Tasks
open Microsoft.Build.Utilities

module ProjectParser =

  type ProjectResolver =
    {
      project: Project
      rar:     ResolveAssemblyReference
    }

  let private mkrar () =
    let x = { new IBuildEngine with
                member be.BuildProjectFile(projectFileName, targetNames, globalProperties, argetOutputs) = true
                member be.LogCustomEvent(e) = ()
                member be.LogErrorEvent(e) = ()
                member be.LogMessageEvent(e) = ()
                member be.LogWarningEvent(e) = ()
                member be.ColumnNumberOfTaskNode with get() = 1
                member be.ContinueOnError with get() = true
                member be.LineNumberOfTaskNode with get() = 1
                member be.ProjectFileOfTaskNode with get() = "" }
    let rar = new ResolveAssemblyReference ()
    do rar.BuildEngine <- x
    do rar.AllowedRelatedFileExtensions <- [| ".pdb"; ".xml"; ".optdata" |]
    do rar.FindRelatedFiles <- true
    do rar.SearchPaths <- [|"{CandidateAssemblyFiles}"
                            "{HintPathFromItem}"
                            "{TargetFrameworkDirectory}"
                            "{AssemblyFolders}"
                            "{GAC}"
                            "{RawFileName}"
                           |]
    do rar.AllowedAssemblyExtensions <- [| ".exe"; ".dll" |]
    rar


  let load (uri: string) : ProjectResolver =
    let p = new Project()
    p.Load(uri)
    { project = p; rar =  mkrar () }

  let getFileName (p: ProjectResolver) : string = p.project.FullFileName

  let getFiles (p: ProjectResolver) : string array =
    let fs = p.project.GetEvaluatedItemsByName("Compile")
    [| for f in fs do yield f.FinalItemSpec |]

  let getReferences (p: ProjectResolver) : string array =
    let convert (bi: BuildItem) : ITaskItem =
      let ti = new TaskItem(bi.FinalItemSpec)
      if bi.HasMetadata("HintPath") then
        ti.SetMetadata("HintPath", bi.GetEvaluatedMetadata("HintPath"))
      ti :> ITaskItem

    // TODO: For HintPath to work, should change PWD to directory of 'p'

    let refs = p.project.GetEvaluatedItemsByName "Reference"
    p.rar.Assemblies <- [| for r in refs do yield convert r |]
    p.rar.TargetProcessorArchitecture <- p.project.GetEvaluatedProperty "PlatformTarget"
    // TODO: Execute may fail
    ignore <| p.rar.Execute ()
    [| for f in p.rar.ResolvedFiles do yield f.ItemSpec |]


  let getOptions (p: ProjectResolver) : string array =
    let getprop s = p.project.GetEvaluatedProperty s
    // TODO: Robustify - convert.ToBoolean may fail
    let optimize     = getprop "Optimize" |> Convert.ToBoolean
    let tailcalls    = getprop "Tailcalls" |> Convert.ToBoolean
    let debugsymbols = getprop "DebugSymbols" |> Convert.ToBoolean
    let defines = (getprop "DefineConstants").Split([|';';',';' '|],
                                                    StringSplitOptions.RemoveEmptyEntries)
    let otherflags = (getprop "OtherFlags").Split([|' '|],
                                                  StringSplitOptions.RemoveEmptyEntries)
    
    [|
      yield "--noframework"
      for symbol in defines do yield "--define:" + symbol
      yield if debugsymbols then  "--debug+" else  "--debug-"
      yield if optimize then "--optimize+" else "--optimize-"
      yield if tailcalls then "--tailcalls+" else "--tailcalls-"
      yield! otherflags
      yield! (getReferences p)
     |]
    
