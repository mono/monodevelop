//  AbstractMargin.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Drawing;
using MonoDevelop.TextEditor.Document;

using Gdk;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class views the line numbers and folding markers.
	/// </summary>
	public abstract class AbstractMargin
	{
		protected System.Drawing.Rectangle drawingPosition = new System.Drawing.Rectangle(0, 0, 0, 0);
		protected TextArea  textArea;
		
		public System.Drawing.Rectangle DrawingPosition {
			get {
				return drawingPosition;
			}
			set {
				drawingPosition = value;
			}
		}
		
		public TextArea TextArea {
			get {
				return textArea;
			}
		}
		
		public IDocument Document {
			get {
				return textArea.Document;
			}
		}
		
		public ITextEditorProperties TextEditorProperties {
			get {
				return textArea.Document.TextEditorProperties;
			}
		}
		
		public virtual Cursor Cursor {
			get {
				//return Cursors.Default;
				return null;
			}
		}
		
		public virtual Size Size {
			get {
				return new Size(-1, -1);
			}
		}
		
		public virtual bool IsVisible {
			get {
				return true;
			}
		}
		
		public AbstractMargin(TextArea textArea)
		{
			this.textArea = textArea;
		}
		
		public abstract void Paint(Gdk.Drawable wnd, System.Drawing.Rectangle rect);
	}
}
