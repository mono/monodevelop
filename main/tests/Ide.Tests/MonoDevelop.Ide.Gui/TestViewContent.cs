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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Gui
{
	public class TestViewContent : ViewContent
	{
		TextEditor data;
		
		public override Control Control {
			get {
				return null;
			}
		}
		
		public TextEditor Data {
			get {
				return this.data;
			}
		}
		public TestViewContent ()
		{
			data = TextEditorFactory.CreateNewEditor ();
			Contents.Add (data);;
			Name = "";
		}

		public TestViewContent (IReadonlyTextDocument doc)
		{
			data = TextEditorFactory.CreateNewEditor (doc);
			Contents.Add (data);
			Name = "";
		}

		protected override void OnContentNameChanged ()
		{
			base.OnContentNameChanged ();
			Name = ContentName;
		}
		
		FilePath name;
		public FilePath Name { 
			get { return name; }
			set { name =  data.FileName = value; }
		}
		
		public int LineCount {
			get {
				return data.LineCount;
			}
		}

		public string Text {
			get {
				return data.Text;
			}
			set {
				data.Text = value;
			}
		}
		
		public int InsertText (int position, string text)
		{
			data.InsertText (position, text);
			return text.Length;
		}
		
		public void DeleteText (int position, int length)
		{
			data.ReplaceText (position, length, "");
		}
		
		public int Length {
			get {
				return data.Length;
			}
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return data.GetTextBetween (startPosition, endPosition);
		}
		public char GetCharAt (int position)
		{
			return data.GetCharAt (position);
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return data.LocationToOffset (line, column);
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			var loc = data.OffsetToLocation (position);
			line = loc.Line;
			column = loc.Column;
		}
		
		public string SelectedText { get { return ""; } set { } }
		
		public int CursorPosition {
			get {
				return data.CaretOffset;
			}
			set {
				data.CaretOffset = value;
			}
		}

		public int SelectionStartPosition { 
			get {
				if (!data.IsSomethingSelected)
					return data.CaretOffset;
				return data.SelectionRange.Offset;
			}
		}
		
		public int SelectionEndPosition { 
			get {
				if (!data.IsSomethingSelected)
					return data.CaretOffset;
				return data.SelectionRange.EndOffset;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			data.SelectionRange = TextSegment.FromBounds (startPosition, endPosition);
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
		
		protected override IEnumerable<object> OnGetContents (Type type)
		{
			return base.OnGetContents(type).Concat (Contents.Where (c => type.IsInstanceOfType (c)));
		}

		public IDisposable OpenUndoGroup ()
		{
			return new DisposeStub ();
		}
		
		public TextEditor GetTextEditorData ()
		{
			return data;
		}

		#region IEditableTextBuffer implementation
		public bool HasInputFocus {
			get {
				return false;
			}
		}
		
		public void RunWhenLoaded (System.Action action)
		{
			action ();
		}
		#endregion
		public event EventHandler CaretPositionSet;
	}
}
