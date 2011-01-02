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

using SortingProperties = MonoDevelop.DesignerSupport.ClassOutlineTextEditorExtensionSortingProperties;

namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Provides a priority list of the groups that items in the class outline can be grouped in.
	/// </summary>
	///
	/// <remarks>
	/// The user can sort the list with button presses and thereby change the order of groups
	/// in the outline, while grouping is active.
	/// </remarks>

	public partial class ClassOutlineTextEditorExtensionPreferencesDialog : Dialog
	{
		public const string FOLDING_REGIONS_NAME  = "Regions";
		public const string NAMESPACES_NAME       = "Namespaces";
		public const string TYPES_NAME            = "Types";
		public const string FIELDS_NAME           = "Fields";
		public const string PROPERTIES_NAME       = "Properties";
		public const string EVENTS_NAME           = "Events";
		public const string METHODS_NAME          = "Methods";
		public const string LOCAL_VARIABLES_NAME  = "Local Variables";
		public const string PARAMETERS_NAME       = "Parameters";

		/*
		 * Used to provide a list of group and name pairs that can be sorted depending
		 * on current properties.
		 */

		struct Pair
		{
			public SortingProperties.Group Group {
				get;
				set;
			}

			public string Name {
				get;
				set;
			}

			public Pair (SortingProperties.Group g, string name)
			{
				Group = g;
				Name = name;
			}

			public static int Compare (Pair a, Pair b)
			{
				return ClassOutlineTextEditorExtensionSorting.CompareGroups (a.Group, b.Group);
			}
		}

		/// <summary>
		/// Creates a new instance and displays the groups in the priority list in the order
		/// specified by the current sorting properties.
		/// </summary>

		public ClassOutlineTextEditorExtensionPreferencesDialog ()
		{
			this.Build ();

			prioritylist.Model = new ListStore (typeof(SortingProperties.Group), typeof(string));
			prioritylist.AppendColumn ("", new CellRendererText (), "text", 1);

			List<Pair> list = new List<Pair> ();

			// Create a random ordered list of groups with matching name tags

			list.Add (new Pair (SortingProperties.Group.FoldingRegion, GettextCatalog.GetString (FOLDING_REGIONS_NAME)));
			list.Add (new Pair (SortingProperties.Group.Namespace,     GettextCatalog.GetString (NAMESPACES_NAME)));
			list.Add (new Pair (SortingProperties.Group.Type,          GettextCatalog.GetString (TYPES_NAME)));
			list.Add (new Pair (SortingProperties.Group.Field,         GettextCatalog.GetString (FIELDS_NAME)));
			list.Add (new Pair (SortingProperties.Group.Property,      GettextCatalog.GetString (PROPERTIES_NAME)));
			list.Add (new Pair (SortingProperties.Group.Event,         GettextCatalog.GetString (EVENTS_NAME)));
			list.Add (new Pair (SortingProperties.Group.Method,        GettextCatalog.GetString (METHODS_NAME)));
			list.Add (new Pair (SortingProperties.Group.LocalVariable, GettextCatalog.GetString (LOCAL_VARIABLES_NAME)));
			list.Add (new Pair (SortingProperties.Group.Parameter,     GettextCatalog.GetString (PARAMETERS_NAME)));

			/*
			 * Sort items based on current property sort key settings.
			 *
			 * NOTE: This will provide a valid list even if the user set identic sort keys
			 *       in the configuration file, or if someone did this programmatically
			 *       in the properties object.
			 */

			list.Sort (Pair.Compare);

			foreach (Pair pair in list) {
				prioritylist.Model.AppendValues (pair.Group, pair.Name);
			}
		}

		/// <summary>
		/// Causes the settings to be stored in the properties.
		/// </summary>

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			// Always store, don't care if nothing changed

			Store ();
		}

		/// <summary>
		/// Stores the settings in the properties.
		/// </summary>

		protected virtual void Store ()
		{
			SortingProperties properties;
			properties = PropertyService.Get (
				ClassOutlineTextEditorExtension.SORTING_PROPERTY,
				SortingProperties.GetDefaultInstance ());

			TreeIter iter;
			byte key = 0;

			/*
			 * For each item in the priority list set the corresponding key in the properties to the new
			 * sort key.
			 */

			if (prioritylist.Model.GetIterFirst (out iter)) {
				do {
					SortingProperties.Group g = (SortingProperties.Group) prioritylist.Model.GetValue (iter, 0);

					SortingProperties.SetKeyForGroup (properties, g, key);

					key++;
				} while (prioritylist.Model.IterNext (ref iter));
			}

			ClassOutlineTextEditorExtension.SortingPropertiesChanged (this, EventArgs.Empty);
		}
	}
}

