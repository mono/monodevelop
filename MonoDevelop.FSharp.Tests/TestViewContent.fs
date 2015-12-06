namespace MonoDevelopTests
open System
open System.Linq
open MonoDevelop.Core
open Mono.TextEditor
open MonoDevelop.Projects.Text
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.Editor

type TestViewContent() =
    inherit AbstractViewContent()
    let caretPositionSet = Event<_>()
    let textChanged = Event<_>()
    let name = FilePath()

    let data = MonoDevelop.Ide.Editor.TextEditorFactory.CreateNewEditor ()
   
    member val Contents = ResizeArray([data :> obj]) with get, set
    member val Data = data

    override x.Load(fileName:FileOpenInformation) = ()
    override x.Control = null
    override x.GetContent(ty) =
        match x.Contents |> Seq.tryFind ty.IsInstanceOfType with
        | Some content -> content
        | None -> base.GetContent (ty)

    override x.GetContents<'a when 'a : not struct > () =
        x.Contents.OfType<'a> ()

    //interface ITextEditorDataProvider with
    member x.GetTextEditorData() = data

    //interface IEditableTextBuffer with
    member x.HasInputFocus = false
    member x.LineCount = data.LineCount
    [<CLIEvent>]
    member x.CaretPositionSet = caretPositionSet.Publish
    [<CLIEvent>]
    member x.TextChanged = textChanged.Publish
    member x.SetCaretTo(line, column) =()
    member x.SetCaretTo(line, column, highlightCaretLine) =()
    member x.SetCaretTo(line, column, highlightCaretLine, centerCaret) =()
    member x.RunWhenLoaded(f) = f()
    member x.SelectedText with get() = "" and set (v:string) = ()
    member x.CursorPosition with get() = data.CaretOffset and set v = data.CaretOffset <- v
    member x.SelectionStartPosition with get() = if data.IsSomethingSelected then data.SelectionRange.Offset else data.CaretOffset
    member x.SelectionEndPosition with get() = if data.IsSomethingSelected then data.SelectionRange.EndOffset else data.CaretOffset
    member x.Select(s, e) =
        if not (data.IsSomethingSelected) then data.CaretOffset
        else data.SelectionRange.EndOffset
    member x.ShowPosition(pos) = ()

    member x.InsertText(pos, str:string) =
        data.InsertText(pos, str)
        str.Length
    member x.DeleteText(pos, length) = data.ReplaceText (pos, length, "")
    member x.EnableUndo = false
    member x.EnableRedo = false
    member x.Undo() = ()
    member x.Redo() = ()
    member x.OpenUndoGroup() = {new IDisposable with member x.Dispose() = ()}
    member x.Text with get() = data.Text and set v = data.Text <- v

    interface ITextFile with
        member x.Text with get() = data.Text
        member x.Name with get() = name
        member x.Length = data.Length
        member x.GetText(s, e) = data.GetTextBetween (s, e)
        member x.GetCharAt(pos) = data.GetCharAt(pos)
        member x.GetPositionFromLineColumn(line, column) = data.LocationToOffset (line, column)
        member x.GetLineColumnFromPosition(position, line, col) = 
            let loc = data.OffsetToLocation (position)
            line <- loc.Line
            col <- loc.Column