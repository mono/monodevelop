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
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.CodeCompletion
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.CompilerBinding
open ICSharpCode.NRefactory.Editor
open ICSharpCode.NRefactory.Completion

/// A list of completions is returned.  Contains title and can generate description (tool-tip shown on the right) of the item.
/// Description is generated lazily because it is quite slow and there can be numerous.
type internal FSharpMemberCompletionData(name, datatipLazy:Lazy<ToolTipText>, glyph) =
    inherit CompletionData(CompletionText = Lexhelp.Keywords.QuoteIdentifierIfNeeded name, 
                           DisplayText = name, 
                           DisplayFlags = DisplayFlags.DescriptionHasMarkup)

    let description = lazy (TipFormatter.formatTip datatipLazy.Value)
    let icon = lazy (MonoDevelop.Core.IconId(ServiceUtils.getIcon glyph))

    new (name, datatip:ToolTipText, glyph) =  new FSharpMemberCompletionData(name, lazy datatip, glyph)
    new (mi:Declaration) =  new FSharpMemberCompletionData(mi.Name, lazy mi.DescriptionText, mi.Glyph)

    override x.Description = name //description.Value   // this is not used
    override x.Icon = icon.Value

    /// Check if the datatip has multiple overloads
    override x.HasOverloads = 
        match datatipLazy.Value with 
        | ToolTipText [xs] ->
            match xs with 
            | ToolTipElementGroup ttg -> true 
            | _ -> false
        | ToolTipText list -> true

    /// Split apart the elements into separate overloads
    override x.OverloadedData =
        match datatipLazy.Value with 
        | ToolTipText xs -> 
            seq{for tooltipElement in xs do
                match tooltipElement with
                | ToolTipElement(a, b) -> yield FSharpMemberCompletionData(name, ToolTipText[tooltipElement], glyph) :> _
                | ToolTipElementGroup(items) ->
                  let overloads =
                      items 
                      |> Seq.map (fun args -> FSharpMemberCompletionData(name, ToolTipText[ToolTipElement(args)], glyph)) 
                      |> Seq.cast
                  yield! overloads
                | _ -> () }


    override x.AddOverload (data: ICompletionData) = ()//not currently called

    // TODO: what does 'smartWrap' indicate?
    override x.CreateTooltipInformation (smartWrap: bool) = 
      
      Debug.WriteLine("computing tooltip for {0}", name)

      match description.Value with
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
type ParameterDataProvider(nameStart: int, name, meths : MethodGroupItem array) = 
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

  override x.ExtendsEditor(doc:Document, editor:IEditableTextBuffer) =
    // Extend any text editor that edits F# files
    CompilerArguments.supportedExtension(IO.Path.GetExtension(doc.FileName.ToString()))

  override x.Initialize() = base.Initialize()

  /// Provide parameter and method overload information when you type '(', '<' or ','
  override x.HandleParameterCompletion(context:CodeCompletionContext, completionChar:char) : MonoDevelop.Ide.CodeCompletion.ParameterDataProvider =
    try
     if (completionChar <> '(' && completionChar <> '<' && completionChar <> ',' ) then null else
      Debug.WriteLine("Getting Parameter Info on completion character {0}", completionChar)
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
          loop 0 offset 

      Debug.WriteLine("Getting Parameter Info, startOffset = {0}", startOffset)
      let config = IdeApp.Workspace.ActiveConfiguration
      if docText = null || config = null || offset >= docText.Length || offset < 0 then null else

      // Try to get typed result - with the specified timeout
      let proj = doc.Project :?> MonoDevelop.Projects.DotNetProject
      let files = CompilerArguments.getSourceFiles(doc.Project.Items) |> Array.ofList
      let args = CompilerArguments.getArgumentsFromProject(proj, config)
      let framework = CompilerArguments.getTargetFramework(proj.TargetFramework.Id)

      match MDLanguageService.Instance.GetTypedParseResultWithTimeout(doc.Project.FileName.ToString(), doc.Editor.FileName, docText, files, args, AllowStaleResults.MatchingFileName, ServiceSettings.blockingTimeout, framework) with
      | None -> null
      | Some tyRes ->
      let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(offset, doc.Editor.Document)
      let methsOpt = tyRes.GetMethods(line, col, lineStr)
      match methsOpt with 
      | None -> 
          Debug.WriteLine("Getting Parameter Info: no methods")
          null 
      | Some(name, meths) -> 
          Debug.WriteLine("Getting Parameter Info: methods!")
          new ParameterDataProvider (startOffset, name, meths) :> _ 
    with _ -> null
        
  override x.KeyPress (key, keyChar, modifier) =
      let result = base.KeyPress (key, keyChar, modifier)
      if ((keyChar = ',' || keyChar = ')') && x.CanRunParameterCompletionCommand ()) then
          base.RunParameterCompletionCommand ()
      result


  // Run completion automatically when the user hits '.'
  // (this means that completion currently also works in comments and strings...)
  override x.HandleCodeCompletion(context, ch, triggerWordLength:byref<int>) =
      let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(context.TriggerOffset, x.Document.Editor.Document)
      
      if ch <> '.' || String.IsNullOrWhiteSpace(lineStr.Substring(0,col-1)) then null else

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

      x.CodeCompletionCommandImpl(context, allowAnyStale)

  /// Completion was triggered explicitly using Ctrl+Space or by the function above  
  override x.CodeCompletionCommand(context) =
      x.CodeCompletionCommandImpl(context, true)

  member x.CodeCompletionCommandImpl(context, allowAnyStale) =
    try 
      let config = IdeApp.Workspace.ActiveConfiguration
      let proj = x.Document.Project :?> MonoDevelop.Projects.DotNetProject
      let files = CompilerArguments.getSourceFiles(x.Document.Project.Items) |> Array.ofList
      let args = CompilerArguments.getArgumentsFromProject(proj, config)
      let framework = CompilerArguments.getTargetFramework(proj.TargetFramework.Id)
      // Try to get typed information from LanguageService (with the specified timeout)
      let stale = if allowAnyStale then AllowStaleResults.MatchingFileName else AllowStaleResults.MatchingSource
      match MDLanguageService.Instance.GetTypedParseResultWithTimeout(x.Document.Project.FileName.ToString(), x.Document.FileName.ToString(), x.Document.Editor.Text, files, args, stale, ServiceSettings.blockingTimeout, framework) with
      | None ->
        let result = CompletionDataList()
        result.Add(FSharpTryAgainMemberCompletionData())
        result :> ICompletionDataList
      | Some tyRes ->
        // Get declarations and generate list for MonoDevelop
        let line, col, lineStr = MonoDevelop.getLineInfoFromOffset(context.TriggerOffset, x.Document.Editor.Document)
        match tyRes.GetDeclarations(line, col, lineStr) with
        | Some(decls, residue) when decls.Items.Any() ->
              let items = decls.Items
                          |> Array.map (fun mi -> FSharpMemberCompletionData(mi) :> ICompletionData)
              let result = CompletionDataList()
              result.AddRange(items)
              result :> ICompletionDataList
        | _ -> null
    with
    | e -> let result = CompletionDataList()
           result.Add(FSharpErrorCompletionData(e))
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


open Microsoft.FSharp.Compiler.Range

type FSharpPathExtension() =
    inherit TextEditorExtension()

    let pathChanged = new Event<_,_>()
    let mutable currentPath = [||]
    let mutable subscriptions = []
    member x.Document = base.Document
    member x.GetEntityMarkup(node: DeclarationItem) =
        let prefix = match node.Kind with
                     | NamespaceDecl-> "Namespace: "
                     | ModuleFileDecl -> "ModuleFile: "
                     | ExnDecl -> "Exn: "
                     | ModuleDecl -> "Module: "
                     | TypeDecl -> "Type: "
                     | MethodDecl -> "Method: "
                     | PropertyDecl -> "Property: "
                     | FieldDecl -> "Field: "
                     | OtherDecl -> "" 
        let name = node.Name.Split('.')
        if name.Length > 0 then prefix + name.Last()
        else prefix + node.Name

    override x.Initialize() =
        currentPath <- [| new PathEntry("No selection", Tag = null) |]
        let positionChanged = x.Document.Editor.Caret.PositionChanged.Subscribe(fun o e -> x.PathUpdated e)
        let documentParsed  = x.Document.DocumentParsed.Subscribe(fun o e -> x.PathUpdated null)
        subscriptions <- positionChanged :: documentParsed :: subscriptions
        
    member private x.PathUpdated(documentLocation) =
        let loc = x.Document.Editor.Caret.Location
        
        if x.Document.ParsedDocument = null then () else
        match x.Document.ParsedDocument.Ast with
        | :? ParseAndCheckResults as ast ->

            let posGt (p1Column, p1Line) (p2Column, p2Line) = 
                (p1Line > p2Line || (p1Line = p2Line && p1Column > p2Column))

            let posEq (p1Column, p1Line) (p2Column, p2Line) = 
                (p1Line = p2Line &&  p1Column = p2Column)

            let posGeq p1 p2 =
                posEq p1 p2 || posGt p1 p2

            let inside (docloc:Mono.TextEditor.DocumentLocation) (start, finish) =
                let cursor = (docloc.Column, docloc.Line)
                posGeq cursor start && posGeq finish cursor

            let toplevel = ast.GetNavigationItems()

            let topLevelTypesInsideCursor =
                toplevel |> Array.filter (fun tl -> let m = tl.Declaration.Range in inside loc ((m.StartColumn, m.StartLine),(m.EndColumn, m.EndLine)))
                         |> Array.sortBy(fun xs -> xs.Declaration.Range.StartLine)

            let newPath = ResizeArray<_>()
            for top in topLevelTypesInsideCursor do
                let name = top.Declaration.Name
                if name.Contains(".") then
                    let nameparts = name.[.. name.LastIndexOf(".")]
                    newPath.Add(PathEntry(ImageService.GetPixbuf(ServiceUtils.getIcon top.Declaration.Glyph, Gtk.IconSize.Menu), x.GetEntityMarkup(top.Declaration), Tag = (ast, nameparts)))
                else newPath.Add(PathEntry(ImageService.GetPixbuf(ServiceUtils.getIcon top.Declaration.Glyph, Gtk.IconSize.Menu), x.GetEntityMarkup(top.Declaration), Tag = ast))
            
            if topLevelTypesInsideCursor.Length > 0 then
                let lastToplevel = topLevelTypesInsideCursor.Last()
                //only first child found is returned, could there be multiple children found?
                let child = lastToplevel.Nested |> Array.tryFind (fun tl -> let m = tl.Range in inside loc ((m.StartColumn, m.StartLine),(m.EndColumn, m.EndLine)))
                let multichild = lastToplevel.Nested |> Array.filter (fun tl -> let m = tl.Range in inside loc ((m.StartColumn, m.StartLine),(m.EndColumn, m.EndLine)))

                Debug.Assert( multichild.Length <= 1, String.Format("{0} children found please investigate!", multichild.Length))
                match child with
                | Some(c) -> newPath.Add(PathEntry(ImageService.GetPixbuf(ServiceUtils.getIcon c.Glyph, Gtk.IconSize.Menu), x.GetEntityMarkup(c) , Tag = lastToplevel))
                | None -> newPath.Add(PathEntry("No selection", Tag = lastToplevel))

            let previousPath = currentPath
            //ensure the path has chnaged from the previous one before setting and raising event.
            let samePath = Seq.forall2 (fun (p1:PathEntry) (p2:PathEntry) -> p1.Markup = p2.Markup) previousPath newPath
            if not samePath then
                if newPath.Count = 0 then currentPath <- [|PathEntry("No selection", Tag = ast)|]
                else currentPath <- newPath.ToArray()

                //invoke pathChanged
                pathChanged.Trigger(x, DocumentPathChangedEventArgs(previousPath))
        | _ -> ()

    override x.Dispose() =
        subscriptions |> List.iter (fun s -> s.Dispose())
        subscriptions <- []

    interface IPathedDocument with
        member x.CurrentPath = currentPath
        member x.CreatePathWidget(index) =
            let path = (x :> IPathedDocument).CurrentPath
            if path = null || index < 0 || index >= path.Length then null else
            let tag = path.[index].Tag
            let window = new DropDownBoxListWindow(new FSharpDataProvider(x, tag))
            window.FixedRowHeight <- 22
            window.MaxVisibleRows <- 14
            window.SelectItem (path.[index].Tag)
            window :> _

        member x.add_PathChanged(handler) = pathChanged.Publish.AddHandler(handler)
        member x.remove_PathChanged(handler) = pathChanged.Publish.RemoveHandler(handler)


and FSharpDataProvider(ext:FSharpPathExtension, tag) =
    let memberList = ResizeArray<_>()

    let reset() =  
        memberList.Clear()
        match tag with
        | :? ParseAndCheckResults as tpr ->
            let navitems = tpr.GetNavigationItems()
            for decl in navitems do
                memberList.Add(decl.Declaration)
        | :? (ParseAndCheckResults * string) as typeAndFilter ->
            let tpr, filter = typeAndFilter 
            let navitems = tpr.GetNavigationItems()
            for decl in navitems do
                if decl.Declaration.Name.StartsWith(filter) then
                    memberList.Add(decl.Declaration)
        | :? TopLevelDeclaration as tld ->
            for item in tld.Nested do
                 memberList.Add(item)
        | _ -> ()

    do reset()

    interface DropDownBoxListWindow.IListDataProvider with
        member x.IconCount = memberList.Count
        member x.Reset() = reset()
        member x.GetTag (n) = memberList.[n] :> obj

        member x.ActivateItem(n) =
            let node = memberList.[n]
            let extEditor = ext.Document.GetContent<IExtensibleTextEditor>()
            if extEditor <> null then
                let (scol,sline) = node.Range.StartColumn, node.Range.StartLine
                extEditor.SetCaretTo(max 1 sline, max 1 scol, true)

        member x.GetMarkup(n) =
            let node = memberList.[n]
            ext.GetEntityMarkup (node)

        member x.GetIcon(n) =
            let node = memberList.[n]
            ImageService.GetPixbuf(ServiceUtils.getIcon node.Glyph, Gtk.IconSize.Menu)         





