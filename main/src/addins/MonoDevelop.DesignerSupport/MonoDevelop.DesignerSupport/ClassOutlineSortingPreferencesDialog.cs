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
	partial class ClassOutlineSortingPreferencesDialog : Dialog
	{
		ClassOutlineSettings settings;
		
		public ClassOutlineSortingPreferencesDialog (ClassOutlineSettings settings)
		{
			this.Build ();

			priorityList.Model = new ListStore (typeof (string), typeof (string));
			priorityList.AppendColumn ("", new CellRendererText (), "text", 1);
			
			priorityList.Model.Clear ();
			foreach (string g in settings.GroupOrder) {
				priorityList.Model.AppendValues (g, ClassOutlineSettings.GetGroupName (g));
			}
			
			this.settings = settings;
		}
		
		public void SaveSettings ()
		{
			TreeIter iter;
			if (priorityList.Model.GetIterFirst (out iter)) {
				var order = new List<string> ();
				do {
					order.Add ((string) priorityList.Model.GetValue (iter, 0));
				} while (priorityList.Model.IterNext (ref iter));
				settings.GroupOrder = order; 
			}
			settings.Save ();
		}
	}
}