namespace MonoDevelop.FSharp

open Microsoft.FSharp.Compiler
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open System.Collections.Generic
open System.IO
open MonoDevelop
open Microsoft.FSharp.Compiler.SourceCodeServices
     

type FSharpParsedDocument(fileName, location: DocumentLocation option) =
    inherit DefaultParsedDocument(fileName,Flags = ParsedDocumentFlags.NonSerializable)

    member val Tokens : (FSharpTokenInfo list * int64) list option = None with get,set
    member val AllSymbolsKeyed = Dictionary<Range.pos, FSharpSymbolUse>() :> IDictionary<_,_> with get, set
    member x.ParsedLocation = location

    member val UnusedCodeRanges : Range.range list option = None with get, set
    member val HasErrors = false with get, set

[<AutoOpen>]
module DocumentContextExt =
    type DocumentContext with
        member x.GetWorkingFolder() =
            if IdeApp.Workbench.ActiveDocument <> null && FileService.isInsideFSharpFile() then
                let doc = IdeApp.Workbench.ActiveDocument.FileName.ToString()
                if doc <> null then Path.GetDirectoryName(doc) |> Some else None
            else None

        member x.TryGetFSharpParsedDocument() =
            x.TryGetParsedDocument()
            |> Option.bind (function :? FSharpParsedDocument as fpd -> Some fpd | _ -> None)