//
// IMultipleTabs.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using System.Linq;

namespace MonoDevelop.Ide.Gui.Content
{
	/// <summary>
	/// ContentView can implement this interface to show multiple tabs below content
	/// e.g. "Designer" and "Split" tabs. Beside specifing multiple tabs each tab can specify
	/// one or two content views to be shown when that tab is activated(clicked by user).
	/// </summary>
	public interface IMultipleTabs
	{
		/// <summary>
		/// Gets the tab page infos see this interface documentation and
		/// <see cref="T:MonoDevelop.Ide.Gui.Content.TabPageInfo"/> constructor for more information.
		/// </summary>
		IEnumerable<TabPageInfo> GetTabPageInfos ();
	}

	public class TabPageInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MonoDevelop.Ide.Gui.Content.TabPageInfo"/> class.
		/// </summary>
		/// <param name="label">Label is text displayed in tab.</param>
		/// <param name="accessibilityDescription">Accessibility description is used to give more context about tab to visually impaired users.</param>
		/// <param name="viewsSelection">Views selection can either be one or two content views to be displayed when this tab is activated.</param>
		public TabPageInfo (string label, string accessibilityDescription, BaseViewContent [] viewsSelection)
		{
			Label = label;
			AccessibilityDescription = accessibilityDescription;
			var count = viewsSelection.Count ();
			if (count < 1 || count > 2)
				throw new ArgumentOutOfRangeException ($"{nameof (viewsSelection)} argument must contain one or two content views.");
			ViewsSelection = viewsSelection;
		}

		public string Label { get; }
		public string AccessibilityDescription { get; }
		public BaseViewContent [] ViewsSelection { get; }
	}
}
