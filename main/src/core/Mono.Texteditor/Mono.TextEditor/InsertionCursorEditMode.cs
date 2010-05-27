// 
// InsertionCursorEditMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public class InsertionCursorEditMode : SimpleEditMode
	{
		TextEditor editor;
		List<DocumentLocation> insertionPoints;
		CursorDrawer drawer;
		
		public InsertionCursorEditMode (TextEditor editor, List<DocumentLocation> insertionPoints)
		{
			this.editor = editor;
			this.insertionPoints = insertionPoints;
			drawer = new CursorDrawer (this);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			switch (key) {
			case Gdk.Key.Up:
				break;
			case Gdk.Key.Down:
				break;
				
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				OnExited (new InsertionCursorEventArgs (true, editor.Caret.Location));
				break;
				
			case Gdk.Key.Escape:
				OnExited (new InsertionCursorEventArgs (false, DocumentLocation.Empty));
				break;
			}
		}
		
		EditMode oldMode;
		public void StartMode ()
		{
			oldMode = editor.CurrentMode;
			
			this.editor.TextViewMargin.AddDrawer (drawer);
			editor.CurrentMode = this;
		}
		
		protected virtual void OnExited (InsertionCursorEventArgs e)
		{
			Editor.CurrentMode = oldMode;
			EventHandler<InsertionCursorEventArgs> handler = this.Exited;
			if (handler != null)
				handler (this, e);
			Editor.Document.CommitUpdateAll ();
		}
		
		public event EventHandler<InsertionCursorEventArgs> Exited;

		class CursorDrawer : MarginDrawer
		{
			InsertionCursorEditMode mode;
			
			public CursorDrawer (InsertionCursorEditMode mode)
			{
				this.mode = mode;
			}
			
			public override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area)
			{
				
			}
		}
	}
	
	[Serializable]
	public sealed class InsertionCursorEventArgs : EventArgs
	{
		public bool Success {
			get;
			private set;
		}
		
		public DocumentLocation InsertionPoint {
			get;
			private set;
		}
		
		public InsertionCursorEventArgs (bool success, DocumentLocation insertionPoint)
		{
			this.Success = success;
			this.InsertionPoint = insertionPoint;
		}
	}
	
}

