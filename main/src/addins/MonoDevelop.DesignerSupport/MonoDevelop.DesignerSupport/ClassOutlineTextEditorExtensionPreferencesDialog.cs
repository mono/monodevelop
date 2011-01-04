//
// ClassOutlineTextEditorExtensionPreferencesDialog.cs
//
// Authors:
//  Helmut Duregger <helmutduregger@gmx.at>
//
// Copyright (c) 2011 Helmut Duregger
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
//

using System;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Core;


namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Provides a priority list of the groups that items in the class outline can be grouped in.
	/// </summary>
	/// <remarks>
	/// The user can sort the list with button presses and thereby change the order of groups
	/// in the outline, while grouping is active.
	/// </remarks>
	public partial class ClassOutlineTextEditorExtensionPreferencesDialog : Dialog
	{
		/*
		 * Used to provide a list of group and name pairs that can be sorted depending
		 * on current properties.
		 */

		struct Pair
		{
			public ClassOutlineSortingProperties.Group Group {
				get;
				set;
			}

			public string Name {
				get;
				set;
			}

			public Pair (ClassOutlineSortingProperties.Group g, string name)
			{
				Group = g;
				Name = name;
			}
		}

		class PairComparer : IComparer<Pair>
		{
			ClassOutlineGroupComparer comparer;

			public PairComparer (ClassOutlineGroupComparer comparer)
			{
				this.comparer = comparer;
			}

			public int Compare (Pair a, Pair b)
			{
				return comparer.Compare (a.Group, b.Group);
			}
		}

		ClassOutlineSortingProperties properties;

		/// <summary>
		/// Creates a new instance and displays the groups in the priority list in the order
		/// specified by the current sorting properties.
		/// </summary>
		public ClassOutlineTextEditorExtensionPreferencesDialog ()
		{
			this.Build ();

			prioritylist.Model = new ListStore (typeof(ClassOutlineSortingProperties.Group), typeof(string));
			prioritylist.AppendColumn ("", new CellRendererText (), "text", 1);

			List<Pair> list = new List<Pair> ();

			// Create a random ordered list of groups with matching name tags

			list.Add (new Pair (ClassOutlineSortingProperties.Group.FoldingRegion, GettextCatalog.GetString ("Regions")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Namespace,     GettextCatalog.GetString ("Namespaces")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Type,          GettextCatalog.GetString ("Types")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Field,         GettextCatalog.GetString ("Fields")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Property,      GettextCatalog.GetString ("Properties")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Event,         GettextCatalog.GetString ("Events")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Method,        GettextCatalog.GetString ("Methods")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.LocalVariable, GettextCatalog.GetString ("Local Variables")));
			list.Add (new Pair (ClassOutlineSortingProperties.Group.Parameter,     GettextCatalog.GetString ("Parameters")));

			/*
			 * Sort items based on current property sort key settings.
			 *
			 * NOTE: This will provide a valid list even if the user set identic sort keys
			 *       in the configuration file, or if someone did this programmatically
			 *       in the properties object.
			 */

			properties = PropertyService.Get (
				ClassOutlineTextEditorExtension.SORTING_PROPERTY,
				ClassOutlineSortingProperties.GetDefaultInstance ());

			list.Sort (new PairComparer (new ClassOutlineGroupComparer (properties)));

			foreach (Pair pair in list) {
				prioritylist.Model.AppendValues (pair.Group, pair.Name);
			}
		}

		/// <summary>
		/// Causes the settings to be stored in the properties.
		/// </summary>
		protected void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			// Always store, don't care if nothing changed

			Store ();
		}

		/// <summary>
		/// Stores the settings in the properties.
		/// </summary>
		protected void Store ()
		{
			TreeIter iter;
			byte key = 0;

			/*
			 * For each item in the priority list set the corresponding key in the properties to the new
			 * sort key.
			 */

			if (prioritylist.Model.GetIterFirst (out iter)) {
				do {
					ClassOutlineSortingProperties.Group g = (ClassOutlineSortingProperties.Group) prioritylist.Model.GetValue (iter, 0);

					properties.SetKeyForGroup (g, key);

					key++;
				} while (prioritylist.Model.IterNext (ref iter));
			}

			ClassOutlineTextEditorExtension.SortingPropertiesChanged (this, EventArgs.Empty);
		}
	}
}

