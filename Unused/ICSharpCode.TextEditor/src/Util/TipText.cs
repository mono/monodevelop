// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

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
