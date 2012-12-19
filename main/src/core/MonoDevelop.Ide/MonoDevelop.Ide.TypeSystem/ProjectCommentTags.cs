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

namespace MonoDevelop.Ide.TypeSystem
{
	[Serializable]
	public class ProjectCommentTags
	{
		readonly Dictionary<string, List<Tag>> tags = new Dictionary<string, List<Tag>> ();

		public IDictionary<string, List<Tag>> Tags {
			get {
				return tags;
			}
		}

		public void UpdateTags (Project project, string fileName, IList<Tag> tagComments)
		{
			var list = tagComments == null || tagComments.Count == 0 ? null : new List<Tag> (tagComments);
			lock (tags) {
				List<Tag> oldList;
				tags.TryGetValue (fileName, out oldList);
				if (list == null && oldList == null)
					return;
				tags[fileName] = list;
				TaskService.InformCommentTasks (new CommentTasksChangedEventArgs (fileName, tagComments, project));
			}
		}

		public void RemoveFile (Project project, string fileName)
		{
			lock (tags) {
				if (!tags.ContainsKey (fileName))
					return;
				tags[fileName] = null;
			}
			
			TaskService.InformCommentTasks (new CommentTasksChangedEventArgs (fileName, null, project));
		}

		internal void Update (Project project)
		{
			foreach (var file in project.Files) {
				TypeSystemService.ParseFile (project, file.FilePath);
			}
		}
	}
}

