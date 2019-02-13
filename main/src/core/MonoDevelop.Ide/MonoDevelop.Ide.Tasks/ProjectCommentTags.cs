// 
// ProjectCommentTags.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Concurrent;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Tasks;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Tasks
{
	class ProjectCommentTags
	{
		Dictionary<string, IReadOnlyList<Tag>> tags = new Dictionary<string, IReadOnlyList<Tag>> ();

		public IDictionary<string, IReadOnlyList<Tag>> Tags {
			get {
				return tags;
			}
		}

		public void UpdateTags (Project project, string fileName, IReadOnlyList<Tag> tagComments)
		{
			var list = tagComments == null || tagComments.Count == 0 ? null : new List<Tag> (tagComments);
			lock (tags) {
				IReadOnlyList<Tag> oldList;
				tags.TryGetValue (fileName, out oldList);
				if (list == null && oldList == null)
					return;
				tags[fileName] = list;
				IdeServices.TaskService.InformCommentTasks (new CommentTasksChangedEventArgs (fileName, tagComments, project));
			}
		}

		public void RemoveFile (Project project, string fileName)
		{
			lock (tags) {
				if (!tags.ContainsKey (fileName))
					return;
				tags[fileName] = null;
			}
			
			IdeServices.TaskService.InformCommentTasks (new CommentTasksChangedEventArgs (fileName, null, project));
		}

		internal async Task UpdateAsync (Project project, ProjectFile[] files, CancellationToken token = default (CancellationToken))
		{
			var changes = new List<CommentTaskChange> ();
			var newTags = new Dictionary<string, IReadOnlyList<Tag>> ();
			foreach (var file in files) {
				if (file.BuildAction == BuildAction.None)
					continue;
				var pd = await IdeApp.TypeSystemService.ParseFile (project, file.FilePath, token).ConfigureAwait (false);
				if (pd != null) {
					var commentTagList = await pd.GetTagCommentsAsync (token).ConfigureAwait (false);
					changes.Add (new CommentTaskChange (file.FilePath, commentTagList, project));
					newTags[file.FilePath] = commentTagList;
				}
			}
			await Runtime.RunInMainThread (delegate {
				this.tags = newTags;
				IdeServices.TaskService.InformCommentTasks (new CommentTasksChangedEventArgs (changes));
			}).ConfigureAwait (false);
		}
	}
}