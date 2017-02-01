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
	public static class AtkCocoaNoopExtensions
	{
		public static void SetAccessibilityLabel (this Atk.Object o, string label)
		{
		}

		public static void SetAccessibilityLabel (this Gtk.CellRenderer r, string label)
		{
		}

		public static void SetAccessibilityDescription (this Gtk.CellRenderer r, string description)
		{
		}

		public static void SetAccessibilityShouldIgnore (this Atk.Object o, bool ignore)
		{
		}

		public static void SetAccessibilityTitle (this Atk.Object o, string title)
		{
		}

		public static void SetAccessibilityDocument (this Atk.Object o, string documentUrl)
		{
		}

		public static void SetAccessibilityFilename (this Atk.Object o, string filename)
		{
		}

		public static void SetAccessibilityIsMainWindow (this Atk.Object o, bool isMainWindow)
		{
		}

		public static void SetAccessibilityMainWindow (this Atk.Object o, Atk.Object mainWindow)
		{
		}

		public static void SetAccessibilityValue (this Atk.Object o, string stringValue)
		{
		}

		public static void SetAccessibilityURL (this Atk.Object o, string url)
		{
		}

		public static void SetAccessibilityRole (this Atk.Object o, string role, string description = null)
		{
		}

		public static void SetAccessibilityRole (this Atk.Object o, AtkCocoa.Roles role, string description = null)
		{
		}

		public static void SetAccessibilitySubRole (this Atk.Object o, string subrole)
		{
		}

		public static void SetAccessibilityTitleUIElement (this Atk.Object o, Atk.Object title)
		{
		}

		public static void SetAccessibilityAlternateUIVisible (this Atk.Object o, bool visible)
		{
		}

		public static void SetAccessibilityOrientation (this Atk.Object o, Gtk.Orientation orientation)
		{
		}

		public static void SetAccessibilityTitleFor (this Atk.Object o, params Atk.Object [] objects)
		{
		}

		public static void SetAccessibilityTabs (this Atk.Object o, AccessibilityElementProxy [] tabs)
		{
		}

		public static void SetAccessibilityTabs (this Atk.Object o, Atk.Object [] tabs)
		{
		}

		public static void AccessibilityAddElementToTitle (this Atk.Object title, Atk.Object o)
		{
		}

		public static void AccessibilityRemoveElementFromTitle (this Atk.Object title, Atk.Object o)
		{
		}

		public static void AccessibilityReplaceAccessibilityElements (this Atk.Object parent, AccessibilityElementProxy [] children)
		{
		}

		public static void SetAccessibilityColumns (this Atk.Object parent, AccessibilityElementProxy [] columns)
		{
		}

		public static void SetAccessibilityRows (this Atk.Object parent, AccessibilityElementProxy [] rows)
		{
		}

		public static void SetActionDelegate (this Atk.Object o, ActionDelegate ad)
		{
		}

		public static void AddAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
		}

		public static void RemoveAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
		}

		public static void SetAccessibleChildren (this Atk.Object o, AccessibilityElementProxy [] children)
		{
		}

		public static void AddAccessibleLinkedUIElement (this Atk.Object o, Atk.Object linked)
		{
		}
	}

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

		public void SetAccessibilityRole (AtkCocoa.Roles role, string description = null)
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
		public int InsertionPointLineNumber {
			get {
				throw new NotImplementedException ();
			}
		}

		public int NumberOfCharacters {
			get {
				throw new NotImplementedException ();
			}
		}

		public string Value {
			get {
				throw new NotImplementedException ();
			}
		}

		public Rectangle GetFrameForRange (AtkCocoa.Range range)
		{
			throw new NotImplementedException ();
		}

		public int GetLineForIndex (int index)
		{
			throw new NotImplementedException ();
		}

		public AtkCocoa.Range GetRangeForIndex (int index)
		{
			throw new NotImplementedException ();
		}

		public AtkCocoa.Range GetRangeForLine (int line)
		{
			throw new NotImplementedException ();
		}

		public AtkCocoa.Range GetRangeForPosition (Point position)
		{
			throw new NotImplementedException ();
		}

		public string GetStringForRange (AtkCocoa.Range range)
		{
			throw new NotImplementedException ();
		}

		public AtkCocoa.Range GetStyleRangeForIndex (int index)
		{
			throw new NotImplementedException ();
		}
	}
}

#endif
