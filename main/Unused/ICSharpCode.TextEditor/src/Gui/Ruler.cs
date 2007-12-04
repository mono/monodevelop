/*
//  Ruler.cs
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
