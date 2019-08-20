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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;

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
		public static async Task<bool> CreatePatchAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken = default)
		{
			bool can = await CanCreatePatchAsync (items, cancellationToken);
			if (test || !can){ return can; }
			
			FilePath basePath = items.FindMostSpecificParent ();
			if (FilePath.Null == basePath)
				return false;
			
			ChangeSet cset = new ChangeSet (items[0].Repository, basePath);
			foreach (VersionControlItem item in items) {
				await cset.AddFileAsync (item.Path);
			}
			return await CreatePatchAsync (cset, test, cancellationToken);
		}
		
		public static async Task<bool> CreatePatchAsync (ChangeSet items, bool test, CancellationToken cancellationToken = default)
		{
			bool can = await CanCreatePatchAsync (items, cancellationToken);
			if (test || !can){ return can; }
			
			Repository repo = items.Repository;
			items = items.Clone ();
			
			List<DiffInfo> diffs = new List<DiffInfo> ();
			
			object[] exts = AddinManager.GetExtensionObjects ("/MonoDevelop/VersionControl/CommitDialogExtensions", typeof(CommitDialogExtension), false);
			List<CommitDialogExtension> activeExtensions = new List<CommitDialogExtension> ();
			try {
				foreach (CommitDialogExtension ext in exts) {
					if (ext.Initialize (items)) {
						activeExtensions.Add (ext);
						if (!ext.OnBeginCommit (items))
							break;
					} else
						ext.Destroy ();
				}
				diffs.AddRange (await repo.PathDiffAsync (items, false, cancellationToken));
			} finally {
				foreach (CommitDialogExtension ext in activeExtensions) {
					ext.OnEndCommit (items, false);
					ext.Destroy ();
				}
			}
			
			string patch = repo.CreatePatch (diffs);
			string filename = string.Format ("{0}.diff", ((string)items.BaseLocalPath.FullPath).TrimEnd (Path.DirectorySeparatorChar));
			IdeApp.Workbench.NewDocument (filename, "text/x-diff", patch);
			return can;
		}
		
		/// <summary>
		/// Determines whether a patch can be created 
		/// from a ChangeSet.
		/// </summary>
		static async Task<bool> CanCreatePatchAsync (ChangeSet items, CancellationToken cancellationTokent)
		{
			if (null == items || 0 == items.Count){ return false; }
			
			var vinfos = await items.Repository.GetVersionInfoAsync (items.Items.Select (i => i.LocalPath), cancellationToken: cancellationTokent);
			return vinfos.All (i => i.CanRevert);
		}
		
		/// <summary>
		/// Determines whether a patch can be created 
		/// from a VersionControlItemList.
		/// </summary>
		static async Task<bool> CanCreatePatchAsync (VersionControlItemList items, CancellationToken cancellationToken)
		{
			if (null == items || 0 == items.Count){ return false; }

			foreach (var item in items) {
				var info = await item.GetVersionInfoAsync (cancellationToken);
				if (!info.CanRevert)
					return false;
			}

			return true;
		}

		internal static void CreatePatch (ChangeSet changeSet, bool v)
		{
			throw new NotImplementedException ();
		}
	}
}
