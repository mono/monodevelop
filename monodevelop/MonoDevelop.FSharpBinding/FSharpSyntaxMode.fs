namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Ide
open MonoDevelop.Core
open Mono.TextEditor
open Mono.TextEditor.Highlighting
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

    type EndIfBlockSpan() as this =
        inherit Span()
        do
            this.Color <- "Preprocessor"
            this.Rule <- "PreProcessorComment"

    type IfBlockSpan() =
        inherit AbstractBlockSpan()

    type ElseBlockSpan() =
        inherit AbstractBlockSpan()
        do
            base.Begin <- Regex("#else")

    type FSharpSpanParser(mode:SyntaxMode, spanStack, defines: string seq) as this =
        inherit SyntaxMode.SpanParser(mode, spanStack)
        do
            mode.Rules |> Seq.iter this.RuleStack.Push
            LoggingService.LogInfo("Creating FSharpSpanParser()")
                            
        member private this.ScanPreProcessorElse(i: byref<int>) =
            if not (spanStack |> contains (fun span -> span.GetType() = typeof<IfBlockSpan>)) then
                i <- i + "#else".Length
                ()
            while spanStack.Count > 0 do
                spanStack.Pop() |> ignore
            let elseBlockSpan = ElseBlockSpan()
            if typeof<IfBlockSpan>.IsAssignableFrom(this.CurSpan.GetType()) then
                elseBlockSpan.Disabled <- not (this.CurSpan :?> IfBlockSpan).Disabled
                this.FoundSpanEnd.Invoke(this.CurSpan, i, 0)
            this.FoundSpanBegin.Invoke(elseBlockSpan, i , "#else".Length)
            i <- i + "#else".Length

        member private this.ScanPreProcessorIf(textOffset:int, i: byref<int>) =
            let mutable endIdx = this.CurText.Length
            let len = endIdx - textOffset
            let parameter = this.CurText.Substring(textOffset + 3, len - 3)
            let existsInDefines = defines |> Seq.exists(fun t->t = parameter.Trim())
            let span = IfBlockSpan()
            span.Disabled <- not existsInDefines
            this.FoundSpanBegin.Invoke(span, i,len)
            i <- i + endIdx



        override this.ScanSpan( i) : bool =
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
                    if this.CurSpan <> null && typeof<IfBlockSpan>.IsAssignableFrom(this.CurSpan.GetType()) then
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

[<AutoOpen>]
module internal Patterns =
    type TokenSymbol =
        {
            TokenColor: TokenColorKind
            SymbolUse: FSharpSymbolUse option
        }
    
    let (|Keyword|_|) ts =
        if (ts.TokenColor) = TokenColorKind.Keyword then Some(Keyword)
        else None
    
    let (|Comment|_|) ts =
        if ts.TokenColor = TokenColorKind.Comment then Some(Comment)
        else None

    let (|StringLiteral|_|) ts =
        if ts.TokenColor = TokenColorKind.String then Some(StringLiteral)
        else None

    let (|NumberLiteral|_|) ts =
        if ts.TokenColor = TokenColorKind.Number then Some(NumberLiteral)
        else None

    let (|PreprocessorKeyword|_|) ts =
        if ts.TokenColor = TokenColorKind.PreprocessorKeyword then Some(PreprocessorKeyword)
        else None

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
        
    let (|ClassName|_|) ts =
        if isIdentifier ts.TokenColor then
            match ts.SymbolUse with
            | Some symbolUse -> 
                match symbolUse.Symbol with
                | :? FSharpEntity as fse ->
                        match fse with
                        | _ when fse.IsFSharpModule -> Some(ClassName)
                        | _ when fse.IsEnum         -> Some(ClassName)
                        | _ when fse.IsValueType    -> Some(ClassName)
                        | _                         -> Some(ClassName)
                | :? FSharpMemberFunctionOrValue as fsm when fsm.CompiledName = ".ctor" && fsm.LogicalEnclosingEntity.IsClass = true -> Some(ClassName)
                | _ -> None
            | None -> None
        else None

    let (|ComputationExpression|_|) ts =
        if isIdentifier ts.TokenColor then
            match ts.SymbolUse with
            | Some symbolUse -> 
                match symbolUse.IsFromComputationExpression with
                | true -> Some(ComputationExpression)
                | false -> None
            | None -> None
        else
            None

    let (|UnusedCode|_|) ts = 
        if ts.TokenColor = TokenColorKind.InactiveCode then
            Some(UnusedCode)
        else
            None

    let isAbstractBlockSpan (span: Span) =
        typeof<FSharpSyntaxModeInternals.AbstractBlockSpan>.IsAssignableFrom(span.GetType())

    let lineStartsWithAbstractBlockSpan (spanStack: CloneableStack<Span>) =
        if (spanStack = null) || spanStack.Count = 0 then
            false
        else
            let headSpan = spanStack.Peek() 
            (headSpan |> isAbstractBlockSpan) 

    let containsAbstractBlockSpan (spanStack: CloneableStack<Span>) =
        if (spanStack = null) then
            false
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

module Seq =
    let headOrDefault (defaultVal) sequence = 
        if (sequence |> Seq.isEmpty) then
            defaultVal
        else    
            sequence |> Seq.head


/// Implements syntax highlighting for F# sources
/// Currently, this just loads the keyword-based highlighting info from resources
type FSharpSyntaxMode(document: MonoDevelop.Ide.Gui.Document) as this =
    inherit SyntaxMode()
    // Mutable Local Variables
    let mutable parsedDocument : FSharp.CompilerBinding.ParseAndCheckResults option = None
    let mutable semanticHighlightingEnabled = PropertyService.Get ("EnableSemanticHighlighting", true);
    let mutable symbolsInFile: FSharpSymbolUse[] option = None
    

    let HandlePropertyChanged (eventArgs: PropertyChangedEventArgs) =
        if eventArgs.Key = "EnableSemanticHighlighting" then
            semanticHighlightingEnabled <- PropertyService.Get ("EnableSemanticHighlighting", true);
    
    let getDefineSymbols (project: MonoDevelop.Projects.Project) =
        seq { 
            let workspace = IdeApp.Workspace
            if (workspace = null || project = null) then
                if this.Document <> null && (this.Document.FileName.EndsWith(".fsx") || this.Document.FileName.EndsWith(".fsscript")) then
                    yield "INTERACTIVE"
                else
                    yield System.String.Empty
                yield System.String.Empty
            let configuration = project.GetConfiguration(workspace.ActiveConfiguration) :?> MonoDevelop.Projects.DotNetProjectConfiguration
            if configuration <> null then
                for s in configuration.GetDefineSymbols() do
                    yield s
        }

    
    

    let getProject (doc: TextDocument) =
        let view = if doc = null then
                     null 
                   else 
                     doc.Annotation<MonoDevelop.SourceEditor.SourceEditorView>()

        let mutable project: MonoDevelop.Projects.Project = null
                        
        if view <> null then
            project <- view.Project
        if project = null then
            project <- document.Project
        if project = null then
            if doc <> null then
                project <- IdeApp.Workspace.GetProjectsContainingFile(doc.FileName) |> Seq.headOrDefault (null)
        project

    let project = getProject(document.Editor.Document)
    let sourceTokenizer: SourceTokenizer = SourceTokenizer(getDefineSymbols(project) |> List.ofSeq, document.FileName.FileName)

    let HandleConfigurationChanged(eventArgs) =  
        for doc in IdeApp.Workbench.Documents do
            let data = doc.Editor
            if (data <> null && doc.Editor <> null) then
                // Force Syntax Mode Reparse
                if typeof<SyntaxMode>.IsAssignableFrom(data.Document.SyntaxMode.GetType()) then
                    let sm = data.Document.SyntaxMode :?> SyntaxMode
                    sm.UpdateDocumentHighlighting()
                    SyntaxModeService.WaitUpdate(data.Document)
                doc.Editor.Parent.TextViewMargin.PurgeLayoutCache()
                doc.ReparseDocument()
                doc.Editor.Parent.QueueDraw()

    let HandleDocumentParsed(eventArgs) =
        if document <> null && not document.IsProjectContextInUpdate then
            let localParsedDocument = document.ParsedDocument
            if localParsedDocument <> null then
                parsedDocument <- Some(localParsedDocument.Ast :?> FSharp.CompilerBinding.ParseAndCheckResults)
                Async.StartWithContinuations(
                    parsedDocument.Value.GetAllUsesOfAllSymbolsInFile(),
                    (
                        fun t -> 
                            symbolsInFile <- t
                            this.UpdateDocumentHighlighting()
                    ),
                    (fun _ -> ()),
                    (fun _ -> ()))
                
            
    let makeChunk (lineNumber: int) (style: ColorScheme) (offset:int) (token: TokenInformation) =
        let mcInternal (cs:ChunkStyle) = 
            Chunk(offset + token.LeftColumn, token.RightColumn - token.LeftColumn + 1, cs.Name)
        
        let symbol = 
            if not (isSimpleToken token.ColorClass) then
                match symbolsInFile with
                | None -> None
                | Some(symbols) -> symbols |> Seq.tryFind (fun s -> s.RangeAlternate.StartLine = lineNumber && s.RangeAlternate.StartColumn = token.LeftColumn)
            else
                None
        let tokenSymbol = { TokenSymbol.TokenColor = token.ColorClass; SymbolUse = symbol }
        let chunkStyle = 
            match tokenSymbol with
            | UnusedCode -> style.ExcludedCode
            | ComputationExpression
            | Keyword -> style.KeywordTypes
            | Comment -> style.CommentsSingleLine
            | StringLiteral -> style.String
            | NumberLiteral -> style.Number
            | ClassName -> style.UserTypes
            | PreprocessorKeyword -> style.Preprocessor
            | _ -> style.PlainText
        let chunks = mcInternal chunkStyle
        chunks

    

    do
        LoggingService.LogInfo("Creating FSharpSyntaxMode()")
        PropertyService.PropertyChanged.Add HandlePropertyChanged
        document.DocumentParsed.Add(HandleDocumentParsed)
        let provider = new ResourceStreamProvider(this.GetType().Assembly, "FSharpSyntaxMode.xml");
        use reader = provider.Open()
        let baseMode = SyntaxMode.Read(reader)
        let rules = baseMode.Rules
        this.rules <- new ResizeArray<_>(rules)
        this.keywords <- new ResizeArray<_>(baseMode.Keywords)
        this.spans <- baseMode.Spans
        this.matches <- baseMode.Matches
        this.prevMarker <- baseMode.PrevMarker
        this.SemanticRules <- new ResizeArray<_>(baseMode.SemanticRules)
        this.keywordTable <- baseMode.keywordTable
        this.properties <- baseMode.Properties
        this.keywords <- baseMode.keywords
        if IdeApp.Workspace <> null then
            IdeApp.Workspace.ActiveConfigurationChanged.Add HandleConfigurationChanged

    member private this.getLexedTokens (style,line:DocumentLine,offset, lineText) =
        let tokenizer = sourceTokenizer.CreateLineTokenizer(lineText)
        Seq.unfold (fun s -> match tokenizer.ScanToken(s) with
                             | Some t, s -> Some(t,s)
                             | _         -> None) 0L 
        |> Seq.map (makeChunk line.LineNumber style offset)
         
    member private this.getLexedChunks (style,line: DocumentLine,offset, lineText) = 
        this.getLexedTokens(style,line,offset,lineText)

    override this.CreateSpanParser(line, spanStack) =
        let ss = if spanStack = null then
                            line.StartSpan.Clone()
                        else
                            spanStack
        let defines = 
                if (this.Document = null) then
                    Seq.empty<string>
                else
                    getDefineSymbols(project)

        FSharpSyntaxModeInternals.FSharpSpanParser(this,ss,defines) :> SyntaxMode.SpanParser

    override this.GetChunks(style,line,offset,length) =
        try 
            match (this.Document,line,offset,length,style) with
            | StringCode styleName when semanticHighlightingEnabled -> Seq.singleton(Chunk(offset,length, styleName))
            | CommentCode styleName -> Seq.singleton(Chunk(offset,length, styleName))
            | ExcludedCode styleName -> Seq.singleton(Chunk(offset,length,styleName))
            | PreProcessorCode styleName -> base.GetChunks(style,line,offset,length) 
            | OtherCode (styleName,lineText) when semanticHighlightingEnabled -> this.getLexedChunks(style,line,offset,lineText)
            | _ -> base.GetChunks(style,line,offset,length)
        with
            | exn -> 
                LoggingService.LogError("Error in FSharpSyntaxMode.GetChunks", exn)
                base.GetChunks(style,line,offset,length)