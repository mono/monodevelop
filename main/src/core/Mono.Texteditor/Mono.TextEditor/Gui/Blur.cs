//
// Blur.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// Translated over from c - original code can be found at http://cairographics.org/cookbook/
//
// Copyright © 2008 Kristian Høgsberg
// Copyright © 2009 Chris Wilson
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace Mono.TextEditor
{
	class Blur : IDisposable
	{
		readonly Cairo.ImageSurface buffer;
		readonly int radius;
		
		public Blur (int width, int height, int radius)
		{
			this.radius = radius;
			buffer = new Cairo.ImageSurface (Cairo.Format.Argb32, width, height);
		}
		
		public Cairo.Context GetContext ()
		{
			return new Cairo.Context (buffer);
		}
		
		
		public void Draw (Cairo.Context cr)
		{
			buffer.Flush ();
			if (buffer.Status == Cairo.Status.NullPointer || buffer.Status == Cairo.Status.NoMemory)
				return;
			var tmp = cairocks_gaussian_blur (buffer.Data, buffer.Width, buffer.Height, buffer.Stride, radius, radius);
			buffer.MarkDirty ();
			
			using (var newImage = new Cairo.ImageSurface (tmp, Cairo.Format.Argb32, buffer.Width, buffer.Height, buffer.Stride)) {
				cr.Operator = Cairo.Operator.Atop;
				cr.SetSourceSurface (newImage, 0, 0);
				cr.Paint ();
			}
		}
		
		static double[] create_kernel (double radius, double deviation)
		{
			int size = 2 * (int)(radius) + 1;
			double[] kernel = new double [size + 1];
			double radiusf = System.Math.Abs (radius) + 1.0f;
			double value = -radius;
			double sum = 0.0f;
			int i;
			
			if (deviation == 0.0f)
				deviation = System.Math.Sqrt (
					-(radiusf * radiusf) / (2.0f * System.Math.Log (1.0f / 255.0f))
					);
			
			kernel [0] = size;
			
			for (i = 0; i < size; i++) {
				kernel [1 + i] = 
					1.0f / (2.506628275f * deviation) *
						System.Math.Exp (-((value * value) / (2.0f * (deviation * deviation))));
				
				sum += kernel [1 + i];
				value += 1.0f;
			}
			
			for (i = 0; i < size; i++)
				kernel [1 + i] /= sum;
			
			return kernel;
		}
		static double[] kernel;
		static double[] tmp;
		static double oldRadius = 0, oldDeviation = 0;

		unsafe static byte[] cairocks_gaussian_blur (byte[] dataArr, int width, int height, int stride, double radius, double deviation)
		{
			int channels = 4;
			int iY;
			int iX;

			if (tmp == null || tmp.Length < height * stride)
				tmp = new double[height * stride];

			if (oldRadius != radius || oldDeviation != deviation) {
				kernel = create_kernel (radius, deviation);
				oldRadius = radius;
				oldDeviation = deviation;
			}

			fixed (double* tmpPtr = tmp)
			fixed (byte* data = dataArr) {
				/* Horizontal pass. */
				int yOffset = 0;
				for (iY = 0; iY < height; iY++) {
					for (iX = 0; iX < width; iX++) {
						double red = 0.0f;
						double green = 0.0f;
						double blue = 0.0f;
						double alpha = 0.0f;
						int offset = (int)(kernel [0]) / -2;
						int baseOffset;
						int i;
						
						for (i = 0; i < (int)(kernel[0]); i++) {
							byte* dataPtr;
							int x = iX + offset;
							double kernip1;
							
							if (x < 0 || x >= width)
								continue;
							
							dataPtr = &data [yOffset + x * channels];
							kernip1 = kernel [i + 1];
							
							alpha += kernip1 * dataPtr [3];
							
							red += kernip1 * dataPtr [2];
							green += kernip1 * dataPtr [1];
							blue += kernip1 * dataPtr [0];
							
							offset++;
						}
						
						baseOffset = iY * stride + iX * channels;
						
						tmpPtr [baseOffset + 3] = alpha;
						tmpPtr [baseOffset + 2] = red;
						tmpPtr [baseOffset + 1] = green;
						tmpPtr [baseOffset]     = blue;
					}
					yOffset += stride;
				}
				
				/* Vertical pass. */
				for (iY = 0; iY < height; iY++) {
					for (iX = 0; iX < width; iX++) {
						double red = 0.0f;
						double green = 0.0f;
						double blue = 0.0f;
						double alpha = 0.0f;
						int offset = (int)(kernel [0]) / -2;
						int baseOffset;
						int i;
						
						for (i = 0; i < (int)(kernel[0]); i++) {
							double* dataPtr;
							int y = iY + offset;
							double kernip1;
							
							if (y < 0 || y >= height) {
								offset++;
								continue;
							}
							
							dataPtr = &tmpPtr [y * stride + iX * channels];
							kernip1 = kernel [i + 1];
							
							alpha += kernip1 * dataPtr [3];
							red += kernip1 * dataPtr [2];
							green += kernip1 * dataPtr [1];
							blue += kernip1 * dataPtr [0];
							
							offset++;
						}
						
						baseOffset = iY * stride + iX * channels;
						
						tmpPtr [baseOffset + 3] = alpha;
						tmpPtr [baseOffset + 2] = red;
						tmpPtr [baseOffset + 1] = green;
						tmpPtr [baseOffset] = blue;
					}
				}

				yOffset = 0;
				for (iY = 0; iY < height; iY++) {
					int offset = yOffset;
					for (iX = 0; iX < width; iX++) {
						
						data [offset] = (byte)tmpPtr [offset];
						offset++;
						data [offset] = (byte)tmpPtr [offset];
						offset++;
						data [offset] = (byte)tmpPtr [offset];
						offset++;
						data [offset] = (byte)tmpPtr [offset];
						offset++;
					}
					yOffset += stride;
				}
				
				return dataArr;
			}
		}
		
		
		
		#region IDisposable implementation
		public void Dispose ()
		{
			buffer.Dispose ();
		}
		#endregion
	}
}


#region Original header
/*
 * Copyright © 2008 Kristian Høgsberg
 * Copyright © 2009 Chris Wilson
 *
 * Permission to use, copy, modify, distribute, and sell this software and its
 * documentation for any purpose is hereby granted without fee, provided that
 * the above copyright notice appear in all copies and that both that copyright
 * notice and this permission notice appear in supporting documentation, and
 * that the name of the copyright holders not be used in advertising or
 * publicity pertaining to distribution of the software without specific,
 * written prior permission.  The copyright holders make no representations
 * about the suitability of this software for any purpose.  It is provided "as
 * is" without express or implied warranty.
 *
 * THE COPYRIGHT HOLDERS DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
 * INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
 * EVENT SHALL THE COPYRIGHT HOLDERS BE LIABLE FOR ANY SPECIAL, INDIRECT OR
 * CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
 * DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
 * TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE
 * OF THIS SOFTWARE.
 */
#endregion
