//
// IDockItemBackend.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Components.Docking
{
	public interface IDockItemBackend
	{
		void SetLabel (string label);
		void SetIcon (Xwt.Drawing.Image icon);
		void SetStyle (DockVisualStyle style);
		void SetContent (Control content);
		void SetBehavior (DockItemBehavior behavior);

		DockItemToolbar CreateToolbar (DockPositionType position);

		void Present (bool giveFocus);
		bool ContentVisible { get; }

		void ShowWidget ();
		void HideWidget ();

		void SetFloatMode (Xwt.Rectangle rect);
		Xwt.Rectangle FloatingPosition { get; }
		void SetAutoHideMode (DockPositionType pos, int size);
		int AutoHideSize { get; }
		void Minimize ();
		void ResetMode ();

		/// <summary>
		/// Returns the size and position of the item in screen coordinates, or Rectangle.Empty if the item is not visible.
		/// </summary>
		Xwt.Rectangle GetAllocation ();
	}
}

