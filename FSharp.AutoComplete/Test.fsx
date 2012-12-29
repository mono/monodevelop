#r "Microsoft.Build.Tasks.v4.0.dll"
#r "Microsoft.Build.Utilities.v4.0.dll"
#r "Microsoft.Build.Framework.dll"
#r "Microsoft.Build.Engine.dll"
#r "Microsoft.Build.dll"

open Microsoft.Build.Tasks
open Microsoft.Build.Utilities
open Microsoft.Build.Framework
open Microsoft.Build.BuildEngine

/// Reference resolution results. All paths are fully qualified.
type ResolutionResults = {
    /// Paths to primary references
    referencePaths:string array
    /// Paths to dependencies
    referenceDependencyPaths:string array
    /// Paths to related files (like .xml and .pdb)
    relatedPaths:string array
    /// Paths to satellite assemblies used for localization.
    referenceSatellitePaths:string array
    /// Additional files required to support multi-file assemblies.
    referenceScatterPaths:string array
    /// Paths to files that reference resolution recommend be copied to the local directory
    referenceCopyLocalPaths:string array
    /// Binding redirects that reference resolution recommends for the app.config file.
    suggestedBindingRedirects:string array
    }
 
 
let Resolve references (outputDirectory:string) : ResolveAssemblyReference =
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
 
    let rar = new ResolveAssemblyReference()
    rar.BuildEngine <- x
    //rar.TargetFrameworkDirectories <- [||]
    rar.AllowedRelatedFileExtensions <- [| ".pdb"; ".xml"; ".optdata" |]
    rar.FindRelatedFiles <- true
//    rar.Assemblies <- [|new Microsoft.Build.Utilities.TaskItem("System") :> ITaskItem|]
//    rar.Assemblies <- [|for r in references -> new Microsoft.Build.Utilities.TaskItem(r):>ITaskItem|]
    rar.Assemblies <- references
    rar.SearchPaths <- [|"{CandidateAssemblyFiles}"
                         "{HintPathFromItem}"
                         "{TargetFrameworkDirectory}"
                       //"{Registry:Software\Microsoft\.NetFramework,v3.5,AssemblyFoldersEx}"
                         "{AssemblyFolders}"
                         "{GAC}"
                         "{RawFileName}"
                         outputDirectory
                        |]
                             
    rar.AllowedAssemblyExtensions <- [| ".exe"; ".dll" |]    
    rar.TargetProcessorArchitecture <- "x86"                    
    if not (rar.Execute()) then
        failwith "Could not resolve"
    rar
    // {
    //     referencePaths = [| for p in rar.ResolvedFiles -> p.ItemSpec |]
    //     referenceDependencyPaths = [| for p in rar.ResolvedDependencyFiles -> p.ItemSpec |]
    //     relatedPaths = [| for p in rar.RelatedFiles -> p.ItemSpec |]
    //     referenceSatellitePaths = [| for p in rar.SatelliteFiles -> p.ItemSpec |]
    //     referenceScatterPaths = [||]// for p in rar.ScatterFiles -> p.ItemSpec |]
    //     referenceCopyLocalPaths = [| for p in rar.CopyLocalFiles -> p.ItemSpec |]
    //     suggestedBindingRedirects = [||]// for p in rar.SuggestedRedirects -> p.ItemSpec |]
    // }
   
// try
//     let s = Resolve([| "System"
//                        "FSharp.Core"
//                        //"System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, processorArchitecture=MSIL"
//                        //"Microsoft.SqlServer.Replication"
//                     |], "")
//     printfn "%A" s
// finally
//     ignore (System.Console.ReadKey())


let p = new Project()
do p.Load("../../Utils/Utils.fsproj")
let refs = p.GetEvaluatedItemsByName("Reference")
let refslist : List<BuildItem> = [for r in refs do yield r]

let convert (bi: BuildItem) : ITaskItem =
  let ti = new TaskItem(bi.FinalItemSpec)
  if bi.HasMetadata("HintPath") then
    ti.SetMetadata("HintPath", bi.GetEvaluatedMetadata("HintPath"))
  ti :> ITaskItem

let r2 = refslist.Head
let t2 = convert r2
[for n in (convert r2).MetadataNames do yield n]
t2.GetMetadata("HintPath")
let bla = r2.GetEvaluatedMetadata("Filename")
    
  // { new ITaskItem with
  //     member this.ItemSpec = bi.FinalItemSpec
  //       // with get ()  = bi.FinalItemSpec
  //       // and  set (x) = bi.FinalItemSpec <- x

  //     member this.MetadataCount = 0//bi.CustomMetadataCount

  //     member this.MetadataNames = new System.Collections.Collection() //bi.CustomMetadataNames
  //   }

let refs' = [|for r in refs do yield convert r|]
