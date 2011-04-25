//
// ISortingProperties.cs
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

namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Stores sorting properties, e.g. if grouping or alphabetically sorting
	/// is enabled. Call SortingPropertiesChanged to notify listeners registered to
	/// EventSortingPropertiesChanged about changes.
	/// </summary>
	public interface ISortingProperties
	{
		/// <summary>
		/// Register at this event hook to be notified of property changes.
		/// </summary>
		event EventHandler EventSortingPropertiesChanged;

		/// <summary>
		/// Call to notify listeners of EventSortingPropertiesChanged events.
		/// </summary>
		/// <param name="o">
		/// The object that changed the properties.
		/// </param>
		/// <param name="e">
		/// Empty event arguments.
		/// </param>
		void SortingPropertiesChanged (object o, EventArgs e);

		/// <summary>
		/// Indicates whether grouping of entries is active.
		/// </summary>
		bool IsGrouping {
			get;
			set;
		}

		/// <summary>
		/// Indicates whether alphabetically sorting of entries is active.
		/// </summary>
		bool IsSortingAlphabetically {
			get;
			set;
		}

		/// <summary>
		/// Returns a list of all groups represented by these properties.
		/// </summary>
		/// <returns>
		/// A <see cref="List"/> of <see cref="Group"/> containing all the groups
		/// represented by these properties. It is not necessary to return a sorted list.
		/// </returns>
		List<Group> GetGroups ();
	}
}

