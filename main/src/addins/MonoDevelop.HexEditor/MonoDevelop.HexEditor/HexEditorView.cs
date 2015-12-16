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
using Xwt;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.HexEditor
{
	class HexEditorView : AbstractXwtViewContent, IUndoHandler, IBookmarkBuffer, IZoomable
	{
		Mono.MHex.HexEditor hexEditor = new Mono.MHex.HexEditor ();
		ScrollView window ;
		
		public override Xwt.Widget Widget {
			get {
				return window;
			}
		}
		
		public HexEditorView ()
		{
			hexEditor.HexEditorStyle = new MonoDevelopHexEditorStyle (hexEditor);
			SetOptions ();
			DefaultSourceEditorOptions.Instance.Changed += Instance_Changed;
			hexEditor.HexEditorData.Replaced += delegate {
				this.IsDirty = true;
			};
			window = new ScrollView (hexEditor);
		}

		public override void Dispose ()
		{
			((MonoDevelopHexEditorStyle)hexEditor.HexEditorStyle).Dispose ();
			DefaultSourceEditorOptions.Instance.Changed -= Instance_Changed;
			base.Dispose ();
		}

		void Instance_Changed (object sender, EventArgs e)
		{
			SetOptions ();
		}

		void SetOptions ()
		{
			var name = FontService.FilterFontName (FontService.GetUnderlyingFontName ("Editor"));
			hexEditor.Options.FontName = name;
			hexEditor.PurgeLayoutCaches ();
			hexEditor.Repaint ();
		}
		
		public override void Save (FileSaveInformation fileSaveInformation)
		{
			File.WriteAllBytes (fileSaveInformation.FileName, hexEditor.HexEditorData.Bytes);
			ContentName = fileSaveInformation.FileName;
			this.IsDirty = false;
		}
		
		public override void Load (FileOpenInformation fileOpenInformation)
		{
			var fileName = fileOpenInformation.FileName;
			using (Stream stream = File.OpenRead (fileName)) { 
				hexEditor.HexEditorData.Buffer = ArrayBuffer.Load (stream);
			}
			
			ContentName = fileName;
			this.IsDirty = false;
			hexEditor.SetFocus ();
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
		
		class UndoGroup : IDisposable
		{
			HexEditorData data;
			
			public UndoGroup (HexEditorData data)
			{
				if (data == null)
					throw new ArgumentNullException ("data");
				this.data = data;
				data.BeginAtomicUndo ();
			}
			
			public void Dispose ()
			{
				if (data != null) {
					data.EndAtomicUndo ();
					data = null;
				}
			}
		}	
		
		IDisposable IUndoHandler.OpenUndoGroup ()
		{
			return new UndoGroup (hexEditor.HexEditorData);
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
		
		#region IZoomable
		bool IZoomable.EnableZoomIn {
			get {
				return hexEditor.Options.CanZoomIn;
			}
		}
		
		bool IZoomable.EnableZoomOut {
			get {
				return hexEditor.Options.CanZoomOut;
			}
		}
		
		bool IZoomable.EnableZoomReset {
			get {
				return hexEditor.Options.CanResetZoom;
			}
		}
		
		void IZoomable.ZoomIn ()
		{
			hexEditor.Options.ZoomIn ();
			hexEditor.Repaint ();
		}
		
		void IZoomable.ZoomOut ()
		{
			hexEditor.Options.ZoomOut ();
			hexEditor.Repaint ();
		}
		
		void IZoomable.ZoomReset ()
		{
			hexEditor.Options.ZoomReset ();
			hexEditor.Repaint ();
		}
		#endregion
	}
}
