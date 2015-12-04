// 
// ProjectSelectorDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Projects
{
	public partial class ProjectSelectorDialog : Gtk.Dialog
	{
		bool allowEmptySelection;
		
		public ProjectSelectorDialog ()
		{
			this.Build ();
			UpdateOk ();
		}
		
		public string Label {
			get { return labelTitle.Text; }
			set { labelTitle.Text = value; }
		}
		
		public bool AllowEmptySelection {
			get {
				return this.allowEmptySelection;
			}
			set {
				allowEmptySelection = value;
				UpdateOk ();
			}
		}
		
		public IBuildTarget SelectedItem {
			get { return selector.SelectedItem; }
			set { selector.SelectedItem = value; }
		}
		
		public IEnumerable<IBuildTarget> ActiveItems {
			get { return selector.ActiveItems; }
			set { selector.ActiveItems = value; }
		}
		
		public bool ShowCheckboxes {
			get { return selector.ShowCheckboxes; }
			set { selector.ShowCheckboxes = value; UpdateOk (); }
		}
		
		public bool CascadeCheckboxSelection {
			get { return selector.CascadeCheckboxSelection; }
			set { selector.CascadeCheckboxSelection = value; }
		}
		
		public IBuildTarget RootItem {
			get { return selector.RootItem; }
			set { selector.RootItem = value; }
		}
		
		public IEnumerable<Type> SelectableItemTypes {
			get { return selector.SelectableItemTypes; }
			set { selector.SelectableItemTypes = value; }
		}

		public Func<IBuildTarget,bool> SelectableFilter {
			get { return selector.SelectableFilter; }
			set { selector.SelectableFilter = value; }
		}

		void UpdateOk ()
		{
			if (selector.ShowCheckboxes)
				buttonOk.Sensitive = AllowEmptySelection || selector.ActiveItems.Any ();
			else
				buttonOk.Sensitive = AllowEmptySelection || selector.SelectedItem != null;
		}

		protected void OnSelectorSelectionChanged (object sender, System.EventArgs e)
		{
			UpdateOk ();
		}

		protected void OnSelectorActiveChanged (object sender, System.EventArgs e)
		{
			UpdateOk ();
		}
	}
}

