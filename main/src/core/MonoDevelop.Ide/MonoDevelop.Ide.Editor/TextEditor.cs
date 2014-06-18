//
// ITextEditor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using MonoDevelop.Core.Text;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Editor
{
	public class TextEditor : ITextDocument
	{
		readonly ITextEditorImpl textEditorImpl;
		IReadonlyTextDocument ReadOnlyTextDocument { get { return textEditorImpl.Document; } }
		ITextDocument ReadWriteTextDocument { get { return (ITextDocument)textEditorImpl.Document; } }

		public ITextSourceVersion Version {
			get {
				return ReadOnlyTextDocument.Version;
			}
		}

		public event EventHandler SelectionChanged {
			add { textEditorImpl.SelectionChanged += value; }
			remove { textEditorImpl.SelectionChanged -= value; }
		}

		public event EventHandler CaretPositionChanged {
			add { textEditorImpl.CaretPositionChanged += value; }
			remove { textEditorImpl.CaretPositionChanged -= value; }
		}

		public event EventHandler BeginUndo {
			add { textEditorImpl.BeginUndo += value; }
			remove { textEditorImpl.BeginUndo -= value; }
		}

		public event EventHandler EndUndo {
			add { textEditorImpl.EndUndo += value; }
			remove { textEditorImpl.EndUndo -= value; }
		}

		public event EventHandler<TextChangeEventArgs> TextChanging {
			add { ReadWriteTextDocument.TextChanging += value; }
			remove { ReadWriteTextDocument.TextChanging -= value; }
		}

		public event EventHandler<TextChangeEventArgs> TextChanged {
			add { ReadWriteTextDocument.TextChanged += value; }
			remove { ReadWriteTextDocument.TextChanged -= value; }
		}

		public ISyntaxMode SyntaxMode {
			get {
				return textEditorImpl.SyntaxMode;
			}
			set {
				textEditorImpl.SyntaxMode = value;
			}
		}

		public ITextEditorOptions Options {
			get {
				return textEditorImpl.Options;
			}
			set {
				textEditorImpl.Options = value;
			}
		}

		public TextLocation CaretLocation {
			get {
				return textEditorImpl.CaretLocation;
			}
			set {
				textEditorImpl.CaretLocation = value;
			}
		}

		public int CaretLine {
			get {
				return CaretLocation.Line;
			}
			set {
				CaretLocation = new TextLocation (value, CaretColumn);
			}
		}

		public int CaretColumn {
			get {
				return CaretLocation.Column;
			}
			set {
				CaretLocation = new TextLocation (CaretLine, value);
			}
		}

		public int CaretOffset {
			get {
				return textEditorImpl.CaretOffset;
			}
			set {
				textEditorImpl.CaretOffset = value;
			}
		}

		public bool IsReadOnly {
			get {
				return ReadOnlyTextDocument.IsReadOnly;
			}
			set {
				ReadWriteTextDocument.IsReadOnly = value;
			}
		}

		public bool IsSomethingSelected {
			get {
				return textEditorImpl.IsSomethingSelected;
			}
		}

		public SelectionMode SelectionMode {
			get {
				return textEditorImpl.SelectionMode;
			}
		}

		public ISegment SelectionRange {
			get {
				return textEditorImpl.SelectionRange;
			}
			set {
				textEditorImpl.SelectionRange = value;
			}
		}

		public DocumentRegion SelectionRegion {
			get {
				return textEditorImpl.SelectionRegion;
			}
			set {
				textEditorImpl.SelectionRegion = value;
			}
		}

		public string SelectedText {
			get {
				return IsSomethingSelected ? ReadOnlyTextDocument.GetTextAt (SelectionRange) : null;
			}
		}

		public IEditorActionHost Actions {
			get {
				return textEditorImpl.Actions;
			}
		}

		public IMarkerHost MarkerHost {
			get {
				return textEditorImpl.MarkerHost;
			}
		}

		public bool IsInAtomicUndo {
			get {
				return ReadWriteTextDocument.IsInAtomicUndo;
			}
		}

		public double LineHeight {
			get {
				return textEditorImpl.LineHeight;
			}
		}

		/// <summary>
		/// Gets or sets the type of the MIME.
		/// </summary>
		/// <value>The type of the MIME.</value>
		public string MimeType {
			get {
				return ReadOnlyTextDocument.MimeType;
			}
			set {
				ReadWriteTextDocument.MimeType = value;
			}
		}

		public string Text {
			get {
				return ReadOnlyTextDocument.Text;
			}
			set {
				ReadWriteTextDocument.Text = value;
			}
		}

		/// <summary>
		/// Gets the eol marker. On a text editor always use that and not GetEolMarker.
		/// The EOL marker of the document may get overwritten my the one from the options.
		/// </summary>
		public string EolMarker {
			get {
				if (Options.OverrideDocumentEolMarker)
					return Options.DefaultEolMarker;
				return ReadOnlyTextDocument.GetEolMarker ();
			}
		}

		public bool UseBOM {
			get {
				return ReadOnlyTextDocument.UseBOM;
			}
			set {
				ReadWriteTextDocument.UseBOM = value;
			}
		}

		public Encoding Encoding {
			get {
				return ReadOnlyTextDocument.Encoding;
			}
			set {
				ReadWriteTextDocument.Encoding = value;
			}
		}

		public int LineCount {
			get {
				return ReadOnlyTextDocument.LineCount;
			}
		}

		string fileName;
		/// <summary>
		/// Gets the name of the file the document is stored in.
		/// Could also be a non-existent dummy file name or null if no name has been set.
		/// </summary>
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
				OnFileNameChanged (EventArgs.Empty);
			}
		}

		protected virtual void OnFileNameChanged (EventArgs e)
		{
			var handler = FileNameChanged;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler FileNameChanged;

		public int TextLength {
			get {
				return ReadOnlyTextDocument.TextLength;
			}
		}

		public TextEditor (ITextEditorImpl textEditorImpl)
		{
			if (textEditorImpl == null)
				throw new ArgumentNullException ("textEditorImpl");
			this.textEditorImpl = textEditorImpl;
		}
	
		public void Undo ()
		{
			ReadWriteTextDocument.Undo ();
		}

		public void Redo ()
		{
			ReadWriteTextDocument.Redo ();
		}

		public IDisposable OpenUndoGroup ()
		{
			return ReadWriteTextDocument.OpenUndoGroup ();
		}

		public void SetSelection (int anchorOffset, int leadOffset)
		{
			textEditorImpl.SetSelection (anchorOffset, leadOffset);
		}

		public void SetSelection (TextLocation anchor, TextLocation lead)
		{
			SetSelection (LocationToOffset (anchor), LocationToOffset (lead));

		}

		public void ClearSelection ()
		{
			textEditorImpl.ClearSelection ();
		}

		public void CenterToCaret ()
		{
			textEditorImpl.CenterToCaret ();
		}

		public void StartCaretPulseAnimation ()
		{
			textEditorImpl.StartCaretPulseAnimation ();
		}

		public int EnsureCaretIsNotVirtual ()
		{
			return textEditorImpl.EnsureCaretIsNotVirtual ();
		}

		public void FixVirtualIndentation ()
		{
			textEditorImpl.FixVirtualIndentation ();
		}

		public Gtk.Widget GetGtkWidget ()
		{
			return textEditorImpl.GetGtkWidget ();
		}

		public void RunWhenLoaded (Action action)
		{
			textEditorImpl.RunWhenLoaded (action);
		}

		public string FormatString (TextLocation insertPosition, string code)
		{
			return textEditorImpl.FormatString (LocationToOffset (insertPosition), code);
		}

		public string FormatString (int offset, string code)
		{
			return textEditorImpl.FormatString (offset, code);
		}

		public void StartInsertionMode (string operation, IList<InsertionPoint> insertionPoints, Action<InsertionCursorEventArgs> action)
		{
			textEditorImpl.StartInsertionMode (operation, insertionPoints, action);
		}

		public void StartTextLinkMode (List<TextLink> links)
		{
			textEditorImpl.StartTextLinkMode (links);
		}

		public void RequestRedraw ()
		{
			textEditorImpl.RequestRedraw ();
		}

		public void InsertAtCaret (string text)
		{
			Insert (CaretOffset, text);
		}

		public TextLocation PointToLocation (double xp, double yp, bool endAtEol = false)
		{
			return textEditorImpl.PointToLocation (xp, yp, endAtEol);
		}

		public Cairo.Point LocationToPoint (TextLocation currentSmartTagBegin)
		{
			return textEditorImpl.LocationToPoint (currentSmartTagBegin);
		}

		public string GetLineText (int line, bool includeDelimiter = false)
		{
			var segment = GetLine (line);
			return GetTextAt (includeDelimiter ? segment.SegmentIncludingDelimiter : segment);
		}

		public int LocationToOffset (int line, int column)
		{
			return ReadOnlyTextDocument.LocationToOffset (new TextLocation (line, column));
		}

		public int LocationToOffset (TextLocation location)
		{
			return ReadOnlyTextDocument.LocationToOffset (location);
		}

		public TextLocation OffsetToLocation (int offset)
		{
			return ReadOnlyTextDocument.OffsetToLocation (offset);
		}

		public void Insert (int offset, string text)
		{
			ReadWriteTextDocument.Insert (offset, text);
		}

		public void Remove (int offset, int count)
		{
			Remove (new TextSegment (offset, count)); 
		}

		public void Remove (ISegment segment)
		{
			ReadWriteTextDocument.Remove (segment);
		}

		public void Replace (int offset, int count, string value)
		{
			ReadWriteTextDocument.Replace (offset, count, value);
		}

		public void Replace (ISegment segment, string value)
		{
			ReadWriteTextDocument.Replace (segment.Offset, segment.Length, value);
		}

		public IDocumentLine GetLine (int lineNumber)
		{
			return ReadOnlyTextDocument.GetLine (lineNumber);
		}

		public IDocumentLine GetLineByOffset (int offset)
		{
			return ReadOnlyTextDocument.GetLineByOffset (offset);
		}

		public int OffsetToLineNumber (int offset)
		{
			return ReadOnlyTextDocument.OffsetToLineNumber (offset);
		}

		public void AddMarker (IDocumentLine line, ITextLineMarker lineMarker)
		{
			textEditorImpl.AddMarker (line, lineMarker);
		}

		public void AddMarker (int lineNumber, ITextLineMarker lineMarker)
		{
			AddMarker (GetLine (lineNumber), lineMarker);
		}

		public void RemoveMarker (ITextLineMarker lineMarker)
		{
			textEditorImpl.RemoveMarker (lineMarker);
		}

		public IEnumerable<ITextLineMarker> GetLineMarker (IDocumentLine line)
		{
			return textEditorImpl.GetLineMarker (line);
		}

		public IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (ISegment segment)
		{
			return textEditorImpl.GetTextSegmentMarkersAt (segment);
		}

		public IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (int offset)
		{
			return textEditorImpl.GetTextSegmentMarkersAt (offset);
		}

		public void AddMarker (ITextSegmentMarker marker)
		{
			textEditorImpl.AddMarker (marker);
		}

		public bool RemoveMarker (ITextSegmentMarker marker)
		{
			return textEditorImpl.RemoveMarker (marker);
		}

		public IEnumerable<IFoldSegment> GetFoldingsFromOffset (int offset)
		{
			return textEditorImpl.GetFoldingsFromOffset (offset);
		}

		public IEnumerable<IFoldSegment> GetFoldingContaining (int lineNumber)
		{
			return GetFoldingContaining (GetLine (lineNumber));
		}

		public IEnumerable<IFoldSegment> GetFoldingContaining (IDocumentLine line)
		{
			return textEditorImpl.GetFoldingContaining (line);
		}

		public IEnumerable<IFoldSegment> GetStartFoldings (int lineNumber)
		{
			return GetStartFoldings (GetLine (lineNumber));
		}

		public IEnumerable<IFoldSegment> GetStartFoldings (IDocumentLine line)
		{
			return textEditorImpl.GetStartFoldings (line);
		}

		public IEnumerable<IFoldSegment> GetEndFoldings (int lineNumber)
		{
			return GetEndFoldings (GetLine (lineNumber));
		}

		public IEnumerable<IFoldSegment> GetEndFoldings (IDocumentLine line)
		{
			return textEditorImpl.GetEndFoldings (line);
		}

		/// <summary>
		/// Gets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		public char this[int offset] { get { return ReadOnlyTextDocument[offset]; } } 

		[Obsolete("Use this[int offset]")]
		public char GetCharAt (int offset)
		{
			return ReadOnlyTextDocument[offset]; 
		}

		public string GetTextAt (int offset, int length)
		{
			return ReadOnlyTextDocument.GetTextAt (offset, length);
		}

		public string GetTextAt (ISegment segment)
		{
			return ReadOnlyTextDocument.GetTextAt (segment);
		}

		public IReadonlyTextDocument CreateDocumentSnapshot ()
		{
			return ReadWriteTextDocument.CreateDocumentSnapshot ();
		}

		internal IViewContent GetViewContent ()
		{
			return textEditorImpl;
		}

		public void SetIndentationTracker (IIndentationTracker indentationTracker)
		{
			if (indentationTracker == null)
				throw new ArgumentNullException ("indentationTracker");
			textEditorImpl.SetIndentationTracker (indentationTracker);
		}

		public void SetSelectionSurroundingProvider (ISelectionSurroundingProvider surroundingProvider)
		{
			if (surroundingProvider == null)
				throw new ArgumentNullException ("surroundingProvider");
			textEditorImpl.SetSelectionSurroundingProvider (surroundingProvider);
		}
	}
}