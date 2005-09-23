//
// UnitTestOptionsDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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


using System;
using System.Collections;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Services;
using MonoDevelop.Gui.Dialogs;

namespace MonoDevelop.NUnit {

	public class UnitTestOptionsDialog : TreeViewOptions
	{
		UnitTest test;
		
		IAddInTreeNode configurationNode;
	
		public UnitTestOptionsDialog (Gtk.Window parent, UnitTest test) : base (parent, null, null)
		{
			IAddInTreeNode node = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/UnitTestOptions/GeneralOptions");
			configurationNode = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/UnitTestOptions/ConfigurationProperties");
				
			this.test = test;
			this.Title = GettextCatalog.GetString ("Unit Test Options");
			
			properties = new DefaultProperties();
			properties.SetProperty ("UnitTest", test);
			AddNodes (properties, Gtk.TreeIter.Zero, node.BuildChildItems (this));			
			SelectFirstNode ();	
		}
		
		void FillConfigurations (Gtk.TreeIter configIter)
		{
			foreach (string name in test.GetConfigurations ()) {
				DefaultProperties configNodeProperties = new DefaultProperties();
				configNodeProperties.SetProperty("UnitTest", test);
				configNodeProperties.SetProperty("Config", name);
				
				ArrayList list = configurationNode.BuildChildItems (this);
				if (list.Count > 1) {
					Gtk.TreeIter newNode = AddPath (name, configIter);
					AddNodes (configNodeProperties, newNode, list);
				} else {
					AddNode (name, configNodeProperties, configIter, (IDialogPanelDescriptor) list [0]);
				}
			}
		}
		
		protected override void AddChildNodes (object customizer, Gtk.TreeIter iter, IDialogPanelDescriptor descriptor)
		{
			if (descriptor.ID != "Configurations") {
				base.AddChildNodes (customizer, iter, descriptor);
			} else {
				FillConfigurations (iter);
			}
		}
	}
}
