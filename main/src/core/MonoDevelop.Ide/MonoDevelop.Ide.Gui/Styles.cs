// 
// Styles.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
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

namespace MonoDevelop.Ide.Gui
{
	static class Styles
	{
		public static readonly Cairo.Color TabBarGradientStartColor = new Cairo.Color (248d / 255d, 248d / 255d, 248d / 255d);
		public static readonly Cairo.Color TabBarGradientMidColor = new Cairo.Color (217d / 255d, 217d / 255d, 217d / 255d);
		public static readonly Cairo.Color TabBarGradientEndColor = new Cairo.Color (183d / 255d, 183d / 255d, 183d / 255d);
		public static readonly Cairo.Color BreadcrumbBackgroundColor = new Cairo.Color (77d / 255d, 77d / 255d, 77d / 255d);
		public static readonly Cairo.Color BreadcrumbGradientStartColor = new Cairo.Color (100d / 255d, 100d / 255d, 100d / 255d);
		public static readonly Cairo.Color BreadcrumbGradientEndColor = new Cairo.Color (51d / 255d, 51d / 255d, 51d / 255d);
		public static readonly Cairo.Color BreadcrumbBorderColor = new Cairo.Color (55d / 255d, 55d / 255d, 55d / 255d);
		public static readonly Cairo.Color BreadcrumbInnerBorderColor = new Cairo.Color (1, 1, 1, 0.1d);
		public static readonly Cairo.Color BreadcrumbTextColor = new Cairo.Color (204d / 255d, 204d / 255d, 204d / 255d);
		public static readonly Cairo.Color BreadcrumbButtonBorderColor = new Cairo.Color (204d / 255d, 204d / 255d, 204d / 255d);
		public static readonly Cairo.Color BreadcrumbButtonFillColor = new Cairo.Color (1, 1, 1, 0.1d);
	}
}

