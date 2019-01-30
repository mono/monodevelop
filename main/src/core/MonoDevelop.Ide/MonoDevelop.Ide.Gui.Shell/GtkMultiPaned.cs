//
// GtkMultiPaned.cs
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

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkMultiPaned
	{
		DocumentViewContainerMode mode;
		Paned paned;

		public GtkMultiPaned (DocumentViewContainerMode mode)
		{
			this.mode = mode;
			paned = CreatePaned ();
			paned.Show ();
		}

		Paned CreatePaned ()
		{
			if (mode == DocumentViewContainerMode.HorizontalSplit)
				return new HPaned ();
			else
				return new VPaned ();
		}

		public Paned Paned {
			get {
				return paned;
			}
		}

		public void AddRange (IEnumerable<Widget> widgets)
		{
			Children.Clear ();
			Children.AddRange (widgets);
			Rebuild ();
		}

		public List<Widget> Children { get; } = new List<Widget> ();

		void Rebuild ()
		{
			ClearPaneds ();
			var currentPaned = paned;
			int currentChild = 1;
			for (int n=0; n<Children.Count; n++) {
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
			}
		}

		public void InsertView (int position, Widget view)
		{
			Children.Insert (position, view);
			Rebuild ();
		}

		public void RemoveAllViews ()
		{
			Children.Clear ();
			Rebuild ();
		}

		public void RemoveView (int tabPos)
		{
			Children.RemoveAt (tabPos);
			Rebuild ();
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			var c = Children [currentIndex];
			Children.RemoveAt (currentIndex);
			Children.Insert (newIndex, c);
			Rebuild ();
		}

		public void ReplaceView (int position, Widget view)
		{
			Children [position] = view;
			Rebuild ();
		}

		public void SelectView (Widget view)
		{
		}
	}
}
