//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.Profiling.HeapShot
{
	public class HeapShotProfilingSnapshotView : AbstractViewContent
	{
		private bool allObjects;
		private string typeName;
		private ReferenceTreeViewer viewer;
		private HeapShotProfilingSnapshot snapshot;
		
		public HeapShotProfilingSnapshotView ()
		{
			allObjects = true;
			typeName = GettextCatalog.GetString ("All Objects");
			
			Initialize ();
		}
		
		public HeapShotProfilingSnapshotView (string typeName)
		{
			allObjects = false;
			this.typeName = typeName;
				
			Initialize ();
		}
		
		private void Initialize ()
		{
			viewer = new ReferenceTreeViewer ();
			viewer.TypeActivated += delegate {
				ShowTypeTreeInView (viewer.SelectedType, viewer.InverseReferences);
			};
		}
		
		public override bool IsDirty {
			get { return false; }
			set {  }
		}

		public override string StockIconId {
			get { return "md-prof-snapshot"; }
		}
		
		public override string UntitledName {
			get { return snapshot.Name + " - " + typeName; }
		}

		public override Widget Control {
			get { return viewer; }
		}
		
		public override void Load (string fileName) {}
		
		public void Load (HeapShotProfilingSnapshot snapshot, bool inverse)
		{
			this.snapshot = snapshot;

			if (allObjects)
				viewer.FillAllTypes (snapshot.ObjectMap);//viewer.FillType (snapshot.ObjectMap, viewer.SelectedType);
			else
				viewer.FillType (snapshot.ObjectMap, typeName);
			viewer.Show ();
			//TODO: toggle 'inverse' on all child views when toggled in the 'all objects' view
		}
		
		private void ShowTypeTreeInView (string typeName, bool inverse)
		{
			HeapShotProfilingSnapshotView view = new HeapShotProfilingSnapshotView (typeName);
			view.Load (snapshot, inverse);
			IdeApp.Workbench.OpenDocument (view, true);
		}
	}
}
