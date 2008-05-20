/* Shadow code from anders */

using System;
using Gdk;

class ConvFilter
{
	public int size;
	public double[] data;
}

class Shadow
{
	public const int BLUR_RADIUS = 5;
	public const int SHADOW_OFFSET = (BLUR_RADIUS * 4 / 5);
	public const double SHADOW_OPACITY = 0.5;

	static ConvFilter filter;
	
	static double Gaussian (double x, double y, double r)
	{
	    return ((1 / (2 * System.Math.PI * r)) *
		    System.Math.Exp ((- (x * x + y * y)) / (2 * r * r)));
	}

	static ConvFilter CreateBlurFilter (int radius)
	{
		ConvFilter filter;
		int x, y;
		double sum;

		filter = new ConvFilter ();
		filter.size = radius * 2 + 1;
		filter.data = new double [filter.size * filter.size];

		sum = 0.0;

		for (y = 0 ; y < filter.size; y++) {
			for (x = 0 ; x < filter.size; x++) {
				sum += filter.data [y * filter.size + x] = Gaussian (x - (filter.size >> 1),
									y - (filter.size >> 1),
									radius);
			}
		}

		for (y = 0; y < filter.size; y++) {
			for (x = 0; x < filter.size; x++)
				filter.data [y * filter.size + x] /= sum;
		}
		return filter;
	}

	unsafe static Pixbuf CreateEffect (int src_width, int src_height, ConvFilter filter, int radius, int offset, double opacity)
	{
		Pixbuf dest;
		int x, y, i, j;
		int src_x, src_y;
		int suma;
		int dest_width, dest_height;
		int dest_rowstride;
		byte* dest_pixels;

		dest_width = src_width + 2 * radius + offset;
		dest_height = src_height + 2 * radius + offset;
		
		dest = new Pixbuf (Colorspace.Rgb, true, 8, dest_width, dest_height);
		dest.Fill (0);
	  
		dest_pixels = (byte*) dest.Pixels;
		
		dest_rowstride = dest.Rowstride;
	  
		for (y = 0; y < dest_height; y++)
		{
			for (x = 0; x < dest_width; x++)
			{
				suma = 0;

				src_x = x - radius;
				src_y = y - radius;

				/* We don't need to compute effect here, since this pixel will be 
				 * discarded when compositing */
				if (src_x >= 0 && src_x < src_width && src_y >= 0 && src_y < src_height) 
				   continue;

				for (i = 0; i < filter.size; i++)
				{
					for (j = 0; j < filter.size; j++)
					{
						src_y = -(radius + offset) + y - (filter.size >> 1) + i;
						src_x = -(radius + offset) + x - (filter.size >> 1) + j;

						if (src_y < 0 || src_y >= src_height ||
						    src_x < 0 || src_x >= src_width)
						  continue;

						suma += (int) (((byte)0xFF) * filter.data [i * filter.size + j]);
					}
				}
				
				byte r = (byte) (suma * opacity);
				dest_pixels [y * dest_rowstride + x * 4 + 3] = r;
			}
		}
		return dest;
	}

	public static Pixbuf AddShadow (int width, int height)
	{
		if (filter == null)
			filter = CreateBlurFilter (BLUR_RADIUS);
	  
		return CreateEffect (width, height, filter, BLUR_RADIUS, SHADOW_OFFSET, SHADOW_OPACITY);
	}
}
