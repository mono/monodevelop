//
// CreatePatchCommand.cs
//
// Author:
//   Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
//
// Copyright (C) 2009 Levi Bard
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
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	/// <summary>
	/// Class for creating patches from VersionControlItems
	/// </summary>
	public class CreatePatchCommand
	{
		/// <summary>
		/// Creates a patch from a VersionControlItemList
		/// </summary>
		/// <param name="items">
		/// A <see cref="VersionControlItemList"/> from which to create a patch.
		/// </param>
		/// <param name="test">
		/// A <see cref="System.Boolean"/>: Whether this is a test run.
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>: Whether the patch creation succeeded.
		/// </returns>
		public static bool CreatePatch (VersionControlItemList items, bool test)
		{
			bool can = CanCreatePatch (items);
			if (test || !can){ return can; }
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			Repository repo = items[0].Repository;
			
			foreach (VersionControlItem item in items) {
				diffs.AddRange (item.Repository.PathDiff (item.Path, null, false));
			}
			
			string patch = repo.CreatePatch (diffs);
			MonoDevelop.Ide.Gui.IdeApp.Workbench.NewDocument (string.Format ("{0}.diff", items[0].Path), "text/x-diff", patch);
			return can;
		}
		
		/// <summary>
		/// Determines whether a patch can be created 
		/// from a VersionControlItemList.
		/// </summary>
		public static bool CanCreatePatch (VersionControlItemList items) 
		{
			if (null == items || 0 == items.Count){ return false; }
			Repository repo = items[0].Repository;
			
			foreach (VersionControlItem item in items) {
				if (item.Repository != repo || !item.Repository.CanRevert (item.Path)) {
					return false;
				}
			}
			
			return true;
		}
	}
}
