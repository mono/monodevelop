//
// XwtControl.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
namespace MonoDevelop.Components
{
	public class XwtControl: AbstractXwtControl
	{
		readonly Xwt.Widget widget;

		public override Xwt.Widget Widget {
			get { return widget; }
		}

		public XwtControl (Xwt.Widget widget)
		{
			this.widget = widget;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}
	}

	public abstract class AbstractXwtControl : Control
	{
		protected override object CreateNativeWidget<T> ()
		{
			// Use the widgets toolkit to get the native widget prepared for embedding.
			// Using Widget.Surface.NativeWidget directly may cause sizing issues when embedded
			// into a different toolkit.
			return Widget.Surface.ToolkitEngine.GetNativeWidget (Widget);
		}

		public abstract Xwt.Widget Widget {
			get;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}
	}
}

