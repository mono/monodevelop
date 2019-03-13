//
// GtkShellDocumentViewContainerSplit.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using Gtk;
using MonoDevelop.Ide.Gui.Documents;
using Xwt.Drawing;
using System.Linq;
using MonoDevelop.Core;
using Gdk;
using System.ComponentModel;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewContainerSplit : IGtkShellDocumentViewContainer
	{
		DocumentViewContainerMode mode;
		Paned paned;
		List<double> relativeSplitSizes = new List<double> ();
		bool settingSize, sizesLoaded;
		Gdk.Rectangle lastRecalcSize;

		public GtkShellDocumentViewContainerSplit (DocumentViewContainerMode mode)
		{
			this.mode = mode;
			paned = CreatePaned ();
			paned.Show ();
			paned.SizeAllocated += Paned_SizeAllocated;
		}

		public Gtk.Widget Widget => paned;


		Paned CreatePaned ()
		{
			Paned p;
			if (mode == DocumentViewContainerMode.VerticalSplit)
				p = new HPaned ();
			else
				p = new VPaned ();
			p.AddNotification ("position",HandleNotifyHandler);
			return p;
		}

		void HandleNotifyHandler (object o, GLib.NotifyArgs args)
		{
			if (!settingSize && sizesLoaded)
				relativeSplitSizes = GetRelativeSplitSizes ().ToList ();
		}

		public List<Widget> Children { get; } = new List<Widget> ();

		public Paned Paned {
			get {
				return paned;
			}
		}

		public GtkShellDocumentViewItem ActiveView {
			get {
				return (GtkShellDocumentViewItem)Children.FirstOrDefault ();
			}
			set {
			}
		}

		public event EventHandler ActiveViewChanged;

		void Rebuild ()
		{
			settingSize = true;
			ClearPaneds ();
			var currentPaned = paned;
			int currentChild = 1;
			for (int n = 0; n < Children.Count; n++) {
				var c = Children [n];
				if (currentChild == 1) {
					currentPaned.Add1 (c);
					currentChild++;
				} else if (n < Children.Count - 1) {
					if (!(paned.Child2 is Paned childPaned)) {
						childPaned = CreatePaned ();
						currentPaned.Add2 (childPaned);
					}
					childPaned.Add1 (c);
					currentPaned = childPaned;
					currentChild = 2;
				} else {
					if (paned.Child2 is Paned childPaned) {
						paned.Remove (childPaned);
						childPaned.Destroy ();
					}
					paned.Add2 (c);
					currentPaned = null;
				}
			}
			if (currentPaned?.Child2 is Paned nextPaned) {
				currentPaned.Remove (nextPaned);
				nextPaned.Destroy ();
			}
			RestoreSizes ();
			settingSize = false;
		}

		void ClearPaneds ()
		{
			var p = paned;
			while (p != null) {
				var next = p.Child2 as Paned;
				if (p.Child1 != null)
					p.Remove (p.Child1);
				if (p.Child2 != null && next == null)
					p.Remove (p.Child2);
				p = next;
			}
		}

		public void Dispose ()
		{
			paned.SizeAllocated -= Paned_SizeAllocated;
		}

		public void AddViews (IEnumerable<GtkShellDocumentViewItem> views)
		{
			Children.Clear ();
			Children.AddRange (views);
			relativeSplitSizes.Clear ();
			for (int n = 0; n < Children.Count; n++)
				relativeSplitSizes.Add (1 / (double)Children.Count);
			Rebuild ();
		}

		public void InsertView (int position, GtkShellDocumentViewItem view)
		{
			if (Children.Count > 0) {
				var part = 1 / ((double)Children.Count + 1);
				for (int i = 0; i < relativeSplitSizes.Count; i++)
					relativeSplitSizes [i] *= 1 - part;
				relativeSplitSizes.Insert (position, part);
			} else
				relativeSplitSizes.Add (1);
			Children.Insert (position, view);
			Rebuild ();
		}

		public void RemoveAllViews ()
		{
			relativeSplitSizes.Clear ();
			Children.Clear ();
			Rebuild ();
		}

		public void RemoveView (int tabPos)
		{
			Children.RemoveAt (tabPos);
			var part = relativeSplitSizes [tabPos];
			relativeSplitSizes.RemoveAt (tabPos);
			for (int i = 0; i < relativeSplitSizes.Count; i++)
				relativeSplitSizes [i] /= 1 - part;
			Rebuild ();
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			var c = Children [currentIndex];
			Children.RemoveAt (currentIndex);
			Children.Insert (newIndex, c);

			var s = relativeSplitSizes [currentIndex];
			relativeSplitSizes.RemoveAt (currentIndex);
			relativeSplitSizes.Insert (newIndex, s);

			Rebuild ();
		}

		public void ReplaceView (int position, GtkShellDocumentViewItem view)
		{
			Children [position] = view;
			Rebuild ();
		}

		public void SelectView (GtkShellDocumentViewItem view)
		{
		}

		public void SetCurrentMode (DocumentViewContainerMode currentMode)
		{
		}

		public IEnumerable<GtkShellDocumentViewItem> GetAllViews ()
		{
			return paned.Children.Cast<GtkShellDocumentViewItem> ();
		}

		public void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
		}

		void Paned_SizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (!lastRecalcSize.Equals (args.Allocation)) {
				lastRecalcSize = args.Allocation;
				RestoreSizes ();
				sizesLoaded = true;
			}
			settingSize = false;
		}

		void RestoreSizes ()
		{
			if (Children.Count > 1) {
				settingSize = true;
				var size = mode == DocumentViewContainerMode.VerticalSplit ? paned.Allocation.Width : paned.Allocation.Height;
				var totalWidth = size - (Children.Count - 1) * HandleSize;
				var p = paned;
				int index = 0;
				while (p != null) {
					paned.Position = (int)(totalWidth * relativeSplitSizes [index++]);
					p = paned.Child2 as Paned;
				}
				settingSize = false;
			}
		}

		int HandleSize => 4;

		public void SetRelativeSplitSizes (double [] sizes)
		{
			relativeSplitSizes = sizes.ToList ();
		}

		public double [] GetRelativeSplitSizes ()
		{
			var size = mode == DocumentViewContainerMode.VerticalSplit ? paned.Allocation.Width : paned.Allocation.Height;
			var totalWidth = size - (Children.Count - 1) * HandleSize;

			var sizes = new double[Children.Count];
			for (int n = 0; n < Children.Count; n++) {
				var child = Children [n];
				var childSize = mode == DocumentViewContainerMode.VerticalSplit ? (double)child.Allocation.Width : (double)child.Allocation.Height;
				sizes [n] = childSize / (double)totalWidth;
			}
			return sizes;
		}
	}
}
