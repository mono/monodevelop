//
// MacDebuggerObjectTypeView.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

using AppKit;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The NSTableViewCell used for the "Type" column.
	/// </summary>
	class MacDebuggerObjectTypeView : MacDebuggerObjectCellViewBase
	{
		public MacDebuggerObjectTypeView (MacObjectValueTreeView treeView) : base (treeView, "type")
		{
			TextField = new NSTextField {
				TranslatesAutoresizingMaskIntoConstraints = false,
				BackgroundColor = NSColor.Clear,
				MaximumNumberOfLines = 1,
				Bordered = false,
				Editable = false
			};

			AddSubview (TextField);

			TextField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
			TextField.LeadingAnchor.ConstraintEqualToAnchor (LeadingAnchor, MarginSize).Active = true;
			TextField.TrailingAnchor.ConstraintEqualToAnchor (TrailingAnchor, -MarginSize).Active = true;
		}

		public MacDebuggerObjectTypeView (IntPtr handle) : base (handle)
		{
		}

		protected override void UpdateContents ()
		{
			var value = Node?.TypeName ?? string.Empty;

			TextField.StringValue = value;
			UpdateFont (TextField);

			OptimalWidth = MarginSize + GetWidthForString (TextField.Font, value) + MarginSize;
		}
	}
}
