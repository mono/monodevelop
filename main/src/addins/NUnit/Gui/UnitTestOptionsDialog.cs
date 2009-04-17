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

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui.Codons;
using Gtk;

namespace MonoDevelop.NUnit {

	public class UnitTestOptionsDialog : OptionsDialog
	{
		ExtensionNode configurationNode;
		UnitTest test;
		
		public UnitTestOptionsDialog (Gtk.Window parent, Properties properties) : base (parent, properties, "/MonoDevelop/NUnit/UnitTestOptions/GeneralOptions", false)
		{
			this.Title = GettextCatalog.GetString ("Unit Test Options");
		
			test = properties.Get<UnitTest>("UnitTest");
			configurationNode = AddinManager.GetExtensionNode("/MonoDevelop/NUnit/UnitTestOptions/ConfigurationOptions");
			
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				OptionsDialogSection section = store.GetValue (iter, 0) as OptionsDialogSection;
				
				if (section != null && section.Id == "Configurations") {
					FillConfigurations (iter);
				}
			}
			ExpandCategories ();
			if (firstSection != null)
				ShowPage (firstSection);
		}
		protected override void OnResponse (Gtk.ResponseType response_id)
		{
			base.OnResponse (response_id);
			Destroy ();
		}

		OptionsDialogSection firstSection = null;
		void FillConfigurations (Gtk.TreeIter configIter)
		{
			foreach (string name in test.GetConfigurations ()) {
				Properties configNodeProperties = new Properties();
				configNodeProperties.Set ("UnitTest", test);
				configNodeProperties.Set ("Config", name);
				Console.WriteLine ("contig: " + name);
				foreach (OptionsDialogSection section in configurationNode.ChildNodes) {
					OptionsDialogSection s = (OptionsDialogSection)section.Clone ();
					if (firstSection == null)
						firstSection = s;
					s.Label = StringParserService.Parse (section.Label, new string[,] { { "Configuration", name } });
					AddSection (configIter, s, configNodeProperties);
				}
			}
		}
	}
}
