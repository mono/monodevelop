//  TipText.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 http://www.icsharpcode.net/ <#Develop>
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

using System.Drawing;
using System.Drawing.Text;

namespace MonoDevelop.TextEditor.Util
{
	class TipText: TipSection
	{
		StringAlignment horzAlign;
		StringAlignment vertAlign;	
		Color           tipColor;
		Font            tipFont;
		StringFormat    tipFormat;
		string          tipText;
        
		public TipText(Graphics graphics, Font font, string text):
			base(graphics)
		{
			tipFont = font; tipText = text;
			
			Color               = SystemColors.InfoText;
			HorizontalAlignment = StringAlignment.Near;
			VerticalAlignment   = StringAlignment.Near;
		}
		
		public override void Draw(PointF location)
		{
			if (IsTextVisible()) {
				RectangleF drawRectangle = new RectangleF
					(location, AllocatedSize);
				
				Graphics.DrawString(tipText, tipFont,
				                    new SolidBrush(Color),
				                    drawRectangle,
				                    GetInternalStringFormat());   
			}
		}
		
		protected StringFormat GetInternalStringFormat()
		{
			if (tipFormat == null) {
				tipFormat = CreateTipStringFormat(horzAlign, vertAlign);
			}
			
			return tipFormat;
		}
		
		protected override void OnMaximumSizeChanged()
		{
			base.OnMaximumSizeChanged();
			
			if (IsTextVisible()) {
				SizeF tipSize = Graphics.MeasureString
					(tipText, tipFont, MaximumSize,
					 GetInternalStringFormat());
				
				SetRequiredSize(tipSize);
			} else {
				SetRequiredSize(SizeF.Empty);
			}
		}
		
		static StringFormat CreateTipStringFormat
			(StringAlignment horizontalAlignment,
			 StringAlignment verticalAlignment)
		{
			//StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone();
            StringFormat format = null;
			//format.FormatFlags = StringFormatFlags.FitBlackBox;
			// note: Align Near, Line Center seemed to do something before
			
			//format.Alignment     = horizontalAlignment;
			//format.LineAlignment = verticalAlignment;
			
			return format;
		}
		
		bool IsTextVisible()
		{
			return tipText != null && tipText.Length > 0;
		}
		
		public Color Color {
			get {
				return tipColor;
			}
			set {
				tipColor = value;
			}
		}
		
		public StringAlignment HorizontalAlignment {
			get {
				return horzAlign;
			}
			set {
				horzAlign = value;
				tipFormat = null;
			}
		}
		
		public StringAlignment VerticalAlignment {
			get {
				return vertAlign;
			}
			set {
				vertAlign = value;
				tipFormat = null;
			}
		}
	}
}
