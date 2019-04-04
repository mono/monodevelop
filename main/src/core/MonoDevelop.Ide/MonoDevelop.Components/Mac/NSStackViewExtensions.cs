//
// NSStackViewExtensions.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 
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

#if MAC
using AppKit;

namespace MonoDevelop.Components.Mac
{
	public static class NSStackViewExtensions
	{
		const int RenderingPriority = (int)NSLayoutPriority.Required;
		const int LowPriority = (int)NSLayoutPriority.DefaultLow - 1;

		public static void AddArrangedSubview (this NSStackView stackView, NSView view, bool expandHorizontally, bool expandVertically)
		{
			stackView.AddArrangedSubview (view);

			view.SetContentHuggingPriorityForOrientation (expandHorizontally ? RenderingPriority : LowPriority, NSLayoutConstraintOrientation.Horizontal);
			view.SetContentHuggingPriorityForOrientation (expandVertically ? LowPriority : RenderingPriority, NSLayoutConstraintOrientation.Vertical);
			view.SetContentCompressionResistancePriority (expandHorizontally ? RenderingPriority : LowPriority, NSLayoutConstraintOrientation.Horizontal);
			view.SetContentCompressionResistancePriority (expandVertically ? LowPriority : RenderingPriority, NSLayoutConstraintOrientation.Vertical);
		}
	}
}

#endif