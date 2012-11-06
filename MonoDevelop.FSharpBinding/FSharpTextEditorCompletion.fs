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
    inherit CompletionData(CompletionText = (if mi.Name |> String.forall (fun c -> System.Char.IsLetterOrDigit c || c = '_') then mi.Name else "``" + mi.Name + "``"), 
                           DisplayText = mi.Name, 
                           DisplayFlags = DisplayFlags.DescriptionHasMarkup)
    override x.Description = TipFormatter.formatTip mi.DescriptionText    
    override x.Icon = new MonoDevelop.Core.IconId(ServiceUtils.getIcon mi.Glyph)


type internal FSharpTryAgainMemberCompletionData() =
    inherit CompletionData(CompletionText = "", DisplayText="Declarations list not yet available...", DisplayFlags = DisplayFlags.None )
    override x.Description = "The declaration list is not yet available or the operation timed out. Try again?"     
    override x.Icon =  new MonoDevelop.Core.IconId("md-event")

type internal FSharpErrorCompletionData(exn:exn) =
    inherit CompletionData(CompletionText = "", DisplayText=exn.Message, DisplayFlags = DisplayFlags.None )
    let text = exn.ToString()
    override x.Description = text
    override x.Icon =  new MonoDevelop.Core.IconId("md-event")

type ParameterDataProvider(nameStart: int, meths: MethodOverloads) = 
    interface IParameterDataProvider with 
        member x.Count = meths.Methods.Length
        member x.StartOffset = nameStart

        // Returns the markup to use to represent the specified method overload
        // in the parameter information window.
        member x.GetHeading (overload:int, parameterMarkup:string[], currentParameter:int) = 
            let meth = meths.Methods.[overload]
            let text = TipFormatter.formatTip  meth.Description 
            let lines = text.Split([| '\n';'\r' |])
            if lines.Length = 0 then meths.Name else  lines.[0]
          //prename = GLib.Markup.EscapeText (function.FullName.Substring (0, len + 2));
          //return prename + "<b>" + function.Name + "</b>" + " (" + paramTxt + ")" + cons;
        
        member x.GetDescription (overload:int, currentParameter:int) = 
            let meth = meths.Methods.[overload]
            let text = TipFormatter.formatTip  meth.Description 
            let lines = text.Split([| '\n';'\r' |])
            if lines.Length <= 1 then "" else lines.[1..] |> String.concat "\n"
        
        // Returns the text to use to represent the specified parameter
        member x.GetParameterDescription (overload:int, paramIndex:int) = 
            let meth = meths.Methods.[overload]
            let param = meth.Parameters.[paramIndex]
            param.Description
          //return GLib.Markup.EscapeText (function.Parameters[paramIndex]);
        
        // Returns the number of parameters of the specified method
        member x.GetParameterCount (overload:int) = 
            let meth = meths.Methods.[overload]
            meth.Parameters.Length
        
        member x.AllowParameterList (overload: int) = 
            false
            //let meth = meths.Methods.[overload]
            //meth.
/// Implements text editor extension for MonoDevelop that shows F# completion    
type FSharpTextEditorCompletion() =
  inherit CompletionTextEditorExtension()

  override x.ExtendsEditor(doc:Document, editor:IEditableTextBuffer) =
    // Extend any text editor that edits F# files
    CompilerArguments.supportedExtension(IO.Path.GetExtension(doc.FileName.ToString()))

  override x.Initialize() = base.Initialize()

  override x.HandleParameterCompletion(context:CodeCompletionContext, completionChar:char) : IParameterDataProvider =
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
      | Some meths -> 
      
      new ParameterDataProvider (startOffset, meths) :> _ 
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
    | e -> 
        let result = new CompletionDataList()
        result.Add(new FSharpErrorCompletionData(e))
        result :> ICompletionDataList

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
      elif (i = cursor) then 0
      else 
          // The first character is a '('
          // Note this will be confused by comments.
          let rec loop depth i parameterIndex = 
              if (i > cursor) then -1 
              elif (i >= editor.Document.TextLength) then  parameterIndex else
              let ch = editor.Document.GetCharAt(i)
              if (ch = '(' || ch = '{' || ch = '[') then loop (depth+1) (i+1) parameterIndex
              elif ((ch = ')' || ch = '}' || ch = ']') && depth > 1 ) then loop (depth-1) (i+1) parameterIndex
              elif (ch = ',' && depth = 1) then loop depth (i+1) (parameterIndex+1)
              elif (ch = ')') then -1
              else loop depth (i+1) parameterIndex
          loop 0 i 1

