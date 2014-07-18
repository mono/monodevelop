// 
// HiddenTextEditorViewContent.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Gui
{
	public class HiddenTextEditorViewContent : MonoDevelop.Ide.Gui.AbstractViewContent, IServiceProvider
	{
		readonly TextEditor editor;

		public TextEditor Editor {
			get {
				return editor;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return null;
			}
		}
		
		public HiddenTextEditorViewContent ()
		{
			editor = TextEditorFactory.CreateNewEditor ();
			Name = "";
		}
		
		public override void Load (FileOpenInformation fileOpenInformation)
		{
		}
		
		public FilePath Name { 
			get;
			set;
		}
		
		public bool HasInputFocus {
			get { return false; }
		}
		
		public int LineCount {
			get {
				return editor.LineCount;
			}
		}
		
		public string Text {
			get {
				return editor.Text;
			}
			set {
				editor.Text = value;
			}
		}
		
		public int InsertText (int position, string text)
		{
			editor.InsertText (position, text);
			return text.Length;
		}
		
		public void DeleteText (int position, int length)
		{
			editor.ReplaceText (position, length, "");
		}
		
		public int Length {
			get {
				return editor.Length;
			}
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return editor.GetTextBetween (startPosition, endPosition);
		}
		public char GetCharAt (int position)
		{
			return editor.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return editor.LocationToOffset (line, column);
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			var loc = editor.OffsetToLocation (position);
			line = loc.Line;
			column = loc.Column;
		}
		
		public string SelectedText { get { return ""; } set { } }
		
		public int CursorPosition {
			get {
				return editor.CaretOffset;
			}
			set {
				editor.CaretOffset = value;
			}
		}

		public int SelectionStartPosition { 
			get {
				if (!editor.IsSomethingSelected)
					return editor.CaretOffset;
				return editor.SelectionRange.Offset;
			}
		}
		
		public int SelectionEndPosition { 
			get {
				if (!editor.IsSomethingSelected)
					return editor.CaretOffset;
				return editor.SelectionRange.EndOffset;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			editor.SelectionRange = new TextSegment (startPosition, endPosition - startPosition);
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
		public void SetCaretTo (int line, int column, bool highlightCaretLine, bool centerCaretLine)
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

		public void RunWhenLoaded (System.Action action)
		{
			action ();
		}

		public IDisposable OpenUndoGroup ()
		{
			return new DisposeStub ();
		}

		#region IServiceProvider implementation

		object IServiceProvider.GetService (Type serviceType)
		{
			if (serviceType.IsInstanceOfType (editor))
				return editor;
			return null;
		}

		#endregion

		public event EventHandler CaretPositionSet;
	}
}
