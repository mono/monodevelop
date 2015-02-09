// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Diagnostics
open MonoDevelop.Core
open MonoDevelop.Debugger
open MonoDevelop.Ide
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.CodeTemplates
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding
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
        |> Seq.map (fun symbol -> FSharpMemberCompletionData(symbol.Symbol.DisplayName, icon, symbol, List.empty) :> _ )

    override x.AddOverload (data: CompletionData) = () //not currently called

    // TODO: what does 'smartWrap' indicate?
    override x.CreateTooltipInformation (smartWrap: bool) = 
      Debug.WriteLine("computing tooltip for {0}", name)
      let tip = SymbolTooltips.getTooltipFromSymbolUse symbol (lazy None)
      match tip  with
      | ToolTips.ToolTip (signature, xmldoc) ->
            let toolTipInfo = new TooltipInformation(SignatureMarkup = signature)
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
      | _ -> TooltipInformation()

type Category(category) =
    inherit CompletionCategory(category, null)
    override x.CompareTo other =
        if other = null then -1 else x.DisplayText.CompareTo other.DisplayText

/// Implements text editor extension for MonoDevelop that shows F# completion    
type FSharpTextEditorCompletion() =
  inherit CompletionTextEditorExtension()

  let mutable suppressParameterCompletion = false
  let mutable lastCharDottedInto = false
         
  let compilerIdentifiers =
      let icon = MonoDevelop.Ide.Gui.Stock.Literal
      let compilerIdentifierCategory = Category "Compiler Identifiers"
      [CompletionData("__LINE__",
                      icon,
                      "Evaluates to the current line number, considering <tt>#line</tt> directives.",
                      CompletionCategory = compilerIdentifierCategory, 
                      DisplayFlags = DisplayFlags.DescriptionHasMarkup)
       CompletionData("__SOURCE_DIRECTORY__",
                      icon,
                      "Evaluates to the current full path of the source directory, considering <tt>#line</tt> directives.",
                      CompletionCategory = compilerIdentifierCategory, 
                      DisplayFlags = DisplayFlags.DescriptionHasMarkup)
       CompletionData("__SOURCE_FILE__",
                      icon,
                      "Evaluates to the current source file name and its path, considering <tt>#line</tt> directives.",
                      CompletionCategory = compilerIdentifierCategory,
                      DisplayFlags = DisplayFlags.DescriptionHasMarkup)]

  // Until we build some functionality around a reversing tokenizer that detect this and other contexts
  // A crude detection of being inside an auto property decl: member val Foo = 10 with get,$ set
  let isAnAutoProperty (editor: Editor.TextEditor) offset =
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
          | ActivePatternCase _ -> MonoDevelop.Ide.Gui.Stock.Enum
          | Field _ -> MonoDevelop.Ide.Gui.Stock.Field
          | UnionCase _ -> IconId("md-type")
          | Class _ -> MonoDevelop.Ide.Gui.Stock.Class
          | Delegate _ -> MonoDevelop.Ide.Gui.Stock.Delegate
          | Constructor _  -> MonoDevelop.Ide.Gui.Stock.Method
          | Event _ -> MonoDevelop.Ide.Gui.Stock.Event
          | Property _ -> MonoDevelop.Ide.Gui.Stock.Property
          | Function _ -> IconId("md-fs-field")
          | Operator _ -> IconId("md-fs-field")
          | ClosureOrNestedFunction _ -> IconId("md-fs-field")
          | Val _ -> MonoDevelop.Ide.Gui.Stock.Field
          | Enum _ -> MonoDevelop.Ide.Gui.Stock.Enum
          | Interface _ -> MonoDevelop.Ide.Gui.Stock.Interface
          | Module _ -> IconId("md-module")
          | Namespace _ -> MonoDevelop.Ide.Gui.Stock.NameSpace
          | Record _ -> MonoDevelop.Ide.Gui.Stock.Class
          | Union _ -> IconId("md-type")
          | ValueType _ -> MonoDevelop.Ide.Gui.Stock.Struct
          | CorePatterns.Entity _ -> IconId("md-type")
          | _ -> MonoDevelop.Ide.Gui.Stock.Event

      let tryGetCategory (symbolUse : FSharpSymbolUse) =
          let category =
            try
                match symbolUse with
                | Constructor c ->
                    Some c.EnclosingEntity.DisplayName
                | TypeAbbreviation ta -> None
                | Class cl -> None
                | Delegate dl -> None
                | Event ev ->
                    Some ev.EnclosingEntity.DisplayName
                | Property pr ->
                    Some pr.EnclosingEntity.DisplayName
                | ActivePatternCase ap ->
                    ap.Name
                    |> SymbolTooltips.escapeText
                    |> Some

                | UnionCase uc ->
                     uc.ReturnType.AbbreviatedType.Format symbolUse.DisplayContext
                     |> SymbolTooltips.escapeText
                     |> Some

                | Function f ->
                    Some f.EnclosingEntity.DisplayName
                | Operator o ->
                    Some o.EnclosingEntity.DisplayName
                | Pattern p ->
                    Some p.EnclosingEntity.DisplayName
                | ClosureOrNestedFunction cl ->
                    //Theres no link to a parent type for a closure (FCS limitation)
                    None
                | Val _ |  Enum _ | Interface _ | Module _ | Namespace _
                | Record _ | Union _ | ValueType _ | CorePatterns.Entity _ -> None
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
           return null
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
        
        if docText = null || offset > docText.Length || startOffset < 0 || offset <= 0 || isAnAutoProperty x.Editor offset then return null 
        else
        LoggingService.LogDebug("FSharpTextEditorCompletion: Getting Parameter Info, startOffset = {0}", startOffset)
        
        let projFile, files, args = MonoDevelop.getCheckerArgs(x.DocumentContext.Project, x.DocumentContext.Name)
        
        // Try to get typed result - within the specified timeout
        let! methsOpt =
            async {let! tyRes = MDLanguageService.Instance.GetTypedParseResultAsync (projFile, x.DocumentContext.Name, docText, files, args, AllowStaleResults.MatchingFileName) 
                   let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(startOffset, x.Editor)
                   let! methsOpt = tyRes.GetMethods(line, col, lineStr)
                   return methsOpt }
        
        match methsOpt with 
        | Some(name, meths) when meths.Length > 0 -> 
            LoggingService.LogInfo ("FSharpTextEditorCompletion: Getting Parameter Info: {0} methods", meths.Length)
            //TODO Convert meths to ParameterHintingData
            let data = ResizeArray()
            return ParameterHintingResult(data, startOffset) 
        | _ -> LoggingService.LogWarning("FSharpTextEditorCompletion: Getting Parameter Info: no methods found")
               return null 
    with ex ->
        LoggingService.LogError ("FSharpTextEditorCompletion: Error in HandleParameterCompletion", ex)
        return null})

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

      // We generally avoids forcing a re-typecheck on '.' presses by using
      // TryGetRecentTypeCheckResults. Forcing a re-typecheck on a user action can cause a 
      // blocking delay in the UI (up to the blockingTimeout=500). We can't do it asynchronously 
      // because MD doesn't allow this yet). 
      //
      // However, if the '.' is pressed then in some situations the F# compiler language service 
      // really does need to re-typecheck, especially when relying on 'expression' typings 
      // (rather than just name lookup). This is specifically the case on '.' because of this curious
      // line in service.fs:
      //   https://github.com/fsharp/fsharp/blob/0e80412570847e3e825b30f8636fc313ebaa3be0/src/fsharp/vs/service.fs#L771
      // where the location is adjusted by +1. However that location won't yield good expression typings 
      // because the resuls returned by TryGetRecentTypeCheckResults will be based on code where the 
      // '.' was not present.
      //
      // Because of this, we like to force re-typechecking in those situations where '.' is pressed 
      // AND when expression typings are highly likely to be useful. The most common example of this
      // is where a character to the left of the '.' is not a letter. This catches situations like
      //     (abc + def).
      // Note, however, that this is just an approximation - for example no re-typecheck is enforced here:
      //      (abc + def).PPP.
      
      let allowAnyStale = 
          match completionChar with 
          | '.' -> 
              let docText = x.Editor.Text
              if docText = null then true else
              let offset = context.TriggerOffset
              offset > 1 && Char.IsLetter (docText.[offset-2])
          | _ -> true

      Debug.WriteLine("allowAnyStale = {0}", allowAnyStale)
      x.CodeCompletionCommandImpl(context, allowAnyStale, token, true, false, completionChar)

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context) =
      let completionChar = x.Editor.GetCharAt(context.TriggerOffset - 1)
      let completionIsDot = completionChar = '.'
      x.CodeCompletionCommandImpl(context, true, Async.DefaultCancellationToken, completionIsDot, true, completionChar)
       .Result

  member x.CodeCompletionCommandImpl(context, allowAnyStale, token, dottedInto, ctrlSpace, completionChar) =
    Async.StartAsTask (cancellationToken = token, computation = async {
    let result = CompletionDataList()
    let fileName = x.DocumentContext.Name
    try
      let projFile, files, args = MonoDevelop.getCheckerArgs(x.DocumentContext.Project, fileName)
      // Try to get typed information from LanguageService (with the specified timeout)
      let stale = if allowAnyStale then AllowStaleResults.MatchingFileName else AllowStaleResults.MatchingSource
      let! typedParseResults = MDLanguageService.Instance.GetTypedParseResultWithTimeout(projFile, fileName, x.Editor.Text, files, args, stale, ServiceSettings.blockingTimeout)

      match typedParseResults with
      | None       -> () //TODOresult.Add(FSharpTryAgainMemberCompletionData())
      | Some tyRes ->
        // Get declarations and generate list for MonoDevelop
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(context.TriggerOffset, x.Editor)
        match tyRes.GetDeclarationSymbols(line, col, lineStr) with
        | Some (symbols, _residue) ->
            let data = getCompletionData symbols
            result.AddRange data
        | None -> ()
    with
    | :? System.Threading.Tasks.TaskCanceledException -> ()
    | e ->
        LoggingService.LogError ("FSharpTextEditorCompletion, An error occured in CodeCompletionCommandImpl", e)
        () //TODOresult.Add(FSharpErrorCompletionData(e))
    
    if completionChar = '.' then lastCharDottedInto <- dottedInto
    else
        // Add the code templates and compiler generated identifiers if the completion char is not '.'
        CodeTemplates.CodeTemplateService.AddCompletionDataForMime ("text/x-fsharp", result)
        result.AddRange (compilerIdentifiers)

    //If we are forcing completion ensure that AutoCompleteUniqueMatch is set
    if ctrlSpace then
        result.AutoCompleteUniqueMatch <- true
    return result :> ICompletionDataList})


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

//TODO interface IDebuggerExpressionResolver with
//  member x.ResolveExpression(editor, doc, offset, startOffset) =
//      let resolver = TextEditorResolverService.GetProvider(editor.MimeType)
//      let resolveResult, dom = resolver.GetLanguageItem(doc,offset)
//      match resolveResult.GetSymbol() with
//      //we are only going to process FSharpResolvedVariable types all other types will not be resolved.
//      //This will cause the tooltip to be displayed as usual for member lookups etc.  
//      | :? FSharpResolvedVariable as resolvedVariable ->
//          startOffset <- dom.BeginColumn
//          (resolvedVariable :> IVariable).Name
//      | _ -> startOffset <- -1
//             null
//
