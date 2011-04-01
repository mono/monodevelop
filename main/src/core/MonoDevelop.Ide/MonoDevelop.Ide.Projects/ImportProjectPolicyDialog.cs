// 
// ImportProjectPolicyDialog.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Projects
{
	partial class ImportProjectPolicyDialog : Gtk.Dialog
	{
		public ImportProjectPolicyDialog ()
		{
			this.Build ();
			selector.RootItem = IdeApp.Workspace;
			selector.SelectedItem = IdeApp.ProjectOperations.CurrentSelectedBuildTarget;
			selector.SelectableItemTypes = new Type[] { typeof(IPolicyProvider) };
			UpdateOk ();
		}
		
		public string PolicyName {
			get { return entryName.Text; }
			set { entryName.Text = value; }
		}
		
		public IPolicyProvider SelectedItem {
			get { return (IPolicyProvider) selector.SelectedItem; }
		}

		protected void OnEntryNameChanged (object sender, System.EventArgs e)
		{
			UpdateOk ();
		}
		
		void UpdateOk ()
		{
			buttonOk.Sensitive = selector.SelectedItem != null && PolicyName.Length > 0;
		}

		protected void OnSelectorSelectionChanged (object sender, System.EventArgs e)
		{
			UpdateOk ();
		}
	}
}

