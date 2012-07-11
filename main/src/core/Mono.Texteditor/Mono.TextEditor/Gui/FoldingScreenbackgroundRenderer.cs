// 
// FoldingScreenbackgroundRenderer.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public class FoldingScreenbackgroundRenderer : IBackgroundRenderer
	{
		TextEditor editor;
		List<FoldSegment> foldSegments;
		[Flags]
		enum Roles
		{
			Between = 0,
			Start = 1,
			End = 2
		}
		TextDocument Document {
			get { return editor.Document; }
		}

		public FoldingScreenbackgroundRenderer (TextEditor editor, IEnumerable<FoldSegment> foldSegments)
		{
			this.editor = editor;
			this.foldSegments = new List<FoldSegment> (foldSegments);
		}

		HslColor GetColor (int i, double brightness, int colorCount)
		{
			HslColor hslColor = new HslColor (editor.ColorStyle.Default.BackgroundColor);
			int colorPosition = i + 1;
			if (i == foldSegments.Count - 1)
				colorPosition += 2;
			if (brightness < 0.5) {
				hslColor.L = hslColor.L * 0.81 + hslColor.L * 0.25 * (colorCount - colorPosition) / colorCount;
			} else {
				hslColor.L = hslColor.L * 0.86 + hslColor.L * 0.1 * colorPosition / colorCount;
			}
			return hslColor;
		}
		
		class DropShadow : IDisposable
		{
			readonly Cairo.ImageSurface buffer;
			readonly int radius;
			
			public DropShadow (int width, int height, int radius)
			{
				this.radius = radius;
				buffer = new Cairo.ImageSurface (Cairo.Format.Argb32, width, height);
			}

			public Cairo.Context GetContext ()
			{
				return new Cairo.Context (buffer);
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

			unsafe static byte[] cairocks_gaussian_blur (Cairo.ImageSurface surface, double radius, double deviation)
			{
				double[] horzBlurArr;
				double[] vertBlurArr;
				double[] kernel;
				byte[] dataArr;
				Cairo.Format format;
				int width;
				int height;
				int stride;
				int channels;
				int iY;
				int iX;
				
				if (surface.Status == Cairo.Status.NullPointer)
					return null;
				
				dataArr = surface.Data;
				format = surface.Format;
				width = surface.Width;
				height = surface.Height;
				stride = surface.Stride;
				
				if (format == Cairo.Format.Argb32)
					channels = 4;
				else if (format == Cairo.Format.Rgb24)
					channels = 3;
				else if (format == Cairo.Format.A8)
					channels = 1;
				else
					return null;
				
				horzBlurArr = new double[height * stride];
				vertBlurArr = new double[height * stride];
				kernel = create_kernel (radius, deviation);

				fixed (double* horzBlur = horzBlurArr)
					fixed (double* vertBlur = vertBlurArr)
						fixed (byte* data = dataArr) {
							/* Horizontal pass. */
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
								
										dataPtr = &data [iY * stride + x * channels];
										kernip1 = kernel [i + 1];
								
										if (channels == 1)
											alpha += kernip1 * dataPtr [0];
										else {
											if (channels == 4)
												alpha += kernip1 * dataPtr [3];
									
											red += kernip1 * dataPtr [2];
											green += kernip1 * dataPtr [1];
											blue += kernip1 * dataPtr [0];
										}
								
										offset++;
									}
							
									baseOffset = iY * stride + iX * channels;
							
									if (channels == 1)
										horzBlur [baseOffset] = alpha;
									else {
										if (channels == 4)
											horzBlur [baseOffset + 3] = alpha;
								
										horzBlur [baseOffset + 2] = red;
										horzBlur [baseOffset + 1] = green;
										horzBlur [baseOffset] = blue;
									}
								}
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
								
										dataPtr = &horzBlur [y * stride + iX * channels];
										kernip1 = kernel [i + 1];
								
										if (channels == 1)
											alpha += kernip1 * dataPtr [0];
										else {
											if (channels == 4)
												alpha += kernip1 * dataPtr [3];
									
											red += kernip1 * dataPtr [2];
											green += kernip1 * dataPtr [1];
											blue += kernip1 * dataPtr [0];
										}
								
										offset++;
									}
							
									baseOffset = iY * stride + iX * channels;
							
									if (channels == 1)
										vertBlur [baseOffset] = alpha;
									else {
										if (channels == 4)
											vertBlur [baseOffset + 3] = alpha;
								
										vertBlur [baseOffset + 2] = red;
										vertBlur [baseOffset + 1] = green;
										vertBlur [baseOffset] = blue;
									}
								}
							}

							for (iY = 0; iY < height; iY++) {
								for (iX = 0; iX < width; iX++) {
									int i = iY * stride + iX * channels;
							
									if (channels == 1)
										data [i] = (byte)(vertBlur [i]);
									else {
										if (channels == 4)
											data [i + 3] = (byte)(
									vertBlur [i + 3]
									);
								
										data [i + 2] = (byte)(vertBlur [i + 2]);
										data [i + 1] = (byte)(vertBlur [i + 1]);
										data [i] = (byte)(vertBlur [i]);
									}
								}
							}
				
							return dataArr;
						}
			}

			public void Draw (Cairo.Context cr)
			{
				buffer.Flush ();
				var tmp = cairocks_gaussian_blur (buffer, radius, 1);
				buffer.MarkDirty ();

				using (var newImage = new Cairo.ImageSurface (tmp, Cairo.Format.Argb32, buffer.Width, buffer.Height, buffer.Stride)) {
					cr.Operator = Cairo.Operator.Atop;
					cr.SetSourceSurface (newImage, 0, 0);
					cr.Paint ();
				}
			}

			#region IDisposable implementation
			public void Dispose ()
			{
				buffer.Dispose ();
			}
			#endregion
		}
		

		public void Draw (Cairo.Context cr, Cairo.Rectangle area)
		{

			TextViewMargin textViewMargin = editor.TextViewMargin;
			ISyntaxMode mode = Document.SyntaxMode != null && editor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : new SyntaxMode (Document);

			TextViewMargin.LayoutWrapper lineLayout = null;
			double brightness = HslColor.Brightness (editor.ColorStyle.Default.BackgroundColor);

			int colorCount = foldSegments.Count + 2;
			cr.Color = GetColor (-1, brightness, colorCount);
			cr.Rectangle (area);
			cr.Fill ();
			for (int i = 0; i < foldSegments.Count; i++) {
				FoldSegment segment = foldSegments [i];

				double xPos = textViewMargin.XOffset;
				double rectangleWidth = 0, rectangleHeight = 0;

				DocumentLine segmentStartLine = segment.StartLine;
				lineLayout = textViewMargin.CreateLinePartLayout (mode, segmentStartLine, segmentStartLine.Offset, segmentStartLine.Length, -1, -1);
				var rectangleStart = lineLayout.Layout.IndexToPos (GetFirstNonWsIdx (lineLayout.Layout.Text));
				xPos = System.Math.Max (textViewMargin.XOffset, (textViewMargin.XOffset + textViewMargin.TextStartPosition + rectangleStart.X / Pango.Scale.PangoScale - editor.HAdjustment.Value));
				
				DocumentLine segmentEndLine = segment.EndLine;
				lineLayout = textViewMargin.CreateLinePartLayout (mode, segmentEndLine, segmentEndLine.Offset, segmentEndLine.Length, -1, -1);
				var rectangleEnd = lineLayout.Layout.IndexToPos (GetFirstNonWsIdx (lineLayout.Layout.Text));
				xPos = System.Math.Min (xPos, System.Math.Max (textViewMargin.XOffset, (textViewMargin.XOffset + textViewMargin.TextStartPosition + rectangleEnd.X / Pango.Scale.PangoScale - editor.HAdjustment.Value)));
				
				int width = editor.Allocation.Width;
				if (editor.HAdjustment.Upper > width) {
					width = (int)(textViewMargin.XOffset + editor.HAdjustment.Upper - editor.HAdjustment.Value);
				}
				rectangleWidth = (int)(width - xPos - 6 * (i + 1));
				var y = editor.LineToY (segment.StartLine.LineNumber);
				var yEnd = editor.LineToY (segment.EndLine.LineNumber + 1);
				if (yEnd == 0)
					yEnd = editor.VAdjustment.Upper;
				rectangleHeight = yEnd - y;

				if (i == foldSegments.Count - 1) {
					var radius = (int)(editor.Options.Zoom * 2);
					int w = 10;
					using (var shadow = new DropShadow ((int)rectangleWidth + w * 2, (int)rectangleHeight + w * 2, radius)) {
						using (var gctx = shadow.GetContext ()) {
							gctx.Color = new Cairo.Color (0, 0, 0, 0);
							gctx.Fill ();

							var a = 0;
							DrawRoundRectangle (gctx, true, true, w - a, w - a, editor.LineHeight / 4, rectangleWidth + a * 2, rectangleHeight + a * 2);
							gctx.Color = new Cairo.Color (0, 0, 0, 1);
							gctx.Fill ();
						}

						cr.Save ();
						cr.Translate (xPos - w, y - editor.VAdjustment.Value - w);
						shadow.Draw (cr);
						cr.Restore ();
					}
				}

				DrawRoundRectangle (cr, true, true, xPos, y - editor.VAdjustment.Value, editor.LineHeight / 4, rectangleWidth, rectangleHeight);

				cr.Color = GetColor (i, brightness, colorCount);
				cr.Fill ();
			}
		}

		public static void DrawRoundRectangle (Cairo.Context cr, bool upperRound, bool lowerRound, double x, double y, double r, double w, double h)
		{
			DrawRoundRectangle (cr, upperRound, upperRound, lowerRound, lowerRound, x, y, r, w, h);
		}
		
		public static void DrawRoundRectangle (Cairo.Context cr, bool topLeftRound, bool topRightRound, bool bottomLeftRound, bool bottomRightRound, double x, double y, double r, double w, double h)
		{
			//  UA****BQ
			//  H      C
			//  *      *
			//  G      D
			//  TF****ES
			
			cr.NewPath ();
			
			if (topLeftRound) {
				cr.MoveTo (x + r, y);                 // Move to A
			} else {
				cr.MoveTo (x, y);             // Move to U
			}
			
			if (topRightRound) {
				cr.LineTo (x + w - r, y);             // Straight line to B
				
				cr.CurveTo (x + w, y, 
				            x + w, y,
				            x + w, y + r); // Curve to C, Control points are both at Q
			} else {
				cr.LineTo (x + w, y);         // Straight line to Q
			}
			
			if (bottomRightRound) {
				cr.LineTo (x + w, y + h - r);                              // Move to D

				cr.CurveTo (x + w, y + h, 
				            x + w, y + h, 
				            x + w - r, y + h); // Curve to E
			} else {
				cr.LineTo (x + w, y + h); // Move to S
			}
			
			if (bottomLeftRound) {
				cr.LineTo (x + r, y + h);                      // Line to F
				cr.CurveTo (x, y + h, 
				            x, y + h, 
				            x, y + h - r); // Curve to G
			} else {
				cr.LineTo (x, y + h); // Line to T
			}
			
			if (topLeftRound) {
				cr.LineTo (x, y + r);              // Line to H
				cr.CurveTo (x, y, 
				            x, y, 
				            x + r, y); // Curve to A
			} else {
				cr.LineTo (x, y); // Line to U
			}
			cr.ClosePath ();
		}
		
		int GetFirstNonWsIdx (string text)
		{
			for (int i = 0; i < text.Length; i++) {
				if (!Char.IsWhiteSpace (text [i]))
					return i;
			}
			return 0;
		}
		
	}
}
