/*
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// Used internally, not for own use.
	/// </summary>
	[ToolboxItem(false)]
	public class Ruler : UserControl
	{
		TextAreaPainter  textarea;
		
		/// <summary>
		/// Creates a new instance of <see cref="Ruler"/>
		/// </summary>	
		public Ruler(TextAreaPainter textarea)
		{
			this.textarea = textarea;
		}
		
		protected override void OnPaint(PaintEventArgs pe)
		{
			float start = textarea.Document.TextEditorProperties.ShowLineNumbers ? 40 : 10;
			for (float i = start; i < Width; i += textarea.FontWidth) {
				int lineheight = ((int)((i - start + 1) / textarea.FontWidth)) % 5 == 0 ? 4 : 6;
				
				lineheight = ((int)((i - start + 1) / textarea.FontWidth)) % 10 == 0 ? 2 : lineheight;
				
				pe.Graphics.DrawLine(Pens.Black, (int)i, Height - lineheight, (int)i, lineheight);
			}
		}
		
		protected override void OnPaintBackground(PaintEventArgs pe)
		{
			HighlightColor hColor = textarea.Document.HighlightingStrategy.GetColorFor("LineNumber");
			Color color = Enabled ? hColor.BackgroundColor : SystemColors.InactiveBorder;
			pe.Graphics.FillRectangle(new SolidBrush(color), pe.ClipRectangle);
		}
	}
}
*/
