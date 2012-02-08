using System;
using System.Drawing;
using System.Drawing.Imaging;

class X {
	static void Main (string[] args)
	{
		var background = new Bitmap ("dmg-bg.png");
		var ctx = Graphics.FromImage (background);
		
		//system.drawing doesn't allow setting the actual font weight
		//so we can't get it as heavy as we need :/
		var font = new Font ("Helvetica", 12, FontStyle.Bold);

		var light = new SolidBrush (Color.FromArgb (255, 255, 255, 255));
		var dark = new SolidBrush (Color.FromArgb (230, 151, 173, 190));

		float x = 10, y = 10;
		ctx.DrawString (args[0], font, light, new PointF (x, y + 1f));
		ctx.DrawString (args[0], font, dark, new PointF (x, y));

		background.Save ("dmg-bg-with-version.png", ImageFormat.Png);
	}
}
