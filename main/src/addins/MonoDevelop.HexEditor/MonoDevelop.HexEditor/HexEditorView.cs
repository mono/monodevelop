// 
// HexEditorView.cs
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
using System.IO;
using MonoDevelop.Ide.Gui;
using Mono.MHex;
using Mono.MHex.Data;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.HexEditor
{
	public class HexEditorView : AbstractViewContent, IUndoHandler, IBookmarkBuffer
	{
		Mono.MHex.HexEditor hexEditor = new Mono.MHex.HexEditor ();
		Gtk.ScrolledWindow window = new Gtk.ScrolledWindow ();
		
		public override Gtk.Widget Control {
			get {
				return window;
			}
		}
		
		public HexEditorView ()
		{
			window.Child = hexEditor;
			window.ShowAll ();
			hexEditor.HexEditorStyle = new MonoDevelopHexEditorStyle (hexEditor);
			SetOptions ();
			MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.Changed += delegate {
				SetOptions ();
			};
			hexEditor.HexEditorData.Replaced += delegate {
				this.IsDirty = true;
			};
		}
		
		void SetOptions ()
		{
			hexEditor.Options.FontName = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance.FontName;
			hexEditor.PurgeLayoutCaches ();
			hexEditor.Repaint ();
		}
		
		public override void Save (string fileName)
		{
			File.WriteAllBytes (fileName, hexEditor.HexEditorData.Buffer.Bytes);
			ContentName = fileName;
			this.IsDirty = false;
		}

		public override void Load (string fileName)
		{
			using (Stream stream = File.OpenRead (fileName)) { 
				hexEditor.HexEditorData.Buffer = ArrayBuffer.Load (stream);
			}
			
			ContentName = fileName;
			this.IsDirty = false;
		}
		
		#region IUndoHandler implementation
		void IUndoHandler.Undo ()
		{
			hexEditor.HexEditorData.Undo ();
		}
		
		
		void IUndoHandler.Redo ()
		{
			hexEditor.HexEditorData.Redo ();
		}
		
		
		void IUndoHandler.BeginAtomicUndo ()
		{
			hexEditor.HexEditorData.BeginAtomicUndo ();
		}
		
		
		void IUndoHandler.EndAtomicUndo ()
		{
			hexEditor.HexEditorData.EndAtomicUndo ();
		}
		
		
		bool IUndoHandler.EnableUndo {
			get {
				return hexEditor.HexEditorData.EnableUndo;
			}
		}
		
		
		bool IUndoHandler.EnableRedo {
			get {
				return hexEditor.HexEditorData.EnableRedo;
			}
		}
		
		#endregion
		
		#region IBookmarkBuffer implementation
		void IBookmarkBuffer.SetBookmarked (int position, bool mark)
		{
			if (mark) {
				hexEditor.HexEditorData.Bookmarks.Add (position);
			} else {
				hexEditor.HexEditorData.Bookmarks.Remove (position);
			}
			hexEditor.Repaint ();
		}
		
		
		bool IBookmarkBuffer.IsBookmarked (int position)
		{
			return hexEditor.HexEditorData.Bookmarks.Contains (position);
		}
		
		
		void IBookmarkBuffer.PrevBookmark ()
		{
			BookmarkActions.GotoPrevious (hexEditor.HexEditorData);
		}
		
		
		void IBookmarkBuffer.NextBookmark ()
		{
			BookmarkActions.GotoNext (hexEditor.HexEditorData);
		}
		
		
		void IBookmarkBuffer.ClearBookmarks ()
		{
			hexEditor.HexEditorData.Bookmarks.Clear ();
			hexEditor.Repaint ();
		}
		
		
		int IBookmarkBuffer.CursorPosition {
			get {
				return (int)hexEditor.HexEditorData.Caret.Offset;
			}
			set {
				hexEditor.HexEditorData.Caret.Offset = value;
			}
		}
		
		#endregion
	}
}
