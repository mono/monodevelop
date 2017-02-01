//
// AtkCocoaHelperMac.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corp
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

#if MAC

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using AppKit;
using CoreGraphics;
using Foundation;
using Gdk;
using ObjCRuntime;

namespace MonoDevelop.Components.AtkCocoaHelper
{
	public static class AtkCocoaMacExtensions
	{
		const string XamarinPrivateAtkCocoaNSAccessibilityKey = "xamarin-private-atkcocoa-nsaccessibility";
		internal static INSAccessibility GetNSAccessibilityElement (GLib.Object o)
		{
			IntPtr handle = GtkWorkarounds.GetData (o, XamarinPrivateAtkCocoaNSAccessibilityKey);

			// The object returned could either be an NSAccessibilityElement or it might be an NSObject that implements INSAccessibility
			return Runtime.GetNSObject<NSObject> (handle, false) as INSAccessibility;
		}

		internal static void SetNSAccessibilityElement (Atk.Object o, INativeObject native)
		{
			GtkWorkarounds.SetData (o, XamarinPrivateAtkCocoaNSAccessibilityKey, native.Handle);
		}

		internal static void DumpAccessibilityTree (NSObject obj = null, int indentLevel = 0)
		{
			if (obj == null) {
				obj = NSApplication.SharedApplication;
			}

			string desc = obj.Description;
			desc = desc.PadLeft (desc.Length + indentLevel, ' ');
			Console.WriteLine ($"{desc}");

			if (!obj.RespondsToSelector (new Selector ("accessibilityChildren"))) {
				string notAccessible = "Not accessible";
				Console.WriteLine ($"{notAccessible.PadLeft (notAccessible.Length + indentLevel + 2, ' ')}");
				return;
			}

			NSArray children = (NSArray)obj.PerformSelector (new Selector ("accessibilityChildren"));
			if (children == null) {
				return;
			}

			for (nuint i = 0; i < children.Count; i++) {
				DumpAccessibilityTree (children.GetItem<NSObject> (i), indentLevel + 2);
			}
		}

		public static void SetAccessibilityLabel (this Atk.Object o, string label)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityLabel = label;
		}

		public static void SetAccessibilityLabel (this Gtk.CellRenderer r, string label)
		{
			var nsa = GetNSAccessibilityElement (r);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityLabel = label;
		}

		public static void SetAccessibilityDescription (this Gtk.CellRenderer r, string description)
		{
			var nsa = GetNSAccessibilityElement (r);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityHelp = description;
		}

		public static void SetAccessibilityShouldIgnore (this Atk.Object o, bool ignore)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityElement = !ignore;
		}

		public static void SetAccessibilityTitle (this Atk.Object o, string title)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityTitle = title;
		}

		public static void SetAccessibilityDocument (this Atk.Object o, string documentUrl)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityDocument = documentUrl;
		}

		public static void SetAccessibilityFilename (this Atk.Object o, string filename)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityFilename = filename;
		}

		public static void SetAccessibilityIsMainWindow (this Atk.Object o, bool isMainWindow)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityMain = isMainWindow;
		}

		public static void SetAccessibilityMainWindow (this Atk.Object o, Atk.Object mainWindow)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			var windowAccessible = GetNSAccessibilityElement (mainWindow);
			if (windowAccessible == null) {
				return;
			}

			nsa.AccessibilityMainWindow = (NSObject)windowAccessible;
		}

		public static void SetAccessibilityValue (this Atk.Object o, string stringValue)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityValue = new NSString (stringValue);
		}

		public static void SetAccessibilityURL (this Atk.Object o, string url)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityUrl = new NSUrl (url);
		}

		public static void SetAccessibilityRole (this Atk.Object o, string role, string description = null)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityRole = role;

			if (!string.IsNullOrEmpty (description)) {
				nsa.AccessibilityRoleDescription = description;
			}
		}

		public static void SetAccessibilityRole (this Atk.Object o, AtkCocoa.Roles role, string description = null)
		{
			o.SetAccessibilityRole (role.ToString (), description);
		}

		public static void SetAccessibilitySubRole (this Atk.Object o, string subrole)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilitySubrole = subrole;
		}

		public static void SetAccessibilityTitleUIElement (this Atk.Object o, Atk.Object title)
		{
			var nsa = GetNSAccessibilityElement (o);
			var titleNsa = GetNSAccessibilityElement (title);

			if (nsa == null || titleNsa == null) {
				return;
			}

			nsa.AccessibilityTitleUIElement = (NSObject)titleNsa;
		}

		public static void SetAccessibilityAlternateUIVisible (this Atk.Object o, bool visible)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityAlternateUIVisible = visible;
		}

		public static void SetAccessibilityOrientation (this Atk.Object o, Gtk.Orientation orientation)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityOrientation = orientation == Gtk.Orientation.Vertical ? NSAccessibilityOrientation.Vertical : NSAccessibilityOrientation.Horizontal;
		}

		public static void SetAccessibilityTitleFor (this Atk.Object o, params Atk.Object [] objects)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			NSObject [] titleElements = new NSObject [objects.Length];
			int idx = 0;

			foreach (var obj in objects) {
				var nsao = GetNSAccessibilityElement (obj);
				if (nsao == null) {
					return;
				}

				titleElements [idx] = (NSObject)nsao;
				idx++;
			}

			nsa.AccessibilityServesAsTitleForUIElements = titleElements;
		}

		public static void SetAccessibilityTabs (this Atk.Object o, AccessibilityElementProxy [] tabs)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityTabs = tabs;
		}

		public static void SetAccessibilityTabs (this Atk.Object o, Atk.Object [] tabs)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			NSObject [] realTabs = new NSObject [tabs.Length];
			int i = 0;
			foreach (var tab in tabs) {
				realTabs [i] = (NSObject)GetNSAccessibilityElement (tab);
				i++;
			}

			nsa.AccessibilityTabs = realTabs;
		}

		public static void AccessibilityAddElementToTitle (this Atk.Object title, Atk.Object o)
		{
			var titleNsa = GetNSAccessibilityElement (title);
			var nsa = GetNSAccessibilityElement (o);

			if (nsa == null || titleNsa == null) {
				return;
			}

			NSObject [] oldElements = titleNsa.AccessibilityServesAsTitleForUIElements;
			int length = oldElements != null ? oldElements.Length : 0;

			if (oldElements != null && oldElements.IndexOf ((NSObject)nsa) != -1) {
				return;
			}

			NSObject [] titleElements = new NSObject [length + 1];
			if (oldElements != null) {
				oldElements.CopyTo (titleElements, 0);
			}
			titleElements [length] = (NSObject)nsa;
		}

		public static void AccessibilityRemoveElementFromTitle (this Atk.Object title, Atk.Object o)
		{
			var titleNsa = GetNSAccessibilityElement (title);
			var nsa = GetNSAccessibilityElement (o);

			if (nsa == null || titleNsa == null) {
				return;
			}

			if (titleNsa.AccessibilityServesAsTitleForUIElements == null) {
				return;
			}

			List<NSObject> oldElements = new List<NSObject> (titleNsa.AccessibilityServesAsTitleForUIElements);
			oldElements.Remove ((NSObject)nsa);

			titleNsa.AccessibilityServesAsTitleForUIElements = oldElements.ToArray ();
		}

		public static void AccessibilityReplaceAccessibilityElements (this Atk.Object parent, AccessibilityElementProxy [] children)
		{
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityChildren = children;
		}

		public static void SetAccessibilityColumns (this Atk.Object parent, AccessibilityElementProxy [] columns)
		{
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityColumns = columns;
		}

		public static void SetAccessibilityRows (this Atk.Object parent, AccessibilityElementProxy [] rows)
		{
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityRows = rows;
		}

		public static void SetActionDelegate (this Atk.Object o, ActionDelegate ad)
		{
			ad.Owner = o;
		}

		public static void AddAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
			var nsa = GetNSAccessibilityElement (o) as NSAccessibilityElement;
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityAddChildElement (child);
		}

		public static void RemoveAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			var children = nsa.AccessibilityChildren;

			if (children == null || children.Length == 0) {
				return;
			}

			var idx = children.IndexOf (child);
			if (idx == -1) {
				return;
			}

			var newChildren = new NSObject [children.Length - 1];

			for (int i = 0, j = 0; i < children.Length; i++) {
				if (i == idx) {
					continue;
				}

				newChildren [j] = children [i];
				j++;
			}

			nsa.AccessibilityChildren = newChildren;
		}

		public static void SetAccessibleChildren (this Atk.Object o, AccessibilityElementProxy [] children)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityChildren = children;
		}

		public static void AddAccessibleLinkedUIElement (this Atk.Object o, Atk.Object linked)
		{
			var nsa = GetNSAccessibilityElement (o);
			var linkedNSA = GetNSAccessibilityElement (linked);
			if (nsa == null || linkedNSA == null) {
				return;
			}

			var current = nsa.AccessibilityLinkedUIElements;
			NSObject [] newLinkedElements;
			if (current != null) {
				int length = nsa.AccessibilityLinkedUIElements.Length;
				newLinkedElements = new NSObject [length + 1];
				Array.Copy (nsa.AccessibilityLinkedUIElements, newLinkedElements, length);
				newLinkedElements [length] = (NSObject)linkedNSA;
			} else {
				newLinkedElements = new NSObject[] { (NSObject)linkedNSA };
			}

			nsa.AccessibilityLinkedUIElements = newLinkedElements;
		}
	}

	public class AccessibilityElementProxy : NSAccessibilityElement, INSAccessibility, IAccessibilityElementProxy
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

		protected Gtk.Widget parent;
		INSAccessibility parentElement;
		Rectangle realFrame;

		// The real parent is the Widget that ultimately this object will belong to
		// It is used to convert the frame
		public void SetRealParent (Gtk.Widget realParent)
		{
			parent = realParent;
			parentElement = AtkCocoaMacExtensions.GetNSAccessibilityElement (parent.Accessible);
		}

		// The frame inside the GtkWidget parent, in Gtk coordinate space
		public void SetFrameInRealParent (Rectangle frame)
		{
			realFrame = frame;
		}

		public void AddAccessibleChild (IAccessibilityElementProxy child)
		{
			var realChild = child as NSAccessibilityElement;
			if (realChild == null) {
				throw new Exception ("The child passed into AddAccessibleChild was not an NSAccessibilityElement");
			}

			AccessibilityAddChildElement (realChild);
		}

		public void SetAccessibilityRole (string role, string description = null)
		{
			AccessibilityRole = role;
			if (!string.IsNullOrEmpty (description)) {
				AccessibilityRoleDescription = description;
			}
		}

		public void SetAccessibilityRole (AtkCocoa.Roles role, string description = null)
		{
			SetAccessibilityRole (role.ToString (), description);
		}

		public void SetAccessibilityValue (string value)
		{
			AccessibilityValue = new NSString (value);
		}

		public void SetAccessibilityTitle (string title)
		{
			AccessibilityTitle = title;
		}

		public void SetAccessibilityLabel (string label)
		{
			AccessibilityLabel = label;
		}

		public void SetAccessibilityIdentifier (string identifier)
		{
			AccessibilityIdentifier = identifier;
		}

		public void SetAccessibilityHelp (string help)
		{
			AccessibilityHelp = help;
		}

		public void SetFrameInParent (Rectangle rect)
		{
			AccessibilityFrameInParentSpace = new CGRect (rect.X, rect.Y, rect.Width, rect.Height);
		}

		protected void GetCoordsInWindow (Gtk.Widget widget, out int x, out int y)
		{
			widget.TranslateCoordinates (widget.Toplevel, 0, 0, out x, out y);
		}

		protected void GetCoordsInScreen (Gtk.Widget widget, int windowX, int windowY, out int screenX, out int screenY)
		{
			var gdkWindow = widget.GdkWindow;

			screenX = windowX;
			screenY = windowY;

			if (gdkWindow == null) {
				return;
			}

			CGRect windowRect = new CGRect (windowX, windowY, 0, 0);

			var ptr = gdk_quartz_window_get_nswindow (gdkWindow.Handle);
			if (ptr == IntPtr.Zero) {
				return;
			}
			NSWindow nsWin = Runtime.GetNSObject<NSWindow> (ptr);

			var screenRect = nsWin.ConvertRectToScreen (windowRect);
			screenX = (int)screenRect.X;
			screenY = (int)screenRect.Y;
		}

		protected bool ConvertGtkYCoordToCocoa (Gtk.Widget widget, int gtkY, out int cocoaY)
		{
			var gdkWindow = widget.GdkWindow;

			cocoaY = gtkY;

			if (gdkWindow == null) {
				return false;
			}

			var ptr = gdk_quartz_window_get_nswindow (gdkWindow.Handle);
			if (ptr == IntPtr.Zero) {
				return false;
			}
			NSWindow nsWin = Runtime.GetNSObject<NSWindow> (ptr);

			// Flip the y coords to Cooca origin
			nfloat halfWindowHeight = nsWin.ContentView.Frame.Height / 2;
			nfloat dy = gtkY - halfWindowHeight;

			cocoaY = (int)(halfWindowHeight - dy);
			return true;
		}

		[DllImport ("libgtk-quartz-2.0.dylib")]
		static extern IntPtr gdk_quartz_window_get_nswindow (IntPtr window);

		[Export ("accessibilityHitTest:")]
		public virtual NSObject GetAccessibilityHitTest (CGPoint pointOnScreen)
		{
			var gdkWindow = parent.GdkWindow;

			if (gdkWindow == null) {
				return this;
			}

			var ptr = gdk_quartz_window_get_nswindow (gdkWindow.Handle);
			if (ptr == IntPtr.Zero) {
				return this;
			}
			NSWindow nsWin = Runtime.GetNSObject<NSWindow> (ptr);

			CGRect screenRect = new CGRect (pointOnScreen.X, pointOnScreen.Y, 1, 1);
			CGRect windowRect = nsWin.ConvertRectFromScreen (screenRect);
			CGPoint pointInWindow = new CGPoint (windowRect.X, windowRect.Y);

			// Flip the y coords to Gtk origin
			nfloat halfWindowHeight = nsWin.ContentView.Frame.Height / 2;
			nfloat dy = pointInWindow.Y - halfWindowHeight;

			CGPoint pointInGtkWindow = new CGPoint (pointInWindow.X, halfWindowHeight - dy);
			int parentInWindowX, parentInWindowY;

			GetCoordsInWindow (parent, out parentInWindowX, out parentInWindowY);

			if (AccessibilityChildren == null) {
				return this;
			}

			foreach (var o in AccessibilityChildren) {
				var proxy = o as AccessibilityElementProxy;
				if (proxy == null) {
					throw new Exception ($"Unsupported type {o.GetType ()} inside AccessibilityElementProxy");
				}

				if (proxy.AccessibilityHidden) {
					continue;
				}

				var frameInRealParent = proxy.realFrame;

				if (frameInRealParent.X + parentInWindowX < pointInGtkWindow.X &&
					frameInRealParent.X + parentInWindowX + frameInRealParent.Width >= pointInGtkWindow.X &&
					frameInRealParent.Y + parentInWindowY < pointInGtkWindow.Y &&
					frameInRealParent.Y + parentInWindowY + frameInRealParent.Height >= pointInGtkWindow.Y) {
					return proxy.GetAccessibilityHitTest (pointOnScreen);
				}
			}

			return this;
		}

		[Export ("accessibilityActionNames")]
		public string [] Actions { get; set; }

		[Export ("accessibilityPerformAction:")]
		public void performAction (string actionName)
		{
			switch (actionName) {
			case "AXCancel":
				OnPerformCancel ();
				break;

			case "AXConfirm":
				OnPerformConfirm ();
				break;

			case "AXDecrement":
				OnPerformDecrement ();
				break;

			case "AXDelete":
				OnPerformDelete ();
				break;

			case "AXIncrement":
				OnPerformIncrement ();
				break;

			case "AXPick":
				OnPerformPick ();
				break;

			case "AXPress":
				OnPerformPress ();
				break;

			case "AXRaise":
				OnPerformRaise ();
				break;

			case "AXShowAlternateUI":
				OnPerformShowAlternateUI ();
				break;

			case "AXShowDefaultUI":
				OnPerformShowDefaultUI ();
				break;

			case "AXShowMenu":
				OnPerformShowPopupMenu ();
				break;

			default:
				break;
			}
		}

		public override bool AccessibilityFocused {
			get {
				return parent.HasFocus;
			}
			set {
				parent.HasFocus = value;
			}
		}

		protected bool OnPerformCancel ()
		{
			PerformCancel?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformConfirm ()
		{
			PerformConfirm?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformDecrement ()
		{
			PerformDecrement?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformDelete ()
		{
			PerformDelete?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformIncrement ()
		{
			PerformIncrement?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformPick ()
		{
			PerformPick?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformPress ()
		{
			PerformPress?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformRaise ()
		{
			PerformRaise?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformShowAlternateUI ()
		{
			PerformShowAlternateUI?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformShowDefaultUI ()
		{
			PerformShowDefaultUI?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformShowPopupMenu ()
		{
			PerformShowPopupMenu?.Invoke (this, EventArgs.Empty);
			return true;
		}
	}

	public class AccessibilityElementButtonProxy : AccessibilityElementProxy, INSAccessibilityButton
	{
		public override bool AccessibilityPerformPress ()
		{
			return OnPerformPress ();
		}
	}

	public abstract class AccessibilityElementNavigableStaticTextProxy : AccessibilityElementProxy, INSAccessibilityNavigableStaticText, IAccessibilityNavigableStaticText
	{
		string INSAccessibilityStaticText.AccessibilityValue {
			get {
				return Value;
			}
		}

		public override nint AccessibilityInsertionPointLineNumber {
			get {
				return InsertionPointLineNumber;
			}
		}

		public override nint AccessibilityNumberOfCharacters {
			get {
				return NumberOfCharacters;
			}
		}

		public abstract int NumberOfCharacters { get; }

		public abstract int InsertionPointLineNumber { get; }

		public abstract string Value { get; }

		// Returned frame is in screen coordinate space
		[Export ("accessibilityFrameForRange:")]
		CGRect AccessibilityFrameForRange (NSRange range)
		{
			var realRange = new AtkCocoa.Range { Location = (int)range.Location, Length = (int)range.Length };
			var frame = GetFrameForRange (realRange);

			int parentX, parentY;

			// Gtk is giving the top left corner of the bounding box, but Cocoa needs the bottom left
			int realFrameY = frame.Y + frame.Height;
			GetCoordsInWindow (parent, out parentX, out parentY);
			int cocoaY;
			if (!ConvertGtkYCoordToCocoa (parent, parentY + realFrameY, out cocoaY)) {
				Console.WriteLine ("Error converting coordinate");
			}

			int screenX, screenY;
			GetCoordsInScreen (parent, parentX + frame.X, cocoaY, out screenX, out screenY);

			return new CGRect (screenX, screenY, frame.Width, frame.Height);
		}

		[Export ("accessibilityLineForIndex:")]
		nint AccessibilityLineForIndex (nint index)
		{
			return GetLineForIndex ((int)index);
		}

		[Export ("accessibilityRangeForLine:")]
		NSRange AccessibilityRangeForLine (nint line)
		{
			var range = GetRangeForLine ((int)line);

			return new NSRange (range.Location, range.Length);
		}

		[Export ("accessibilityStringForRange:")]
		string AccessibilityStringForRange (NSRange range)
		{
			var realRange = new AtkCocoa.Range { Location = (int)range.Location, Length = (int)range.Length };
			return GetStringForRange (realRange);
		}

		[Export ("accessibilityRangeForIndex:")]
		NSRange AccessibilityRangeForIndex (nint index)
		{
			var realRange = GetRangeForIndex ((int)index);
			return new NSRange (realRange.Location, realRange.Length);
		}

		[Export ("accessibilityStyleRangeForIndex:")]
		NSRange AccessibililtyStyleRangeForIndex (nint index)
		{
			var realRange = GetStyleRangeForIndex ((int)index);
			return new NSRange (realRange.Location, realRange.Length);
		}

		[Export ("accessibilityRangeForPosition:")]
		NSRange AccessibilityRangeForPosition (CGPoint position)
		{
			var point = new Point ((int)position.X, (int)position.Y);
			var realRange = GetRangeForPosition (point);
			return new NSRange (realRange.Location, realRange.Length);
		}

		public virtual Rectangle GetFrameForRange (AtkCocoa.Range range)
		{
			throw new NotImplementedException ();
		}

		public virtual int GetLineForIndex (int index)
		{
			throw new NotImplementedException ();
		}

		public virtual AtkCocoa.Range GetRangeForLine (int line)
		{
			throw new NotImplementedException ();
		}

		public virtual string GetStringForRange (AtkCocoa.Range range)
		{
			throw new NotImplementedException ();
		}

		public virtual AtkCocoa.Range GetRangeForIndex (int index)
		{
			throw new NotImplementedException ();
		}

		public virtual AtkCocoa.Range GetStyleRangeForIndex (int index)
		{
			throw new NotImplementedException ();
		}

		public virtual AtkCocoa.Range GetRangeForPosition (Point position)
		{
			throw new NotImplementedException ();
		}
	}
}

#endif
