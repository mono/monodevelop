// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace rec Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Immutable
open System.Threading
open System.Threading.Tasks

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.CodeFixes
open Microsoft.CodeAnalysis.CodeActions

open Microsoft.FSharp.Compiler
//open Microsoft.FSharp.Compiler.Parser
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.SourceCodeServices
open MonoDevelop.FSharp
open MonoDevelop
open MonoDevelop.Ide.Editor
open Microsoft.CodeAnalysis
open MonoDevelop.Refactoring

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal InsertContext =
    /// Corrects insertion line number based on kind of scope and text surrounding the insertion point.
    let adjustInsertionPoint (text: string) ctx  =

        let sourceText = String.getLines text
        let getLineStr line = sourceText.[line].ToString().Trim()
        let line =
            match ctx.ScopeKind with
            | ScopeKind.TopModule ->
                if ctx.Pos.Line > 1 then
                    // it's an implicit module without any open declarations    
                    let line = getLineStr (ctx.Pos.Line - 2)
                    let isImpliciteTopLevelModule = not (line.StartsWith "module" && not (line.EndsWith "="))
                    if isImpliciteTopLevelModule then 1 else ctx.Pos.Line
                else 1
            | ScopeKind.Namespace ->
                // for namespaces the start line is start line of the first nested entity
                if ctx.Pos.Line > 1 then
                    [0..ctx.Pos.Line - 1]
                    |> List.mapi (fun i line -> i, getLineStr line)
                    |> List.tryPick (fun (i, lineStr) -> 
                        if lineStr.StartsWith "namespace" then Some i
                        else None)
                    |> function
                        // move to the next line below "namespace" and convert it to F# 1-based line number
                        | Some line -> line + 2 
                        | None -> ctx.Pos.Line
                else 1  
            | _ -> ctx.Pos.Line

        let toOffSet line column = 
            let lengthOfLines = 
                sourceText
                |> Array.take (line - 1) // Don't count the current line
                |> Array.map (fun x -> x.Length)
                |> Array.sum 

            let numOfNewLines = (line - 1) // Again, don't count the current line
            lengthOfLines + column + numOfNewLines


        toOffSet ctx.Pos.Line ctx.Pos.Column
        //{ ctx.Pos with Line = line }

    /// <summary>
    /// Inserts open declaration into `SourceText`. 
    /// </summary>
    /// <param name="sourceText">SourceText.</param>
    /// <param name="ctx">Insertion context. Typically returned from tryGetInsertionContext</param>
    /// <param name="ns">Namespace to open.</param>
    //let insertOpenDeclaration (sourceText: SourceText) (ctx: InsertContext) (ns: string) : SourceText =
        //let insert line lineStr (sourceText: SourceText) : SourceText =
        //    let pos = sourceText.Lines.[line].Start
        //    sourceText.WithChanges(TextChange(TextSpan(pos, 0), lineStr + Environment.NewLine))

        //let pos = adjustInsertionPoint sourceText ctx
        //let docLine = pos.Line - 1
        //let lineStr = (String.replicate pos.Column " ") + "open " + ns
        //let sourceText = sourceText |> insert docLine lineStr
        //// if there's no a blank line between open declaration block and the rest of the code, we add one
        //let sourceText = 
        //    if sourceText.Lines.[docLine + 1].ToString().Trim() <> "" then 
        //        sourceText |> insert (docLine + 1) ""
        //    else sourceText
        //// for top level module we add a blank line between the module declaration and first open statement
        //if (pos.Column = 0 || ctx.ScopeKind = ScopeKind.Namespace) && docLine > 0
        //    && not (sourceText.Lines.[docLine - 1].ToString().Trim().StartsWith "open") then
        //        sourceText |> insert docLine ""
        //else sourceText


    let insertOpenDeclartionWithEditor lastIdent findIdent (editor:TextEditor) (ctx) (ns:string, multipleNames, name) = 


        //match findIdent () with 
        //| Some (symbolUse: FSharpSymbolUse) -> 
            let activeDocFileName = editor.FileName.ToString ()
            let offset = adjustInsertionPoint editor.Text ctx
            //let symbols =
            //    languageService.GetUsesOfSymbolInProject (ctx.Project.FileName.ToString(), activeDocFileName, editor.Text, symbolUse.Symbol)
            //    |> (fun p -> Async.RunSynchronously(p, timeout=ServiceSettings.maximumTimeout))

            //let locations =
            //    symbols |> Array.map (Symbols.getTextSpanTrimmed lastIdent)

            //let file =
                //locations
                //|> Array.map fst
                //|> Array.head

            //if fileLocations.Count = 1 then

            let displayText = "open " + ns + if multipleNames then " (" + name + ")" else "" + Environment.NewLine
            TextReplaceChange (FileName = activeDocFileName,
                               Offset = offset,
                               RemovedChars = 0,
                               InsertedText = displayText,
                               Description = String.Format ("Replace '{0}' with '{1}'", lastIdent, ns))  :> Change
            |> Array.singleton
            //| None ->[||] 
            //|> Array.iter (fun x -> x.)

            //MessageService.ShowCustomDialog (Dialog.op_Implicit (new Rename.RenameItemDialog("Add Open", symbolUse.Symbol.DisplayName, performChanges symbolUse locations)))
            //|> ignore

            //if (segment.Offset <= editor.CaretOffset && editor.CaretOffset <= segment.EndOffset) then
            //    link.Links.Insert (0, segment)
            ////else
            //link.AddLink (segment)

            //links.Add (link)
            //editor.StartTextLinkMode (TextLinkModeOptions (links))

[<ExportCodeFixProvider("F#", Name = "AddOpen"); Shared>]
type internal FSharpAddOpenCodeFixProvider
    [<ImportingConstructor>]
    (
        document: MonoDevelop.Ide.Gui.Document,
        assemblyContentProvider: AssemblyContentProvider,
        findIdent: unit -> FSharpSymbolUse option,
        monitor
    ) =
    inherit CodeFixProvider()
    let fixableDiagnosticIds = ["FS0039"]
    
    let checker = MonoDevelop.FSharp.MDLanguageService.Instance.Checker
    let fixUnderscoresInMenuText (text: string) = text.Replace("_", "__")
    // TryGetOptionsForEditingDocumentOrProject
    //let projectInfoManger = MonoDevelop.FSharp.MDLanguageService.Instance.Getproj

    //let qualifySymbolFix (context: CodeFixContext) (fullName, qualifier) = 
        //CodeAction.Create(
            //fixUnderscoresInMenuText fullName,
            //fun (cancellationToken: CancellationToken) -> 
                //async {
                //    let! (sourceText : SourceText) = context.Document.GetTextAsync()
                //    return context.Document.WithText(sourceText.Replace(context.Span, qualifier))
                //} |> CommonRoslynHelpers.StartAsyncAsTask(cancellationToken))

    //let openNamespaceFix (contextDocument: Document) ctx name ns multipleNames = 
        //let displayText = "open " + ns + if multipleNames then " (" + name + ")" else ""
        //// TODO when fresh Roslyn NuGet packages are published, assign "Namespace" Tag to this CodeAction to show proper glyph.
        //CodeAction.Create(
            //fixUnderscoresInMenuText displayText,
            //(fun (cancellationToken: CancellationToken) -> 
            //    async {
            //        let! sourceText = contextDocument.GetTextAsync()
            //        return contextDocument.WithText(InsertContext.insertOpenDeclaration sourceText ctx ns)
            //    } |> CommonRoslynHelpers.StartAsyncAsTask(cancellationToken)),
            //displayText)

    let getSuggestions lastIdent (candidates: (Entity * InsertContext) list) =
        //let openNamespaceFixes =
            candidates
            |> Seq.choose (fun (entity, ctx) -> entity.Namespace |> Option.map (fun ns -> ns, entity.Name, ctx))
            |> Seq.groupBy (fun (ns, _, _) -> ns)
            |> Seq.map (fun (ns, xs) -> 
                ns, 
                xs 
                |> Seq.map (fun (_, name, ctx) -> name, ctx) 
                |> Seq.distinctBy (fun (name, _) -> name)
                |> Seq.sort
                |> Seq.toArray)
            |> Seq.map (fun (ns, names) ->
                let multipleNames = names |> Array.length > 1
                names |> Seq.map (fun (name, ctx) -> ns, name, ctx, multipleNames))

                //names |> Seq.map (fun (name) -> ns, name, multipleNames))
            |> Seq.concat
            |> Seq.map (fun (ns, name, ctx, multipleNames) -> 

                ns, 
                    fun () -> 
                        let changes = InsertContext.insertOpenDeclartionWithEditor lastIdent findIdent document.Editor ctx (ns, multipleNames, name)
                        RefactoringService.AcceptChanges(monitor, changes)
                )
                //openNamespaceFix contextDocument ctx name ns multipleNames)
                //"open " + ns + if multipleNames then " (" + name + ")" else "" )
            //|> Seq.toList
            
        //let quilifySymbolFixes =
            //candidates
            //|> Seq.map (fun (entity, _) -> entity.FullRelativeName, entity.Qualifier)
            //|> Seq.distinct
            //|> Seq.sort
            //|> Seq.map (qualifySymbolFix context)
            //|> Seq.toList

        //openNamespaceFixes
        //(openNamespaceFixes )//@ quilifySymbolFixes)
        //|> List.map (fun x codefix -> 
        //    //context.RegisterCodeFix
        //    (codeFix, (context.Diagnostics |> Seq.filter (fun x -> fixableDiagnosticIds |> List.contains x.Id))
        //)

    override __.FixableDiagnosticIds = fixableDiagnosticIds.ToImmutableArray()

    override __.RegisterCodeFixesAsync context : Task = Task.CompletedTask

    member __.CodeFixesAsync (ast:ParseAndCheckResults) (unresolvedIdentRange:Range.range) (cancellationToken) = // (context:CodeFixContext) =
        asyncMaybe {
            //let _, sourceText = document.Editor
            //let editor = document.Editor
            //let sourceText = document.Editor.Text
            //let! options = languageService.GetCheckerOptions(contextDocument.FilePath, contextDocument.Project.FilePath, sourceText.ToString())
            //let! sourceText = contextDocument.GetTextAsync(cancellationToken)
            //let! _, parsedInput, checkResults = checker.ParseAndCheckDocument(contextDocument, options, allowStaleResults = true, sourceText = sourceText)
            //let! ast = editor.DocumentContext.TryGetAst()
            let! parsedInput = ast.ParseTree
            let! checkResults = ast.CheckResults

            let sourceText = document.Editor.Text
            //let! parsedInput = document.TryGetFSharpParsedDocument()
            //parseedInput.

            //let unresolvedIdentRange =
                //let startLinePos = sourceText.Lines.GetLinePosition context.Span.Start
                //let startPos = Pos.fromZ startLinePos.Line startLinePos.Character
                //let endLinePos = sourceText.Lines.GetLinePosition context.Span.End
                //let endPos = Pos.fromZ endLinePos.Line endLinePos.Character
                //Range.mkRange contextDocument.FilePath startPos endPos

            let isAttribute = UntypedParseImpl.GetEntityKind(unresolvedIdentRange.Start, parsedInput) = Some EntityKind.Attribute

            let entities =
                assemblyContentProvider.GetAllEntitiesInProjectAndReferencedAssemblies checkResults
                |> List.collect (fun e -> 
                     [ yield e.TopRequireQualifiedAccessParent, e.AutoOpenParent, e.Namespace, e.CleanedIdents
                       if isAttribute then
                           let lastIdent = e.CleanedIdents.[e.CleanedIdents.Length - 1]
                           if lastIdent.EndsWith "Attribute" && e.Kind LookupType.Precise = EntityKind.Attribute then
                               yield 
                                   e.TopRequireQualifiedAccessParent, 
                                   e.AutoOpenParent,
                                   e.Namespace,
                                   e.CleanedIdents 
                                   |> Array.replace (e.CleanedIdents.Length - 1) (lastIdent.Substring(0, lastIdent.Length - 9)) ])

            //entities |> List.iter (printfn "%A")

            let longIdent = ParsedInput.getLongIdentAt parsedInput unresolvedIdentRange.End

            let! maybeUnresolvedIdents =
                longIdent 
                |> Option.map (fun longIdent ->
                    longIdent
                    |> List.map (fun ident ->
                        { Ident = ident.idText
                          Resolved = not (ident.idRange = unresolvedIdentRange) })
                    |> List.toArray)
            let createEntity = ParsedInput.tryFindInsertionContext unresolvedIdentRange.StartLine parsedInput maybeUnresolvedIdents
            let candidates = entities |> Seq.map createEntity |> Seq.concat |> Seq.toList
            return getSuggestions "Application" candidates |> Seq.toList
        } 
        //|> Async.Ignore 
        //|> CommonRoslynHelpers.StartAsyncUnitAsTask(cancellationToken)


namespace MonoDevelop.FSharp.CodeActions
open MonoDevelop.CodeActions
open MonoDevelop.Ide.Editor.Extension
open System.Threading
open System.Threading.Tasks
open System
open MonoDevelop.Components.Commands
open MonoDevelop.Refactoring

//type FSharpCodeActionEditorExtension() = 
    //inherit TextEditorExtension() 

    //let quickFixCancellationTokenSource = new CancellationTokenSource ()

    //member internal this.PopupQuickFixMenu (evt:Gdk.EventButton, menuAction:Action<CodeFixMenu>, ?point:Nullable<Xwt.Point>): unit = 
    //    async {
    //        Thread.Sleep 1000   
    //    } |> Async.Start

    //member this.SmartTagMarginMarker_ShowPopup (sender:obj, e:EventArgs ):unit  = 

    //    let marker = sender :?> (MonoDevelop.SourceEditor.SmartTagMarginMarker)
    //    marker |> ignore


    //[<CommandHandler (RefactoryCommands.QuickFix)>]
    //member this.OnQuickFixCommand () = 
    //    //if not AnalysisOptions.EnableFancyFeatures || smartTagMarginMarker = null then 
    //    //    //Fixes = RefactoringService.GetValidActions (Editor, DocumentContext, Editor.CaretLocation).Result;
    //    //    PopupQuickFixMenu (null, null);
    //    //else
    //        //CancelSmartTagPopupTimeout ();
    //    this.PopupQuickFixMenu (null, fun menu -> ());



    //member internal this.GetCurrentFixesAsync (cancellationToken:CancellationToken ):Task<CodeActionContainer> = 
        //    //var loc = Editor.CaretOffset;
        //    //var ad = DocumentContext.AnalysisDocument;
        //    //var line = Editor.GetLine (Editor.CaretLine);

        //    //if (ad == null) {
        //    //  return Task.FromResult (CodeActionContainer.Empty);
        //    //}
        //    //TextSpan span;
        //    //if (Editor.IsSomethingSelected) {
        //    //    var selectionRange = Editor.SelectionRange;
        //    //    span = selectionRange.Offset >= 0 ? TextSpan.FromBounds (selectionRange.Offset, selectionRange.EndOffset) : TextSpan.FromBounds (loc, loc);
        //    //} else {
        //    //    span = TextSpan.FromBounds (loc, loc);
        //    //}

        //Async.StartAsTask(async { return null }, cancellationToken = cancellationToken )


//namespace MonoDevelop.CodeActions

open Gdk
open MonoDevelop.SourceEditor
open MonoDevelop.AnalysisCore.Gui
//open RefactoringEssentials
open MonoDevelop.Refactoring
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Editor
open MonoDevelop.Core.Text
open MonoDevelop.Core
open MonoDevelop.Components.Commands
open MonoDevelop.Components
open MonoDevelop.CodeIssues
open MonoDevelop.AnalysisCore
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.CodeRefactorings
open Microsoft.CodeAnalysis.CodeFixes
open Microsoft.CodeAnalysis
open Gtk
open System.Threading.Tasks
open System.Threading
open System.Reflection
open System.Linq
open System.Collections.Immutable
open System
open System.Collections.Generic

type FsharpCodeActionEditorExtension() as this =
    inherit TextEditorExtension()
    let mutable menuTimeout = 150
    let mutable smartTagTask = null
    //let mutable quickFixCancellationTokenSource = ObjectCreationExpressionSyntax
    //let mutable codeFixService =
    //    this.Ide.Composition.CompositionManager.``GetExportedValue<ICodeFixService>``
    //        ()
    //let mutable codeRefactoringService =
        //this.Ide.Composition.CompositionManager.``GetExportedValue<ICodeRefactoringService>``
            //()

    let mutable smartTagMarginMarker = null
    let mutable beginVersion = null
    member val internal smartTagPopupTimeoutId: int32 = -1 with get, set
    member val internal HasCurrentFixes: bool = false with get, set

    //member this.CancelSmartTagPopupTimeout() =
        //if this.smartTagPopupTimeoutId <> 0 then
            //GLib.Source.Remove(smartTagPopupTimeoutId)
            //this.smartTagPopupTimeoutId <- 0

    //member this.RemoveWidget() =
        //if smartTagMarginMarker <> null then
        //    this.Editor.RemoveMarker(this.smartTagMarginMarker)
        //    ``smartTagMarginMarker.ShowPopup -= SmartTagMarginMarker_ShowPopup``
        //    smartTagMarginMarker <- null
        //else this.CancelSmartTagPopupTimeout()

    //member this.Dispose() =
        //this.CancelQuickFixTimer()
        //this.RefactoringPreviewTooltipWindow.HidePreviewTooltip()
        //``Editor.CaretPositionChanged -= HandleCaretPositionChanged``
        //``DocumentContext.DocumentParsed -= HandleDocumentDocumentParsed``
        //``Editor.TextChanged -= Editor_TextChanged``
        //``Editor.BeginAtomicUndoOperation -= Editor_BeginAtomicUndoOperation``
        //``Editor.EndAtomicUndoOperation -= Editor_EndAtomicUndoOperation``
        //this.RemoveWidget()
        //base.Dispose()

    //member this.CancelQuickFixTimer() =
        //quickFixCancellationTokenSource.Cancel()
        //quickFixCancellationTokenSource <- ObjectCreationExpressionSyntax
        //smartTagTask <- null

    //member this.HandleCaretPositionChanged(sender: obj, e: EventArgs) =
    //    if this.Editor.IsInAtomicUndo then ()
    //    else
    //        this.CancelQuickFixTimer()
    //        let mutable token = quickFixCancellationTokenSource.Token
    //        if this.AnalysisOptions.EnableFancyFeatures
    //           && this.DocumentContext.ParsedDocument <> null then
    //            if HasCurrentFixes then
    //                let mutable curOffset = this.Editor.CaretOffset
    //                for fix in smartTagTask.Result.CodeFixActions do
    //                    if not fix.TextSpan.Contains(curOffset) then
    //                        this.RemoveWidget()
    //                        BreakStatement
    //            smartTagTask <- this.GetCurrentFixesAsync(token)
    //        else this.RemoveWidget()

    //member this.GetCurrentFixesAsync(cancellationToken: CancellationToken) =
    //    let mutable loc = this.Editor.CaretOffset
    //    let mutable ad = this.DocumentContext.AnalysisDocument
    //    let mutable line = this.Editor.GetLine(this.Editor.CaretLine)
    //    let mutable span = null
    //    if this.Editor.IsSomethingSelected then
    //        let mutable selectionRange = this.Editor.SelectionRange
    //        span <- ConditionalExpressionSyntax
    //    else span <- this.TextSpan.FromBounds(loc, loc)
    //    this.Task.Run(fun () -> TryStatement, cancellationToken)

    //member this.FilterOnUIThread(collections: ``ImmutableArray<CodeFixCollection>``,
    //                             workspace: Workspace) =
    //    this.Runtime.AssertMainThread()
    //    let mutable caretOffset = this.Editor.CaretOffset
    //    collections.``Select (c => FilterOnUIThread (c, workspace))``.``Where(x => x != null)``.``OrderBy(x => GetDistance (x, caretOffset))``.ToImmutableArray
    //        ()

    //member this.GetDistance(fixCollection: CodeFixCollection, caretOffset: int) =
    //    ConditionalExpressionSyntax

    //member this.FilterOnUIThread(collection: CodeFixCollection,
    //                             workspace: Workspace) =
    //    this.Runtime.AssertMainThread()
    //    let mutable applicableFixes =
    //        collection.Fixes.WhereAsArray
    //            (fun f -> this.IsApplicable(f.Action, workspace))
    //    ConditionalExpressionSyntax

    //member this.IsApplicable(action: Microsoft.CodeAnalysis.CodeActions.CodeAction,
    //                         workspace: Workspace) =
    //    if not action.PerformFinalApplicabilityCheck then True
    //    else
    //        this.Runtime.AssertMainThread()
    //        action.IsApplicable(workspace)

    //member this.PopupQuickFixMenu(evt: Gdk.EventButton,
    //                              menuAction: ``Action<CodeFixMenu>``,
    //                              point: Xwt.``Point?``) =
    //    use ``_`` =
    //        this.Refactoring.Counters.FixesMenu.BeginTiming
    //            ("Show quick fixes menu")
    //    let mutable token = quickFixCancellationTokenSource.Token
    //    let mutable fixes = AwaitExpressionSyntax
    //    if token.IsCancellationRequested then ()
    //    Editor.SuppressTooltips <- True
    //    this.PopupQuickFixMenu(evt, fixes, menuAction, point)

    //member this.PopupQuickFixMenu(evt: Gdk.EventButton,
    //                              fixes: CodeActionContainer,
    //                              menuAction: ``Action<CodeFixMenu>``,
    //                              point: Xwt.``Point?``) =
    //    let mutable token = quickFixCancellationTokenSource.Token
    //    if token.IsCancellationRequested then ()
    //    let mutable menu =
    //        this.CodeFixMenuService.CreateFixMenu(Editor, fixes, token)
    //    if token.IsCancellationRequested then ()
    //    if menu.Items.Count = 0 then ()
    //    if menuAction <> null then menuAction (menu)
    //    let mutable rect = null
    //    let mutable widget = Editor
    //    if not point.HasValue then
    //        let mutable p =
    //            this.Editor.LocationToPoint(this.Editor.CaretLocation)
    //        rect <- ObjectCreationExpressionSyntax
    //    else rect <- ObjectCreationExpressionSyntax
    //    this.ShowFixesMenu(widget, rect, menu)

    //member this.ShowFixesMenu(parent: Widget, evt: Gdk.Rectangle,
    //                          entrySet: CodeFixMenu) =
    //    if parent = null || parent.GdkWindow = null then
    //        Editor.SuppressTooltips <- False
    //        True
    //    else TryStatement; True

    //member this.CreateContextMenu(entrySet: CodeFixMenu) =
    //    let mutable menu = ObjectCreationExpressionSyntax
    //    for item in entrySet.Items do
    //        if item = this.CodeFixMenuEntry.Separator then
    //            menu.Items.Add(ObjectCreationExpressionSyntax)
    //            ContinueStatement
    //        let mutable menuItem = ObjectCreationExpressionSyntax
    //        menuItem.Context <- item.Action
    //        if item.Action = null then
    //            if not ParenthesizedExpressionSyntax
    //               || itemAsMenu.Items.Count <= 0 then
    //                menuItem.Sensitive <- False
    //        let mutable subMenu = item :?> CodeFixMenu
    //        if subMenu <> null then
    //            menuItem.SubMenu <- this.CreateContextMenu(subMenu)
    //            menuItem.Selected.AddHandler<_>
    //                (fun () ->
    //                this.RefactoringPreviewTooltipWindow.HidePreviewTooltip())
    //            menuItem.Deselected.AddHandler<_>
    //                (fun () ->
    //                this.RefactoringPreviewTooltipWindow.HidePreviewTooltip())
    //        else
    //            menuItem.Clicked.AddHandler<_>
    //                (fun (sender, e) ->
    //                this.``((System``.``Action)((ContextMenuItem)sender)``.``Context)``
    //                    ())
    //            menuItem.Selected.AddHandler<_>
    //                (fun (sender, e) ->
    //                this.RefactoringPreviewTooltipWindow.HidePreviewTooltip()
    //                if item.ShowPreviewTooltip <> null then
    //                    item.ShowPreviewTooltip(e))
    //            menuItem.Deselected.AddHandler<_>
    //                (fun () ->
    //                this.RefactoringPreviewTooltipWindow.HidePreviewTooltip())
    //        menu.Items.Add(menuItem)
    //    menu.Closed.AddHandler<_>
    //        (fun () -> this.RefactoringPreviewTooltipWindow.HidePreviewTooltip())
    //    menu

    //member this.CreateSmartTag(fixes: CodeActionContainer, offset: int) =
    //    if not this.AnalysisOptions.EnableFancyFeatures || fixes.IsEmpty then
    //        this.RemoveWidget()
    //        ()
    //    else
    //        let mutable editor = Editor
    //        if editor = null then
    //            this.RemoveWidget()
    //            ()
    //        if this.DocumentContext.ParsedDocument = null
    //           || this.DocumentContext.ParsedDocument.IsInvalid then
    //            this.RemoveWidget()
    //            ()
    //        let mutable severity = fixes.GetSmartTagSeverity()
    //        if ConditionalAccessExpressionSyntax <> editor.CaretLine then
    //            this.RemoveWidget()
    //            smartTagMarginMarker <- ObjectCreationExpressionSyntax
    //            smartTagMarginMarker.ShowPopup.AddHandler<_>
    //                (SmartTagMarginMarker_ShowPopup)
    //            editor.AddMarker
    //                (editor.GetLine(editor.CaretLine), smartTagMarginMarker)
    //        else
    //            smartTagMarginMarker.SmartTagSeverity <- severity
    //            let mutable view = editor.``GetContent<SourceEditorView>`` ()
    //            view.TextEditor.RedrawMarginLine
    //                (view.TextEditor.TextArea.QuickFixMargin, editor.CaretLine)

    //member this.SmartTagMarginMarker_ShowPopup(sender: obj, e: EventArgs) =
    //    let mutable marker = CastExpressionSyntax
    //    this.CancelSmartTagPopupTimeout()
    //    smartTagPopupTimeoutId <- this.GLib.Timeout.Add(menuTimeout,
    //                                                    fun () ->
    //                                                        this.PopupQuickFixMenu
    //                                                            (null,
    //                                                             fun menu -> (),
    //                                                             ObjectCreationExpressionSyntax)
    //                                                        smartTagPopupTimeoutId <- 0
    //                                                        False)

    //member this.Initialize() =
    //    base.Initialize()
    //    DocumentContext.DocumentParsed.AddHandler<_>
    //        (HandleDocumentDocumentParsed)
    //    Editor.CaretPositionChanged.AddHandler<_> (HandleCaretPositionChanged)
    //    Editor.TextChanged.AddHandler<_> (Editor_TextChanged)
    //    Editor.BeginAtomicUndoOperation.AddHandler<_>
    //        (Editor_BeginAtomicUndoOperation)
    //    Editor.EndAtomicUndoOperation.AddHandler<_>
    //        (Editor_EndAtomicUndoOperation)

    //member this.Editor_BeginAtomicUndoOperation(sender: obj, e: EventArgs) =
    //    beginVersion <- this.Editor.Version

    //member this.Editor_EndAtomicUndoOperation(sender: obj, e: EventArgs) =
    //    if beginVersion <> null
    //       && beginVersion.CompareAge(this.Editor.Version) <> 0 then
    //        this.RemoveWidget()
    //    else beginVersion <- null

    //member this.Editor_TextChanged(sender: obj,
    //                               e: MonoDevelop.Core.Text.TextChangeEventArgs) =
    //    if this.Editor.IsInAtomicUndo then ()
    //    else
    //        this.RemoveWidget()
    //        this.HandleCaretPositionChanged(null, this.EventArgs.Empty)

    //member this.HandleDocumentDocumentParsed(sender: obj, e: EventArgs) =
    //    this.HandleCaretPositionChanged(null, this.EventArgs.Empty)

    //member this.CurrentSmartTagPopup() =
        //this.CancelSmartTagPopupTimeout()
        //smartTagPopupTimeoutId <- this.GLib.Timeout.Add(menuTimeout,
                                                        //fun () ->
                                                            //this.PopupQuickFixMenu
                                                            //    (null,
                                                            //     fun menu -> ())
                                                            //smartTagPopupTimeoutId <- 0
                                                            //False)

    [<CommandHandler(RefactoryCommands.QuickFix)>]
    member this.OnQuickFixCommand() =
        //if not AnalysisOptions.EnableFancyFeatures
           //|| smartTagMarginMarker = null then
            //this.PopupQuickFixMenu(null, null)
            ()
        //else
            //this.CancelSmartTagPopupTimeout()
            //this.PopupQuickFixMenu(null, fun menu -> ())
            
open MonoDevelop.Core
open System.Threading
open System.Threading.Tasks
open MonoDevelop.Ide
open MonoDevelop.CodeActions
open System
open MonoDevelop.Components.Commands

open MonoDevelop.Ide.CodeCompletion
open System.Collections.Immutable
open Microsoft.CodeAnalysis
open MonoDevelop.Projects
open MonoDevelop.Ide
open Microsoft.CodeAnalysis.Options
open MonoDevelop.Ide.Editor
open MonoDevelop.Core.Instrumentation
open System.Diagnostics
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.CodeIssues
open MonoDevelop.CodeActions
open System.Threading
open System.Threading.Tasks
open MonoDevelop.AnalysisCore
open System.Linq
open MonoDevelop.Ide.Gui
open MonoDevelop.Core
open Mono.Addins
open System
open System.Collections.Generic



