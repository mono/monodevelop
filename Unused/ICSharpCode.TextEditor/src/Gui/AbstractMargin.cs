// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
