namespace MonoDevelop.FSharp

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Tasks
open MonoDevelop.Ide.TypeSystem
open System.Collections.Generic
open System.IO

type FSharpParsedDocument(fileName, location: DocumentLocation option) =
    inherit DefaultParsedDocument(fileName,Flags = ParsedDocumentFlags.NonSerializable)
    let specialCommentTags = 
        CommentTag.SpecialCommentTags
        |> Seq.map(fun t -> t.Tag)
        |> Set.ofSeq

    member val Tokens : (FSharpTokenInfo list * string) list option = None with get,set
    member val AllSymbolsKeyed = Dictionary<Range.pos, FSharpSymbolUse>() :> IDictionary<_,_> with get, set
    member x.ParsedLocation = location

    member val UnusedCodeRanges : Range.range list option = None with get, set
    member val HasErrors = false with get, set

    override x.GetTagCommentsAsync(cancellationToken) =
        let tokenListToComment (tokenList: FSharpTokenInfo list, lineText: string) =
            let comment = 
                tokenList
                |> Array.ofList
                |> Array.filter(fun token -> token.CharClass = FSharpTokenCharKind.LineComment)
                |> Array.map(fun token -> lineText.[token.LeftColumn..token.RightColumn])
                |> System.String.Concat

            comment.TrimStart('/', ' ')

        async {
                match x.Tokens with
                | Some tokens ->
                    let tokensByLine =
                        tokens 
                        |> List.mapi (fun line tokenList -> line+1, tokenListToComment tokenList)
                        |> List.choose(fun (line, s) ->
                                            specialCommentTags 
                                            |> Set.tryFind(fun tag -> s.StartsWith tag)
                                            |> Option.bind(fun t -> Some <| Tag(t, s, DocumentRegion(line, 1, line, 1))))

                    return ResizeArray(tokensByLine) :> IReadOnlyList<_>
                | None -> return ResizeArray() :> IReadOnlyList<_>
        }
        |> StartAsyncAsTask cancellationToken

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
