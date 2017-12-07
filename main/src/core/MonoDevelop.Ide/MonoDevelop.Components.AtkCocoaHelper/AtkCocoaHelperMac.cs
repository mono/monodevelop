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
using System.Linq;
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

		public static void SetLabel (this Atk.Object o, string label)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityLabel = label;
		}

		public static void SetHidden (this Atk.Object o, bool hidden)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityHidden = hidden;
		}

		public static void SetLabel (this Gtk.CellRenderer r, string label)
		{
			var nsa = GetNSAccessibilityElement (r);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityLabel = label;
		}

		public static void SetDescription (this Gtk.CellRenderer r, string description)
		{
			var nsa = GetNSAccessibilityElement (r);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityHelp = description;
		}

		public static void SetShouldIgnore (this Atk.Object o, bool ignore)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityElement = !ignore;
		}

		public static void SetTitle (this Atk.Object o, string title)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityTitle = title;
		}

		public static void SetDocument (this Atk.Object o, string documentUrl)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityDocument = documentUrl;
		}

		public static void SetFilename (this Atk.Object o, string filename)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityFilename = filename;
		}

		public static void SetIsMainWindow (this Atk.Object o, bool isMainWindow)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityMain = isMainWindow;
		}

		public static void SetMainWindow (this Atk.Object o, Atk.Object mainWindow)
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

		public static void SetValue (this Atk.Object o, string stringValue)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityValue = new NSString (stringValue);
		}

		public static void SetUrl (this Atk.Object o, string url)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityUrl = new NSUrl (url);
		}

		public static void SetRole (this Atk.Object o, string role, string description = null)
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

		public static void SetRole (this Atk.Object o, AtkCocoa.Roles role, string description = null)
		{
			o.SetRole (role.ToString (), description);
		}

		public static void SetSubRole (this Atk.Object o, string subrole)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilitySubrole = subrole;
		}

		public static void SetTitleUIElement (this Atk.Object o, Atk.Object title)
		{
			var nsa = GetNSAccessibilityElement (o);
			var titleNsa = GetNSAccessibilityElement (title);

			if (nsa == null || titleNsa == null) {
				return;
			}

			nsa.AccessibilityTitleUIElement = (NSObject)titleNsa;
		}

		public static void SetAlternateUIVisible (this Atk.Object o, bool visible)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityAlternateUIVisible = visible;
		}

		public static void SetOrientation (this Atk.Object o, Gtk.Orientation orientation)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityOrientation = orientation == Gtk.Orientation.Vertical ? NSAccessibilityOrientation.Vertical : NSAccessibilityOrientation.Horizontal;
		}

		public static void SetTitleFor (this Atk.Object o, params Atk.Object [] objects)
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

		public static void SetTabs (this Atk.Object o, params AccessibilityElementProxy [] tabs)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			nsa.AccessibilityTabs = ConvertToRealProxyArray (tabs);
		}

		public static void SetTabs (this Atk.Object o, params Atk.Object [] tabs)
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

		public static void AddElementToTitle (this Atk.Object title, Atk.Object o)
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

			titleNsa.AccessibilityServesAsTitleForUIElements = titleElements;
		}

		public static void RemoveElementFromTitle (this Atk.Object title, Atk.Object o)
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

		static RealAccessibilityElementProxy [] ConvertToRealProxyArray (AccessibilityElementProxy [] proxies)
		{
			if (proxies == null) {
				return null;
			}

			var realProxies = new RealAccessibilityElementProxy [proxies.Length];
			int idx = 0;
			foreach (var p in proxies) {
				var rp = p.Proxy as RealAccessibilityElementProxy;
				if (rp == null) {
					throw new Exception ($"Invalid type {p.GetType ()} in accessibleChildren");
				}

				realProxies [idx] = rp;
				idx++;
			}

			return realProxies;
		}

		public static void ReplaceAccessibilityElements (this Atk.Object parent, AccessibilityElementProxy [] children)
		{
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityChildren = ConvertToRealProxyArray (children);
		}

		public static void SetColumns (this Atk.Object parent, AccessibilityElementProxy [] columns)
		{
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityColumns = ConvertToRealProxyArray (columns);
		}

		public static void SetRows (this Atk.Object parent, AccessibilityElementProxy [] rows)
		{
			var nsa = GetNSAccessibilityElement (parent);

			if (nsa == null) {
				return;
			}

			nsa.AccessibilityRows = ConvertToRealProxyArray (rows);
		}

		public static void AddAccessibleElement (this Atk.Object o, AccessibilityElementProxy child)
		{
			var nsa = GetNSAccessibilityElement (o) as NSAccessibilityElement;
			if (nsa == null) {
				return;
			}

			var p = child.Proxy as RealAccessibilityElementProxy;
			if (p == null) {
				throw new Exception ($"Invalid proxy child type {p.GetType ()}");
			}
			nsa.AccessibilityAddChildElement (p);
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

			var p = child.Proxy as RealAccessibilityElementProxy;
			if (p == null) {
				throw new Exception ($"Invalid proxy child type {p.GetType ()}");
			}

			var idx = children.IndexOf (p);
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

			nsa.AccessibilityChildren = ConvertToRealProxyArray (children);
		}

		public static void AddLinkedUIElement (this Atk.Object o, Atk.Object linked)
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
				newLinkedElements = new NSObject [] { (NSObject)linkedNSA };
			}

			nsa.AccessibilityLinkedUIElements = newLinkedElements;
		}

		public static void AddLinkedUIElement (this Atk.Object o, params Atk.Object [] linked)
		{
			var nsa = GetNSAccessibilityElement (o);
			if (nsa == null) {
				return;
			}

			var current = nsa.AccessibilityLinkedUIElements;

			int length = current != null ? current.Length : 0;
			var newLinkedElements = new NSObject [length + linked.Length];

			if (current != null) {
				Array.Copy (current, newLinkedElements, length);
			}

			int idx = length;
			foreach (var e in linked) {
				var nsaLinked = GetNSAccessibilityElement (e);
				newLinkedElements [idx] = (NSObject)nsaLinked;
				idx++;
			}

			nsa.AccessibilityLinkedUIElements = newLinkedElements;
		}

		public static void MakeAccessibilityAnnouncement (this Atk.Object o,  string message)
		{
			if (o == null)
				return;
			var nsObject = GetNSAccessibilityElement (o) as NSObject;
			if (nsObject == null)
				return;
			var dictionary =
				new NSDictionary (NSAccessibilityNotificationUserInfoKeys.AnnouncementKey, new NSString (message),
								  NSAccessibilityNotificationUserInfoKeys.PriorityKey, NSAccessibilityPriorityLevel.High);
			NSAccessibility.PostNotification (nsObject, NSAccessibilityNotifications.AnnouncementRequestedNotification, dictionary);
		}
	}

	public class AccessibilityElementProxy : IAccessibilityElementProxy
	{
		RealAccessibilityElementProxy realProxyElement;
		internal object Proxy {
			get {
				return realProxyElement;
			}
			private set {
				realProxyElement = value as RealAccessibilityElementProxy;
			}
		}

		public AccessibilityElementProxy () : this (new RealAccessibilityElementProxy ())
		{
		}

		AccessibilityElementProxy (NSAccessibilityElement realProxy)
		{
			Proxy = realProxy;
		}

		public static AccessibilityElementProxy ButtonElementProxy ()
		{
			return new AccessibilityElementProxy (new RealAccessibilityElementButtonProxy ());
		}

		public static AccessibilityElementProxy TextElementProxy ()
		{
			return new AccessibilityElementProxy (new RealAccessibilityElementNavigableStaticTextProxy ());
		}

		public string Identifier {
			get {
				return realProxyElement.AccessibilityIdentifier;
			}
			set {
				realProxyElement.AccessibilityIdentifier = value;
			}
		}

		public string Help {
			get {
				return realProxyElement.AccessibilityHelp;
			}
			set {
				realProxyElement.AccessibilityHelp = value;
			}
		}

		public string Label {
			get {
				return realProxyElement.AccessibilityLabel;
			}
			set {
				realProxyElement.AccessibilityLabel = value;
			}
		}

		public string Title {
			get {
				return realProxyElement.AccessibilityTitle;
			}
			set {
				realProxyElement.AccessibilityTitle = value;
			}
		}

		public string Value {
			get {
				return (NSString)realProxyElement.AccessibilityValue;
			}
			set {
				realProxyElement.AccessibilityValue = new NSString (value);
			}
		}

		public bool Hidden {
			get {
				return realProxyElement.AccessibilityHidden;
			}
			set {
				realProxyElement.AccessibilityHidden = value;
			}
		}

		public Gtk.Widget GtkParent {
			get {
				return realProxyElement.GtkParent;
			}
			set {
				realProxyElement.GtkParent = value;
			}
		}

		public Gdk.Rectangle FrameInGtkParent {
			get {
				return realProxyElement.FrameInGtkParent;
			}
			set {
				realProxyElement.FrameInGtkParent = value;
			}
		}

		public Gdk.Rectangle FrameInParent {
			get {
				return realProxyElement.FrameInParent;
			}
			set {
				realProxyElement.FrameInParent = value;
			}
		}

		public void AddAccessibleChild (IAccessibilityElementProxy child)
		{
			var proxy = (AccessibilityElementProxy)child;
			var realChild = proxy.realProxyElement;

			realProxyElement.AccessibilityAddChildElement (realChild);
		}

		public void SetRole (string role, string description = null)
		{
			realProxyElement.SetRole (role, description);
		}

		public void SetRole (AtkCocoa.Roles role, string description = null)
		{
			realProxyElement.SetRole (role, description);
		}

		public event EventHandler PerformCancel {
			add {
				realProxyElement.PerformCancel += value;
			}
			remove {
				realProxyElement.PerformCancel -= value;
			}
		}
		public event EventHandler PerformConfirm {
			add {
				realProxyElement.PerformConfirm += value;
			}
			remove {
				realProxyElement.PerformConfirm -= value;
			}
		}
		public event EventHandler PerformDecrement {
			add {
				realProxyElement.PerformDecrement += value;
			}
			remove {
				realProxyElement.PerformDecrement -= value;
			}
		}
		public event EventHandler PerformDelete {
			add {
				realProxyElement.PerformDelete += value;
			}
			remove {
				realProxyElement.PerformDelete -= value;
			}
		}
		public event EventHandler PerformIncrement {
			add {
				realProxyElement.PerformIncrement += value;
			}
			remove {
				realProxyElement.PerformIncrement -= value;
			}
		}
		public event EventHandler PerformPick {
			add {
				realProxyElement.PerformPick += value;
			}
			remove {
				realProxyElement.PerformPick -= value;
			}
		}
		public event EventHandler PerformPress {
			add {
				realProxyElement.PerformPress += value;
			}
			remove {
				realProxyElement.PerformPress -= value;
			}
		}
		public event EventHandler PerformRaise {
			add {
				realProxyElement.PerformRaise += value;
			}
			remove {
				realProxyElement.PerformRaise -= value;
			}
		}
		public event EventHandler PerformShowAlternateUI {
			add {
				realProxyElement.PerformShowAlternateUI += value;
			}
			remove {
				realProxyElement.PerformShowAlternateUI -= value;
			}
		}
		public event EventHandler PerformShowDefaultUI {
			add {
				realProxyElement.PerformShowDefaultUI += value;
			}
			remove {
				realProxyElement.PerformShowDefaultUI -= value;
			}
		}
		public event EventHandler PerformShowMenu {
			add {
				realProxyElement.PerformShowMenu += value;
			}
			remove {
				realProxyElement.PerformShowMenu -= value;
			}
		}

		// For Navigable Text elements
		public Func<string> Contents {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.Contents = value;
			}
		}
		public Func<int> NumberOfCharacters {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.NumberOfCharacters = value;
			}
		}
		public Func<int> InsertionPointLineNumber {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.InsertionPointLineNumber = value;
			}
		}
		public Func<AtkCocoa.Range, Rectangle> FrameForRange {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetFrameForRange = value;
			}
		}
		public Func<int, int> LineForIndex {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetLineForIndex = value;
			}
		}
		public Func<int, AtkCocoa.Range> RangeForLine {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetRangeForLine = value;
			}
		}
		public Func<AtkCocoa.Range, string> StringForRange {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetStringForRange = value;
			}
		}
		public Func<int, AtkCocoa.Range> RangeForIndex {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetRangeForIndex = value;
			}
		}
		public Func<int, AtkCocoa.Range> StyleRangeForIndex {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetStyleRangeForIndex = value;
			}
		}
		public Func<Point, AtkCocoa.Range> RangeForPosition {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetRangeForPosition = value;
			}
		}
		public Func<AtkCocoa.Range> GetVisibleCharacterRange {
			set {
				var p = realProxyElement as RealAccessibilityElementNavigableStaticTextProxy;
				if (p == null) {
					throw new Exception ("Not a Text element");
				}

				p.GetVisibleCharacterRange = value;
			}
		}

		public int Index {
			get {
				var p = realProxyElement;
				if (p == null) {
					throw new Exception ("Not proxy element");
				}

				return (int) p.AccessibilityIndex;
			}

			set {
				var p = realProxyElement;
				if (p == null) {
					throw new Exception ("Not a proxy element");
				}

				p.AccessibilityIndex = value;
			}
		}
	}

	class RealAccessibilityElementProxy : NSAccessibilityElement, INSAccessibility
	{
		event EventHandler performCancel;
		public event EventHandler PerformCancel {
			add {
				if (performCancel == null) {
					AddAction (AtkCocoa.Actions.AXCancel);
				}
				performCancel += value;
			}
			remove {
				performCancel -= value;
				if (performCancel == null) {
					RemoveAction (AtkCocoa.Actions.AXCancel);
				}
			}
		}

		event EventHandler performConfirm;
		public event EventHandler PerformConfirm {
			add {
				if (performConfirm == null) {
					AddAction (AtkCocoa.Actions.AXConfirm);
				}
				performConfirm += value;
			}
			remove {
				performConfirm -= value;
				if (performConfirm == null) {
					RemoveAction (AtkCocoa.Actions.AXConfirm);
				}
			}
		}
		event EventHandler performDecrement;
		public event EventHandler PerformDecrement {
			add {
				if (performDecrement == null) {
					AddAction (AtkCocoa.Actions.AXDecrement);
				}
				performDecrement += value;
			}
			remove {
				performDecrement -= value;
				if (performDecrement == null) {
					RemoveAction (AtkCocoa.Actions.AXDecrement);
				}
			}
		}
		event EventHandler performDelete;
		public event EventHandler PerformDelete {
			add {
				if (performDelete == null) {
					AddAction (AtkCocoa.Actions.AXDelete);
				}
				performDelete += value;
			}
			remove {
				performDelete -= value;
				if (performDelete == null) {
					RemoveAction (AtkCocoa.Actions.AXDelete);
				}
			}
		}
		event EventHandler performIncrement;
		public event EventHandler PerformIncrement {
			add {
				if (performIncrement == null) {
					AddAction (AtkCocoa.Actions.AXIncrement);
				}
				performIncrement += value;
			}
			remove {
				performIncrement -= value;
				if (performIncrement == null) {
					RemoveAction (AtkCocoa.Actions.AXIncrement);
				}
			}
		}
		event EventHandler performPick;
		public event EventHandler PerformPick {
			add {
				if (performPick == null) {
					AddAction (AtkCocoa.Actions.AXPick);
				}
				performPick += value;
			}
			remove {
				performPick -= value;
				if (performPick == null) {
					RemoveAction (AtkCocoa.Actions.AXPick);
				}
			}
		}
		event EventHandler performPress;
		public event EventHandler PerformPress {
			add {
				if (performPress == null) {
					AddAction (AtkCocoa.Actions.AXPress);
				}
				performPress += value;
			}
			remove {
				performPress -= value;
				if (performPress == null) {
					RemoveAction (AtkCocoa.Actions.AXPress);
				}
			}
		}
		event EventHandler performRaise;
		public event EventHandler PerformRaise {
			add {
				if (performRaise == null) {
					AddAction (AtkCocoa.Actions.AXRaise);
				}
				performRaise += value;
			}
			remove {
				performRaise -= value;
				if (performRaise == null) {
					RemoveAction (AtkCocoa.Actions.AXRaise);
				}
			}
		}
		event EventHandler performShowAlternateUI;
		public event EventHandler PerformShowAlternateUI {
			add {
				if (performShowAlternateUI == null) {
					AddAction (AtkCocoa.Actions.AXShowAlternateUI);
				}
				performShowAlternateUI += value;
			}
			remove {
				performShowAlternateUI -= value;
				if (performShowAlternateUI == null) {
					RemoveAction (AtkCocoa.Actions.AXShowAlternateUI);
				}
			}
		}
		event EventHandler performShowDefaultUI;
		public event EventHandler PerformShowDefaultUI {
			add {
				if (performShowDefaultUI == null) {
					AddAction (AtkCocoa.Actions.AXShowDefaultUI);
				}
				performShowDefaultUI += value;
			}
			remove {
				performShowDefaultUI -= value;
				if (performShowDefaultUI == null) {
					RemoveAction (AtkCocoa.Actions.AXShowDefaultUI);
				}
			}
		}
		event EventHandler performShowMenu;
		public event EventHandler PerformShowMenu {
			add {
				if (performShowMenu == null) {
					AddAction (AtkCocoa.Actions.AXShowMenu);
				}
				performShowMenu += value;
			}
			remove {
				performShowMenu -= value;
				if (performShowMenu == null) {
					RemoveAction (AtkCocoa.Actions.AXShowMenu);
				}
			}
		}

		protected WeakReference<Gtk.Widget> parentRef = new WeakReference<Gtk.Widget> (null);
		Rectangle realFrame;

		void UpdateActions ()
		{
			actions = realActions.Select (arg => arg.ToString ()).ToArray ();
		}

		void AddAction (AtkCocoa.Actions action)
		{
			realActions.Add (action);
			UpdateActions ();
		}

		void RemoveAction (AtkCocoa.Actions action)
		{
			realActions.Remove (action);
			UpdateActions ();
		}

		// The real parent is the Widget that ultimately this object will belong to
		// It is used to convert the frame
		public Gtk.Widget GtkParent {
			get {
				parentRef.TryGetTarget (out var parent);
				return parent;
			}
			set {
				parentRef.SetTarget (value);
			}
		}

		// The frame inside the GtkWidget parent, in Gtk coordinate space
		public Rectangle FrameInGtkParent {
			get {
				return realFrame;
			}
			set {
				realFrame = value;
			}
		}

		public void AddAccessibleChild (IAccessibilityElementProxy child)
		{
			var realChild = child as NSAccessibilityElement;
			if (realChild == null) {
				throw new Exception ("The child passed into AddAccessibleChild was not an NSAccessibilityElement");
			}

			AccessibilityAddChildElement (realChild);
		}

		public void SetRole (string role, string description = null)
		{
			AccessibilityRole = role;
			if (!string.IsNullOrEmpty (description)) {
				AccessibilityRoleDescription = description;
			}
		}

		public void SetRole (AtkCocoa.Roles role, string description = null)
		{
			SetRole (role.ToString (), description);
		}

		public void SetValue (string value)
		{
			AccessibilityValue = new NSString (value);
		}

		public void SetTitle (string title)
		{
			AccessibilityTitle = title;
		}

		public void SetLabel (string label)
		{
			AccessibilityLabel = label;
		}

		public void SetIdentifier (string identifier)
		{
			AccessibilityIdentifier = identifier;
		}

		public void SetHelp (string help)
		{
			AccessibilityHelp = help;
		}

		public void SetHidden (bool hidden)
		{
			AccessibilityHidden = hidden;
		}

		// The frame in the parent in Cocoa space.
		// FIXME: Can this be calculated when setting GtkFrame?
		Rectangle frameInParent;
		public Rectangle FrameInParent {
			get {
				return frameInParent;
			}
			set {
				frameInParent = value;
				AccessibilityFrameInParentSpace = new CGRect (value.X, value.Y, value.Width, value.Height);
			}
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
			parentRef.TryGetTarget (out var parent);
			if (parent == null) {
				return null;
			}

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
				var proxy = o as RealAccessibilityElementProxy;
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

		HashSet<AtkCocoa.Actions> realActions = new HashSet<AtkCocoa.Actions> ();
		string [] actions;
		[Export ("accessibilityActionNames")]
		public string [] Actions {
			get {
				return actions;
			}
		}

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
				parentRef.TryGetTarget (out var parent);
				if (parent == null) {
					return false;
				}
				return parent.HasFocus;
			}
			set {
				parentRef.TryGetTarget (out var parent);
				if (parent == null) {
					return;
				}
				parent.HasFocus = value;
			}
		}

		protected bool OnPerformCancel ()
		{
			performCancel?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformConfirm ()
		{
			performConfirm?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformDecrement ()
		{
			performDecrement?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformDelete ()
		{
			performDelete?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformIncrement ()
		{
			performIncrement?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformPick ()
		{
			performPick?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformPress ()
		{
			performPress?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformRaise ()
		{
			performRaise?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformShowAlternateUI ()
		{
			performShowAlternateUI?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformShowDefaultUI ()
		{
			performShowDefaultUI?.Invoke (this, EventArgs.Empty);
			return true;
		}

		protected bool OnPerformShowPopupMenu ()
		{
			performShowMenu?.Invoke (this, EventArgs.Empty);
			return true;
		}
	}

	class RealAccessibilityElementButtonProxy : RealAccessibilityElementProxy, INSAccessibilityButton
	{
		public override bool AccessibilityPerformPress ()
		{
			return OnPerformPress ();
		}
	}

	class RealAccessibilityElementNavigableStaticTextProxy : RealAccessibilityElementProxy, INSAccessibilityNavigableStaticText
	{
		string INSAccessibilityStaticText.AccessibilityValue {
			get {
				if (Contents == null) {
					return base.AccessibilityValue as NSString;
				}

				return Contents ();
			}
		}

		public override nint AccessibilityInsertionPointLineNumber {
			get {
				return InsertionPointLineNumber ();
			}
		}

		public override nint AccessibilityNumberOfCharacters {
			get {
				return NumberOfCharacters ();
			}
		}

		public override NSRange AccessibilityVisibleCharacterRange {
			get {
				var realRange = GetVisibleCharacterRange ();
				return new NSRange (realRange.Location, realRange.Length);
			}
		}

		public Func<string> Contents { get; set; }
		public Func<int> NumberOfCharacters { get; set; }
		public Func<int> InsertionPointLineNumber { get; set; }
		public Func<AtkCocoa.Range, Rectangle> GetFrameForRange { get; set; }
		public Func<int, int> GetLineForIndex { get; set; }
		public Func<int, AtkCocoa.Range> GetRangeForLine { get; set; }
		public Func<AtkCocoa.Range, string> GetStringForRange { get; set; }
		public Func<int, AtkCocoa.Range> GetRangeForIndex { get; set; }
		public Func<int, AtkCocoa.Range> GetStyleRangeForIndex { get; set; }
		public Func<Point, AtkCocoa.Range> GetRangeForPosition { get; set; }
		public Func<AtkCocoa.Range> GetVisibleCharacterRange { get; set;  }

		// Returned frame is in screen coordinate space
		[Export ("accessibilityFrameForRange:")]
		CGRect AccessibilityFrameForRange (NSRange range)
		{
			parentRef.TryGetTarget (out var parent);
			if (parent == null) {
				return CGRect.Empty;
			}

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
	}
}
#endif
