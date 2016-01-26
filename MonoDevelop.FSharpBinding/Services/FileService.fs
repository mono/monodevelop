
namespace MonoDevelop.FSharp
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library
open System.IO
open System.Threading
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Core
open MonoDevelop.Ide.TypeSystem
open ExtCore.Control
type Version = int

type FileSystem (defaultFileSystem : IFileSystem, openDocuments: unit -> Document seq) =
    let timestamps = new System.Collections.Generic.Dictionary<string, int * System.DateTime>()
    let getOpenDoc filename =
       let docs = openDocuments()
       docs |> Seq.tryFind(fun d -> d.FileName.FullPath.ToString() = filename)

    let getOpenDocContent (filename: string) =
        match getOpenDoc filename with
        | Some d ->
           let bytes = System.Text.Encoding.UTF8.GetBytes (d.Editor.Text)
           Some bytes
        | _ -> None

    interface IFileSystem with
        member x.FileStreamReadShim fileName =
            getOpenDocContent fileName
            |> Option.map (fun bytes -> new MemoryStream (bytes) :> Stream)
            |> Option.getOrElse (fun () -> defaultFileSystem.FileStreamReadShim fileName)

        member x.ReadAllBytesShim fileName =
            getOpenDocContent fileName
            |> Option.getOrElse (fun () -> defaultFileSystem.ReadAllBytesShim fileName)

        member x.GetLastWriteTimeShim fileName =
            let r = maybe {
               let! doc = getOpenDoc fileName
               if doc.IsDirty then
                 let key, newhash = fileName, doc.Editor.Text.GetHashCode()
                 return match timestamps.TryGetValue (key) with
                        | true, (hash, date) when hash = newhash -> date
                        | _       -> let d = System.DateTime.Now
                                     timestamps.[key] <- (newhash,d)
                                     d
               else return! None
             }
            r |> Option.getOrElse (fun () -> defaultFileSystem.GetLastWriteTimeShim fileName)

        member x.GetTempPathShim() = defaultFileSystem.GetTempPathShim()
        member x.FileStreamCreateShim fileName = defaultFileSystem.FileStreamCreateShim fileName
        member x.FileStreamWriteExistingShim fileName = defaultFileSystem.FileStreamWriteExistingShim fileName
        member x.GetFullPathShim fileName = defaultFileSystem.GetFullPathShim fileName
        member x.IsInvalidPathShim fileName = defaultFileSystem.IsInvalidPathShim fileName
        member x.IsPathRootedShim fileName = defaultFileSystem.IsPathRootedShim fileName
        member x.SafeExists fileName = defaultFileSystem.SafeExists fileName
        member x.FileDelete fileName = defaultFileSystem.FileDelete fileName
        member x.AssemblyLoadFrom fileName = defaultFileSystem.AssemblyLoadFrom fileName
        member x.AssemblyLoad(assemblyName) = defaultFileSystem.AssemblyLoad assemblyName
