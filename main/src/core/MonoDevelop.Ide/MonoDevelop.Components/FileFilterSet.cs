// 
// FileFilterSet.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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
using System.Collections.Generic;
using MonoDevelop.Components.Extensions;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class FileFilterSet
	{
		List<SelectFileDialogFilter> filters = new List<SelectFileDialogFilter> ();
		
		internal FileFilterSet ()
		{
		}
		
		public SelectFileDialogFilter DefaultFilter { get; set; }  
		
		internal IList<SelectFileDialogFilter> Filters {
			get { return filters; }
		}
		
		public SelectFileDialogFilter AddFilter (string label, params string[] patterns)
		{
			return AddFilter (new SelectFileDialogFilter (label, patterns));
		}
		
		public SelectFileDialogFilter AddFilter (SelectFileDialogFilter filter)
		{
			if (useDefaultFilters)
				throw new InvalidOperationException ("Cannot mix default filters and custom filters");
			filters.Add (filter);
			return filter;
		}
		
		public SelectFileDialogFilter AddAllFilesFilter ()
		{
			return AddFilter (SelectFileDialogFilter.AllFiles);
		}
		
		public bool HasFilters {
			get {
				return filters.Count > 0;
			}
		}
		
		/// <summary>
		/// Adds the default file filters registered by MD core and addins. Includes the All Files filter.
		/// </summary>
		public void AddDefaultFileFilters ()
		{
			if (HasFilters)
				throw new InvalidOperationException ("Cannot mix default filters and custom filters");
			if (useDefaultFilters)
				throw new InvalidOperationException ("Already called");
			
			useDefaultFilters = true;

			if (Platform.IsWindows)
				filters.Add (SelectFileDialogFilter.AllFiles);

			foreach (var f in GetDefaultFilters ())
				filters.Add (f);

			if (!Platform.IsWindows)
				filters.Add (SelectFileDialogFilter.AllFiles);

			LoadDefaultFilter ();
		}
		
		static IEnumerable<SelectFileDialogFilter> GetDefaultFilters ()
		{
			foreach (var f in ParseFilters (AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/ProjectFileFilters")))
				yield return f;
			foreach (var f in ParseFilters (AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/FileFilters")))
				yield return f;
		}
		
		static IEnumerable<SelectFileDialogFilter> ParseFilters (System.Collections.IEnumerable filterStrings)
		{
			if (filterStrings == null)
				yield break;
			var filterNames = new HashSet<string>();
			foreach (string filterStr in filterStrings) {
				var parts = filterStr.Split('|');
				var filterName = parts[0];
				if (filterNames.Add(filterName))
				{
					var f = new SelectFileDialogFilter(filterName, parts[1].Split(';'));
					yield return f;
				}
			}
		}
		
		bool useDefaultFilters;
		
		///<summary>Loads last default filter from MD prefs</summary>
		void LoadDefaultFilter ()
		{
			// Load last used filter pattern
			var lastPattern = PropertyService.Get ("Monodevelop.FileSelector.LastPattern", "*");
			foreach (var filter in Filters) {
				if (filter.Patterns != null && filter.Patterns.Contains (lastPattern)) {
					DefaultFilter = filter;
					break;
				}
			}
		}
		
		///<summary>Saves last default filter to MD prefs, if necessary</summary>
		internal void SaveDefaultFilter ()
		{
			if (!useDefaultFilters)
				return;
			
			// Save active filter
			//it may be null if e.g. SetSelectedFile was used
			if (DefaultFilter != null && DefaultFilter.Patterns != null && DefaultFilter.Patterns.Count > 0)
				PropertyService.Set ("Monodevelop.FileSelector.LastPattern", DefaultFilter.Patterns[0]);
		}
	}
}

