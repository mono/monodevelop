//
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

namespace MonoDevelop.Components.AtkCocoaHelper
{
	// AtkCocoaHelper wraps NSAccessibilityElement to set NSAccessibility properties that aren't supported by Atk
	public static class AtkCocoa
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
			AXCell,
			AXColumn,
			AXGroup,
			AXImage,
			AXMenuButton,
			AXRadioButton,
			AXRow,
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

		public struct Range
		{
			public int Location { get; set; }
			public int Length { get; set; }
		}

#if MAC
		const string XamarinPrivateAtkCocoaNSAccessibilityKey = "xamarin-private-atkcocoa-nsaccessibility";
		internal static INSAccessibility GetNSAccessibilityElement (Atk.Object o)
		{
			IntPtr handle = GtkWorkarounds.GetData (o, XamarinPrivateAtkCocoaNSAccessibilityKey);

			// The object returned could either be an NSAccessibilityElement or it might be an NSObject that implements INSAccessibility
			return Runtime.GetNSObject<NSObject> (handle, false) as INSAccessibility;
		}

		internal static void SetNSAccessibilityElement (Atk.Object o, INativeObject native)
		{
			GtkWorkarounds.SetData (o, XamarinPrivateAtkCocoaNSAccessibilityKey, native.Handle);
		}
#endif

#if MAC
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

		public static void SetAccessibilityDocument (this Atk.Object o, string documentUrl)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityDocument = documentUrl;
#endif
		}

		public static void SetAccessibilityFilename (this Atk.Object o, string filename)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityFilename = filename;
#endif
		}

		public static void SetAccessibilityIsMainWindow (this Atk.Object o, bool isMainWindow)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityMain = isMainWindow;
#endif
		}

		public static void SetAccessibilityMainWindow (this Atk.Object o, Atk.Object mainWindow)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			var windowAccessible = GetNSAccessibilityElement (mainWindow);
			if (windowAccessible == null) {
				return;
			}

			nsa.AccessibilityMainWindow = (NSObject)windowAccessible;
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

			nsa.AccessibilityTitleUIElement = (NSObject)titleNsa;
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

				titleElements [idx] = (NSObject)nsao;
				idx++;
			}

			nsa.AccessibilityServesAsTitleForUIElements = titleElements;
#endif
		}

		public static void SetAccessibilityTabs (this Atk.Object o, AccessibilityElementProxy [] tabs)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityTabs = tabs;
#endif
		}

		public static void SetAccessibilityTabs (this Atk.Object o, Atk.Object [] tabs)
		{
#if MAC
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

			if (oldElements != null && oldElements.IndexOf ((NSObject)nsa) != -1) {
				return;
			}

			NSObject [] titleElements = new NSObject [length + 1];
			if (oldElements != null) {
				oldElements.CopyTo (titleElements, 0);
			}
			titleElements [length] = (NSObject)nsa;
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
			oldElements.Remove ((NSObject)nsa);

			titleNsa.AccessibilityServesAsTitleForUIElements = oldElements.ToArray ();
#endif
		}

		public static void AccessibilityReplaceAccessibilityElements (this Atk.Object parent, AccessibilityElementProxy [] children)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityChildren = children;
#endif
		}

		public static void SetAccessibilityColumns (this Atk.Object parent, AccessibilityElementProxy [] columns)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityColumns = columns;
#endif
		}

		public static void SetAccessibilityRows (this Atk.Object parent, AccessibilityElementProxy [] rows)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityRows = rows;
#endif
		}

		public static void SetActionDelegate (this Atk.Object o, ActionDelegate ad)
		{
			ad.Owner = o;
		}

		public static void AddAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
#if MAC
			var nsa = GetNSAccessibilityElement (o) as NSAccessibilityElement;
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
	}

	public class ActionDelegate
	{
		public AtkCocoa.Actions [] Actions { get; set; }

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

	// On anything other than Mac this is just a dummy class to prevent needing to have #ifdefs all over the main code
	public interface IAccessibilityElementProxy
	{
		event EventHandler PerformCancel;
		event EventHandler PerformConfirm;
		event EventHandler PerformDecrement;
		event EventHandler PerformDelete;
		event EventHandler PerformIncrement;
		event EventHandler PerformPick;
		event EventHandler PerformPress;
		event EventHandler PerformRaise;
		event EventHandler PerformShowAlternateUI;
		event EventHandler PerformShowDefaultUI;
		event EventHandler PerformShowPopupMenu;

		void SetRealParent (Gtk.Widget realParent);
		void SetFrameInRealParent (Gdk.Rectangle frame);
		void AddAccessibleChild (IAccessibilityElementProxy child);
		void SetAccessibilityRole (string role, string description = null);
		void SetAccessibilityRole (AtkCocoa.Roles role, string description = null);
		void SetAccessibilityValue (string value);
		void SetAccessibilityTitle (string title);
		void SetAccessibilityLabel (string label);
		void SetAccessibilityIdentifier (string identifier);
		void SetAccessibilityHelp (string help);
		void SetFrameInParent (Gdk.Rectangle rect);
	}

	public interface IAccessibilityNavigableStaticText
	{
		int NumberOfCharacters { get; }
		int InsertionPointLineNumber { get; }
		string Value { get; }

		// Returns frame in Gtk.Widget parent space.
		Gdk.Rectangle GetFrameForRange (AtkCocoa.Range range);
		int GetLineForIndex (int index);
		AtkCocoa.Range GetRangeForLine (int line);
		string GetStringForRange (AtkCocoa.Range range);
		AtkCocoa.Range GetRangeForIndex (int index);
		AtkCocoa.Range GetStyleRangeForIndex (int index);
		AtkCocoa.Range GetRangeForPosition (Gdk.Point position);
	}

	/*
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
*/
}