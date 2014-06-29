
namespace MonoDevelop.FSharp
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library
open System.IO
open System.Threading
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Core
open MonoDevelop.Ide.TypeSystem
open FSharp.CompilerBinding
type Version = int

module FileSystemImpl = 
    let files = new System.Collections.Generic.Dictionary<string, int * System.DateTime>()
    let inline getOpenDoc filename = 
       IdeApp.Workbench.Documents |> Seq.tryFind(fun d -> d.Editor.Document.FileName = filename)

    let inline getOpenDocContent (filename: string) =
     //   dumpDocs ()
        match getOpenDoc filename with
        | Some f -> 
           let bytes = System.Text.Encoding.UTF8.GetBytes (f.Editor.Document.Text);
           Some bytes //let m = new System.IO.MemoryStream(bytes)
           //Some (m :> Stream) 
        | _ -> None

    let inline getOrElse f o = 
        match o with
        | Some v -> v
        | _      -> f()

open FileSystemImpl
type FileSystem (defaultFileSystem : IFileSystem) =
    let mutable cnt = 1
    interface IFileSystem with
        member x.FileStreamReadShim fileName = 
            getOpenDocContent fileName
            |> Option.map (fun bytes -> new MemoryStream (bytes) :> Stream)
            |> getOrElse (fun () -> defaultFileSystem.FileStreamReadShim fileName)
        
        member x.ReadAllBytesShim fileName =
            getOpenDocContent fileName 
            |> getOrElse (fun () -> defaultFileSystem.ReadAllBytesShim fileName)
        
        member x.GetLastWriteTimeShim fileName =
            let r = maybe {
               let! doc = FileSystemImpl.getOpenDoc fileName
               if doc.IsDirty then
                 let! parsedDoc = Option.ofNull doc.ParsedDocument
                 let! parsedFile = Option.ofNull parsedDoc.ParsedFile
                 let! date = Option.ofNullable parsedFile.LastWriteTime
                 return date.ToLocalTime()
               else return! None
             }
            getOrElse (fun () -> defaultFileSystem.GetLastWriteTimeShim fileName) r
        
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