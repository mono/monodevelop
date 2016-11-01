﻿//
// AtkCocoaHelper.cs
//
// Author:
//       Iain Holmes <iain@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc
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
using System.Runtime.InteropServices;

#if MAC
using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
#endif

namespace MonoDevelop.Components
{
	// AtkCocoaHelper wraps NSAccessibilityElement to set NSAccessibility properties that aren't supported by Atk
	public static class AtkCocoaHelper
	{
		public enum Actions
		{
			AXCancel,
			AXConfirm,
			AXDecrement,
			AXDelete,
			AXIncrement,
			AXPick,
			AXPress,
			AXRelease,
			AXShowAlternateUI,
			AXShowDefaultUI,
			AXShowMenu
		};

		public enum Roles
		{
			AXButton,
			AXGroup,
			AXImage,
			AXRadioButton,
			AXRuler,
			AXSplitGroup,
			AXSplitter,
			AXStaticText,
			AXTabGroup,
			AXTextArea
		};

		public enum SubRoles
		{
			AXCloseButton,
		};

#if MAC
		const string XamarinPrivateAtkCocoaNSAccessibilityKey = "xamarin-private-atkcocoa-nsaccessibility";
		internal static NSAccessibilityElement GetNSAccessibilityElement (Atk.Object o)
		{
			IntPtr handle = GtkWorkarounds.GetData (o, XamarinPrivateAtkCocoaNSAccessibilityKey);

			return Runtime.GetNSObject<NSAccessibilityElement> (handle, false);
		}

		internal static void SetNSAccessibilityElement (Atk.Object o, INativeObject native)
		{
			GtkWorkarounds.SetData (o, XamarinPrivateAtkCocoaNSAccessibilityKey, native.Handle);
		}
#endif

		public static void SetAccessibilityLabel (this Atk.Object o, string label)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityLabel = label;
#endif
		}

		public static void SetAccessibilityShouldIgnore (this Atk.Object o, bool ignore)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityElement = !ignore;
#endif
		}

		public static void SetAccessibilityTitle (this Atk.Object o, string title)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityTitle = title;
#endif
		}

		public static void SetAccessibilityValue (this Atk.Object o, string stringValue)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityValue = new NSString (stringValue);
#endif
		}

		public static void SetAccessibilityURL (this Atk.Object o, string url)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityUrl = new NSUrl (url);
#endif
		}

		public static void SetAccessibilityRole (this Atk.Object o, string role, string description = null)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityRole = role;

			if (!string.IsNullOrEmpty (description)) {
				nsa.AccessibilityRoleDescription = description;
			}
#endif
		}

		public static void SetAccessibilityRole (this Atk.Object o, Roles role, string description = null)
		{
			o.SetAccessibilityRole (role.ToString (), description);
		}

		public static void SetAccessibilitySubRole (this Atk.Object o, string subrole)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilitySubrole = subrole;
#endif
		}

		public static void SetAccessibilityTitleUIElement (this Atk.Object o, Atk.Object title)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			var titleNsa = GetNSAccessibilityElement (title);

			if (nsa == null || titleNsa == null) {
				return;
			}

			nsa.AccessibilityTitleUIElement = titleNsa;
#endif
		}

		public static void SetAccessibilityAlternateUIVisible (this Atk.Object o, bool visible)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityAlternateUIVisible = visible;
#endif
		}

		public static void SetAccessibilityOrientation (this Atk.Object o, Gtk.Orientation orientation)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityOrientation = orientation == Gtk.Orientation.Vertical ? NSAccessibilityOrientation.Vertical : NSAccessibilityOrientation.Horizontal;
#endif

		}

		public static void SetAccessibilityTitleFor (this Atk.Object o, params Atk.Object [] objects)
		{
#if MAC
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

				titleElements [idx] = nsao;
				idx++;
			}

			nsa.AccessibilityServesAsTitleForUIElements = titleElements;
#endif
		}

		public static void AccessibilityAddElementToTitle (this Atk.Object title, Atk.Object o)
		{
#if MAC
			var titleNsa = GetNSAccessibilityElement (title);
			var nsa = GetNSAccessibilityElement (o);

			if (nsa == null || titleNsa == null) {
				return;
			}

			NSObject [] oldElements = titleNsa.AccessibilityServesAsTitleForUIElements;
			int length = oldElements != null ? oldElements.Length : 0;

			if (oldElements != null && oldElements.IndexOf (nsa) != -1) {
				return;
			}

			NSObject [] titleElements = new NSObject [length + 1];
			if (oldElements != null) {
				oldElements.CopyTo (titleElements, 0);
			}
			titleElements [length] = nsa;
#endif
		}

		public static void AccessibilityRemoveElementFromTitle (this Atk.Object title, Atk.Object o)
		{
#if MAC
			var titleNsa = GetNSAccessibilityElement (title);
			var nsa = GetNSAccessibilityElement (o);

			if (nsa == null || titleNsa == null) {
				return;
			}

			if (titleNsa.AccessibilityServesAsTitleForUIElements == null) {
				return;
			}

			List<NSObject> oldElements = new List<NSObject> (titleNsa.AccessibilityServesAsTitleForUIElements);
			oldElements.Remove (nsa);

			titleNsa.AccessibilityServesAsTitleForUIElements = oldElements.ToArray ();
#endif
		}

		public class ActionDelegate
		{
			public Actions [] Actions { get; set; }

			Atk.Object owner;
			internal Atk.Object Owner {
				set {
					owner = value;

					if (owner.GetType () == typeof (Atk.NoOpObject)) {
						return;
					}

					var signal = GLib.Signal.Lookup (owner, "request-actions", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (RequestActionsHandler));

					signal = GLib.Signal.Lookup (owner, "perform-cancel", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformCancelHandler));
					signal = GLib.Signal.Lookup (owner, "perform-confirm", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformConfirmHandler));
					signal = GLib.Signal.Lookup (owner, "perform-decrement", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformDecrementHandler));
					signal = GLib.Signal.Lookup (owner, "perform-delete", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformDeleteHandler));
					signal = GLib.Signal.Lookup (owner, "perform-increment", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformIncrementHandler));
					signal = GLib.Signal.Lookup (owner, "perform-pick", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformPickHandler));
					signal = GLib.Signal.Lookup (owner, "perform-press", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformPressHandler));
					signal = GLib.Signal.Lookup (owner, "perform-raise", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformRaiseHandler));
					signal = GLib.Signal.Lookup (owner, "perform-show-alternate-ui", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformShowAlternateUIHandler));
					signal = GLib.Signal.Lookup (owner, "perform-show-default-ui", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformShowDefaultUIHandler));
					signal = GLib.Signal.Lookup (owner, "perform-show-menu", typeof (GLib.SignalArgs));
					signal.AddDelegate (new EventHandler<GLib.SignalArgs> (PerformShowMenuHandler));
				}
			}

			void RequestActionsHandler (object sender, GLib.SignalArgs args)
			{
				// +1 so we can add a NULL to terminate the array
				int actionCount = Actions.Length + 1;
				IntPtr intPtr = Marshal.AllocHGlobal (actionCount * Marshal.SizeOf<IntPtr> ());
				IntPtr [] actions = new IntPtr [actionCount];

				int i = 0;
				foreach (var action in Actions) {
					actions [i] = Marshal.StringToHGlobalAnsi (action.ToString ());
					i++;
				}

				// Terminator
				actions [i] = IntPtr.Zero;

				Marshal.Copy (actions, 0, intPtr, actionCount);

				args.RetVal = intPtr;
			}

			void PerformCancelHandler (object sender, GLib.SignalArgs args)
			{
				PerformCancel?.Invoke (this, args);
			}

			void PerformConfirmHandler (object sender, GLib.SignalArgs args)
			{
				PerformConfirm?.Invoke (this, args);
			}

			void PerformDecrementHandler (object sender, GLib.SignalArgs args)
			{
				PerformDecrement?.Invoke (this, args);
			}

			void PerformDeleteHandler (object sender, GLib.SignalArgs args)
			{
				PerformDelete?.Invoke (this, args);
			}

			void PerformIncrementHandler (object sender, GLib.SignalArgs args)
			{
				PerformIncrement?.Invoke (this, args);
			}

			void PerformPickHandler (object sender, GLib.SignalArgs args)
			{
				PerformPick?.Invoke (this, args);
			}

			void PerformPressHandler (object sender, GLib.SignalArgs args)
			{
				PerformPress?.Invoke (this, args);
			}

			void PerformRaiseHandler (object sender, GLib.SignalArgs args)
			{
				PerformRaise?.Invoke (this, args);
			}

			void PerformShowAlternateUIHandler (object sender, GLib.SignalArgs args)
			{
				PerformShowAlternateUI?.Invoke (this, args);
			}

			void PerformShowDefaultUIHandler (object sender, GLib.SignalArgs args)
			{
				PerformShowDefaultUI?.Invoke (this, args);
			}

			void PerformShowMenuHandler (object sender, GLib.SignalArgs args)
			{
				PerformShowMenu?.Invoke (this, args);
			}

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
			public event EventHandler PerformShowMenu;
		}

		public static void SetActionDelegate (this Atk.Object o, ActionDelegate ad)
		{
			ad.Owner = o;
		}

		public static void AddAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityAddChildElement (child);
#endif
		}

		public static void RemoveAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
#if MAC
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
#endif
		}

		// On anything other than Mac this is just a dummy class to prevent needing to have #ifdefs all over the main code
#if MAC
		public class AccessibilityElementProxy : NSAccessibilityElement, INSAccessibility
#else
		public class AccessibilityElementProxy
#endif
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

#if MAC
			protected Gtk.Widget parent;
			NSAccessibilityElement parentElement;
			Gdk.Rectangle realFrame;
#endif

			// The real parent is the Widget that ultimately this object will belong to
			// It is used to convert the frame
			public void SetRealParent (Gtk.Widget realParent)
			{
#if MAC
				parent = realParent;
				parentElement = GetNSAccessibilityElement (parent.Accessible);
#endif
			}

			// The frame inside the GtkWidget parent, in Gtk coordinate space
			public void SetFrameInRealParent (Gdk.Rectangle frame)
			{
#if MAC
				realFrame = frame;
#endif
			}

			public void AddAccessibleChild (AccessibilityElementProxy child)
			{
#if MAC
				AccessibilityAddChildElement (child);
#endif
			}

			public void SetAccessibilityRole (string role, string description = null)
			{
#if MAC
				AccessibilityRole = role;
				if (!string.IsNullOrEmpty (description)) {
					AccessibilityRoleDescription = description;
				}
#endif
			}

			public void SetAccessibilityRole (AtkCocoaHelper.Roles role, string description = null)
			{
#if MAC
				SetAccessibilityRole (role.ToString (), description);
#endif
			}

			public void SetAccessibilityTitle (string title)
			{
#if MAC
				AccessibilityTitle = title;
#endif
			}

			public void SetAccessibilityLabel (string label)
			{
#if MAC
				AccessibilityLabel = label;
#endif
			}

			public void SetAccessibilityIdentifier (string identifier)
			{
#if MAC
				AccessibilityIdentifier = identifier;
#endif
			}

			public void SetAccessibilityHelp (string help)
			{
#if MAC
				AccessibilityHelp = help;
#endif
			}

			public void SetFrameInParent (Gdk.Rectangle rect)
			{
#if MAC
				AccessibilityFrameInParentSpace = new CoreGraphics.CGRect (rect.X, rect.Y, rect.Width, rect.Height);
#endif
			}

#if MAC
			protected void GetCoordsInWindow (Gtk.Widget widget, out int x, out int y)
			{
				widget.TranslateCoordinates (widget.Toplevel, 0, 0, out x, out y);
			}

			protected void GetCoordsInScreen (Gtk.Widget widget, int windowX, int windowY, out int screenX, out int screenY)
			{
				Gdk.Window gdkWindow = widget.GdkWindow;

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
				Gdk.Window gdkWindow = widget.GdkWindow;

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
			public virtual NSObject GetAccessibilityHitTest (CoreGraphics.CGPoint pointOnScreen)
			{
				Gdk.Window gdkWindow = parent.GdkWindow;

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

					Gdk.Rectangle frameInRealParent = proxy.realFrame;

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
#endif
		}

#if MAC
		public class AccessibilityElementButtonProxy : AccessibilityElementProxy, INSAccessibilityButton
#else
		public class AccessibilityElementButtonProxy
#endif
		{
#if MAC
			public override bool AccessibilityPerformPress ()
			{
				return OnPerformPress ();
			}
#endif
		}

		public struct Range
		{
			public int Location { get; set; }
			public int Length { get; set; }
		}

#if MAC
		public abstract class AccessibilityElementNavigableStaticTextProxy : AccessibilityElementProxy, INSAccessibilityNavigableStaticText
#else
		public abstract class AccessibilityElementNavigableStaticTextProxy
#endif
		{
#if MAC
			string INSAccessibilityStaticText.AccessibilityValue {
				get {
					return Value;
				}
			}

			[Export ("setAccessibilityValue:")]
			public void SetAccessibilityValue (string value)
			{
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

			// Returned frame is in screen coordinate space
			[Export ("accessibilityFrameForRange:")]
			CGRect AccessibilityFrameForRange (NSRange range)
			{
				var realRange = new Range { Location = (int) range.Location, Length = (int) range.Length };
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
				var realRange = new Range { Location = (int)range.Location, Length = (int)range.Length };
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
				Gdk.Point point = new Gdk.Point ((int)position.X, (int)position.Y);
				var realRange = GetRangeForPosition (point);
				return new NSRange (realRange.Location, realRange.Length);
			}
#endif

			protected abstract int NumberOfCharacters { get; }
			protected abstract int InsertionPointLineNumber { get; }
			protected abstract string Value { get; }

			// Returns frame in Gtk.Widget parent space.
			protected abstract Gdk.Rectangle GetFrameForRange (Range range);
			protected abstract int GetLineForIndex (int index);
			protected abstract Range GetRangeForLine (int line);
			protected abstract string GetStringForRange (Range range);
			protected abstract Range GetRangeForIndex (int index);
			protected abstract Range GetStyleRangeForIndex (int index);
			protected abstract Range GetRangeForPosition (Gdk.Point position);
		}

		public abstract class AtkCellRendererProxy : Atk.Object
		{
			public AccessibilityElementProxy Accessible { get; private set; }
			protected AtkCellRendererProxy ()
			{
				Accessible = new AccessibilityElementProxy ();

				// Set the element as secret data on the Atk.Object so AtkCocoa can do something with it
				AtkCocoaHelper.SetNSAccessibilityElement (this, Accessible);
			}
		}
	}
}