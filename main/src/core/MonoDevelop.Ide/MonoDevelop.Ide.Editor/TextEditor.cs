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

namespace MonoDevelop.Ide.Editor
{
	public class TextEditor : ITextEditorImpl
	{
		readonly ITextEditorImpl textEditorImpl;

		public ITextSourceVersion Version {
			get {
				return textEditorImpl.Version;
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
			add { textEditorImpl.TextChanging += value; }
			remove { textEditorImpl.TextChanging -= value; }
		}

		public event EventHandler<TextChangeEventArgs> TextChanged {
			add { textEditorImpl.TextChanged += value; }
			remove { textEditorImpl.TextChanged -= value; }
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

		public bool ReadOnly {
			get {
				return textEditorImpl.ReadOnly;
			}
			set {
				textEditorImpl.ReadOnly = value;
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
				return IsSomethingSelected ? textEditorImpl.GetTextAt (SelectionRange) : null;
			}
		}

		public IEditorActionHost Actions {
			get {
				return textEditorImpl.Actions;
			}
		}

		public bool IsInAtomicUndo {
			get {
				return textEditorImpl.IsInAtomicUndo;
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
			get;
			set;
		}

		public string Text {
			get {
				return textEditorImpl.Text;
			}
			set {
				textEditorImpl.Text = value;
			}
		}

		public string EolMarker {
			get {
				return textEditorImpl.EolMarker;
			}
		}

		public int LineCount {
			get {
				return textEditorImpl.LineCount;
			}
		}

		public IEnumerable<IDocumentLine> Lines {
			get {
				return GetLinesStartingAt (1);
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
				return textEditorImpl.TextLength;
			}
		}
	
		public void Undo ()
		{
			textEditorImpl.Undo ();
		}

		public void Redo ()
		{
			textEditorImpl.Redo ();
		}

		public IDisposable OpenUndoGroup ()
		{
			return textEditorImpl.OpenUndoGroup ();
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
			return textEditorImpl.FormatString (insertPosition, code);
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

		public Cairo.PointD LocationToPoint (TextLocation currentSmartTagBegin)
		{
			return textEditorImpl.LocationToPoint (currentSmartTagBegin);
		}

		public string GetLineText (int line, bool includeDelimiter = false)
		{
			var segment = GetLine (line);
			return GetTextAt (includeDelimiter ? segment.SegmentIncludingDelimiter : segment);
		}

		public IEnumerable<IDocumentLine> GetLinesBetween (int startLine, int endLine)
		{
			var curLine = GetLine (startLine);
			int count = endLine - startLine;
			while (curLine != null && count --> 0) {
				yield return curLine;
				curLine = curLine.NextLine;
			}
		}

		public IEnumerable<IDocumentLine> GetLinesStartingAt (int startLine)
		{
			var curLine = GetLine (startLine);
			while (curLine != null) {
				yield return curLine;
				curLine = curLine.NextLine;
			}
		}

		public IEnumerable<IDocumentLine> GetLinesReverseStartingAt (int startLine)
		{
			var curLine = GetLine (startLine);
			while (curLine != null) {
				yield return curLine;
				curLine = curLine.PreviousLine;
			}
		}

		public int LocationToOffset (int line, int column)
		{
			return textEditorImpl.LocationToOffset (new TextLocation (line, column));
		}

		public int LocationToOffset (TextLocation location)
		{
			return textEditorImpl.LocationToOffset (location);
		}

		public TextLocation OffsetToLocation (int offset)
		{
			return textEditorImpl.OffsetToLocation (offset);
		}

		public int Insert (int offset, string text)
		{
			return textEditorImpl.Insert (offset, text);
		}

		public void Remove (int offset, int count)
		{
			Remove (new TextSegment (offset, count)); 
		}

		public void Remove (ISegment segment)
		{
			textEditorImpl.Remove (segment);
		}

		public int Replace (int offset, int count, string value)
		{
			return textEditorImpl.Replace (offset, count, value);
		}

		public string GetTextBetween (int startOffset, int endOffset)
		{
			return GetTextAt (startOffset, endOffset - startOffset);
		}

		public string GetTextBetween (TextLocation start, TextLocation end)
		{
			return GetTextBetween (LocationToOffset (start), LocationToOffset (end));
		}

		public IDocumentLine GetLine (int lineNumber)
		{
			return textEditorImpl.GetLine (lineNumber);
		}

		public IDocumentLine GetLineByOffset (int offset)
		{
			return textEditorImpl.GetLineByOffset (offset);
		}

		public int OffsetToLineNumber (int offset)
		{
			return textEditorImpl.OffsetToLineNumber (offset);
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

		public char GetCharAt (int offset)
		{
			return textEditorImpl.GetCharAt (offset);
		}

		public string GetTextAt (int offset, int length)
		{
			return GetTextAt (offset, length);
		}

		public string GetTextAt (ISegment segment)
		{
			return textEditorImpl.GetTextAt (segment);
		}
	}

	public static class DocumentExtensions
	{
		public static string GetTextBetween (this TextEditor document, int startLine, int startColumn, int endLine, int endColumn)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetTextBetween (new TextLocation (startLine, startColumn), new TextLocation (endLine, endColumn));
		}

		public static string GetLineIndent (this TextEditor document, int lineNumber)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return document.GetLineIndent (document.GetLine (lineNumber));
		}

		public static string GetLineIndent (this TextEditor document, IDocumentLine segment)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return segment.GetIndentation (document);
		}

		static int[] GetDiffCodes (TextEditor document, ref int codeCounter, Dictionary<string, int> codeDictionary, bool includeEol)
		{
			int i = 0;
			var result = new int[document.LineCount];
			foreach (var line in document.Lines) {
				string lineText = document.GetTextAt (line.Offset, includeEol ? line.LengthIncludingDelimiter : line.Length);
				int curCode;
				if (!codeDictionary.TryGetValue (lineText, out curCode)) {
					codeDictionary[lineText] = curCode = ++codeCounter;
				}
				result[i] = curCode;
				i++;
			}
			return result;
		}

		public static IEnumerable<Hunk> Diff (this TextEditor document, TextEditor changedDocument, bool includeEol = true)
		{
			var codeDictionary = new Dictionary<string, int> ();
			int codeCounter = 0;
			return MonoDevelop.Ide.Editor.Diff.GetDiff<int> (GetDiffCodes (document, ref codeCounter, codeDictionary, includeEol),
				GetDiffCodes (changedDocument, ref codeCounter, codeDictionary, includeEol));
		}
	}
}

