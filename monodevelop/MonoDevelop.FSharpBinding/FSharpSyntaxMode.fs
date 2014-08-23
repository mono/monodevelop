namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open MonoDevelop.Ide
open MonoDevelop.Core
//open MonoDevelop.Projects.Dom.Parser
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding

module FSharpSyntaxModeInternals =
    let contains predicate sequence =
        sequence |> Seq.exists predicate
        
    type System.String with
        member this.IsAt (idx, pattern:string) =
            let strVal = this
            let mutable result = true
            if (idx + pattern.Length > strVal.Length) then
                false
            else
                for i in [0..pattern.Length-1] do
                    let patPart = pattern.[i]
                    let strValPart = strVal.[idx + i]
                    if (patPart <> strValPart) then
                        result <- false
                result

    type AbstractBlockSpan (isValid) =
        inherit Span()
        let mutable disabled = false
        member private this.setColor() =
            base.TagColor <- "Preprocessor"
            if disabled || not isValid then
                base.Color <- "Excluded Code"
                base.Rule <- "PreProcessorComment"
            else
                base.Color <- "Plain Text"
                base.Rule <- "<root>"
        member this.IsValid with get() = isValid
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


    type IfBlockSpan(isValid) =
        inherit AbstractBlockSpan(isValid)
        override this.ToString() =
            sprintf "[IfBlockSpan: IsValid: %b, Disabled: %b, Color: %s, Rule: %s]" isValid this.Disabled this.Color this.Rule

    type ElseBlockSpan(isValid) =
        inherit AbstractBlockSpan(isValid)
        do
            base.Begin <- Regex("#else")
        override this.ToString() =
            sprintf "[ElseBlockSpan: IsValid: %b, Disabled: %b, Color: %s, Rule: %s]" isValid this.Disabled this.Color this.Rule

    type CommentBlockSpan() as this =
        inherit Span()
        do 
            this.Rule <- "Comment"
            this.Begin <- Regex("(*")
            this.End <- Regex("*)")
            this.Color <- "Comment(Block)"

    type FSharpSpanParser(mode:SyntaxMode, spanStack, defines: string seq) as this =
        inherit SyntaxMode.SpanParser(mode, spanStack)
        do
            mode.Rules |> Seq.iter this.RuleStack.Push
            LoggingService.LogInfo("Creating FSharpSpanParser()")
        
        let isDisabledSpan (span: Span) =
            let spanExists = span <> null 
            if (not spanExists) then
                false
            else
                typeof<AbstractBlockSpan>.IsAssignableFrom(span.GetType()) && 
                (span :?> AbstractBlockSpan).Disabled

        member private this.CreatePreProcessorSpan() =
            Span(TagColor="Preprocessor", Color="Preprocessor", Rule="String",StopAtEol=true)

        
                            
        member private this.ScanPreProcessorElse(i: byref<int>) =
            if not (spanStack |> contains (fun span -> span.GetType() = typeof<IfBlockSpan>)) then
                this.ScanSpan (&i) |> ignore
                ()
            let mutable previousResult = false
            for span in spanStack do
                    if span.GetType() = typeof<IfBlockSpan> then
                        previousResult <- (span :?> IfBlockSpan).IsValid
            while spanStack.Count > 0 do
                spanStack.Pop() |> ignore
            let elseBlockSpan = ElseBlockSpan(true)
            if typeof<IfBlockSpan>.IsAssignableFrom(this.CurSpan.GetType()) then
                elseBlockSpan.Disabled <- not (this.CurSpan :?> IfBlockSpan).Disabled
                this.FoundSpanEnd.Invoke(this.CurSpan, i, 0)
            this.FoundSpanBegin.Invoke(elseBlockSpan, i , "#else".Length)
            i <- i + "#else".Length
            let preprocessorSpan = this.CreatePreProcessorSpan()
            this.FoundSpanBegin.Invoke(preprocessorSpan, i, 0)

        member private this.ScanPreProcessorIf(textOffset:int, i: byref<int>) =
            let mutable endIdx = this.CurText.Length
            let mutable idx = 0
            while ((idx <- this.CurText.IndexOf('/', idx)); idx >= 0) && idx + 1 < this.CurText.Length do
                let next = this.CurText.[idx + 1]
                if next = '/' then
                    endIdx <- idx - 1
                idx <- idx + 1
            let len = endIdx - textOffset
            let parameter = this.CurText.Substring(textOffset + 3, len - 3)
            let existsInDefines = defines |> Seq.exists(fun t->t = parameter.Trim())
            let span = IfBlockSpan(true)
            span.Disabled <- not existsInDefines
            this.FoundSpanBegin.Invoke(span, i,len)
            i <- i + endIdx



        override this.ScanSpan( i) : bool =
            let textOffset = i - this.StartOffset
            let currentText = this.CurText
            if (textOffset < this.CurText.Length && this.CurRule.Name <> "Comment" &&
                this.CurRule.Name <> "String" && this.CurRule.Name <> "VerbatimString" &&
                this.CurText.[textOffset] = '#' && this.IsFirstNonWsChar(textOffset)) then
                
                if currentText.IsAt(textOffset, "#else") then
                    this.ScanPreProcessorElse(&i)
                    true
                elif currentText.IsAt(textOffset, "#if") then
                    this.ScanPreProcessorIf(textOffset,&i)
                    false      
                elif currentText.IsAt(textOffset,"#endif") then
                    let span = EndIfBlockSpan()
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

    let (|ExcludedCode|StringCode|PreProcessorCode|CommentCode|OtherCode|) (document: TextDocument,line: DocumentLine) =
        let docText = document.GetTextAt(line.Segment)
        if docText.StartsWith("#if") || docText.StartsWith("#else") || docText.StartsWith("#endif") then
            PreProcessorCode
        elif (hasSpans(line)) then
            if not (lineStartsWithAbstractBlockSpan line.StartSpan)  then
                if (containsAbstractBlockSpan line.StartSpan) &&
                    (getAbstractBlockSpan line.StartSpan).Disabled  then
                    ExcludedCode
                elif hasSpans(line) && line.StartSpan.Peek().Rule = "String" || line.StartSpan.Peek().Rule = "VerbatimString" || line.StartSpan.Peek().Rule = "TripleQuotedString" then
                    StringCode
                elif hasSpans(line) && (line.StartSpan.Peek().Rule = "Comment" || line.StartSpan.Peek().Rule = "MultiComment") then
                    CommentCode
                else OtherCode
            elif containsAbstractBlockSpan line.StartSpan &&
               (getAbstractBlockSpan line.StartSpan).Disabled then
                ExcludedCode
            else
                OtherCode
        else OtherCode

module Seq =
    let headOrDefault (defaultVal) sequence = 
        if (sequence |> Seq.isEmpty) then
            defaultVal
        else    
            sequence |> Seq.head


/// Implements syntax highlighting for F# sources
/// Currently, this just loads the keyword-based highlighting info from resources
type FSharpSyntaxMode() as this =
    inherit SyntaxMode()
    let mutable document : MonoDevelop.Ide.Gui.Document = null
    let mutable parsedDocument : FSharp.CompilerBinding.ParseAndCheckResults option = None
    let mutable sourceTokenizer: SourceTokenizer option = None
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
        let view = doc.Annotation<MonoDevelop.SourceEditor.SourceEditorView>()

        let mutable project: MonoDevelop.Projects.Project = null
                        
        if view <> null then
            project <- view.Project
        if project = null then
            let ideDocument = IdeApp.Workbench.GetDocument(doc.FileName)
            if ideDocument <> null then
                project <- ideDocument.Project
        if project = null then
            project <- IdeApp.Workspace.GetProjectsContainingFile(doc.FileName) |> Seq.headOrDefault (null)
        project

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
                sourceTokenizer <- Some(SourceTokenizer(getDefineSymbols(getProject(this.Document)) |> List.ofSeq, document.FileName.FileName))
                this.UpdateDocumentHighlighting()
                IdeApp.Workbench.ActiveDocument.Editor.Parent.TextViewMargin.PurgeLayoutCache()
                IdeApp.Workbench.ActiveDocument.Editor.Parent.QueueDraw()
            
    let makeChunk (lineNumber: int) (style: ColorScheme) (offset:int) (lineText:string) (token: TokenInformation) =
        let mcInternal (cs:ChunkStyle) = 
            Chunk(offset + token.LeftColumn, token.FullMatchedLength, cs.Name)
        
        let symbol = 
            if not (isSimpleToken token.ColorClass) then
                maybe {
                    let! parsedDocument = parsedDocument
                    return! asyncMaybe 
                                {
                                    let! symbol = parsedDocument.GetSymbol(lineNumber, token.LeftColumn, lineText)
                                    return symbol
                                } |> Async.RunSynchronously
                }
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

    let mutable m_HasRegisteredParsedEvent = false

    

    do
        LoggingService.LogInfo("Creating FSharpSyntaxMode()")
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

    member private this.HasRegisteredParsedEvent 
        with get() = m_HasRegisteredParsedEvent
        and set(value) = m_HasRegisteredParsedEvent <- value

    member private this.getLexedTokens (style,line:DocumentLine,offset,length, sourceTokenizer:SourceTokenizer) =
        let lineText = this.Document.GetText(line.Segment)
        let tokenizer = sourceTokenizer.CreateLineTokenizer(lineText)
        let tokens = Seq.unfold (fun s -> match tokenizer.ScanToken(s) with
                                                | Some t, s -> Some(t,s)
                                                | _         -> None) 0L |> Array.ofSeq
        tokens |> Seq.map (makeChunk line.LineNumber style line.Segment.Offset lineText)        

    member private this.getLexedChunks (style,line: DocumentLine,offset,length) = 
        
        match (sourceTokenizer) with
        | Some(sourceTokenizer) -> 
            this.getLexedTokens(style,line,offset,length,sourceTokenizer)
        | _ -> 
            if (this.Document <> null) then
                let localSourceTokenizer = SourceTokenizer(getDefineSymbols(getProject(this.Document)) |> List.ofSeq,this.Document.FileName)
                sourceTokenizer <- Some(localSourceTokenizer)
                this.getLexedTokens(style,line,offset,length,localSourceTokenizer)
            else
                base.GetChunks(style,line,offset,length)

    override this.CreateSpanParser(line, spanStack) =
        let ss = if spanStack = null then
                            line.StartSpan.Clone()
                        else
                            spanStack
        let defines = 
                if (this.Document = null) then
                    Seq.empty<string>
                else
                    getDefineSymbols(getProject(this.Document))

        FSharpSyntaxModeInternals.FSharpSpanParser(this,ss,defines) :> SyntaxMode.SpanParser

    override this.GetChunks(style,line,offset,length) =
        try 
            let docs = IdeApp.Workbench.Documents 
            let doc = docs |> Seq.tryFind(fun t-> t.FileName.CanonicalPath.ToString() = this.Document.FileName)
            match doc with
            | None -> ()
            | Some(doc) -> 
                document <- doc
                if not this.HasRegisteredParsedEvent then
                    this.HasRegisteredParsedEvent <- true
                    document.DocumentParsed.Add(HandleDocumentParsed)
            match (this.Document,line) with
            | StringCode -> Seq.singleton(Chunk(offset,length, style.String.Name))
            | CommentCode -> Seq.singleton(Chunk(offset,length, style.CommentsMultiLine.Name))
            | ExcludedCode -> Seq.singleton(Chunk(offset,length,"Excluded Code"))
            | PreProcessorCode -> base.GetChunks(style,line,offset,length) 
            | OtherCode  -> this.getLexedChunks(style,line,offset,length)
        with
            | exn -> 
                LoggingService.LogError("Error in FSharpSyntaxMode.GetChunks", exn)
                base.GetChunks(style,line,offset,length)