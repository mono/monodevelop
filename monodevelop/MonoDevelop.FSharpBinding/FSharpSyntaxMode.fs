namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Ide
open MonoDevelop.Core
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding

module FSharpSyntaxModeInternals =
    let contains predicate sequence =
        sequence |> Seq.exists predicate

    type AbstractBlockSpan () =
        inherit Span()
        let mutable disabled = false
        member private this.setColor() =
            base.TagColor <- "Preprocessor"
            if disabled then
                base.Color <- "Excluded Code"
                base.Rule <- "PreProcessorComment"
            else
                base.Color <- "Plain Text"
                base.Rule <- "<root>"
        member this.Disabled
            with get() =
                    disabled
            and set(value) =
                disabled <- value
                this.setColor()

    type EndIfBlockSpan() =
        inherit Span(Color = "Preprocessor", Rule = "PreProcessorComment")

    type IfBlockSpan() =
        inherit AbstractBlockSpan()

    type ElseBlockSpan() =
        inherit AbstractBlockSpan(Begin = Regex("#else"))

    type FSharpSpanParser(mode:SyntaxMode, spanStack, defines: string list) =
        inherit SyntaxMode.SpanParser(mode, spanStack)
        do LoggingService.LogDebug ("Creating FSharpSpanParser()")

        member private this.ScanPreProcessorElse(i: byref<int>) =
            if not (spanStack |> contains (fun span -> span.GetType() = typeof<IfBlockSpan>)) then
                i <- i + "#else".Length

            while spanStack.Count > 0 do
                spanStack.Pop() |> ignore
            let elseBlockSpan = ElseBlockSpan()
            if this.CurSpan <> null && typeof<AbstractBlockSpan>.IsAssignableFrom(this.CurSpan.GetType()) then
                elseBlockSpan.Disabled <- not (this.CurSpan :?> AbstractBlockSpan).Disabled
                this.FoundSpanEnd.Invoke(this.CurSpan, i, 0)
            this.FoundSpanBegin.Invoke(elseBlockSpan, i , "#else".Length)
            i <- i + "#else".Length

        member private this.ScanPreProcessorIf(textOffset:int, i: byref<int>) =
            let endIdx = this.CurText.Length
            let len = endIdx - textOffset
            let parameter = this.CurText.Substring(textOffset + 3, len - 3)
            let existsInDefines = defines |> List.exists(fun t->t = parameter.Trim())
            let span = IfBlockSpan()
            span.Disabled <- not existsInDefines
            this.FoundSpanBegin.Invoke(span, i,len)
            i <- i + endIdx

        override this.ScanSpan( i) : bool =
            try 
                let textOffset = i - this.StartOffset
                let currentText = this.CurText
                if (textOffset < this.CurText.Length && this.CurRule.Name <> "Comment" &&
                    this.CurRule.Name <> "String" && this.CurRule.Name <> "VerbatimString" &&
                    this.CurText.[textOffset] = '#' && this.IsFirstNonWsChar(textOffset)) then

                    if currentText.Substring(textOffset).StartsWith("#else") then
                        this.ScanPreProcessorElse(&i)
                        true
                    elif currentText.Substring(textOffset).StartsWith("#if") then
                        this.ScanPreProcessorIf(textOffset,&i)
                        false
                    elif currentText.Substring(textOffset).StartsWith("#endif") then
                        let span = EndIfBlockSpan()
                        if this.CurSpan <> null && typeof<AbstractBlockSpan>.IsAssignableFrom(this.CurSpan.GetType()) then
                            this.FoundSpanEnd.Invoke(this.CurSpan, i, 0)
                        this.FoundSpanBegin.Invoke(span, i + textOffset, 6)
                        this.FoundSpanEnd.Invoke(span, i + textOffset + 6,0)
                        true
                    elif (this.CurSpan <> null && this.CurSpan.Color <> "Excluded Code") then
                        base.ScanSpan(&i)
                    else
                        false
                elif (this.CurSpan <> null && this.CurSpan.Color <> "Excluded Code") then
                    base.ScanSpan(&i)
                else
                    base.ScanSpan(&i)
            with
            | exn -> 
                LoggingService.LogError("An error occurred in FSharpSpanParser.ScanSpan", exn)
                base.ScanSpan(&i)


open MonoDevelop.FSharp.FSharpSymbolHelper
[<AutoOpen>]
module internal Patterns =
    type TokenSymbol = 
        {
            TokenInfo : TokenInformation;
            SymbolUse: FSharpSymbolUse option
            ExtraColorInfo: (Microsoft.FSharp.Compiler.Range.range * TokenColorKind) option
        }

    let (|Keyword|_|) ts =
        if (ts.TokenInfo.ColorClass) = TokenColorKind.Keyword || 
           (ts.ExtraColorInfo.IsSome && (snd ts.ExtraColorInfo.Value) = TokenColorKind.Keyword)  then Some(Keyword)
        else None

    let (|Comment|_|) ts =
        if ts.TokenInfo.ColorClass = TokenColorKind.Comment then Some Comment
        else None

    let (|StringLiteral|_|) ts =
        if ts.TokenInfo.ColorClass = TokenColorKind.String then Some StringLiteral
        else None

    let (|NumberLiteral|_|) ts =
        if ts.TokenInfo.ColorClass = TokenColorKind.Number then Some NumberLiteral
        else None

    let (|PreprocessorKeyword|_|) ts =
        if ts.TokenInfo.ColorClass = TokenColorKind.PreprocessorKeyword then Some PreprocessorKeyword
        else None

    let (|Punctuation|_|) (ts:TokenSymbol) =
        let token = Parser.tokenTagToTokenId ts.TokenInfo.Tag
        match token with
        | Parser.tokenId.TOKEN_PLUS_MINUS_OP
        | Parser.tokenId.TOKEN_MINUS
        | Parser.tokenId.TOKEN_STAR
        | Parser.tokenId.TOKEN_INFIX_STAR_DIV_MOD_OP
        | Parser.tokenId.TOKEN_PERCENT_OP
        | Parser.tokenId.TOKEN_INFIX_AT_HAT_OP
        | Parser.tokenId.TOKEN_QMARK
        | Parser.tokenId.TOKEN_COLON
        | Parser.tokenId.TOKEN_EQUALS
        | Parser.tokenId.TOKEN_SEMICOLON
        | Parser.tokenId.TOKEN_COMMA
        | Parser.tokenId.TOKEN_DOT
        | Parser.tokenId.TOKEN_DOT_DOT
        | Parser.tokenId.TOKEN_INT32_DOT_DOT
        | Parser.tokenId.TOKEN_UNDERSCORE
        | Parser.tokenId.TOKEN_BAR
        | Parser.tokenId.TOKEN_BAR_RBRACK
        | Parser.tokenId.TOKEN_LBRACK_LESS
        | Parser.tokenId.TOKEN_COLON_GREATER
        | Parser.tokenId.TOKEN_COLON_QMARK_GREATER
        | Parser.tokenId.TOKEN_COLON_QMARK
        | Parser.tokenId.TOKEN_INFIX_BAR_OP
        | Parser.tokenId.TOKEN_INFIX_COMPARE_OP
        | Parser.tokenId.TOKEN_COLON_COLON
        | Parser.tokenId.TOKEN_AMP_AMP
        | Parser.tokenId.TOKEN_PREFIX_OP
        | Parser.tokenId.TOKEN_COLON_EQUALS
        | Parser.tokenId.TOKEN_BAR_BAR
            -> Some Punctuation
        | _ -> None

    let (|PunctuationBrackets|_|) (ts:TokenSymbol) =
        let token = Parser.tokenTagToTokenId ts.TokenInfo.Tag
        match token with
        | Parser.tokenId.TOKEN_LPAREN
        | Parser.tokenId.TOKEN_RPAREN
        | Parser.tokenId.TOKEN_LBRACK
        | Parser.tokenId.TOKEN_RBRACK
        | Parser.tokenId.TOKEN_LBRACE
        | Parser.tokenId.TOKEN_RBRACE 
        | Parser.tokenId.TOKEN_LBRACK_LESS
        | Parser.tokenId.TOKEN_GREATER_RBRACK
        | Parser.tokenId.TOKEN_LESS
        | Parser.tokenId.TOKEN_GREATER
        | Parser.tokenId.TOKEN_LBRACK_BAR
        | Parser.tokenId.TOKEN_BAR_RBRACK -> Some PunctuationBrackets
        | _ -> None

    let private isIdentifier =
        function
        | TokenColorKind.Identifier
        | TokenColorKind.UpperIdentifier -> true
        | _ -> false

    let isSimpleToken tck =
        match tck with
        | TokenColorKind.Identifier
        | TokenColorKind.UpperIdentifier -> false
        | _ -> true

    let (|IdentifierSymbol|_|) ts =
        if isIdentifier ts.TokenInfo.ColorClass && ts.SymbolUse.IsSome then
            IdentifierSymbol(ts.SymbolUse.Value) |> Some
        else None

    let (|Namespace|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Namespace -> Some Namespace
            | _ -> None
        | _ -> None

    let (|Class|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Class -> Some Class
            | _ -> None
        | _ -> None

    let (|Property|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Property -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Field|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | CorePatterns.Field _ -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Function|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse with
            | ExtendedPatterns.Function
            | ExtendedPatterns.ClosureOrNested ->  Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Val|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse with
            | ExtendedPatterns.Val -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Delegate|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Delegate -> Some Delegate
            | _ -> None
        | _ -> None

    let (|Event|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse ->
            match symbolUse.Symbol with
            | ExtendedPatterns.Event -> Some symbolUse.IsFromDefinition
            | _ -> None
        | _ -> None

    let (|Enum|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Enum -> Some Enum
            | _ -> None
        | _ -> None

    let (|Record|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Record -> Some Record
            | _ -> None
        | _ -> None

    let (|ValueType|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.ValueType -> Some ValueType
            | _ -> None
        | _ -> None

    let (|Module|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Module -> Some Module
            | _ -> None
        | _ -> None

    let (|Union|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Union -> Some Union
            | _ -> None
        | _ -> None

    let (|GenericParameter|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | CorePatterns.GenericParameter _ -> Some GenericParameter
            | _ -> None
        | _ -> None

    let (|UnionCase|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | CorePatterns.UnionCase _ -> Some UnionCase
            | _ -> None
        | _ -> None

    let (|ActivePatternCase|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | CorePatterns.ActivePatternCase _ -> Some ActivePatternCase
            | _ -> None
        | _ -> None

    let (|Interface|_|) ts =
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.Interface _ -> Some Interface
            | _ -> None
        | _ -> None

    let (|TypeAbbreviation|_|) ts = 
        match ts with
        | IdentifierSymbol symbolUse -> 
            match symbolUse.Symbol with
            | ExtendedPatterns.TypeAbbreviation _ -> Some TypeAbbreviation
            | _ -> None
        | _ -> None

    let (|ComputationExpression|_|) ts =
        if isIdentifier ts.TokenInfo.ColorClass then
            match ts.SymbolUse with
            | Some symbolUse when symbolUse.IsFromComputationExpression -> Some ComputationExpression
            | _ -> None
        else None

    let (|UnusedCode|_|) ts =
        if ts.TokenInfo.ColorClass = TokenColorKind.InactiveCode then Some UnusedCode
        else None

    let isAbstractBlockSpan (span: Span) =
        typeof<FSharpSyntaxModeInternals.AbstractBlockSpan>.IsAssignableFrom(span.GetType())

    let lineStartsWithAbstractBlockSpan (spanStack: CloneableStack<Span>) =
        if spanStack = null || spanStack.Count = 0 then false
        else
            let headSpan = spanStack.Peek()
            headSpan |> isAbstractBlockSpan

    let containsAbstractBlockSpan (spanStack: CloneableStack<Span>) =
        if (spanStack = null) then false
        else
            let cloned = spanStack.Clone()
            cloned
            |> List.ofSeq
            |> List.exists isAbstractBlockSpan

    let getAbstractBlockSpan (spanStack: CloneableStack<Span>) =
        let cloned = spanStack.Clone()
        cloned
        |> Seq.find isAbstractBlockSpan :?> FSharpSyntaxModeInternals.AbstractBlockSpan

    let hasSpans (line: DocumentLine) =
        line <> null && line.StartSpan <> null && line.StartSpan.Count > 0

    let (|ExcludedCode|StringCode|PreProcessorCode|CommentCode|OtherCode|) (document: TextDocument,line: DocumentLine,offset,length, style: Highlighting.ColorScheme) =
        let docText = document.GetTextAt(offset,length)
        if docText.StartsWith("#if") || docText.StartsWith("#else") || docText.StartsWith("#endif") then
            PreProcessorCode style.Preprocessor.Name
        elif (hasSpans(line)) then
            if not (lineStartsWithAbstractBlockSpan line.StartSpan)  then
                if (containsAbstractBlockSpan line.StartSpan) &&
                    (getAbstractBlockSpan line.StartSpan).Disabled  then
                    ExcludedCode style.ExcludedCode.Name
                elif hasSpans(line) && line.StartSpan.Peek().Rule = "String" || line.StartSpan.Peek().Rule = "VerbatimString" || line.StartSpan.Peek().Rule = "TripleQuotedString" then
                    StringCode style.String.Name
                elif hasSpans(line) && (line.StartSpan.Peek().Rule = "Comment" || line.StartSpan.Peek().Rule = "MultiComment") then
                    CommentCode style.CommentsMultiLine.Name
                else OtherCode (style.PlainText.Name, docText)
            elif containsAbstractBlockSpan line.StartSpan &&
               (getAbstractBlockSpan line.StartSpan).Disabled then
                ExcludedCode style.ExcludedCode.Name
            else
                OtherCode (style.PlainText.Name, docText)
        else OtherCode (style.PlainText.Name, docText)

/// Implements syntax highlighting for F# sources
/// Currently, this just loads the keyword-based highlighting info from resources
type FSharpSyntaxMode(document: MonoDevelop.Ide.Gui.Document) as this =
    inherit SyntaxMode()
    // Mutable Local Variables
    let mutable semanticHighlightingEnabled = PropertyService.Get ("EnableSemanticHighlighting", true)
    let mutable symbolsInFile: FSharpSymbolUse[] option = None

    let handlePropertyChanged =
        EventHandler<PropertyChangedEventArgs>
            (fun o eventArgs -> if eventArgs.Key = "EnableSemanticHighlighting" then
                                    semanticHighlightingEnabled <- PropertyService.Get ("EnableSemanticHighlighting", true))

    let getDefineSymbols (project: MonoDevelop.Projects.Project option) =
        [ let workspace = IdeApp.Workspace
          if workspace = null then
              if this.Document <> null && (this.Document.FileName.EndsWith(".fsx") || this.Document.FileName.EndsWith(".fsscript")) then
                  yield "INTERACTIVE"
              else
                  yield "COMPILED"
          match project with
          | Some p -> match p.GetConfiguration(workspace.ActiveConfiguration) with
                      | :? MonoDevelop.Projects.DotNetProjectConfiguration as configuration ->
                          for s in configuration.GetDefineSymbols() do
                              yield s
                      | _ -> ()
          | None -> ()]

    let getProject (doc : Gui.Document) =
        if doc <> null && doc.Project <> null then Some doc.Project else
        let view = doc.Annotation<MonoDevelop.SourceEditor.SourceEditorView>()
        if view <> null && view.Project <> null then Some view.Project
        else IdeApp.Workspace.GetProjectsContainingFile(doc.FileName.ToString()) |> Seq.tryHead

    let project = getProject document

    let sourceTokenizer= SourceTokenizer(getDefineSymbols project, document.FileName.FileName)

    let handleConfigurationChanged =
        EventHandler
            (fun o e ->
                for doc in IdeApp.Workbench.Documents do
                    let data = doc.Editor
                    if data <> null then
                        // Force Syntax Mode Reparse
                        if typeof<SyntaxMode>.IsAssignableFrom(data.Document.SyntaxMode.GetType()) then
                            let sm = data.Document.SyntaxMode :?> SyntaxMode
                            sm.UpdateDocumentHighlighting()
                            SyntaxModeService.WaitUpdate(data.Document)
                        data.Parent.TextViewMargin.PurgeLayoutCache()
                        doc.ReparseDocument()
                        data.Parent.QueueDraw())

    let getAndProcessSymbols (pd:ParseAndCheckResults) =
        async {let! symbols = pd.GetAllUsesOfAllSymbolsInFile()
               do symbolsInFile <- symbols
               if document <> null &&
                  document.Editor <> null &&
                  document.Editor.Parent <> null &&
                  document.Editor.Parent.TextViewMargin <> null
               then Gtk.Application.Invoke (fun _ _ -> document.Editor.Parent.TextViewMargin.PurgeLayoutCache ()
                                                       document.Editor.Parent.QueueDraw())}
    let handleDocumentParsed =
        EventHandler
            (fun _ _ ->
                if document <> null && not document.IsProjectContextInUpdate && semanticHighlightingEnabled then
                    let localParsedDocument = document.ParsedDocument
                    if localParsedDocument <> null then
                        localParsedDocument.Ast 
                        |> tryCast<ParseAndCheckResults>
                        |> Option.iter (getAndProcessSymbols >> Async.Start))

    let makeChunk (lineNumber: int) (style: ColorScheme) (offset:int) (extraColorInfo: (Microsoft.FSharp.Compiler.Range.range * TokenColorKind)[] option) (token: TokenInformation) =
        let symbol =
            if isSimpleToken token.ColorClass then None else
            match symbolsInFile with
            | None -> None
            | Some(symbols) ->
                symbols
                |> Seq.tryFind (fun s -> s.RangeAlternate.StartLine = lineNumber && s.RangeAlternate.EndColumn = token.RightColumn+1)

        let extraColor =
            match extraColorInfo with
            | None -> None
            | Some(extraColourInfo) ->
                extraColourInfo
                |> Array.tryFind (fun (rng, _) -> rng.StartLine = lineNumber && rng.EndColumn = token.RightColumn+1)

        let tokenSymbol = { TokenInfo = token; SymbolUse = symbol; ExtraColorInfo = extraColor }
        let chunkStyle =
            match tokenSymbol with
            | UnusedCode -> style.ExcludedCode
            | ComputationExpression
            | Keyword -> style.KeywordTypes
            | Comment -> style.CommentsSingleLine
            | StringLiteral -> style.String
            | NumberLiteral -> style.Number
            | PunctuationBrackets -> style.PunctuationForBrackets
            | Punctuation -> style.Punctuation
            | Module|ActivePatternCase|Record|Union|TypeAbbreviation|Class -> style.UserTypes
            | Namespace -> style.KeywordNamespace
            | Property fromDef -> if fromDef then style.UserPropertyDeclaration else style.UserPropertyUsage
            | Field fromDef -> if fromDef then style.UserFieldDeclaration else style.UserFieldUsage
            | Function fromDef -> if fromDef then style.UserMethodDeclaration else style.UserMethodUsage
            | Val fromDef -> if fromDef then style.UserFieldDeclaration else style.UserFieldUsage
            | UnionCase | Enum -> style.UserTypesEnums
            | Delegate -> style.UserTypesDelegatess
            | Event fromDef -> if fromDef then style.UserEventDeclaration else style.UserEventUsage
            | Interface -> style.UserTypesInterfaces
            | ValueType -> style.UserTypesValueTypes
            | PreprocessorKeyword -> style.Preprocessor
            | _ -> style.PlainText
        let chunks = Chunk(offset + token.LeftColumn, token.RightColumn - token.LeftColumn + 1, chunkStyle.Name)
        chunks

    let getLexedTokens (style, line:DocumentLine, offset, lineText, extraColorInfo) =
        let tokenizer = sourceTokenizer.CreateLineTokenizer(lineText)
        Seq.unfold (fun s -> match tokenizer.ScanToken(s) with
                             | Some t, s -> Some(t,s)
                             | _         -> None) 0L
        |> Seq.map (makeChunk line.LineNumber style offset extraColorInfo)

    do
        LoggingService.LogDebug ("Creating FSharpSyntaxMode()")
        let provider = ResourceStreamProvider(this.GetType().Assembly, "FSharpSyntaxMode.xml")
        use reader = provider.Open()
        let baseMode = SyntaxMode.Read(reader)
        let rules = baseMode.Rules
        this.rules <- ResizeArray<_>(rules)
        this.keywords <- ResizeArray<_>(baseMode.Keywords)
        this.spans <- baseMode.Spans
        this.matches <- baseMode.Matches
        this.prevMarker <- baseMode.PrevMarker
        this.SemanticRules <- ResizeArray<_>(baseMode.SemanticRules)
        this.keywordTable <- baseMode.keywordTable
        this.properties <- baseMode.Properties
        this.keywords <- baseMode.keywords

    let propertyChangedHandler = PropertyService.PropertyChanged.Subscribe handlePropertyChanged
    let documentParsedHandler = document.DocumentParsed.Subscribe handleDocumentParsed
    let configHandler =
        maybe { let! workspace = IdeApp.Workspace |> Option.ofNull
                return workspace.ActiveConfigurationChanged.Subscribe handleConfigurationChanged }

    override this.CreateSpanParser(line, spanStack) =
        let ss =
            if spanStack = null then line.StartSpan.Clone()
            else spanStack

        let defines =
            if (this.Document = null) then List.empty
            else getDefineSymbols(project)

        FSharpSyntaxModeInternals.FSharpSpanParser(this,ss,defines) :> SyntaxMode.SpanParser

    override this.GetChunks(style, line, offset, length) =
        try
            let extraColorInfo =
                maybe { let! document = Option.ofNull document
                        let! parsedDocument = Option.ofNull document.ParsedDocument
                        let! pc = tryCast<ParseAndCheckResults> document.ParsedDocument.Ast
                        return! pc.GetExtraColorizations() }

            match (this.Document, line, offset, length, style) with
            | ExcludedCode styleName -> Seq.singleton(Chunk(offset,length,styleName))
            | PreProcessorCode _ -> base.GetChunks(style,line,offset,length)
            | OtherCode (_,lineText) when semanticHighlightingEnabled -> getLexedTokens(style,line,offset,lineText,extraColorInfo)
            | _ -> base.GetChunks(style,line,offset,length)
        with
            | exn ->
                LoggingService.LogError("Error in FSharpSyntaxMode.GetChunks", exn)
                base.GetChunks(style,line,offset,length)

    interface IDisposable with
        member x.Dispose () =
            propertyChangedHandler.Dispose ()
            documentParsedHandler.Dispose ()
            configHandler |> Option.iter (fun ch -> ch.Dispose ())