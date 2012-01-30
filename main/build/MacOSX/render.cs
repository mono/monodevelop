using System;
using System.Drawing;
using System.Drawing.Imaging;

class X {
	static void Main ()
	{
		var background = new Bitmap ("dmg-bg.png");
		var ctx = Graphics.FromImage (background);
		var font = new Font ("Helvetica", 36);
		var color = Color.FromArgb (255, 159, 180, 213);
		var brush = new SolidBrush (color);
		ctx.DrawString ("FOOBAR", font, brush, new PointF (10, 12));
		background.Save ("new.png", ImageFormat.Png);
	}
}