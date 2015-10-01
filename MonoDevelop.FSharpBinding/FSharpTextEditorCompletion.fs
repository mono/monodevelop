// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Diagnostics
open Symbols
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

  override x.AddOverload (_data) = () //not currently called

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

//TODO: finish porting symbol version
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

  /// Returns the markup to use to represent the method overload in the parameter information window.
  override x.CreateTooltipInformation (_editor, _context, currentParameter:int, _smartWrap:bool, cancel) =
    Async.StartAsTask(
      async {
        // Get the lower part of the text for the display of an overload
        let signature, comment =
            match TooltipFormatting.formatTip meth.Description with
            | [signature,comment] -> signature,comment
            //With multiple tips just take the head.  
            //This shouldnt happen as we split them in the resolver provider
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

          // Try to highlight the current parameter in bold. Hack apart the text based on (, comma, and ),
          // then put it back together again.
          //
          // @todo This will not be perfect when the text contains generic types with more than one type
          // parameter since they will have extra commas. 

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

  // Until we build some functionality around a reversing tokenizer that detect this and other contexts
  // A crude detection of being inside an auto property decl: member val Foo = 10 with get,$ set
  let isAnAutoProperty (_editor: TextEditor) _offset =
    //let line, col, txt = editor.GetLineInfoFromOffset(offset)
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
                let ent = c.EnclosingEntity.UnAnnotate()
                Some (ent.DisplayName, ent)
            | Event ev ->
                let ent = ev.EnclosingEntity.UnAnnotate()
                Some (ent.DisplayName, ent)
            | Property pr ->
                let ent  = pr.EnclosingEntity.UnAnnotate()
                Some (ent.DisplayName, ent)
            | ActivePatternCase ap ->
                if ap.Group.Names.Count > 1 then
                  match ap.Group.EnclosingEntity with
                  | Some enclosing ->
                      let ent = enclosing.UnAnnotate()
                      Some(SymbolTooltips.escapeText ent.DisplayName, ent)
                  | None -> None
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
                  let real = f.EnclosingEntity.UnAnnotate()
                  Some(real.DisplayName, real)
            | Operator o ->
                let ent = o.EnclosingEntity.UnAnnotate()
                Some (ent.DisplayName, ent)
            | Pattern p ->
                let ent = p.EnclosingEntity.UnAnnotate()
                Some (ent.DisplayName, ent)
            | Val v ->
                let ent = v.EnclosingEntity.UnAnnotate()
                Some (ent.DisplayName, ent)
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
          | Some (c, ent) -> 
              let category = getOrAddCategory ent c
              cd.CompletionCategory <- category
          | None -> ()
          cd
      | _ -> null //FSharpTryAgainMemberCompletionData() :> ICompletionData
    symbols |> List.map symbolToCompletionData 


  let codeCompletionCommandImpl(editor:TextEditor, documentContext:DocumentContext, context:CodeCompletionContext, dottedInto, ctrlSpace, completionChar) =
    async {
      let result = CompletionDataList()
      result.IsSorted <- true
      let filename = documentContext.Name
      try
        // Try to get typed information from LanguageService (with the specified timeout)
        let projectFile = documentContext.Project |> function null -> filename | project -> project.FileName.ToString()

        let curVersion = editor.Version
        let isObsolete =
            IsResultObsolete(fun () -> 
            let doc = IdeApp.Workbench.GetDocument(filename)
            let newVersion = doc.Editor.Version
            if newVersion.BelongsToSameDocumentAs(curVersion) && newVersion.CompareAge(curVersion) = 0
            then
              LoggingService.LogDebug ("FSharpTextEditorCompletion - codeCompletionCommandImpl: type check of {0} is not obsolete",  IO.Path.GetFileName filename)
              false
            else
              LoggingService.LogDebug ("FSharpTextEditorCompletion - codeCompletionCommandImpl: type check of {0} is obsolete, cancelled", IO.Path.GetFileName filename)
              true )

        let! typedParseResults =
          MDLanguageService.Instance.GetTypedParseResultWithTimeout(projectFile,
                                                                    filename,
                                                                    editor.Text,
                                                                    AllowStaleResults.MatchingSource,
                                                                    ServiceSettings.maximumTimeout,
                                                                    isObsolete)

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
    do x.Editor.SetIndentationTracker (FSharpIndentationTracker(base.Editor))
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

          let filename =x.DocumentContext.Name
          let curVersion = x.Editor.Version
          let isObsolete =
              IsResultObsolete(fun () -> 
              let doc = IdeApp.Workbench.GetDocument(filename)
              let newVersion = doc.Editor.Version
              if newVersion.BelongsToSameDocumentAs(curVersion) && newVersion.CompareAge(curVersion) = 0
              then
                LoggingService.LogDebug ("FSharpTextEditorCompletion - HandleParameterCompletionAsync: type check of {0} is not obsolete",  IO.Path.GetFileName filename)
                false
              else
                LoggingService.LogDebug ("FSharpTextEditorCompletion - HandleParameterCompletionAsync: type check of {0} is obsolete, cancelled", IO.Path.GetFileName filename)
                true )

          // Try to get typed result - within the specified timeout
          let! methsOpt =
              async {let projectFile = x.DocumentContext.Project |> function null -> filename | project -> project.FileName.ToString()
                     let! tyRes = MDLanguageService.Instance.GetTypedParseResultWithTimeout (projectFile,
                                                                                             filename,
                                                                                             docText,
                                                                                             AllowStaleResults.MatchingSource,
                                                                                             ServiceSettings.maximumTimeout,
                                                                                             isObsolete) 
                     match tyRes with
                     | Some tyRes ->
                         let line, col, lineStr = x.Editor.GetLineInfoFromOffset (startOffset)
                         let! methsOpt = tyRes.GetMethods(line, col, lineStr)
                         //TODO: Use the symbol version
                         //let! allMethodSymbols = tyRes.GetMethodsAsSymbols (line, col, lineStr)
                         return methsOpt
                     | None -> return None}
          
          match methsOpt with
          | Some(name, meths) when meths.Length > 0 -> 
              LoggingService.LogDebug ("FSharpTextEditorCompletion: Getting Parameter Info: {0} methods", meths.Length)
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
    // base.KeyPress will execute RunParameterCompletionCommand so suppress it here.  

    suppressParameterCompletion <- not (isValidParamCompletionDecriptor descriptor)
    base.KeyPress (descriptor)
//
//    suppressParameterCompletion <- false
//    if (descriptor.KeyChar = ')' && x.CanRunParameterCompletionCommand ()) then
//      base.RunParameterCompletionCommand ()
//
//    result

  // Run completion automatically when the user hits '.'
  // (this means that completion currently also works in comments and strings...)
  override x.HandleCodeCompletionAsync(context, completionChar, token) =
    if completionChar <> '.' then null else
    let computation = 
      async {
        if isCurrentTokenInvalid x.Editor x.DocumentContext.ParsedDocument x.DocumentContext.Project context.TriggerOffset then return null else
        return! codeCompletionCommandImpl(x.Editor, x.DocumentContext, context, true, false, completionChar) }
    Async.StartAsTask (computation =computation, cancellationToken = token)

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context) =
    let completionChar = x.Editor.GetCharAt(context.TriggerOffset - 1)
    let completionIsDot = completionChar = '.'
    Async.StartAsTask(
      async {
        if isCurrentTokenInvalid x.Editor x.DocumentContext.ParsedDocument x.DocumentContext.Project context.TriggerOffset then return null
        else return! codeCompletionCommandImpl(x.Editor, x.DocumentContext,context, completionIsDot, true, completionChar) } )

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
                  | SymbolUse.ActivePatternCase apc ->
                      Some (apc.DeclarationLocation, apc.DisplayName)
                  | SymbolUse.Entity ent ->
                      Some (ent.DeclarationLocation, ent.DisplayName)
                  | SymbolUse.Field field ->
                      Some (field.DeclarationLocation, field.DisplayName)
                  | SymbolUse.GenericParameter _gp -> None
                  //| CorePatterns.MemberFunctionOrValue
                  | SymbolUse.Parameter _p -> None
                  | SymbolUse.StaticParameter _sp -> None
                  | SymbolUse.UnionCase _uc -> None
                  | SymbolUse.Class _c -> None
                  | SymbolUse.ClosureOrNestedFunction _cl -> None
                  | SymbolUse.Constructor _ctor -> None
                  | SymbolUse.Delegate _del -> None
                  | SymbolUse.Enum _enum -> None
                  | SymbolUse.Event _ev -> None
                  | SymbolUse.Function _f -> None
                  | SymbolUse.Interface _i -> None
                  | SymbolUse.Module _m -> None
                  | SymbolUse.Namespace _ns -> None
                  | SymbolUse.Operator _op -> None
                  | SymbolUse.Pattern _p -> None
                  | SymbolUse.Property _pr ->
                      let loc = symbolUse.RangeAlternate
                      Some (loc, lineTxt.[loc.StartColumn..loc.EndColumn])
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