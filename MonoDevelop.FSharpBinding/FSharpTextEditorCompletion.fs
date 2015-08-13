// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Diagnostics
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Debugger
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.CodeTemplates
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp.NRefactory
open ICSharpCode.NRefactory.TypeSystem
open ExtCore.Control

type internal FSharpMemberCompletionData(name, icon, symbol:FSharpSymbolUse, overloads:FSharpSymbolUse list) =
  inherit CompletionData(CompletionText = Lexhelp.Keywords.QuoteIdentifierIfNeeded name, 
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

  override x.AddOverload (_data) = () //not currently called

  // TODO: what does 'smartWrap' indicate?
  override x.CreateTooltipInformation (_smartWrap, cancel) =
    Async.StartAsTask(
      async {
        LoggingService.LogInfo("computing tooltip for {0}", name)
        let tip = SymbolTooltips.getTooltipFromSymbolUse symbol
        match tip  with
        | ToolTips.ToolTip (signature, xmldoc) ->
            let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
            let result = 
              match xmldoc with
              | Full(summary) -> toolTipInfo.SummaryMarkup <- summary
                                 toolTipInfo
              | Lookup(key, potentialFilename) ->
                  let summary = 
                    maybe {let! filename = potentialFilename
                           let! markup = TooltipXmlDoc.findDocForEntity(filename, key)
                           let summary = TooltipsXml.getTooltipSummary Styles.simpleMarkup markup
                           return summary }
                  summary |> Option.iter (fun summary -> toolTipInfo.SummaryMarkup <- summary)
                  toolTipInfo
              | EmptyDoc -> toolTipInfo
            return result
        | _ -> return TooltipInformation() }, cancellationToken = cancel)

type Category(category) =
  inherit CompletionCategory(category, null)
  override x.CompareTo other =
    if other = null then -1 else x.DisplayText.CompareTo other.DisplayText

//TODO: finish porting symbol version once it hit FCS
type FSharpParameterHintingData (name, meth : FSharpMethodGroupItem(*, symbol:FSharpSymbolUse*) ) =
  inherit ParameterHintingData (null)

  override x.ParameterCount =
//    match symbol.Symbol with
//    | :? FSharpMemberOrFunctionOrValue as fsm ->
//        let cpg = fsm.CurriedParameterGroups
//        cpg.Count
//    | _ -> 
    meth.Parameters.Length

  override x.IsParameterListAllowed = false

  override x.GetParameterName i =
//    match symbol.Symbol with
//    | :? FSharpMemberOrFunctionOrValue as fsm ->
//        let p = fsm.CurriedParameterGroups.[i]
//        ""
//    | _ -> ""
    meth.Parameters.[i].ParameterName

  /// Returns the markup to use to represent the specified method overload
  /// in the parameter information window.
  override x.CreateTooltipInformation (_editor, _context, currentParameter:int, _smartWrap:bool, cancel) =
    Async.StartAsTask(
      async {
        // Get the lower part of the text for the display of an overload
        let signature, comment =
            match TooltipFormatting.formatTip meth.Description with
            | [signature,comment] -> signature,comment
            //With multiple tips just take the head.  
            //This shouldnt happen anyway as we split them in the resolver provider
            | multiple ->
                multiple 
                |> List.head
                |> (fun (signature,comment) -> signature,comment)

        let description =
            let param = 
                meth.Parameters |> Array.mapi (fun i param -> 
                    let paramDesc = 
                        // Sometimes the parameter decription is hidden in the XML docs
                        match TooltipFormatting.extractParamTip param.ParameterName meth.Description with 
                        | Some(tip) -> tip
                        | None -> param.Description
                    let name = if i = currentParameter then  "<b>" + param.ParameterName + "</b>" else param.ParameterName
                    let text = name + ": " + GLib.Markup.EscapeText paramDesc
                    text )
            if String.IsNullOrEmpty comment then String.Join("\n", param)
            else comment + "\n" + String.Join("\n", param)
            
        
        // Returns the text to use to represent the specified parameter
        let paramDescription = 
          if currentParameter < 0 || currentParameter >= meth.Parameters.Length  then "" else 
          let param = meth.Parameters.[currentParameter]
          param.ParameterName 

        let heading = 

          let lines = signature.Split [| '\n';'\r' |]

          // Try to highlight the current parameter in bold. Hack apart the text based on (, comma, and ), then
          // put it back together again.
          //
          // @todo This will not be perfect when the text contains generic types with more than one type parameter
          // since they will have extra commas. 

          let text = if lines.Length = 0 then name else  lines.[0]
          let textL = text.Split '('
          if textL.Length <> 2 then text else
          //TODO: what was text0 used for?
          let _text0 = textL.[0]
          let text1 = textL.[1]
          let text1L = text1.Split ')'
          if text1L.Length <> 2 then text else
          let text10 = text1L.[0]
          let text11 = text1L.[1]
          let text10L =  text10.Split ','
          let text10L = text10L |> Array.mapi (fun i x -> if i = currentParameter then "<b>" + x + "</b>" else x)
          textL.[0] + "(" + String.Join(",", text10L) + ")" + text11

        let tooltipInfo = TooltipInformation(SummaryMarkup = description, SignatureMarkup = String.wrapText heading 80, FooterMarkup = paramDescription)
        return tooltipInfo }, cancellationToken = cancel)


/// Implements text editor extension for MonoDevelop that shows F# completion    
type FSharpTextEditorCompletion() =
  inherit CompletionTextEditorExtension()

  let keywordCompletionData =
      [for keyValuePair in KeywordList.keywordDescriptions do
         yield CompletionData(keyValuePair.Key, IconId("md-keyword"),keyValuePair.Value) ]

  let mutable suppressParameterCompletion = false
  let mutable lastCharDottedInto = false
         
  let compilerIdentifiers =
    let icon = Stock.Literal
    let compilerIdentifierCategory = Category "Compiler Identifiers"
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

  // Until we build some functionality around a reversing tokenizer that detect this and other contexts
  // A crude detection of being inside an auto property decl: member val Foo = 10 with get,$ set
  let isAnAutoProperty (_editor: TextEditor) _offset =
    false
  //TODO
  //  let lastStart = editor.FindPrevWordOffset(offset)
  //  let lastEnd = editor.FindCurrentWordEnd(lastStart)
  //  let lastWord = editor.GetTextBetween(lastStart, lastEnd)

  //  let prevStart = editor.FindPrevWordOffset(lastStart)
  //  let prevEnd = editor.FindCurrentWordEnd(prevStart)
  //  let previousWord = editor.GetTextBetween(prevStart, prevEnd)
  //  lastWord = "get" && previousWord = "with"

  let getCompletionData (symbols:FSharpSymbolUse list list) =
    let categories = Dictionary<string, Category>()
    let getOrAddCategory id =
      let found, item = categories.TryGetValue id
      if found then item
      else let cat = Category id 
           categories.Add (id,cat)
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
      | Function _ -> IconId("md-fs-field")
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
      | CorePatterns.Entity _ -> IconId("md-type")
      | _ -> Stock.Event

    let tryGetCategory (symbolUse : FSharpSymbolUse) =
      let category =
        try
            match symbolUse with
            | Constructor c ->
                Some c.EnclosingEntity.DisplayName
            | TypeAbbreviation _ta -> None
            | Class _cl -> None
            | Delegate _dl -> None
            | Event ev ->
                Some ev.EnclosingEntity.DisplayName
            | Property pr ->
                Some pr.EnclosingEntity.DisplayName
            | ActivePatternCase ap ->
                if ap.Group.Names.Count > 1 then
                  match ap.Group.EnclosingEntity with
                  | Some enclosing ->
                      enclosing.DisplayName
                      |> SymbolTooltips.escapeText
                      |> Some
                  | None -> None
                else None

            | UnionCase uc ->
                if uc.UnionCaseFields.Count > 1 then
                    uc.ReturnType.AbbreviatedType.Format symbolUse.DisplayContext
                    |> SymbolTooltips.escapeText
                    |> Some
                else None
                 
            | Function f ->
                Some f.EnclosingEntity.DisplayName
            | Operator o ->
                Some o.EnclosingEntity.DisplayName
            | Pattern p ->
                Some p.EnclosingEntity.DisplayName
            | ClosureOrNestedFunction _cl ->
                //Theres no link to a parent type for a closure (FCS limitation)
                None
            | Val v -> Some v.EnclosingEntity.DisplayName
            | Enum _ | Interface _ | Module _ | Namespace _ | Record _ | Union _ | ValueType _ | CorePatterns.Entity _ -> None
            | _ ->
                None
        with exn ->
            None
      category

    let symbolToCompletionData (symbols : FSharpSymbolUse list) =
      match symbols with
      | head :: tail ->
          let cd = FSharpMemberCompletionData(head.Symbol.DisplayName, symbolToIcon head, head, tail) :> CompletionData
          match tryGetCategory head with
          | Some c -> let category = getOrAddCategory c
                      cd.CompletionCategory <- category
          | None -> ()
          cd
      | _ -> null //FSharpTryAgainMemberCompletionData() :> ICompletionData

    symbols |> List.map symbolToCompletionData


  let codeCompletionCommandImpl(editor:TextEditor, documentContext:DocumentContext, context:CodeCompletionContext, allowAnyStale, dottedInto, ctrlSpace, completionChar) =
    async {
      let result = CompletionDataList()
      let fileName = documentContext.Name
      try
        // Try to get typed information from LanguageService (with the specified timeout)
        let stale = if allowAnyStale then AllowStaleResults.MatchingFileName else AllowStaleResults.MatchingSource
        let projectFile = documentContext.Project |> function null -> fileName | project -> project.FileName.ToString()
        let! typedParseResults = MDLanguageService.Instance.GetTypedParseResultWithTimeout(projectFile, fileName, editor.Text, stale, ServiceSettings.blockingTimeout)

        match typedParseResults with
        | None       -> () //TODOresult.Add(FSharpTryAgainMemberCompletionData())
        | Some tyRes ->
          // Get declarations and generate list for MonoDevelop
          let line, col, lineStr = editor.GetLineInfoFromOffset context.TriggerOffset
          match tyRes.GetDeclarationSymbols(line, col, lineStr) with
          | Some (symbols, _residue) ->
              let data = getCompletionData symbols
              result.AddRange data
          | None -> ()
      with
      | :? Threading.Tasks.TaskCanceledException -> ()
      | e ->
          LoggingService.LogError ("FSharpTextEditorCompletion, An error occured in CodeCompletionCommandImpl", e)
          () //TODOresult.Add(FSharpErrorCompletionData(e))
      
      if completionChar = '.' then lastCharDottedInto <- dottedInto
      else
        // Add the code templates and compiler generated identifiers if the completion char is not '.'
        CodeTemplates.CodeTemplateService.AddCompletionDataForMime ("text/x-fsharp", result)
        result.AddRange compilerIdentifiers
        result.AddRange keywordCompletionData
      //If we are forcing completion ensure that AutoCompleteUniqueMatch is set
      if ctrlSpace then
        result.AutoCompleteUniqueMatch <- true
      return result :> ICompletionDataList }


  let isCurrentTokenInvalid (editor:TextEditor) (parsedDocument:TypeSystem.ParsedDocument) project offset =
    try
      let line, col, lineStr = editor.GetLineInfoFromOffset offset
      let filepath = (editor.FileName.ToString())
      let defines = CompilerArguments.getDefineSymbols filepath (project |> Option.ofNull)

      let quickLine = 
        maybe { 
          let! parsedDoc = parsedDocument |> Option.ofNull
          let! fsparsedDoc = parsedDoc |> Option.tryCast<FSharpParsedDocument>
          let! tokenisedLines = fsparsedDoc.Tokens
          let (Tokens.TokenisedLine(_lineNo, _lineOffset, _tokens, state)) = tokenisedLines.[line-1]
          let linedetail = Seq.singleton (Tokens.LineDetail(line, offset, lineStr))
          return Tokens.getTokensWithInitialState state linedetail filepath defines }

      let isTokenAtOffset col t = col-1 >= t.LeftColumn && col-1 <= t.RightColumn

      let caretToken = 
        match quickLine with
        | Some line ->
          //we have a line
          match line with
          | [single] ->
            let (Tokens.TokenisedLine(_lineNumber, _offset, lineTokens, _state)) = single
            lineTokens |> List.tryFind (isTokenAtOffset col)
          | _ -> None //should only be one
        | None ->
          let lineDetails =
            [ for i in 1..line do
                let line = editor.GetLine(i)
                yield Tokens.LineDetail(line.LineNumber, line.Offset, editor.GetTextAt(line.Offset, line.Length)) ]
          let tokens = Tokens.getTokens lineDetails filepath defines
          let (Tokens.TokenisedLine(_lineNumber, _offset, lineTokens, _state)) = tokens.[line-1]
          lineTokens |> List.tryFind (isTokenAtOffset col)

      let isTokenInvalid = 
        match caretToken with
        | Some token -> token.ColorClass = FSharpTokenColorKind.Comment ||
                        token.ColorClass = FSharpTokenColorKind.String ||
                        token.ColorClass = FSharpTokenColorKind.Text
        | None -> true
      isTokenInvalid
    with ex -> true

  //only used for testing
  member x.Initialize(editor, context) =
    x.DocumentContext <- context
    x.Editor <- editor

  override x.CompletionLanguage = "F#"
  override x.Initialize() = 
    do x.Editor.SetIndentationTracker (FSharpIndentationTracker(base.Editor))
    base.Initialize()

  /// Provide parameter and method overload information when you type '(', '<' or ','
  override x.HandleParameterCompletionAsync (context, _completionChar, token) =
    //TODO refactor computation to remove some return statements (clarity)
    Async.StartAsTask (cancellationToken = token, computation = async {
    try
        if suppressParameterCompletion then
           suppressParameterCompletion <- false
           return ParameterHintingResult.Empty
        else
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
        LoggingService.LogDebug("FSharpTextEditorCompletion: Getting Parameter Info, startOffset = {0}", startOffset)
        
        // Try to get typed result - within the specified timeout
        let! methsOpt =
            async {let projectFile = x.DocumentContext.Project |> function null -> x.DocumentContext.Name | project -> project.FileName.ToString()
                   let! tyRes = MDLanguageService.Instance.GetTypedParseResultWithTimeout (projectFile,
                                                                                           x.DocumentContext.Name,
                                                                                           docText,
                                                                                           AllowStaleResults.MatchingFileName,
                                                                                           ServiceSettings.blockingTimeout) 
                   match tyRes with
                   | Some tyRes ->
                       let line, col, lineStr = x.Editor.GetLineInfoFromOffset (startOffset)
                       let! methsOpt = tyRes.GetMethods(line, col, lineStr)
                       //TODO: Use the symbol version when available
                       //let! allMethodSymbols = tyRes.GetMethodsAsSymbols (line, col, lineStr)
                       return methsOpt
                   | None -> return None}
        
        match methsOpt with
        | Some(name, meths) when meths.Length > 0 -> 
            LoggingService.LogInfo ("FSharpTextEditorCompletion: Getting Parameter Info: {0} methods", meths.Length)
            let hintingData =
                meths
                |> Array.map (fun meth -> FSharpParameterHintingData (name, meth) :> ParameterHintingData)
                |> ResizeArray.ofArray

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
    // Avoid two dots in sucession turning inte ie '.CompareWith.' instead of '..'
    let suppressMemberCompletion = lastCharDottedInto && descriptor.KeyChar = '.'
    lastCharDottedInto <- false
    if suppressMemberCompletion then true else
    // base.KeyPress will execute RunParameterCompletionCommand
    // so suppress it here.  
    suppressParameterCompletion <- descriptor.KeyChar <> '(' && descriptor.KeyChar <> '<' && descriptor.KeyChar <> ','
    
    let result = base.KeyPress (descriptor)

    suppressParameterCompletion <- false
    if (descriptor.KeyChar = ')' && x.CanRunParameterCompletionCommand ()) then
      base.RunParameterCompletionCommand ()

    result

  // Run completion automatically when the user hits '.'
  // (this means that completion currently also works in comments and strings...)
  override x.HandleCodeCompletionAsync(context, completionChar, token) =
    if completionChar <> '.' then null else

    // We generally avoid forcing a re-typecheck on '.' by using TryGetRecentTypeCheckResults. Forcing a re-typecheck on a user action 
    // can cause a blocking delay in the UI (up to the blockingTimeout=500). We can't do it asynchronously because MD doesn't allow this yet). 
    //
    // However, if a '.' is pressed then in some situations the F# compiler language service really does need to re-typecheck, especially when
    // relying on 'expression' typings (rather than just name lookup). This is specifically the case on '.' because of this curious
    // line in service.fs: https://github.com/fsharp/fsharp/blob/0e80412570847e3e825b30f8636fc313ebaa3be0/src/fsharp/vs/service.fs#L771
    // where the location is adjusted by +1. However that location won't yield good expression typings because the results returned by 
    // TryGetRecentTypeCheckResults will be based on code where the '.' was not present.
    //
    // Because of this, we like to force re-typechecking in those situations where '.' is pressed AND when expression typings are highly 
    // likely to be useful. The most common example of this is where a character to the left of the '.' is not a letter. This catches 
    // situations like (abc + def).
    // Note, however, that this is just an approximation - for example no re-typecheck is enforced here: (abc + def).PPP.
    let computation = 
      async {
        if isCurrentTokenInvalid x.Editor x.DocumentContext.ParsedDocument x.DocumentContext.Project context.TriggerOffset then return null else
        
        let allowAnyStale = 
          match completionChar with 
          | '.' -> 
              let docText = x.Editor.Text
              if docText = null then true else
              let offset = context.TriggerOffset
              offset > 1 && Char.IsLetter (docText.[offset-2])
          | _ -> true 

        return! codeCompletionCommandImpl(x.Editor, x.DocumentContext, context, allowAnyStale, true, false, completionChar) }
    Async.StartAsTask (computation =computation, cancellationToken = token)

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context) =
    let completionChar = x.Editor.GetCharAt(context.TriggerOffset - 1)
    let completionIsDot = completionChar = '.'
    Async.StartAsTask(
      async {
        if isCurrentTokenInvalid x.Editor x.DocumentContext.ParsedDocument x.DocumentContext.Project context.TriggerOffset then return null
        else return! codeCompletionCommandImpl(x.Editor, x.DocumentContext,context, true, completionIsDot, true, completionChar) } )

  // Returns the index of the parameter where the cursor is currently positioned.
  // -1 means the cursor is outside the method parameter list
  // 0 means no parameter entered
  // > 0 is the index of the parameter (1-based)
  override x.GetCurrentParameterIndex (startOffset: int) = 
    let editor = x.Editor
    let cursor = editor.CaretOffset 
    let i = startOffset // the original context
    if (i < 0 || i >= editor.Length || editor.GetCharAt (i) = ')') then -1 
    elif (i + 1 = cursor && (match editor.GetCharAt(i) with '(' | '<' -> true | _ -> false)) then 0
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
      res

  interface IDebuggerExpressionResolver with
    member x.ResolveExpressionAsync (doc, context, offset, cancellationToken) =
      let computation = async {
        let ast = context.ParsedDocument.TryGetAst()
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
                  | CorePatterns.ActivePatternCase apc ->
                      Some (apc.DeclarationLocation, apc.DisplayName)
                  | CorePatterns.Entity ent ->
                      Some (ent.DeclarationLocation, ent.DisplayName)
                  | CorePatterns.Field field ->
                      Some (field.DeclarationLocation, field.DisplayName)
                  | CorePatterns.GenericParameter _gp -> None
                  //| CorePatterns.MemberFunctionOrValue
                  | CorePatterns.Parameter _p -> None
                  | CorePatterns.StaticParameter _sp -> None
                  | CorePatterns.UnionCase _uc -> None
                  | ExtendedPatterns.Class _c -> None
                  | ExtendedPatterns.ClosureOrNestedFunction _cl -> None
                  | ExtendedPatterns.Constructor _ctor -> None
                  | ExtendedPatterns.Delegate _del -> None
                  | ExtendedPatterns.Enum _enum -> None
                  | ExtendedPatterns.Event _ev -> None
                  | ExtendedPatterns.Function _f -> None
                  | ExtendedPatterns.Interface _i -> None
                  | ExtendedPatterns.Module _m -> None
                  | ExtendedPatterns.Namespace _ns -> None
                  | ExtendedPatterns.Operator _op -> None
                  | ExtendedPatterns.Pattern _p -> None
                  | ExtendedPatterns.Property _pr ->
                      let loc = symbolUse.RangeAlternate
                      Some (loc, lineTxt.[loc.StartColumn..loc.EndColumn])
                  | ExtendedPatterns.Record r ->
                      let loc = r.DeclarationLocation
                      Some (loc, r.DisplayName)
                  | ExtendedPatterns.TypeAbbreviation _ta -> None
                  | ExtendedPatterns.Union _un -> None
                  | ExtendedPatterns.Val v ->
                      let loc = v.DeclarationLocation
                      Some (loc, v.DisplayName)
                  | ExtendedPatterns.ValueType _vt -> None
                  | _ -> None
              | _ -> None
        match location with
        | None -> return DebugDataTipInfo()
        | Some (range, name) ->
          let ts = Symbols.getTextSpan range doc
          return DebugDataTipInfo(ts, name)}

      Async.StartAsTask (computation, cancellationToken = cancellationToken)