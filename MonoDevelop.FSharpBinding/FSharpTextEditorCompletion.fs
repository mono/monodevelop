// --------------------------------------------------------------------------------------
// Provides IntelliSense completion for F# in MonoDevelop
// (this file implements MonoDevelop interfaces and calls 'LanguageService')
// --------------------------------------------------------------------------------------
namespace MonoDevelop.FSharp

open System
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.CodeCompletion
//open MonoDevelop.Projects.Dom
//open MonoDevelop.Projects.Dom.Parser
open Microsoft.FSharp.Compiler.SourceCodeServices

open ICSharpCode.NRefactory.Editor
open ICSharpCode.NRefactory.Completion

/// Item that is returned in a list of completions
/// Contains title and can generate description (tool-tip shown on 
/// the right) of the item. Description should be generated lazily
/// because this is quite slow (in the F# services)
type internal FSharpMemberCompletionData(mi:Declaration) =
  inherit CompletionData(CompletionText = (if mi.Name |> String.forall (fun c -> System.Char.IsLetterOrDigit c || c = '_') then mi.Name else "``" + mi.Name + "``"), 
                         DisplayText = mi.Name, 
                         DisplayFlags = DisplayFlags.DescriptionHasMarkup)
  override x.Description = 
     (TipFormatter.formatTip mi.DescriptionText)     
  override x.Icon = 
    new MonoDevelop.Core.IconId(ServiceUtils.getIcon mi.Glyph)

type internal FSharpTryAgainMemberCompletionData() =
  inherit CompletionData(CompletionText = "", DisplayText="Declarations list not yet available...", DisplayFlags = DisplayFlags.None )
  override x.Description = "The declaration list is not yet available or the operation timed out. Try again?"     
  override x.Icon =  new MonoDevelop.Core.IconId("md-event")

type internal FSharpErrorCompletionData(exn:exn) =
  inherit CompletionData(CompletionText = "", DisplayText=exn.Message, DisplayFlags = DisplayFlags.None )
  let text = exn.ToString()
  override x.Description = text
  override x.Icon =  new MonoDevelop.Core.IconId("md-event")

/// Implements text editor extension for MonoDevelop that shows F# completion    
type FSharpTextEditorCompletion() =
  inherit CompletionTextEditorExtension()

  override x.ExtendsEditor(doc:Document, editor:IEditableTextBuffer) =
    // Extend any text editor that edits F# files
    CompilerArguments.supportedExtension(IO.Path.GetExtension(doc.FileName.ToString()))

  override x.Initialize() =
    base.Initialize()

  override x.KeyPress(key:Gdk.Key, keyChar:char, modifier:Gdk.ModifierType) = 
    base.KeyPress(key, keyChar, modifier)

  override x.HandleParameterCompletion(context:CodeCompletionContext, ch:char) : IParameterDataProvider =
    null
        
  override x.HandleCodeCompletion(context:CodeCompletionContext, ch:char, triggerWordLength:byref<int>) : ICompletionDataList=
    // Run completion automatically when the user hits '.'
    // (this means that completion currently also works in comments and strings...)
    if ch = '.' then x.CodeCompletionCommand(context)
    else null 

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context:CodeCompletionContext) : ICompletionDataList =
    try 
      let config = IdeApp.Workspace.ActiveConfiguration
      let req = x.Document.FileName, x.Document.Editor.Text, x.Document.Project, config
      
      // Try to get typed information from LanguageService (with the specified timeout)
      let tyRes = LanguageService.Service.GetTypedParseResult(req, timeout = ServiceSettings.blockingTimeout)
      
      // Get declarations and generate list for MonoDevelop
      let meths = tyRes.GetDeclarations(x.Document) 
      if meths.Items.Length > 0 then
        let result = new CompletionDataList()
        for mi in meths.Items do result.Add(new FSharpMemberCompletionData(mi))
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
    
