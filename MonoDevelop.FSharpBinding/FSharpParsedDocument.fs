namespace MonoDevelop.FSharp

open ICSharpCode.NRefactory.TypeSystem
open Microsoft.FSharp.Compiler
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open System
open System.Collections.Generic
open System.IO
open System.Threading
open MonoDevelop
open Microsoft.FSharp.Compiler.SourceCodeServices
     
type FSharpParsedDocument(fileName) =
    inherit DefaultParsedDocument(fileName,Flags = ParsedDocumentFlags.NonSerializable)
    member val Tokens : (FSharpTokenInfo list * int64) list option = None with get,set
    member val AllSymbolsKeyed = Dictionary<Range.pos, FSharpSymbolUse>() :> IDictionary<_,_> with get, set
    
[<AutoOpen>]
module DocumentContextExt =
    type DocumentContext with
        member x.TryGetFSharpParsedDocument() =
            x.TryGetParsedDocument()
            |> Option.bind (function :? FSharpParsedDocument as fpd -> Some fpd | _ -> None)