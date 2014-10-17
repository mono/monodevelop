// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open System.Linq
open System.Diagnostics
open MonoDevelop.Core
open MonoDevelop.Components
open MonoDevelop.Debugger
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.CodeCompletion
open MonoDevelop.Ide.CodeTemplates
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding
open ICSharpCode.NRefactory.Editor
open ICSharpCode.NRefactory.Completion
open MonoDevelop.FSharp.NRefactory

open ICSharpCode.NRefactory.Semantics
open ICSharpCode.NRefactory.TypeSystem
open ICSharpCode.NRefactory.TypeSystem.Implementation

/// A list of completions is returned.  Contains title and can generate description (tool-tip shown on the right) of the item.
/// Description is generated lazily because it is quite slow and there can be numerous.
type internal FSharpMemberCompletionData(name, getTip, glyph) =
    inherit CompletionData(CompletionText = Lexhelp.Keywords.QuoteIdentifierIfNeeded name, 
                           DisplayText = name, 
                           DisplayFlags = DisplayFlags.DescriptionHasMarkup)

    let icon = lazy (MonoDevelop.Core.IconId(ServiceUtils.getIcon glyph))

    new (name, datatip:FSharpToolTipText, glyph) = new FSharpMemberCompletionData(name, (fun () -> datatip), glyph)
    new (mi:FSharpDeclaration) =  new FSharpMemberCompletionData(mi.Name, (fun () -> mi.DescriptionText), mi.Glyph)

    override x.Description = name //description.Value   // this is not used
    override x.Icon = icon.Value

    /// Check if the datatip has multiple overloads
    override x.HasOverloads = 
        match getTip() with 
        | FSharpToolTipText [xs] ->
            match xs with 
            | FSharpToolTipElement.Group ttg -> true 
            | _ -> false
        | FSharpToolTipText list -> true

    /// Split apart the elements into separate overloads
    override x.OverloadedData =
        match getTip() with 
        | FSharpToolTipText xs -> 
            seq{for tooltipElement in xs do
                match tooltipElement with
                | FSharpToolTipElement.Single(a, b) -> yield FSharpMemberCompletionData(name, FSharpToolTipText[tooltipElement], glyph) :> _
                | FSharpToolTipElement.Group(items) ->
                  let overloads =
                      items 
                      |> Seq.map (fun args -> FSharpMemberCompletionData(name, FSharpToolTipText[FSharpToolTipElement.Single(args)], glyph)) 
                      |> Seq.cast
                  yield! overloads
                | FSharpToolTipElement.CompositionError error ->
                    //show the composition error in the tip, hiding it makes it more difficult to diagnose
                    yield FSharpMemberCompletionData(name, FSharpToolTipText[FSharpToolTipElement.Single(error,FSharpXmlDoc.None)], 0) :> _
                | FSharpToolTipElement.None -> () }


    override x.AddOverload (data: ICompletionData) = ()//not currently called

    // TODO: what does 'smartWrap' indicate?
    override x.CreateTooltipInformation (smartWrap: bool) = 
      
      Debug.WriteLine("computing tooltip for {0}", name)
      let description = TipFormatter.formatTip (getTip())
      match description with
      | [signature,comment] -> TooltipInformation(SummaryMarkup = comment, SignatureMarkup = signature)
      //With multiple tips just take the head.  
      //This shouldnt happen anyway as we split them in the resolver provider
      | multiple -> multiple |> List.head |> (fun (signature,comment) -> TooltipInformation(SummaryMarkup = comment, SignatureMarkup = signature))


/// Completion data representing a delayed fetch of completion data
type internal FSharpTryAgainMemberCompletionData() =
    inherit CompletionData(CompletionText = "", DisplayText="Declarations list not yet available...", DisplayFlags = DisplayFlags.None )
    override x.Description = "The declaration list is not yet available or the operation timed out. Try again?"     
    override x.Icon =  new MonoDevelop.Core.IconId("md-event")

/// Completion data representing a failure in the ability to get completion data
type internal FSharpErrorCompletionData(exn:exn) =
    inherit CompletionData(CompletionText = "", DisplayText=exn.Message, DisplayFlags = DisplayFlags.None )
    let text = exn.ToString()
    override x.Description = text
    override x.Icon =  new MonoDevelop.Core.IconId("md-event")

/// Provide information to the 'method overloads' windows that comes up when you type '('
type ParameterDataProvider(nameStart: int, name, meths : FSharpMethodGroupItem array) = 
    inherit MonoDevelop.Ide.CodeCompletion.ParameterDataProvider (nameStart)
    override x.Count = meths.Length
        /// Returns the markup to use to represent the specified method overload
        /// in the parameter information window.
    override x.CreateTooltipInformation (overload:int, currentParameter:int, smartWrap:bool) = 
        // Get the lower part of the text for the display of an overload
        let meth = meths.[overload]
        let signature, comment =
            match TipFormatter.formatTip meth.Description with
            | [signature,comment] -> signature,comment
            //With multiple tips just take the head.  
            //This shouldnt happen anyway as we split them in the resolver provider
            | multiple -> multiple |> List.head |> (fun (signature,comment) -> signature,comment)

        let description =
            let param = 
                meth.Parameters |> Array.mapi (fun i param -> 
                    let paramDesc = 
                        // Sometimes the parameter decription is hidden in the XML docs
                        match TipFormatter.extractParamTip param.ParameterName meth.Description with 
                        | Some(tip) -> tip
                        | None -> param.Description
                    let name = if i = currentParameter then  "<b>" + param.ParameterName + "</b>" else param.ParameterName
                    let text = name + ": " + GLib.Markup.EscapeText paramDesc
                    text )
            if String.IsNullOrEmpty comment then String.Join("\n", param)
            else comment + "\n" + String.Join("\n", param)
            
        
        // Returns the text to use to represent the specified parameter
        let paramDescription = 
            let meth = meths.[overload]
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
            let text0 = textL.[0]
            let text1 = textL.[1]
            let text1L = text1.Split ')'
            if text1L.Length <> 2 then text else
            let text10 = text1L.[0]
            let text11 = text1L.[1]
            let text10L =  text10.Split ','
            let text10L = text10L |> Array.mapi (fun i x -> if i = currentParameter then "<b>" + x + "</b>" else x)
            textL.[0] + "(" + String.Join(",", text10L) + ")" + text11

        let tooltipInfo = TooltipInformation(SummaryMarkup   = description,
                                             SignatureMarkup = heading,
                                             FooterMarkup    = paramDescription)
        tooltipInfo

    /// Returns the number of parameters of the specified method
    override x.GetParameterCount(overload:int) = 
        let meth = meths.[overload]
        meth.Parameters.Length
        
    // @todo should return 'true' for param-list methods
    override x.AllowParameterList (overload: int) = 
        false

    override x.GetParameterName (overload:int, paramIndex:int) =
        let meth = meths.[overload]
        let prm = meth.Parameters.[paramIndex]
        prm.ParameterName

/// Implements text editor extension for MonoDevelop that shows F# completion    
type FSharpTextEditorCompletion() =
  inherit CompletionTextEditorExtension()

  let mutable suppressParameterCompletion = false
  let mutable lastCharDottedInto = false

  let compilerIdentifierCategory =
      {new CompletionCategory("Compiler Identifiers", "") with
            member x.CompareTo _ = 0 }
        
  let compilerIdentifiers =
      let icon = IconId("md-field")
      [CompletionData("__LINE__",
                      icon,
                      "Evaluates to the current line number, considering <tt>#line</tt> directives.",
                      CompletionCategory = compilerIdentifierCategory, 
                      DisplayFlags = DisplayFlags.DescriptionHasMarkup) :> ICompletionData
       CompletionData("__SOURCE_DIRECTORY__",
                      icon,
                      "Evaluates to the current full path of the source directory, considering <tt>#line</tt> directives.",
                      CompletionCategory = compilerIdentifierCategory, 
                      DisplayFlags = DisplayFlags.DescriptionHasMarkup) :> _
       CompletionData("__SOURCE_FILE__",
                      icon,
                      "Evaluates to the current source file name and its path, considering <tt>#line</tt> directives.",
                      CompletionCategory = compilerIdentifierCategory,
                      DisplayFlags = DisplayFlags.DescriptionHasMarkup) :> _ ]


  override x.ExtendsEditor(doc:Document, editor:IEditableTextBuffer) =
    // Extend any text editor that edits F# files
    CompilerArguments.supportedExtension(IO.Path.GetExtension(doc.FileName.ToString()))

  override x.Initialize() = 
      do base.Document.Editor.IndentationTracker <- FSharpIndentationTracker(base.Document) :> Mono.TextEditor.IIndentationTracker 
      base.Initialize()

  /// Provide parameter and method overload information when you type '(', '<' or ','
  override x.HandleParameterCompletion(context:CodeCompletionContext, completionChar:char) : MonoDevelop.Ide.CodeCompletion.ParameterDataProvider =
    try
      if suppressParameterCompletion then
         suppressParameterCompletion <- false
         null
      else
      let doc = x.Document
      let docText = doc.Editor.Text
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

      if docText = null || offset > docText.Length || startOffset < 0 || offset <= 0 then 
        null 
      else
      Debug.WriteLine("Getting Parameter Info, startOffset = {0}", startOffset)


      // Try to get typed result - with the specified timeout
      let projFile, files, args, framework = MonoDevelop.getCheckerArgs(doc.Project, doc.FileName.FullPath.ToString())
      let typedParseResults =
        MDLanguageService.Instance.GetTypedParseResultWithTimeout(projFile, doc.FileName.FullPath.ToString(), docText, files, args, AllowStaleResults.MatchingFileName, ServiceSettings.blockingTimeout, framework) 
        |> Async.RunSynchronously

      match typedParseResults with
      | None -> null
      | Some tyRes ->
      let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(startOffset, doc.Editor.Document)
      let methsOpt = tyRes.GetMethods(line, col, lineStr) |> Async.RunSynchronously
      match methsOpt with 
      | None -> 
          Debug.WriteLine("Getting Parameter Info: no methods")
          null 
      | Some(name, meths) -> 
          Debug.WriteLine("Getting Parameter Info: methods!")
          new ParameterDataProvider (startOffset, name, meths) :> _ 
    with _ -> null

  override x.KeyPress (key, keyChar, modifier) =
      // Avoid two dots in sucession turning inte ie '.CompareWith.' instead of '..'
      let suppressMemberCompletion = lastCharDottedInto && keyChar = '.'
      lastCharDottedInto <- false
      if suppressMemberCompletion then true else
      // base.KeyPress will execute RunParameterCompletionCommand,
      // so suppress it here.  
      suppressParameterCompletion <-
         keyChar <> '(' && keyChar <> '<' && keyChar <> ','
      
      let result = base.KeyPress (key, keyChar, modifier)

      suppressParameterCompletion <- false
      if (keyChar = ')' && x.CanRunParameterCompletionCommand ()) then
          base.RunParameterCompletionCommand ()

      result

  // Run completion automatically when the user hits '.'
  // (this means that completion currently also works in comments and strings...)
  override x.HandleCodeCompletion(context, ch, triggerWordLength:byref<int>) =
      if ch <> '.' then null else

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
          match ch with 
          | '.' -> 
              let doc = x.Document
              let docText = doc.Editor.Text
              if docText = null then true else
              let offset = context.TriggerOffset
              offset > 1 && Char.IsLetter (docText.[offset-2])
          | _ -> true

      Debug.WriteLine("allowAnyStale = {0}", allowAnyStale)

      x.CodeCompletionCommandImpl(context, allowAnyStale, dottedInto = true, ctrlSpace = false)

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context) =
      x.CodeCompletionCommandImpl(context, allowAnyStale = true, dottedInto = false, ctrlSpace = true)

  member x.CodeCompletionCommandImpl(context, allowAnyStale, dottedInto, ctrlSpace) =
    let result = CompletionDataList()
    let doc = x.Document
    try
      let projFile, files, args, framework = MonoDevelop.getCheckerArgs(doc.Project, doc.FileName.FullPath.ToString())
      // Try to get typed information from LanguageService (with the specified timeout)
      let stale = if allowAnyStale then AllowStaleResults.MatchingFileName else AllowStaleResults.MatchingSource
      let typedParseResults = 
          MDLanguageService.Instance.GetTypedParseResultWithTimeout(projFile, doc.FileName.FullPath.ToString(), doc.Editor.Text, files, args, stale, ServiceSettings.blockingTimeout, framework)
          |> Async.RunSynchronously
      lastCharDottedInto <- dottedInto
      match typedParseResults with
      | None       -> result.Add(FSharpTryAgainMemberCompletionData())
      | Some tyRes ->
        // Get declarations and generate list for MonoDevelop
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(context.TriggerOffset, doc.Editor.Document)
        match tyRes.GetDeclarations(line, col, lineStr) with
        | Some(decls, residue) when decls.Items.Any() ->
              let items = decls.Items
                          |> Array.map (fun mi -> FSharpMemberCompletionData(mi) :> ICompletionData)
              result.AddRange(items)
        | _ -> ()
    with
    | e -> result.Add(FSharpErrorCompletionData(e))
    
    // Add the code templates and compiler generated identifiers
    if not dottedInto then
      let templates = CodeTemplateService.GetCodeTemplatesForFile(doc.FileName.ToString())
                      |> Seq.map (fun t -> CodeTemplateCompletionData(doc, t))
                      |> Seq.cast<ICompletionData>
     
      result.AddRange (templates)
      result.AddRange (compilerIdentifiers)

    //If we are forcing completion ensure that AutoCompleteUniqueMatch is set
    if ctrlSpace then
        result.AutoCompleteUniqueMatch <- true
    result :> ICompletionDataList

  // T find out what this is used for
  override x.GetParameterCompletionCommandOffset(cpos:byref<int>) = false
    
  // Returns the index of the parameter where the cursor is currently positioned.
  // -1 means the cursor is outside the method parameter list
  // 0 means no parameter entered
  // > 0 is the index of the parameter (1-based)
  override x.GetCurrentParameterIndex (startOffset: int) = 
      let editor = x.Document.Editor
      let cursor = editor.Caret.Offset
      let i = startOffset // the original context
      if (i < 0 || i >= editor.Length || editor.GetCharAt (i) = ')') then -1 
      elif (i + 1 = cursor && (match editor.Document.GetCharAt(i) with '(' | '<' -> true | _ -> false)) then 0
      else 
          // The first character is a '('
          // Note this will be confused by comments.
          let rec loop depth i parameterIndex = 
              if (i = cursor) then parameterIndex
              elif (i > cursor) then -1 
              elif (i >= editor.Document.TextLength) then  parameterIndex else
              let ch = editor.Document.GetCharAt(i)
              if (ch = '(' || ch = '{' || ch = '[') then loop (depth+1) (i+1) parameterIndex
              elif ((ch = ')' || ch = '}' || ch = ']') && depth > 1 ) then loop (depth-1) (i+1) parameterIndex
              elif (ch = ',' && depth = 1) then loop depth (i+1) (parameterIndex+1)
              elif (ch = ')' || ch = '>') then -1
              else loop depth (i+1) parameterIndex
          let res = loop 0 i 1
          res

  interface IDebuggerExpressionResolver with
    member x.ResolveExpression(editor, doc, offset, startOffset) =

        let resolveResult, dom = doc.GetLanguageItem(offset)
        match resolveResult.GetSymbol() with
        //we are only going to process FSharpResolvedVariable types all other types will not be resolved.
        //This will cause the tooltip to be displayed as usual for member lookups etc.  
        | :? FSharpResolvedVariable as resolvedVariable ->
            startOffset <- dom.BeginColumn
            (resolvedVariable :> IVariable).Name
        | _ -> startOffset <- -1
               null
