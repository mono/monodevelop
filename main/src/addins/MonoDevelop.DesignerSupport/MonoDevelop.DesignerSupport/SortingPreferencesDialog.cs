//
// SortingPreferencesDialog.cs
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
	public partial class SortingPreferencesDialog : Dialog
	{
		ISortingProperties properties;

		/// <summary>
		/// Creates a new instance and displays the groups in the priority list in the order
		/// specified by the current sorting properties.
		/// </summary>
		public SortingPreferencesDialog (ISortingProperties properties)
		{
			this.Build ();

			this.properties = properties;

			priorityList.Model = new ListStore (typeof(Group), typeof(string));
			priorityList.AppendColumn ("", new CellRendererText (), "text", 1);

			List<Group> groups = properties.GetGroups ();

			//
			// Sort items based on current property sort keys.
			//
			// NOTE: This will provide a valid list even if someone set identic sort keys
			//       in the properties object. We will extract unique keys when storing the
			//       changes.
			//

			groups.Sort (new GroupComparer ());

			foreach (Group g in groups) {
				priorityList.Model.AppendValues (g, g.Name);
			}
		}

		/// <summary>
		/// Causes the settings to be stored in the properties.
		/// </summary>
		protected void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			// Always store, don't care if the user did not change anything.
			// We still want to extract unique sort keys.

			Store ();
		}

		/// <summary>
		/// Stores the settings in the properties.
		/// </summary>
		protected void Store ()
		{
			//
			// For each item in the priority list set the sort key to the index in the list
			// thereby creating unique sort keys.
			//

			TreeIter iter;
			int      currentSortKey = 0;

			if (priorityList.Model.GetIterFirst (out iter)) {

				do {
					var g = (Group) priorityList.Model.GetValue (iter, 0);

					g.SortKey = currentSortKey;

					currentSortKey++;
				} while (priorityList.Model.IterNext (ref iter));

			}

			// Notify listeners of changes to the sorting order

			properties.SortingPropertiesChanged (this, EventArgs.Empty);
		}
	}
}

