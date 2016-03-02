// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Threading.Tasks
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Debugger
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.CodeCompletion
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open Microsoft.FSharp.Compiler.SourceCodeServices

module Completion = 
    type internal FSharpMemberCompletionData(name, icon, symbol:FSharpSymbolUse, overloads:FSharpSymbolUse list) =
        inherit CompletionData(CompletionText = PrettyNaming.QuoteIdentifierIfNeeded name,
                               DisplayText = name,
                               DisplayFlags = DisplayFlags.DescriptionHasMarkup,
                               Icon = icon)
    
        /// Check if the datatip has multiple overloads
        override x.HasOverloads = not (List.isEmpty overloads)

        /// Split apart the elements into separate overloads
        override x.OverloadedData =
            overloads
            |> List.map (fun symbol -> FSharpMemberCompletionData(symbol.Symbol.DisplayName, icon, symbol, []) :> CompletionData)
            |> ResizeArray.ofList :> _
    
        // TODO: what does 'smartWrap' indicate?
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
    
    let getCompletionData (symbols:FSharpSymbolUse list list) =
        let categories = Dictionary<string, Category>()
        let getOrAddCategory symbol id =
            match categories.TryGetValue id with
            | true, item -> item
            | _ -> let cat = Category(id, symbol)
                   categories.Add (id, cat)
                   cat

        let symbolToIcon (symbolUse:FSharpSymbolUse) =
            match symbolUse with
            | ActivePatternCase _ -> Stock.Enum
            | Field _ -> Stock.Field
            | UnionCase _ -> IconId("md-type")
            | Class _ -> Stock.Class
            | Delegate _ -> Stock.Delegate
            | Constructor _  -> Stock.Method
            | Event _ -> Stock.Event
            | Property _ -> Stock.Property
            | Function f ->
                if f.IsExtensionMember then IconId("md-extensionmethod")
                elif f.IsMember then IconId("md-method")
                else IconId("md-fs-field")
            | Operator _ -> IconId("md-fs-field")
            | ClosureOrNestedFunction _ -> IconId("md-fs-field")
            | Val _ -> Stock.Field
            | Enum _ -> Stock.Enum
            | Interface _ -> Stock.Interface
            | Module _ -> IconId("md-module")
            | Namespace _ -> Stock.NameSpace
            | Record _ -> Stock.Class
            | Union _ -> IconId("md-type")
            | ValueType _ -> Stock.Struct
            | SymbolUse.Entity _ -> IconId("md-type")
            | _ -> Stock.Event

        let tryGetCategory (symbolUse : FSharpSymbolUse) =
            let category =
                try
                    match symbolUse with
                    | Constructor c ->
                        c.EnclosingEntitySafe
                        |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                                  un.DisplayName, un)
                    | Event ev ->
                        ev.EnclosingEntitySafe
                        |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                                  un.DisplayName, un)
                    | Property pr ->
                        pr.EnclosingEntitySafe
                        |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                                  un.DisplayName, un)
                    | ActivePatternCase ap ->
                        if ap.Group.Names.Count > 1 then
                            ap.Group.EnclosingEntity
                            |> Option.map (fun enclosing -> let un = enclosing.UnAnnotate()
                                                            SymbolTooltips.escapeText un.DisplayName, un)
                        else None
                    | UnionCase uc ->
                        if uc.UnionCaseFields.Count > 1 then
                            let ent = uc.ReturnType.TypeDefinition.UnAnnotate()
                            Some(SymbolTooltips.escapeText ent.DisplayName, ent)
                        else None
                    | Function f ->
                        if f.IsExtensionMember then
                            let real = f.LogicalEnclosingEntity.UnAnnotate()
                            Some(real.DisplayName, real)
                        else
                            f.EnclosingEntitySafe
                            |> Option.map (fun real -> let un = real.UnAnnotate()
                                                       un.DisplayName, un)
                    | Operator o ->
                        o.EnclosingEntitySafe
                        |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                                  un.DisplayName, un)
                    | Pattern p ->
                        p.EnclosingEntitySafe
                        |> Option.map (fun ent -> let un = ent.UnAnnotate()
                                                  un.DisplayName, ent)
                    | Val v ->
                        v.EnclosingEntitySafe
                        |> Option.map (fun ent -> let un  = ent.UnAnnotate()
                                                  un.DisplayName, un)
                    | TypeAbbreviation ta ->
                        //TODO:  Check this is correct, I suspect we should return None here
                        let ent = ta.UnAnnotate()
                        Some (ent.DisplayName, ent)
                    //The following have no logical parent to display
                    //Theres no link to a parent type for a closure (FCS limitation)
                    | ClosureOrNestedFunction _cl -> None
                    //The F# compiler does not currently expose an Entitys parent, only children
                    | Class _cl -> None
                    | Delegate _dl -> None
                    | Enum _en  -> None
                    | Interface _  -> None
                    | Module _  -> None
                    | Namespace _  -> None
                    | Record _  -> None
                    | Union _  -> None
                    | ValueType _  -> None
                    | _ -> None
                with exn -> None
            category

        let symbolToCompletionData (symbols : FSharpSymbolUse list) =
            match symbols with
            | head :: tail ->
                let cd = FSharpMemberCompletionData(head.Symbol.DisplayName, symbolToIcon head, head, tail) :> CompletionData
                //cd.PriorityGroup <- 1 + inheritanceDepth l.Head.Symbol
                match tryGetCategory head with
                | Some (id, ent) ->
                    let category = getOrAddCategory ent id
                    cd.CompletionCategory <- category
                | None -> ()
                cd
            | _ -> null //FSharpTryAgainMemberCompletionData() :> ICompletionData
        symbols |> List.map symbolToCompletionData

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

    let getParseResults (documentContext:DocumentContext, text) =
        async {
            let filename = documentContext.Name
            // Try to get typed information from LanguageService (with the specified timeout)
            let projectFile = documentContext.Project |> function null -> filename | project -> project.FileName.ToString()
            return! languageService.GetTypedParseResultWithTimeout(projectFile, filename, 0, text, AllowStaleResults.MatchingSource, ServiceSettings.maximumTimeout, IsResultObsolete(fun() -> false))
        }


    let shouldComplete (editor, context:CodeCompletionContext, ctrlSpace) =
        let isValidToken() =
            let token = Tokens.getTokenAtPoint editor editor.DocumentContext context.TriggerOffset
            not (Tokens.isInvalidCompletionToken token)

        let isValidCompletionChar() =
            let completionChar = editor.GetCharAt(context.TriggerOffset - 1)
            (Char.IsLetter completionChar || ctrlSpace) || completionChar = '.'

        isValidCompletionChar() &&
        isValidToken() 
                
    // cache parse results for current filename/line number
    let mutable parseCache = (Unchecked.defaultof<FilePath>, None) 

    let parseLock = obj()
    let getParseResultsFromCacheOrCompiler (documentContext:DocumentContext, editor:TextEditor) =
        lock parseLock (fun() ->
            async {
                match parseCache with
                | (filename, parseResults) when filename = editor.FileName -> 
                    LoggingService.logDebug "Completion: got parse results from cache"
                    return parseResults
                | _ -> 
                    let! (parseResults: ParseAndCheckResults option) = 
                        getParseResults(documentContext, editor.Text)

                    match parseResults with
                    | Some _ -> parseCache <- (editor.FileName, parseResults)
                                LoggingService.logDebug "Completion: got some parse results"
                                return parseResults
                    | None -> LoggingService.logDebug "Completion: got no parse results"
                              return None
            })

    let codeCompletionCommandImpl(editor, documentContext, context:CodeCompletionContext, ctrlSpace) =
        async {
            let result = CompletionDataList()

            let emptyResult = result :> ICompletionDataList
            if not (shouldComplete(editor, context, ctrlSpace)) then
                return emptyResult
            else
            let line, col, lineStr = editor.GetLineInfoFromOffset context.TriggerOffset

            let completionChar = editor.GetCharAt(context.TriggerOffset - 1)
            let lineToCaret = lineStr.Substring (0,col)

            let isFunctionIdentifier() =
                Regex.IsMatch(lineToCaret, "\s?(fun)\s+[^-]+$")

            let isModuleOrTypeIdentifier() =
                Regex.IsMatch(lineToCaret, "\s?(module|type)\s+[^=]+$") && not (lineToCaret.Contains("="))

            let isLetIdentifier() =
                if Regex.IsMatch(lineToCaret, "\s?(let|override|member)\s+[^=]+$") 
                     && not (lineToCaret.Contains("=")) then
                     let document = new TextDocument(lineToCaret)
                     let syntaxMode = SyntaxModeService.GetSyntaxMode (document, "text/x-fsharp")
                     let documentLine = document.GetLine 1
                     let chunkStyle = syntaxMode.GetChunks(getColourScheme(), documentLine, col, lineToCaret.Length)
                                      |> Seq.map (fun c -> c.Style)   
                                      |> Seq.head
                     chunkStyle <> "User Types"
                else
                    false

            result.IsSorted <- true

            if isModuleOrTypeIdentifier() || isLetIdentifier() then
                let (_, residue) = Parsing.findLongIdentsAndResidue(col, lineStr)
                result.DefaultCompletionString <- residue
                result.TriggerWordLength <- residue.Length 

                // To prevent "No completions found" when typing an identifier
                // here -> `let myident|`
                // but allow completions
                // here -> `let mutab|`
                // but not here -> `let m|`
                let filteredModifiers = modifierCompletionData 
                                        |> Seq.filter (fun c -> c .DisplayText.StartsWith(residue))
                if residue.Length > 1 || ctrlSpace then
                    result.AddRange filteredModifiers
            elif isFunctionIdentifier() then
                ()
            else
                try
                    let! (typedParseResults: ParseAndCheckResults option) = 
                        getParseResultsFromCacheOrCompiler(documentContext, editor)
                                 
                    match typedParseResults with
                    | None       -> () //TODOresult.Add(FSharpTryAgainMemberCompletionData())
                    | Some tyRes ->
                        // Get declarations and generate list for MonoDevelop
                        let! symbols = tyRes.GetDeclarationSymbols(line, col, lineStr)
                        match symbols with
                        | Some (symbols, residue) ->
                            let data = getCompletionData symbols
                            result.AddRange data

                            if completionChar <> '.' && result.Count > 0 then
                                LoggingService.logDebug "Completion: residue %s" residue
                                result.DefaultCompletionString <- residue
                                result.TriggerWordLength <- residue.Length

                            //TODO Use previous token and pattern match to detect whitespace
                            if Regex.IsMatch(lineToCaret, "(^|\s+|\()\w+$") then
                                // Add the code templates and compiler generated identifiers if the completion char is not '.'
                                CodeTemplates.CodeTemplateService.AddCompletionDataForMime ("text/x-fsharp", result)
                                result.AddRange compilerIdentifiers
                                        
                                result.AddRange keywordCompletionData
                        | None -> ()
                with
                | :? Threading.Tasks.TaskCanceledException -> 
                    ()
                | e ->
                    LoggingService.LogError ("FSharpTextEditorCompletion, An error occured in CodeCompletionCommandImpl", e)
                    () //TODOresult.Add(FSharpErrorCompletionData(e))
            result.AutoCompleteEmptyMatch <- false
            result.AutoCompleteUniqueMatch <- ctrlSpace

            return result :> ICompletionDataList }

type FSharpParameterHintingData (symbol:FSharpSymbolUse) =
    inherit ParameterHintingData (null)

    let getTooltipInformation symbol paramIndex =
        async {
            match symbol with
            | MemberFunctionOrValue _f ->
                let tooltipInfo = SymbolTooltips.getParameterTooltipInformation symbol paramIndex
                return tooltipInfo
            | symbol ->
                LoggingService.LogDebug(sprintf "FSharpParameterHintingData - CreateTooltipInformation could not create tooltip for %A" symbol.Symbol)
                return null }

    override x.ParameterCount =
        match symbol.Symbol with
        | :? FSharpMemberOrFunctionOrValue as fsm ->
            let cpg = fsm.CurriedParameterGroups
            cpg.[0].Count
        | _ -> 0

    override x.IsParameterListAllowed =
        match symbol.Symbol with
        | :? FSharpMemberOrFunctionOrValue as fsm 
            when fsm.CurriedParameterGroups.Count > 0 ->
                //TODO: How do we handle non tupled arguments?
                let group = fsm.CurriedParameterGroups.[0] 
                if group.Count > 0 then
                    let last = group |> Seq.last
                    last.IsParamArrayArg
                else
                    false
        | _ -> false

    override x.GetParameterName i =
        match symbol.Symbol with
        | :? FSharpMemberOrFunctionOrValue as fsm 
            when fsm.CurriedParameterGroups.Count > 0 &&
                 fsm.CurriedParameterGroups.[0].Count > 0 ->
                //TODO: How do we handle non tupled arguments?
                let group = fsm.CurriedParameterGroups.[0]
                let param = group.[i]
                match param.Name with
                | Some n -> n
                | None -> param.DisplayName
        | _ -> ""

    /// Returns the markup to use to represent the method overload in the parameter information window.
    override x.CreateTooltipInformation (_editor, _context, paramIndex: int, _smartWrap:bool, cancel) =
        Async.StartAsTask(getTooltipInformation symbol (Math.Max(paramIndex, 0)), cancellationToken = cancel)

/// Implements text editor extension for MonoDevelop that shows F# completion
type FSharpTextEditorCompletion() =
    inherit CompletionTextEditorExtension()

    let mutable suppressParameterCompletion = false

    // Until we build some functionality around a reversing tokenizer that detect this and other contexts
    // A crude detection of being inside an auto property decl: member val Foo = 10 with get,$ set
    let isAnAutoProperty (_editor: TextEditor) _offset =
        false
    //TODO
    //  let lastEnd = editor.FindCurrentWordEnd(lastStart)
    //  let lastWord = editor.GetTextBetween(lastStart, lastEnd)

    //  let prevStart = editor.FindPrevWordOffset(lastStart)
    //  let prevEnd = editor.FindCurrentWordEnd(prevStart)
    //  let previousWord = editor.GetTextBetween(prevStart, prevEnd)
    //  lastWord = "get" && previousWord = "with"

    let isValidParamCompletionDecriptor (d:KeyDescriptor) =
        d.KeyChar = '(' || d.KeyChar = '<' || d.KeyChar = ',' || (d.KeyChar = ' ' && d.ModifierKeys = ModifierKeys.Control)

    let validCompletionChar c =
        c = '(' || c = ',' || c = '<'

    //only used for testing
    member x.Initialize(editor, context) =
        x.DocumentContext <- context
        x.Editor <- editor

    override x.CompletionLanguage = "F#"
    override x.Initialize() =
        let mutable unparsedChanges = true
        let mutable lastEditedLine = -1

        do 
            x.Editor.SetIndentationTracker (FSharpIndentationTracker(x.Editor))
            x.Editor.TextChanged.Subscribe
                (fun(args) -> unparsedChanges <- true
                              lastEditedLine <- x.Editor.OffsetToLineNumber args.Offset
                            ) |> ignore

            x.Editor.CaretPositionChanged.Subscribe
                (fun (_e) -> 

                    if unparsedChanges && x.Editor.CaretLine <> lastEditedLine
                       && MonoDevelop.isDocumentVisible (x.Editor.FileName.ToString()) then

                        lock Completion.parseLock (fun () ->
                            LoggingService.logDebug "%s" "Completion: pre-emptively fetching new parse results"
                            async {
                                let! (parseResults: ParseAndCheckResults option) = 
                                    Completion.getParseResults(x.DocumentContext, x.Editor.Text)

                                if parseResults.IsSome then
                                    unparsedChanges <- false
                                    lastEditedLine <- x.Editor.CaretLine
                                    Completion.parseCache <- (x.Editor.FileName, parseResults)
                            }
                            |> Async.StartImmediate
                        ))
                    |> ignore
        
        base.Initialize()
    
    /// Provide parameter and method overload information when you type '(', '<' or ','
    override x.HandleParameterCompletionAsync (context, completionChar, token) =
      //TODO refactor computation to remove some return statements (clarity)
      if suppressParameterCompletion || not (validCompletionChar completionChar)
      then suppressParameterCompletion <- false
           System.Threading.Tasks.Task.FromResult(ParameterHintingResult.Empty)
      else
        Async.StartAsTask (cancellationToken = token, computation = async {
        try
            let docText = x.Editor.Text
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

            if docText = null || offset > docText.Length || startOffset < 0 || offset <= 0 || isAnAutoProperty x.Editor offset
            then return ParameterHintingResult.Empty
            else
            LoggingService.LogDebug("FSharpTextEditorCompletion - HandleParameterCompletionAsync: Getting Parameter Info, startOffset = {0}", startOffset)

            // Try to get typed result - within the specified timeout
            let! methsOpt =
                async { 
                    let line, col, lineStr = x.Editor.GetLineInfoFromOffset (startOffset)
                    let! tyRes = Completion.getParseResultsFromCacheOrCompiler (x.DocumentContext, x.Editor)
                    match tyRes with
                    | Some tyRes ->
                        let! allMethodSymbols = tyRes.GetMethodsAsSymbols (line, col, lineStr)
                        return allMethodSymbols
                    | None -> return None
                }

            match methsOpt with
            | Some(meths) when meths.Length > 0 ->
                LoggingService.LogDebug ("FSharpTextEditorCompletion: Getting Parameter Info: {0} methods", meths.Length)
                let hintingData =
                    meths
                    |> List.map (fun meth -> FSharpParameterHintingData (meth) :> ParameterHintingData)
                    |> ResizeArray.ofList

                return ParameterHintingResult(hintingData, startOffset)
            | _ -> LoggingService.LogWarning("FSharpTextEditorCompletion: Getting Parameter Info: no methods found")
                   return ParameterHintingResult.Empty
        with
        | :? Threading.Tasks.TaskCanceledException ->
            return ParameterHintingResult.Empty
        | ex ->
            LoggingService.LogError ("FSharpTextEditorCompletion: Error in HandleParameterCompletion", ex)
            return ParameterHintingResult.Empty})

    override x.KeyPress (descriptor:KeyDescriptor) =
        suppressParameterCompletion <- not (isValidParamCompletionDecriptor descriptor)
        base.KeyPress (descriptor)
  
    // Run completion automatically when the user hits '.'
    override x.HandleCodeCompletionAsync(context, completionChar, token) =
        if IdeApp.Preferences.EnableAutoCodeCompletion.Value || completionChar = '.' then
            let computation =
                Completion.codeCompletionCommandImpl(x.Editor, x.DocumentContext, context, false) 
                    
            Async.StartAsTask (computation = computation, cancellationToken = token)
        else
            Task.FromResult null

    /// Completion was triggered explicitly using Ctrl+Space or by the function above
    override x.CodeCompletionCommand(context) =
        Completion.codeCompletionCommandImpl(x.Editor, x.DocumentContext, context, true)
        |> Async.StartAsTask

    // Returns the index of the parameter where the cursor is currently positioned.
    // -1 means the cursor is outside the method parameter list
    // 0 means no parameter entered
    // > 0 is the index of the parameter (1-based)
    override x.GetCurrentParameterIndex (startOffset: int, token) =
        let computation =
            async {
                let editor = x.Editor
                let cursor = editor.CaretOffset
                let i = startOffset // the original context
                if (i < 0 || i >= editor.Length || editor.GetCharAt (i) = ')') then return -1
                elif (i + 1 = cursor && (match editor.GetCharAt(i) with '(' | '<' -> true | _ -> false)) then return 0
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
                    let res = loop 0 i 1
                    return res
            }
        Async.StartAsTask (computation = computation, cancellationToken = token)

    interface IDebuggerExpressionResolver with
        member x.ResolveExpressionAsync (doc, context, offset, cancellationToken) =
            let computation = async {
                let ast = context.TryGetAst()
                let location =
                    match ast with
                    | None -> None
                    | Some pcr ->
                        let location = doc.OffsetToLocation(offset)
                        let line = doc.GetLine location.Line
                        let lineTxt = doc.GetTextAt (line.Offset, line.Length)
                        let symbol = pcr.GetSymbolAtLocation (location.Line, location.Column, lineTxt) |> Async.RunSynchronously
                        match symbol with
                        | Some symbolUse when not symbolUse.IsFromDefinition ->
                            match symbolUse with
                            | SymbolUse.ActivePatternCase apc ->
                                Some (apc.DeclarationLocation, apc.DisplayName)
                            | SymbolUse.Entity _ent -> None
                            | SymbolUse.Field field ->
                                Some (field.DeclarationLocation, field.DisplayName)
                            | SymbolUse.GenericParameter gp ->
                                Some (gp.DeclarationLocation, gp.DisplayName)
                            //| CorePatterns.MemberFunctionOrValue
                            | SymbolUse.Parameter p ->
                                Some (p.DeclarationLocation, p.DisplayName)
                            | SymbolUse.StaticParameter sp ->
                                Some (sp.DeclarationLocation, sp.DisplayName)
                            | SymbolUse.UnionCase _uc -> None
                            | SymbolUse.Class _c -> None
                            | SymbolUse.ClosureOrNestedFunction _cl -> None
                            | SymbolUse.Constructor _ctor -> None
                            | SymbolUse.Delegate _del -> None
                            | SymbolUse.Enum enum ->
                                Some (enum.DeclarationLocation, enum.DisplayName)
                            | SymbolUse.Event _ev -> None
                            | SymbolUse.Function _f -> None
                            | SymbolUse.Interface _i -> None
                            | SymbolUse.Module _m -> None
                            | SymbolUse.Namespace _ns -> None
                            | SymbolUse.Operator _op -> None
                            | SymbolUse.Pattern _p -> None
                            | SymbolUse.Property _pr ->
                                let loc = symbolUse.RangeAlternate
                                Some (loc, lineTxt.Substring(loc.StartColumn, loc.EndColumn-loc.StartColumn))
                            | SymbolUse.Record r ->
                                let loc = r.DeclarationLocation
                                Some (loc, r.DisplayName)
                            | SymbolUse.TypeAbbreviation _ta -> None
                            | SymbolUse.Union _un -> None
                            | SymbolUse.Val v ->
                                let loc = v.DeclarationLocation
                                Some (loc, v.DisplayName)
                            | SymbolUse.ValueType _vt -> None
                            | _ -> None
                        | _ -> None
                match location with
                | None -> return DebugDataTipInfo()
                | Some (range, name) ->
                    let ts = Symbols.getTextSpan range doc
                    return DebugDataTipInfo(ts, name)}

            Async.StartAsTask (computation, cancellationToken = cancellationToken)
