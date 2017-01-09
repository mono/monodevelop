//
// AtkCocoaHelperNoOp.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2017 
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

#if !MAC

using System;
using Gdk;
using Gtk;

namespace MonoDevelop.Components.AtkCocoaHelper
{
	public class AccessibilityElementProxy : IAccessibilityElementProxy
	{
		public event EventHandler PerformCancel;
		public event EventHandler PerformConfirm;
		public event EventHandler PerformDecrement;
		public event EventHandler PerformDelete;
		public event EventHandler PerformIncrement;
		public event EventHandler PerformPick;
		public event EventHandler PerformPress;
		public event EventHandler PerformRaise;
		public event EventHandler PerformShowAlternateUI;
		public event EventHandler PerformShowDefaultUI;
		public event EventHandler PerformShowPopupMenu;

		public void AddAccessibleChild (IAccessibilityElementProxy child)
		{
		}

		public void SetAccessibilityHelp (string help)
		{
		}

		public void SetAccessibilityIdentifier (string identifier)
		{
		}

		public void SetAccessibilityLabel (string label)
		{
		}

		public void SetAccessibilityRole (AtkCocoaHelper.Roles role, string description = null)
		{
		}

		public void SetAccessibilityRole (string role, string description = null)
		{
		}

		public void SetAccessibilityTitle (string title)
		{
		}

		public void SetAccessibilityValue (string value)
		{
		}

		public void SetFrameInParent (Rectangle rect)
		{
		}

		public void SetFrameInRealParent (Rectangle frame)
		{
		}

		public void SetRealParent (Widget realParent)
		{
		}
	}

	public class AccessibilityElementButtonProxy
	{
	}

	public abstract class AccessibilityElementNavigableStaticTextProxy : IAccessibilityNavigableStaticText
	{
	}
}

#endif
