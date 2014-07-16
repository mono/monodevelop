namespace MonoDevelopTests
open System
open System.Linq
open MonoDevelop.Core
open Mono.TextEditor
open MonoDevelop.Projects.Text
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Content

type TestViewContent() =
    inherit AbstractViewContent()
    let document = TextDocument()
    let data = new TextEditorData(document)
    let caretPositionSet = DelegateEvent<_>()
    let textChanged = DelegateEvent<_>()
    let name = FilePath()

    member x.GetTextEditorData() = data
    member val Contents = ResizeArray<obj>() with get, set

    override x.Load(fileName) = ()
    override x.Control = null
    override x.GetContent(ty) =
        let xx = x.Contents.FirstOrDefault(fun o -> ty.IsInstanceOfType(ty))
        if xx = null then base.GetContent(ty) else xx

    interface ITextEditorDataProvider with
        member x.GetTextEditorData() = data

    interface IEditableTextBuffer with
        member x.HasInputFocus = false
        member x.LineCount = document.LineCount
        [<CLIEvent>]
        member x.CaretPositionSet = caretPositionSet.Publish
        [<CLIEvent>]
        member x.TextChanged = textChanged.Publish
        member x.SetCaretTo(line, column) =()
        member x.SetCaretTo(line, column, highlightCaretLine) =()
        member x.SetCaretTo(line, column, highlightCaretLine, centerCaret) =()
        member x.RunWhenLoaded(f) = f.Invoke()
        member x.SelectedText with get() = "" and set v = ()
        member x.CursorPosition with get() = data.Caret.Offset and set v = data.Caret.Offset <- v
        member x.SelectionStartPosition with get() = if data.IsSomethingSelected then data.SelectionRange.Offset else data.Caret.Offset
        member x.SelectionEndPosition with get() = if data.IsSomethingSelected then data.SelectionRange.EndOffset else data.Caret.Offset
        member x.Select(start, end') = data.SelectionRange <- new TextSegment (start, end' - start)
        member x.ShowPosition(pos) = ()

        member x.InsertText(pos, str) =
            document.Insert(pos, str)
            str.Length
        member x.DeleteText(pos, length) = document.Replace (pos, length, "")
        member x.EnableUndo = false
        member x.EnableRedo = false
        member x.Undo() = ()
        member x.Redo() = ()
        member x.OpenUndoGroup() = {new IDisposable with member x.Dispose() = ()}
        member x.Text with get() = document.Text and set v = document.Text <- v
    interface ITextFile with
        member x.Text with get() = document.Text
        member x.Name with get() = name
        member x.Length = document.TextLength
        member x.GetText(start, end') = document.GetTextBetween (start, end')
        member x.GetCharAt(pos) = document.GetCharAt(pos)
        member x.GetPositionFromLineColumn(line, column) = document.LocationToOffset (line, column)
        member x.GetLineColumnFromPosition(position, line, col) = 
            let loc = document.OffsetToLocation (position)
            line <- loc.Line
            col <- loc.Column