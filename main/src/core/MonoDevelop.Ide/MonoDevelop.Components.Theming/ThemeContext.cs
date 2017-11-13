//
// Theme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Cairo;

namespace MonoDevelop.Components.Theming
{
	class ThemeContext
	{
		private double radius = 3.0;
		public double Radius {
			get { return radius; }
			set { radius = value; }
		}

		private double fill_alpha = 1.0;
		public double FillAlpha {
			get { return fill_alpha; }
			set { fill_alpha = HelperMethods.Clamp (0.0, 1.0, value); }
		}

		private double line_width = 1.0;
		public double LineWidth {
			get { return line_width; }
			set { line_width = value; }
		}

		private bool show_stroke = true;
		public bool ShowStroke {
			get { return show_stroke; }
			set { show_stroke = value; }
		}

		private double x;
		public double X {
			get { return x; }
			set { x = value; }
		}

		private double y;
		public double Y {
			get { return y; }
			set { y = value; }
		}

		private Cairo.Context cairo;
		public Cairo.Context Cairo {
			get { return cairo; }
			set { cairo = value; }
		}
	}
}
