//
// TestViewContent.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharpBinding.Tests
{
	public class TestViewContent : AbstractViewContent, IEditableTextBuffer, Mono.TextEditor.ITextEditorDataProvider
	{
		TextEditorData data;
		
		public override Gtk.Widget Control {
			get {
				return null;
			}
		}
		
		public TextEditorData Data {
			get {
				return this.data;
			}
		}
		public TestViewContent ()
		{
			document = new Mono.TextEditor.TextDocument ();
			data = new TextEditorData (document);
			Name = "";
		}
		
		public override void Load(string fileName)
		{
		}
		
		FilePath name;
		public FilePath Name { 
			get { return name; }
			set { name =  document.FileName = value; }
		}
		
		public int LineCount {
			get {
				return document.LineCount;
			}
		}
		
		Mono.TextEditor.TextDocument document;
		public string Text {
			get {
				return document.Text;
			}
			set {
				document.Text = value;
			}
		}
		
		public int InsertText (int position, string text)
		{
			document.Insert (position, text);
			return text.Length;
		}
		
		public void DeleteText (int position, int length)
		{
			document.Replace (position, length, "");
		}
		
		public int Length {
			get {
				return document.TextLength;
			}
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return document.GetTextBetween (startPosition, endPosition);
		}
		public char GetCharAt (int position)
		{
			return document.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return document.LocationToOffset (line, column);
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			DocumentLocation loc = document.OffsetToLocation (position);
			line = loc.Line;
			column = loc.Column;
		}
		
		public string SelectedText { get { return ""; } set { } }
		
		public int CursorPosition {
			get {
				return data.Caret.Offset;
			}
			set {
				data.Caret.Offset = value;
			}
		}

		public int SelectionStartPosition { 
			get {
				if (!data.IsSomethingSelected)
					return data.Caret.Offset;
				return data.SelectionRange.Offset;
			}
		}
		
		public int SelectionEndPosition { 
			get {
				if (!data.IsSomethingSelected)
					return data.Caret.Offset;
				return data.SelectionRange.EndOffset;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			data.SelectionRange = new TextSegment (startPosition, endPosition - startPosition);
		}
		
		public void ShowPosition (int position)
		{
		}
		
		public bool EnableUndo {
			get {
				return false;
			}
		}
		public bool EnableRedo {
			get {
				return false;
			}
		}
		
		public void SetCaretTo (int line, int column)
		{
		}
		public void SetCaretTo (int line, int column, bool highlightCaretLine)
		{
		}
		public void SetCaretTo (int line, int column, bool highlightCaretLine, bool centerCaret)
		{
		}
		
		public void Undo()
		{
		}
		public void Redo()
		{
		}
		
		class DisposeStub : IDisposable
		{
			public void Dispose ()
			{
			}
		}
		
		public List<object> Contents = new List<object> ();
		
		public override object GetContent (Type type) 
		{
			return Contents.FirstOrDefault (o => type.IsInstanceOfType (type)) ??  base.GetContent (type);
		}
		
		public IDisposable OpenUndoGroup ()
		{
			return new DisposeStub ();
		}
		
		public TextEditorData GetTextEditorData ()
		{
			return data;
		}
		#region IEditableTextBuffer implementation
		public bool HasInputFocus {
			get {
				return false;
			}
		}
		
		#endregion
		public event EventHandler CaretPositionSet;
		public event EventHandler<TextChangedEventArgs> TextChanged;
	}
}
