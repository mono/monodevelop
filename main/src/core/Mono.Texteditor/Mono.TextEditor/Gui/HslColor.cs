// 
// HslColor.cs
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
using Gdk;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public struct HslColor
	{
		public double H {
			get;
			set;
		}
		
		public double S {
			get;
			set;
		}
		
		public double L {
			get;
			set;
		}
		
		void ToRgb(out double r, out double g, out double b)
		{
			if (L == 0) {
				r = g = b = 0;
				return;
			}
			
			if (S == 0) {
				r = g = b = L;
			} else {
				double temp2 = L <= 0.5 ? L * (1.0 + S) : L + S -(L * S);
				double temp1 = 2.0 * L - temp2;
				
				double[] t3 = new double[] { H + 1.0 / 3.0, H, H - 1.0 / 3.0};
				double[] clr= new double[] { 0, 0, 0};
				for (int i = 0; i < 3; i++) {
					if (t3[i] < 0)
						t3[i] += 1.0;
					if (t3[i] > 1)
						t3[i]-=1.0;
					if (6.0 * t3[i] < 1.0)
						clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6.0;
					else if (2.0 * t3[i] < 1.0)
						clr[i] = temp2;
					else if (3.0 * t3[i] < 2.0)
						clr[i] = (temp1 + (temp2 - temp1) * ((2.0 / 3.0) - t3[i]) * 6.0);
					else
						clr[i] = temp1;
				}
				
				r = clr[0];
				g = clr[1];
				b = clr[2];
			}
		}
		
		public static implicit operator Color (HslColor hsl)
		{
			double r = 0, g = 0, b = 0;
			hsl.ToRgb (out r, out g, out b);
			return new Color ((byte)(255 * r), 
			                  (byte)(255 * g), 
			                  (byte)(255 * b));
		}
		
		public static implicit operator Cairo.Color (HslColor hsl)
		{
			double r = 0, g = 0, b = 0;
			hsl.ToRgb (out r, out g, out b);
			return new Cairo.Color (r, g, b);
		}
		
		public static implicit operator HslColor (Color color)
		{
			return new HslColor (color);
		}
		
		public static implicit operator HslColor (Cairo.Color color)
		{
			return new HslColor (color);
		}
		
		public static HslColor FromHsl (double h, double s, double l)
		{
			return new HslColor () {
				H = h,
				S = s,
				L = l
			};
		}

		public uint ToPixel ()
		{
			double r, g, b;
			ToRgb(out r, out g, out b);
			uint rv = (uint)(r * 255);
			uint gv = (uint)(g * 255);
			uint bv = (uint)(b * 255);
			return rv << 16 | gv << 8 | bv;
		}

		public static HslColor FromPixel (uint pixel)
		{
			var r = ((pixel >> 16) & 0xFF) / 255.0;
			var g = ((pixel >> 8) & 0xFF) / 255.0;
			var b = (pixel & 0xFF) / 255.0;
			return new HslColor (r, g, b);
		}
		
		public HslColor (double r, double g, double b) : this ()
		{
			double v = System.Math.Max (r, g);
			v = System.Math.Max (v, b);

			double m = System.Math.Min (r, g);
			m = System.Math.Min (m, b);
			
			this.L = (m + v) / 2.0;
			if (this.L <= 0.0)
				return;
			double vm = v - m;
			this.S = vm;
			
			if (this.S > 0.0) {
				this.S /= (this.L <= 0.5) ? (v + m) : (2.0 - v - m);
			} else {
				return;
			}
			
			double r2 = (v - r) / vm;
			double g2 = (v - g) / vm;
			double b2 = (v - b) / vm;
			
			if (r == v) {
				this.H = (g == m ? 5.0 + b2 : 1.0 - g2);
			} else if (g == v) {
				this.H = (b == m ? 1.0 + r2 : 3.0 - b2);
			} else {
				this.H = (r == m ? 3.0 + g2 : 5.0 - r2);
			}
			this.H /= 6.0;
		}
		
		public HslColor (Color color) : this (color.Red / (double)ushort.MaxValue, color.Green / (double)ushort.MaxValue, color.Blue / (double)ushort.MaxValue)
		{
		}
		
		public HslColor (Cairo.Color color) : this (color.R, color.G, color.B)
		{
		}
		
		public static HslColor Parse (string color)
		{
			Gdk.Color col = new Gdk.Color (0, 0, 0);
			Gdk.Color.Parse (color, ref col);
			return (HslColor)col;
		}

		public static double Brightness (HslColor c)
		{
			return Brightness ((Cairo.Color)c);
		}

		public static double Brightness (Cairo.Color c)
		{
			double r = c.R;
			double g = c.G;
			double b = c.B;
			return System.Math.Sqrt (r * .241 + g * .691 + b * .068);
		}

		public static double Brightness (Gdk.Color c)
		{
			double r = c.Red / (double)ushort.MaxValue;
			double g = c.Green / (double)ushort.MaxValue;
			double b = c.Blue / (double)ushort.MaxValue;
			return System.Math.Sqrt (r * .241 + g * .691 + b * .068);
		}
		
		public static List<HslColor> GenerateHighlightColors (HslColor backGround, HslColor foreGround, int n)
		{
			double bgH = (backGround.H == 0 && backGround.S == 0) ? 2 / 3.0 : backGround.H;
			var result = new List<HslColor> ();
			for (int i = 0; i < n; i++) {
				double h = bgH + (i + 1.0) / (double)n;
				
				// for monochromatic backround the h value doesn't matter
				if (i + 1 == n && !(backGround.H == 0 && backGround.S == 0))
					h = bgH + 0.5;
				
				if (h > 1.0)
					h -= 1.0;
					
				double s = 0.85;
				double l = 0.5;
				if (backGround.H == 0 && backGround.S == 0 && backGround.L < 0.5)
					l = 0.8;
				result.Add (Mono.TextEditor.HslColor.FromHsl (h, s, l));
			}
			return result;
		}
		
		public override string ToString ()
		{
			return string.Format ("[HslColor: H={0}, S={1}, L={2}]", H, S, L);
		}
		
		public string ToPangoString ()
		{
			var resultColor = (Cairo.Color)this;
			return string.Format ("#{0:x2}{1:x2}{2:x2}",
				(int)(resultColor.R * 255),
				(int)(resultColor.G * 255), 
				(int)(resultColor.B * 255));
		}
	}
}
