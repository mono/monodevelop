// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.CodeCompletion
open Microsoft.FSharp.Compiler.SourceCodeServices

open ICSharpCode.NRefactory.Editor
open ICSharpCode.NRefactory.Completion

/// Item that is returned in a list of completions Contains title and can generate description (tool-tip shown on 
/// the right) of the item. Description should be generated lazily because this is quite slow (in the F# services)
type internal FSharpMemberCompletionData(mi:Declaration) =
    inherit CompletionData(CompletionText = (if mi.Name |> String.forall PrettyNaming.IsIdentifierPartCharacter then mi.Name else "``" + mi.Name + "``"), 
                           DisplayText = mi.Name, 
                           DisplayFlags = DisplayFlags.DescriptionHasMarkup)
    override x.Description = TipFormatter.formatTip false mi.DescriptionText    
    override x.Icon = new MonoDevelop.Core.IconId(ServiceUtils.getIcon mi.Glyph)

    // TODO: what does 'smartWrap' indicate?
    override x.CreateTooltipInformation (smartWrap: bool) = 
      let description = TipFormatter.formatTip false mi.DescriptionText
      let lines = description.Split('\n','\r')
      let lines = if lines.Length >= 1 then  lines else [| "" |]
      // Include the indented format of the item in the 'signature'
      let signatureLines = 
          [| yield lines.[0]
             yield! lines.[1..] |> Seq.takeWhile (fun s -> s.StartsWith(" ")) |]
      // Skip the indented format of the item for the 'summary'
      let summaryLines = [|  yield! lines.[1..] |> Seq.skipWhile (fun s -> s.StartsWith(" ")) |]
      let summaryLines =  summaryLines |> Array.filter (String.IsNullOrEmpty >> not) 
      let tooltipInfo = new TooltipInformation ()
      tooltipInfo.SummaryMarkup <- summaryLines |> String.concat "\n"
      tooltipInfo.SignatureMarkup <- signatureLines |> String.concat "\n"
      tooltipInfo

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
type ParameterDataProvider(nameStart: int, meths: MethodOverloads) = 
    inherit MonoDevelop.Ide.CodeCompletion.ParameterDataProvider (nameStart)
    override x.Count = meths.Methods.Length
        /// Returns the markup to use to represent the specified method overload
        /// in the parameter information window.
    override x.CreateTooltipInformation (overload:int, currentParameter:int, smartWrap:bool) = 
        // Get the lower part of the text for the display of an overload
        let description = 
            let meth = meths.Methods.[overload]
            let text = TipFormatter.formatTip false meth.Description 
            let allLines = text.Split([| '\n';'\r' |], StringSplitOptions.RemoveEmptyEntries)
            let body = if allLines.Length <= 1 then None else Some <| String.Join("\n", allLines.[1..])
            let param = 
                meth.Parameters |> Array.mapi (fun i param -> 
                    let paramDesc = 
                        // Sometimes the parameter decription is hidden in the XML docs
                        match TipFormatter.extractParamTip param.Name meth.Description  with 
                        | Some tip -> tip
                        | None -> param.Description
                    let name = if i = currentParameter then  "<b>" + param.Name + "</b>" else param.Name
                    let text = name + ": " + GLib.Markup.EscapeText paramDesc
                    text )
            match body with
            | None -> String.Join("\n", param)
            | Some body -> body + "\n\n" + String.Join("\n", param)
            
        
        // Returns the text to use to represent the specified parameter
        let paramDescription = 
            let meth = meths.Methods.[overload]
            if currentParameter < 0 || currentParameter >= meth.Parameters.Length  then "" else 
            let param = meth.Parameters.[currentParameter]
            param.Name 

        let heading = 
            let meth = meths.Methods.[overload]
            let text = TipFormatter.formatTip false meth.Description 
            let lines = text.Split [| '\n';'\r' |]

            // Try to highlight the current parameter in bold. Hack apart the text based on (, comma, and ), then
            // put it back together again.
            //
            // @todo This will not be perfect when the text contains generic types with more than one type parameter
            // since they will have extra commas. 

            let text = if lines.Length = 0 then meths.Name else  lines.[0]
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
        let meth = meths.Methods.[overload]
        meth.Parameters.Length
        
    // @todo should return 'true' for param-list methods
    override x.AllowParameterList (overload: int) = 
        false

    override x.GetParameterName (overload:int, paramIndex:int) =
        let meth = meths.Methods.[overload]
        let prm = meth.Parameters.[paramIndex]
        prm.Name

/// Implements text editor extension for MonoDevelop that shows F# completion    
type FSharpTextEditorCompletion() =
  inherit CompletionTextEditorExtension()

  override x.ExtendsEditor(doc:Document, editor:IEditableTextBuffer) =
    // Extend any text editor that edits F# files
    CompilerArguments.supportedExtension(IO.Path.GetExtension(doc.FileName.ToString()))

  override x.Initialize() = base.Initialize()

  /// Provide parameter and method overload information when you type '(', ',' or ')'
  override x.HandleParameterCompletion(context:CodeCompletionContext, completionChar:char) : MonoDevelop.Ide.CodeCompletion.ParameterDataProvider =
    try
     if (completionChar <> '(' && completionChar <> ',' && completionChar <> ')' ) then null else
      let doc = x.Document
      let docText = doc.Editor.Text
      let offset = context.TriggerOffset
      let startOffset =
          let rec loop depth i = 
              if (i <= 0) then i else
              let ch = docText.[i] 
              if ((ch = '(' || ch = '{' || ch = '[') && depth > 0) then loop (depth - 1) (i-1) 
              elif ((ch = ')' || ch = '}' || ch = ']')) then loop (depth+1) (i-1) 
              elif (ch = '(') then i
              else loop depth (i-1) 
          loop 0 offset 
      if docText = null || offset >= docText.Length || offset < 0 then null else
      let config = IdeApp.Workspace.ActiveConfiguration
      if config = null then null else
      // Try to get typed result - with the specified timeout
      let tyRes = LanguageService.Service.GetTypedParseResult(new FilePath(doc.Editor.FileName), docText, doc.Project, config, timeout = ServiceSettings.blockingTimeout)
      let methsOpt = tyRes.GetMethods(startOffset, doc.Editor.Document)
      match methsOpt with 
      | None -> null 
      | Some meths -> new ParameterDataProvider (startOffset, meths) :> _ 
    with _ -> null
        
  override x.KeyPress (key, keyChar, modifier) =
      let result = base.KeyPress (key, keyChar, modifier)
      if ((keyChar = ',' || keyChar = ')') && x.CanRunParameterCompletionCommand ()) then
          base.RunParameterCompletionCommand ()
      result


  // Run completion automatically when the user hits '.'
  // (this means that completion currently also works in comments and strings...)
  override x.HandleCodeCompletion(context:CodeCompletionContext, ch:char, triggerWordLength:byref<int>) : ICompletionDataList=
      if ch <> '.' then null else
      x.CodeCompletionCommand(context)

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context:CodeCompletionContext) : ICompletionDataList =
    try 
      let config = IdeApp.Workspace.ActiveConfiguration
      
      // Try to get typed information from LanguageService (with the specified timeout)
      let tyRes = LanguageService.Service.GetTypedParseResult(x.Document.FileName, x.Document.Editor.Text, x.Document.Project, config, timeout = ServiceSettings.blockingTimeout)
      
      // Get declarations and generate list for MonoDevelop
      let decls = tyRes.GetDeclarations(x.Document) 
      if decls.Items.Length > 0 then
        let result = new CompletionDataList()
        for mi in decls.Items do result.Add(new FSharpMemberCompletionData(mi))
        result :> ICompletionDataList
      else null
    with 
    | :? System.TimeoutException -> 
        let result = new CompletionDataList()
        result.Add(new FSharpTryAgainMemberCompletionData())
        result :> ICompletionDataList
    | e -> let result = new CompletionDataList()
           result.Add(new FSharpErrorCompletionData(e))
           result :> ICompletionDataList

  // @todo find out what this is used for
  override x.GetParameterCompletionCommandOffset(cpos:byref<int>) =
      false
    
  // Returns the index of the parameter where the cursor is currently positioned.
  // -1 means the cursor is outside the method parameter list
  // 0 means no parameter entered
  // > 0 is the index of the parameter (1-based)
  override x.GetCurrentParameterIndex (startOffset: int) = 
      let editor = x.Document.Editor
      let cursor = editor.Caret.Offset
      let i = startOffset // the original context
      if (i < 0 || i >= editor.Length || editor.GetCharAt (i) = ')') then -1 
      elif (i + 1 = cursor && editor.Document.GetCharAt(i) = '(') then 0
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
              elif (ch = ')') then -1
              else loop depth (i+1) parameterIndex
          let res = loop 0 i 1
          res

