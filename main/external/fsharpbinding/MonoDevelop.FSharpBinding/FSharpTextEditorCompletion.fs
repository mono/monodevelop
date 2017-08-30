// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Threading.Tasks
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Core.Text
open MonoDevelop.FSharp.Shared
open MonoDevelop.Ide
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.TypeSystem
open ExtCore.Control

type FSharpMemberCompletionData(name, icon, symbol:FSharpSymbolUse, overloads:FSharpSymbolUse list) =
    inherit CompletionData(CompletionText = PrettyNaming.QuoteIdentifierIfNeeded name,
                           DisplayText = name,
                           DisplayFlags = DisplayFlags.DescriptionHasMarkup,
                           Icon = icon)

    /// Check if the datatip has multiple overloads
    override x.HasOverloads = not (List.isEmpty overloads)
    override x.GetRightSideDescription _selected =
        let formatType (t:FSharpType) =
            try "<small>" + syntaxHighlight (t.Format symbol.DisplayContext) + "</small>"
            with ex -> ""
        SymbolTooltips.returnType symbol
        |> Option.map formatType
        |> Option.fill ""

    /// Split apart the elements into separate overloads
    override x.OverloadedData =
        overloads
        |> List.map (fun symbol -> FSharpMemberCompletionData(name, icon, symbol, []) :> CompletionData)
        |> ResizeArray.ofList :> _

    override x.CreateTooltipInformation (_smartWrap, cancel) =
        Async.StartAsTask(SymbolTooltips.getTooltipInformation symbol, cancellationToken = cancel)
    
    type SimpleCategory(text) =
        inherit CompletionCategory(text, null)
        override x.CompareTo other =
            if other = null then -1 else x.DisplayText.CompareTo other.DisplayText
           
    type Category(text, s:FSharpSymbol) =
        inherit CompletionCategory(text, null)
    
        let ancestry (e: FSharpEntity) =
            e.UnAnnotate()
            |> Seq.unfold (fun x -> x.BaseType
                                    |> Option.map (fun x -> let entity = x.TypeDefinition.UnAnnotate()
                                                            entity, entity))
            |> Seq.append (e.AllInterfaces
                           |> Seq.map (fun a -> a.TypeDefinition.UnAnnotate()))
    
        member x.Symbol = s
        override x.CompareTo other =
            match other with
            | null -> 1
            | :? Category as other ->
                match s, other.Symbol with
                | (:? FSharpEntity as aa), (:? FSharpEntity as bb) ->
                    let comparisonResult =
                        let aaAllBases = ancestry aa

                        match (aaAllBases |> Seq.tryFind (fun a -> a.IsEffectivelySameAs bb)) with
                        | Some _ ->  -1
                        | _ ->
                            let bbAllBases = ancestry bb
                            match (bbAllBases |> Seq.tryFind (fun a -> a.IsEffectivelySameAs aa)) with
                            | Some _ ->  1
                            | _ -> aa.DisplayName.CompareTo(bb.DisplayName)

                    comparisonResult
                | a, b -> a.DisplayName.CompareTo(b.DisplayName)
            | _ -> -1

type FsiMemberCompletionData(displayText, completionText, icon) =
    inherit CompletionData(CompletionText = completionText,
                           DisplayText = displayText,
                           DisplayFlags = DisplayFlags.DescriptionHasMarkup,
                           Icon = icon)

    override x.CreateTooltipInformation (_smartWrap, cancel) =
        match FSharpInteractivePad.Fsi with
        | Some pad ->
            match pad.Session with
            | Some session ->              
                // get completions from remote fsi process
                pad.RequestTooltip displayText

                let computation =
                    async {
                        let! tooltip = Async.AwaitEvent (session.TooltipReceived)
                        match tooltip with
                        | MonoDevelop.FSharp.Shared.ToolTips.ToolTip (signature, xmldoc, footer) ->
                            let! tooltipInfo = SymbolTooltips.getTooltipInformationFromTip (signature, xmldoc, footer)
                            return tooltipInfo
                        | MonoDevelop.FSharp.Shared.ToolTips.EmptyTip ->
                            return TooltipInformation()
                    }
                Async.StartAsTask(computation, cancellationToken = cancel)
            | _ -> Task.FromResult (TooltipInformation())
        | _ -> Task.FromResult (TooltipInformation())

module Completion = 
    type Context = { 
        completionChar: char
        lineToCaret: string
        editor: TextEditor
        documentContext: DocumentContext
        triggerOffset: int
        column: int
        line: int
        ctrlSpace: bool
    }

    let (|InvalidToken|_|) context =
        let token = Tokens.getTokenAtPoint context.editor context.editor.DocumentContext context.triggerOffset
        if Tokens.isInvalidCompletionToken token then
            Some InvalidToken
        else
            None

    let (|InsideBlockComment|_|) context =
        let leftCommentDelimiter = context.editor.Text.LastIndexOf("(*", context.triggerOffset)
        if leftCommentDelimiter = -1 then
            None
        else
            let rightCommentDelimiter = context.editor.Text.IndexOf("*)", leftCommentDelimiter)
            if rightCommentDelimiter = -1 || rightCommentDelimiter > context.triggerOffset then
                Some InsideBlockComment
            else
                None

    let (|InvalidCompletionChar|_|) context =
        if Char.IsLetter context.completionChar || context.ctrlSpace || context.completionChar = '.' || context.completionChar = '#' then
            None
        else
            Some InvalidCompletionChar

    let (|LiteralNumber|_|) context =
        if Regex.IsMatch(context.lineToCaret, "\s?[0-9]+[\w.]*$", RegexOptions.Compiled) then
            Some LiteralNumber
        else
            None

    let (|FunctionIdentifier|_|) context =
        if Regex.IsMatch(context.lineToCaret, "\s?(fun)\s+[^-]+$", RegexOptions.Compiled) then
            Some FunctionIdentifier
        else
            None

    let (|ModuleOrTypeIdentifier|_|) context =
        if Regex.IsMatch(context.lineToCaret, "\s?(module|type)\s+[^=]+$", RegexOptions.Compiled) then
            Some ModuleOrTypeIdentifier
        else
            None

    let (|DoubleDot|_|) context =
        if Regex.IsMatch(context.lineToCaret, "\[[^\]]+\.+$", RegexOptions.Compiled) then
            Some DoubleDot
        else
            None
    
    let (|Attribute|_|) context =
        if Regex.IsMatch(context.lineToCaret, "\[<\w+$", RegexOptions.Compiled) then
            Some Attribute
        else
            None

    let (|FilePath|_|) context =
        let matches = Regex.Matches(context.lineToCaret, "^\s*#(load|r)\s+@*\"([^\"]*)$", RegexOptions.Compiled)
        if matches.Count > 0 then
            Some (matches.[0].Groups.[1].Value, matches.[0].Groups.[2].Value.Replace(@"\\", @"\"))
        else
            None

    let (|LetIdentifier|_|) context =
        if Regex.IsMatch(context.lineToCaret, "\s?(let!?|override|member|for)\s+[^=:]+$", RegexOptions.Compiled) then
             Some LetIdentifier
        else
            None

    let symbolToIcon (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | SymbolUse.ActivePatternCase _ -> Stock.Enum
        | SymbolUse.Field _ -> Stock.Field
        | SymbolUse.UnionCase _ -> IconId("md-type")
        | SymbolUse.Class _ -> Stock.Class
        | SymbolUse.Delegate _ -> Stock.Delegate
        | SymbolUse.Constructor _  -> Stock.Method
        | SymbolUse.Event _ -> Stock.Event
        | SymbolUse.Property _ -> Stock.Property
        | Function f ->
            if f.IsExtensionMember then IconId("md-extensionmethod")
            elif f.IsMember then IconId("md-method")
            else IconId("md-fs-field")
        | SymbolUse.Operator _ -> IconId("md-fs-field")
        | SymbolUse.ClosureOrNestedFunction _ -> IconId("md-fs-field")
        | SymbolUse.Val _ -> Stock.Field
        | SymbolUse.Enum _ -> Stock.Enum
        | SymbolUse.Interface _ -> Stock.Interface
        | SymbolUse.Module _ -> IconId("md-module")
        | SymbolUse.Namespace _ -> Stock.NameSpace
        | SymbolUse.Record _ -> Stock.Class
        | SymbolUse.Union _ -> IconId("md-type")
        | SymbolUse.ValueType _ -> Stock.Struct
        | SymbolUse.Entity _ -> IconId("md-type")
        | _ -> Stock.Event
        
    let symbolStringToIcon icon =
        match icon with
        | "ActivePatternCase" -> Stock.Enum
        | "Field" -> Stock.Field
        | "UnionCase" -> IconId("md-type")
        | "Class" -> Stock.Class
        | "Delegate" -> Stock.Delegate
        | "Constructor" -> Stock.Method
        | "Event" -> Stock.Event
        | "Property" -> Stock.Property
        | "ExtensionMethod" -> IconId("md-extensionmethod")
        | "Method" -> IconId("md-method")
        | "Operator" -> IconId("md-fs-field")
        | "ClosureOrNestedFunction" -> IconId("md-fs-field")
        | "Val" -> Stock.Field
        | "Enum" -> Stock.Enum
        | "Interface" -> Stock.Interface
        | "Module" -> IconId("md-module")
        | "Namespace" -> Stock.NameSpace
        | "Record" -> Stock.Class
        | "Union" -> IconId("md-type")
        | "ValueType" -> Stock.Struct
        | "Entity" -> IconId("md-type")
        | _ -> Stock.Event
        
    let tryGetCategory (symbolUse : FSharpSymbolUse) =
        let category =
            try
                match symbolUse with
                | SymbolUse.Constructor c ->
                    c.EnclosingEntity
                    |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                              un.DisplayName, un)
                | SymbolUse.Event ev ->
                    ev.EnclosingEntity
                    |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                              un.DisplayName, un)
                | SymbolUse.Property pr ->
                    pr.EnclosingEntity
                    |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                              un.DisplayName, un)
                | SymbolUse.ActivePatternCase ap ->
                    if ap.Group.Names.Count > 1 then
                        ap.Group.EnclosingEntity
                        |> Option.map (fun enclosing -> let un = enclosing.UnAnnotate()
                                                        un.DisplayName, un)
                    else None
                | SymbolUse.UnionCase uc ->
                    if uc.UnionCaseFields.Count > 1 then
                        let ent = uc.ReturnType.TypeDefinition.UnAnnotate()
                        Some(ent.DisplayName, ent)
                    else None
                | SymbolUse.Function f ->
                    if f.IsExtensionMember then
                        let real = f.LogicalEnclosingEntity.UnAnnotate()
                        Some(real.DisplayName, real)
                    else
                        f.EnclosingEntity
                        |> Option.map (fun real -> let un = real.UnAnnotate()
                                                   un.DisplayName, un)
                | SymbolUse.Operator o ->
                    o.EnclosingEntity
                    |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                              un.DisplayName, un)
                | SymbolUse.Pattern p ->
                    p.EnclosingEntity
                    |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                              un.DisplayName, ent)
                | SymbolUse.Val v ->
                    v.EnclosingEntity
                    |> Option.map (fun ent -> let un  = ent.UnAnnotate()
                                              un.DisplayName, un)
                | SymbolUse.TypeAbbreviation ta ->
                    //TODO:  Check this is correct, I suspect we should return None here
                    let ent = ta.UnAnnotate()
                    Some (ent.DisplayName, ent)
                //The following have no logical parent to display
                //Theres no link to a parent type for a closure (FCS limitation)
                | SymbolUse.ClosureOrNestedFunction _cl -> None
                //The F# compiler does not currently expose an Entitys parent, only children
                //| Class _ | Delegate _ | Enum _ | Interface _ | Module _
                //| Namespace _ | Record _ | Union _ | ValueType _  -> None
                | _ -> None
            with exn -> None
        category
        
    let getCompletionData (symbols:FSharpSymbolUse list list) isInsideAttribute =
        let categories = Dictionary<string, Category>()
        let getOrAddCategory symbol id =
            match categories.TryGetValue id with
            | true, item -> item
            | _ -> let cat = Category(id, symbol)
                   categories.Add (id, cat)
                   cat

        let symbolToCompletionData (symbols : FSharpSymbolUse list) =
            match symbols with
            | head :: tail ->
                let completion =
                    if isInsideAttribute then
                        match head with
                        | SymbolUse.Attribute ent ->
                            let name = ent.DisplayName
                            let name =
                                if name.EndsWith("Attribute") then
                                    name.Remove(name.Length - 9)
                                else
                                    name
                            Some (FSharpMemberCompletionData(name, symbolToIcon head, head, tail) :> CompletionData)
                        | _ -> None
                    else
                        Some (FSharpMemberCompletionData(head.Symbol.DisplayName, symbolToIcon head, head, tail) :> CompletionData)

                match tryGetCategory head, completion with
                | Some (id, ent), Some comp -> 
                    let category = getOrAddCategory ent id
                    comp.CompletionCategory <- category
                | _, _ -> ()

                completion
            | _ -> None
        
        symbols |> List.choose symbolToCompletionData

    let compilerIdentifiers =
        let icon = Stock.Literal
        let compilerIdentifierCategory = SimpleCategory "Compiler Identifiers"
        [ CompletionData("__LINE__", icon,
                         "Evaluates to the current line number, considering <tt>#line</tt> directives.",
                          CompletionCategory = compilerIdentifierCategory,
                          DisplayFlags = DisplayFlags.DescriptionHasMarkup)
          CompletionData("__SOURCE_DIRECTORY__", icon,
                         "Evaluates to the current full path of the source directory, considering <tt>#line</tt> directives.",
                          CompletionCategory = compilerIdentifierCategory,
                          DisplayFlags = DisplayFlags.DescriptionHasMarkup)
          CompletionData("__SOURCE_FILE__", icon,
                         "Evaluates to the current source file name and its path, considering <tt>#line</tt> directives.",
                          CompletionCategory = compilerIdentifierCategory,
                          DisplayFlags = DisplayFlags.DescriptionHasMarkup) ]

    let keywordCompletionData =
        [for keyValuePair in KeywordList.keywordDescriptions do
            yield CompletionData(keyValuePair.Key, IconId("md-keyword"),keyValuePair.Value) ]

    let modifierCompletionData =
        [for keyValuePair in KeywordList.modifiers do
            yield CompletionData(keyValuePair.Key, IconId("md-keyword"),keyValuePair.Value) ]

    let parseLock = obj()

    let filterResults (data: seq<CompletionData>) residue =
        data |> Seq.filter(fun c -> residue = "" || (Char.ToLowerInvariant c.DisplayText.[0]) = (Char.ToLowerInvariant residue.[0]))

    let getFsiCompletions context = 

        async {
            let { column = column
                  lineToCaret = lineToCaret
                  completionChar = completionChar } = context

            let result = CompletionDataList()

            match FSharpInteractivePad.Fsi with
            | Some pad ->
                match pad.Session with
                | Some session ->              
                    // get completions from remote fsi process
                    pad.RequestCompletions lineToCaret column
                    let completions = 
                        Async.AwaitEvent (session.CompletionsReceived)
                        |> Async.RunSynchronously
                        |> Array.map (fun c -> FsiMemberCompletionData(c.displayText, c.completionText, symbolStringToIcon c.icon))
                        |> Seq.cast<CompletionData>

                    result.AddRange completions
                    let _longName,residue = Parsing.findLongIdentsAndResidue(column, lineToCaret)
                    if completionChar <> '.' && result.Count > 0 then

                        LoggingService.logDebug "Completion: residue %s" residue
                        result.DefaultCompletionString <- residue
                        result.TriggerWordLength <- residue.Length

                    //TODO Use previous token and pattern match to detect whitespace
                    if Regex.IsMatch(lineToCaret, "(^|\s+|\()\w+$", RegexOptions.Compiled) then
                        // Add the code templates and compiler generated identifiers if the completion char is not '.'
                        CodeTemplates.CodeTemplateService.AddCompletionDataForMime ("text/x-fsharp", result)
                        result.AddRange (filterResults compilerIdentifiers residue)
                        result.AddRange (filterResults keywordCompletionData residue)
                    return result
                | None -> return result
            | None -> return result
        }

    let getCompletions context =
        async {
            try
                let { 
                    line = line
                    column = column
                    documentContext = documentContext
                    lineToCaret = lineToCaret
                    completionChar = completionChar
                    editor = editor
                    } = context

                let typedParseResults =
                    lock parseLock (fun() ->
                        maybe {
                            let! document = documentContext.TryGetFSharpParsedDocument()
                            let! location = document.ParsedLocation
                            let trimmedLine = lineToCaret.TrimEnd()
                            let reparse = trimmedLine.EndsWith("->") || trimmedLine.Contains(").") || trimmedLine.EndsWith("].")
                            if location.Line = context.line && not reparse then
                                LoggingService.logDebug "Completion: got parse results from cache"
                                return! document.TryGetAst()
                            else
                                LoggingService.logDebug "Completion: syncing parse results"
                                // force sync
                                let projectFile = documentContext.Project |> function null -> document.FileName| proj -> proj.FileName.ToString()
                                let ast = languageService.ParseAndCheckFileInProject(projectFile, document.FileName, 0, editor.Text, true)
                                          |> Async.RunSynchronously
                                document.Ast <- ast
                                return ast
                        })

                let result = CompletionDataList()

                let addIdentCompletions() =
                    let (idents, residue) = Parsing.findLongIdentsAndResidue(column, lineToCaret)
                    if idents.IsEmpty then
                        let lineWithoutResidue = lineToCaret.[0..column-residue.Length-1]
                        if not (lineWithoutResidue.EndsWith ".") then
                            let tokens = Lexer.tokenizeLine lineWithoutResidue [||] 0 lineWithoutResidue Lexer.singleLineQueryLexState
                            let tokenToCompletion (token:FSharpTokenInfo) =
                                let displayText = lineToCaret.[token.LeftColumn..token.RightColumn]
                                CompletionData(displayText, IconId "md-fs-field", displayText, displayText)

                            // Add ident completions from the current line
                            // as the semantic parse might not be up to date
                            let lineCompletions = 
                                tokens 
                                |> List.filter (fun token -> token.TokenName = "IDENT")
                                |> List.map tokenToCompletion

                            result.AddRange (filterResults lineCompletions residue
                                             |> Seq.filter(fun r -> not (result.Exists(fun e -> e.DisplayText = r.DisplayText))))
                        result.DefaultCompletionString <- residue
                        result.TriggerWordLength <- residue.Length

                match typedParseResults with
                | None -> 
                    addIdentCompletions()
                | Some tyRes ->
                    // Get declarations and generate list for MonoDevelop
                    let! symbols = tyRes.GetDeclarationSymbols(line, column, lineToCaret)
                    match symbols with
                    | Some (symbols, residue) ->
                        let isInAttribute = 
                            match context with
                            | Attribute -> true
                            | _ -> false

                        let data = getCompletionData symbols isInAttribute
                        result.AddRange (filterResults data residue)

                        if completionChar <> '.' && result.Count > 0 then
                            LoggingService.logDebug "Completion: residue %s" residue
                            result.DefaultCompletionString <- residue
                            result.TriggerWordLength <- residue.Length

                            
                        //TODO Use previous token and pattern match to detect whitespace
                        if Regex.IsMatch(lineToCaret, "(^|\s+|\()\w+$", RegexOptions.Compiled) then
                            // Add the code templates and compiler generated identifiers if the completion char is not '.'
                            CodeTemplates.CodeTemplateService.AddCompletionDataForMime ("text/x-fsharp", result)
                            result.AddRange (filterResults compilerIdentifiers residue)
                                    
                            result.AddRange (filterResults keywordCompletionData residue)
                    | None -> addIdentCompletions()
                
                return result
            with
            | :? Threading.Tasks.TaskCanceledException -> 
                return CompletionDataList()
            | e ->
                LoggingService.LogError ("FSharpTextEditorCompletion, An error occured in CodeCompletionCommandImpl", e)
                return CompletionDataList()
        }

    let getCompletionList (completions: MonoDevelop.FSharp.Shared.PathCompletion) =
        let result = CompletionDataList()
        result.DefaultCompletionString <- completions.residue
        result.TriggerWordLength <- completions.residue.Length
        let completions = 
            completions.paths
            |> Seq.map (fun path -> CompletionData(path))
        result.AddRange completions
        result

    let getModifiers context =
        let { 
            column = column
            lineToCaret = lineToCaret
            ctrlSpace = ctrlSpace
            } = context

        let (_, residue) = Parsing.findLongIdentsAndResidue(column, lineToCaret)
        let result = CompletionDataList()
        result.DefaultCompletionString <- residue
        result.TriggerWordLength <- residue.Length 
        // To prevent the "No completions found" when typing an identifier
        // here -> `let myident|`
        // but allow completions
        // here -> `let mutab|`
        // but not here -> `let m|`
        let filteredModifiers = modifierCompletionData 
                                |> Seq.filter (fun c -> c.DisplayText.StartsWith(residue))
        if residue.Length > 1 || ctrlSpace then
            result.AddRange filteredModifiers
        result

    let codeCompletionCommandImpl(editor:TextEditor, documentContext:DocumentContext, context:CodeCompletionContext, ctrlSpace) =
        async {
            let line, col, lineStr = editor.GetLineInfoFromOffset context.TriggerOffset
            let completionContext = {
                completionChar = editor.GetCharAt(context.TriggerOffset - 1)
                lineToCaret = lineStr.[0..col-1]
                line = line
                column = col
                editor = editor
                triggerOffset = context.TriggerOffset
                ctrlSpace = ctrlSpace
                documentContext = documentContext
            }

            let! results = async {
                match completionContext with
                | FilePath (directive, path) ->
                    let workingFolder =
                        match documentContext |> Option.tryCast<FsiDocumentContext> with
                        | Some ctx -> ctx.WorkingFolder
                        | _ -> documentContext.GetWorkingFolder()

                    let completions = MonoDevelop.FSharp.Shared.Completion.getPathCompletion workingFolder directive path
                    return getCompletionList completions
                | InvalidToken
                | InsideBlockComment
                | InvalidCompletionChar
                | DoubleDot
                | LiteralNumber
                | FunctionIdentifier -> 
                    return CompletionDataList()
                | ModuleOrTypeIdentifier
                | LetIdentifier ->
                    return getModifiers completionContext
                | _ ->
                    if documentContext :? FsiDocumentContext then
                        return! getFsiCompletions completionContext
                    else
                        return! getCompletions completionContext
            }

            results.IsSorted <- true
            results.AutoCompleteEmptyMatch <- false
            results.AutoCompleteUniqueMatch <- ctrlSpace

            return results :> ICompletionDataList 
        }

type FSharpParameterHintingData (symbol:FSharpSymbolUse) =
    inherit ParameterHintingData (null)

    let getTooltipInformation symbol paramIndex =
        async {
            match symbol with
            | MemberFunctionOrValue _f ->
                let tooltipInfo = MonoDevelop.FSharp.SymbolTooltips.getParameterTooltipInformation symbol paramIndex
                return tooltipInfo
            | symbol ->
                LoggingService.LogDebug(sprintf "FSharpParameterHintingData - CreateTooltipInformation could not create tooltip for %A" symbol.Symbol)
                return null }

    override x.ParameterCount =
        MonoDevelop.FSharp.Shared.ParameterHinting.parameterCount symbol.Symbol

    override x.IsParameterListAllowed =
        MonoDevelop.FSharp.Shared.ParameterHinting.isParameterListAllowed symbol.Symbol

    override x.GetParameterName i =
        MonoDevelop.FSharp.Shared.ParameterHinting.getParameterName symbol.Symbol i

    /// Returns the markup to use to represent the method overload in the parameter information window.
    override x.CreateTooltipInformation (_editor, _context, paramIndex: int, _smartWrap:bool, cancel) =
        Async.StartAsTask(getTooltipInformation symbol (Math.Max(paramIndex, 0)), cancellationToken = cancel)

type FsiParameterHintingData (tooltip: MonoDevelop.FSharp.Shared.ParameterTooltip) =
    inherit ParameterHintingData (null)

    override x.ParameterCount =
       match tooltip with
       | MonoDevelop.FSharp.Shared.ParameterTooltip.ToolTip (_, _, parameters) -> parameters.Length
       | _ -> 0

    override x.IsParameterListAllowed =
        match tooltip with
        | MonoDevelop.FSharp.Shared.ParameterTooltip.ToolTip (_, _, parameters) -> parameters.Length > 0
        | _ -> false

    override x.GetParameterName i =
        match tooltip with
        | MonoDevelop.FSharp.Shared.ParameterTooltip.ToolTip (_, _, parameters) -> parameters.[i]
        | _ -> null

    /// Returns the markup to use to represent the method overload in the parameter information window.
    override x.CreateTooltipInformation (_editor, _context, paramIndex: int, _smartWrap:bool, cancel) =
        let computation =
            async {
                match tooltip with
                | MonoDevelop.FSharp.Shared.ParameterTooltip.ToolTip (signature, doc, parameters) -> 
                    let signature, parameterName = 
                        if paramIndex = -1 || paramIndex < parameters.Length - 1 then
                            Highlight.syntaxHighlight signature, null
                        else
                            let paramName = parameters.[paramIndex]
                            let lines =
                                String.getLines signature
                                |> Array.mapi (fun i line -> 
                                                if i = paramIndex + 1 then
                                                    let regex = new System.Text.RegularExpressions.Regex(paramName)
                                                    regex.Replace(line, sprintf "_STARTUNDERLINE_%s_ENDUNDERLINE_" paramName, 1)
                                                else
                                                    line)
                            let signature = Highlight.syntaxHighlight (String.concat "\n" lines)
                            let signature = signature.Replace("_STARTUNDERLINE_", "<u>").Replace("_ENDUNDERLINE_", "</u>")
                                             
                            signature, parameters.[paramIndex]
                    
                    return SymbolTooltips.getTooltipInformationFromSignature doc signature parameterName
                | _ -> return TooltipInformation()
            }
        Async.StartAsTask(computation, cancellationToken = cancel)

module ParameterHinting =

    // Until we build some functionality around a reversing tokenizer that detect this and other contexts
    // A crude detection of being inside an auto property decl: member val Foo = 10 with get,$ set
    let isAnAutoProperty (_editor: TextEditor) _offset =
        false

    let getHints (editor:TextEditor, documentContext:DocumentContext, context:CodeCompletionContext) =
        async {
        try
            let docText = editor.Text
            let offset = context.TriggerOffset
            // Parse backwards, skipping (...) and { ... } and [ ... ] to determine the parameter index.
            // This is an approximation.
            let startOffset =
                let rec loop depth i =
                    if (i <= 0) then i else
                        let ch = docText.[i]
                        if ((ch = '(' || ch = '{' || ch = '[') && depth > 0) then loop (depth - 1) (i-1)
                        elif ((ch = ')' || ch = '}' || ch = ']')) then loop (depth+1) (i-1)
                        elif (ch = '(' || ch = '<') then i
                        else loop depth (i-1)
                loop 0 (offset-1)

            if docText = null || offset > docText.Length || startOffset < 0 || offset <= 0 || isAnAutoProperty editor offset
            then return ParameterHintingResult.Empty
            else
            LoggingService.LogDebug("FSharpTextEditorCompletion - HandleParameterCompletionAsync: Getting Parameter Info, startOffset = {0}", startOffset)

            if documentContext :? FsiDocumentContext then
            
                match FSharpInteractivePad.Fsi with
                | Some pad ->
                    match pad.Session with
                    | Some session ->
                        let _line, col, lineStr = editor.GetLineInfoFromOffset (startOffset)
                        pad.RequestParameterHint lineStr col
                        let tooltips = 
                            Async.AwaitEvent (session.ParameterHintReceived)
                            |> Async.RunSynchronously

                        let hintingData =
                            tooltips
                            |> Array.map (fun meth -> FsiParameterHintingData (meth) :> ParameterHintingData)
                            |> ResizeArray.ofArray
                        if hintingData.Count > 0 then
                            return ParameterHintingResult(hintingData, startOffset)
                        else
                            return ParameterHintingResult.Empty
                    | _ -> return ParameterHintingResult.Empty
                | _ -> return ParameterHintingResult.Empty
            else
            let filename = documentContext.Name

            // Try to get typed result - within the specified timeout
            let! methsOpt =
                async { let projectFile = documentContext.Project |> function null -> filename | project -> project.FileName.ToString()
                        let! tyRes = languageService.GetTypedParseResultWithTimeout (projectFile, filename, 0, docText, AllowStaleResults.MatchingSource, ServiceSettings.maximumTimeout, (fun() -> false) )
                        match tyRes with
                        | Some tyRes ->
                            let line, col, lineStr = editor.GetLineInfoFromOffset (startOffset)
                            let! allMethodSymbols = tyRes.GetMethodsAsSymbols (line, col, lineStr)
                            return allMethodSymbols
                        | None -> return None}

            match methsOpt with
            | Some(meths) when meths.Length > 0 ->
                LoggingService.logDebug "FSharpTextEditorCompletion: Getting Parameter Info: %d methods" meths.Length
                let hintingData =
                    meths
                    |> List.map (fun meth -> FSharpParameterHintingData (meth) :> ParameterHintingData)
                    |> ResizeArray.ofList

                return ParameterHintingResult(hintingData, startOffset)
            | _ -> LoggingService.logWarning "FSharpTextEditorCompletion: Getting Parameter Info: no methods found"
                   return ParameterHintingResult.Empty
        with
        | :? Threading.Tasks.TaskCanceledException ->
            return ParameterHintingResult.Empty
        | ex ->
            LoggingService.LogError ("FSharpTextEditorCompletion: Error in HandleParameterCompletion", ex)
            return ParameterHintingResult.Empty
        }

    // Returns the index of the parameter where the cursor is currently positioned.
    // -1 means the cursor is outside the method parameter list
    // 0 means no parameter entered
    // > 0 is the index of the parameter (1-based)
    let getParameterIndex (editor:TextEditor, startOffset) = 
        let cursor = editor.CaretOffset
        let i = startOffset // the original context
        if (i < 0 || i >= editor.Length || editor.GetCharAt (i) = ')') then -1
        //elif (i + 1 = cursor && (match editor.GetCharAt(i) with '(' | '<' -> true | _ -> false)) then 0
        else
            // The first character is a '('
            // Note this will be confused by comments.
            let rec loop depth i parameterIndex =
                if (i = cursor) then parameterIndex
                elif (i > cursor) then -1
                elif (i >= editor.Length) then  parameterIndex else
                let ch = editor.GetCharAt(i)
                if (ch = '(' || ch = '{' || ch = '[') then loop (depth+1) (i+1) parameterIndex
                elif ((ch = ')' || ch = '}' || ch = ']') && depth > 1 ) then loop (depth-1) (i+1) parameterIndex
                elif (ch = ',' && depth = 1) then loop depth (i+1) (parameterIndex+1)
                elif (ch = ')' || ch = '>') then -1
                else loop depth (i+1) parameterIndex
            loop 0 i 1

/// Implements text editor extension for MonoDevelop that shows F# completion
type FSharpTextEditorCompletion() =
    inherit CompletionTextEditorExtension()

    let mutable suppressParameterCompletion = false

    let isValidParamCompletionDecriptor (d:KeyDescriptor) =
        d.KeyChar = '(' || d.KeyChar = '<' || d.KeyChar = ',' || (d.KeyChar = ' ' && d.ModifierKeys = ModifierKeys.Control)

    let validCompletionChar c =
        c = '(' || c = ',' || c = '<'

    override x.CompletionLanguage = "F#"
    override x.Initialize() =
        do x.Editor.IndentationTracker <- FSharpIndentationTracker(x.Editor)
        base.Initialize()

    /// Provide parameter and method overload information when you type '(', '<' or ','
    override x.HandleParameterCompletionAsync (context, completionChar, token) =
        //TODO refactor computation to remove some return statements (clarity)
        if suppressParameterCompletion || not (validCompletionChar completionChar)
        then suppressParameterCompletion <- false
             System.Threading.Tasks.Task.FromResult(ParameterHintingResult.Empty)
        else
            let computation = ParameterHinting.getHints(x.Editor, x.DocumentContext, context)
            Async.StartAsTask (cancellationToken = token, computation = computation)

    override x.KeyPress (descriptor:KeyDescriptor) =
        suppressParameterCompletion <- not (isValidParamCompletionDecriptor descriptor)
        base.KeyPress (descriptor)
  
    // Run completion automatically when the user hits '.'
    override x.HandleCodeCompletionAsync(context, _triggerInfo, token) =
        if IdeApp.Preferences.EnableAutoCodeCompletion.Value then
            let computation =
                Completion.codeCompletionCommandImpl(x.Editor, x.DocumentContext, context, false) 
                        
            Async.StartAsTask (computation = computation, cancellationToken = token)
        else
            Task.FromResult null


    override x.GetCurrentParameterIndex (startOffset: int, token) =
        let computation =
            async {
                return ParameterHinting.getParameterIndex(x.Editor, startOffset)
            }
        Async.StartAsTask (computation = computation, cancellationToken = token)
