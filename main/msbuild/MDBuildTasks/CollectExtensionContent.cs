//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace MDBuildTasks
{
	public class CollectExtensionContent : Task
	{
		[Required]
		public ITaskItem[] ExtensionContent { get; set; }

		[Output]
		public ITaskItem[] ExtensionContentWithLinkMetadata { get; set; }

		public override bool Execute ()
		{
			var items = new List<ITaskItem> ();

			foreach (var file in ExtensionContent) {
				string path = file.GetMetadata ("FullPath");
				var link = GetLinkPath (file, path);
				var item = new TaskItem (path);
				item.SetMetadata ("Link", link);
				items.Add (item);
			}

			ExtensionContentWithLinkMetadata = items.ToArray ();

			return true;
		}

		static string GetLinkPath (ITaskItem file, string path)
		{
			string link = file.GetMetadata ("Link");
			if (!string.IsNullOrEmpty (link)) {
				return link;
			}

			string projectDir;
			var definingProject = file.GetMetadata ("DefiningProjectFullPath");
			if (!string.IsNullOrEmpty (definingProject)) {
				projectDir = Path.GetDirectoryName (definingProject);
			} else {
				projectDir = Environment.CurrentDirectory;
			}

			projectDir = Path.GetFullPath (projectDir);
			if (projectDir[projectDir.Length - 1] != Path.DirectorySeparatorChar) {
				projectDir += Path.DirectorySeparatorChar;
			}

			if (path.StartsWith (projectDir, StringComparison.Ordinal)) {
				link = path.Substring (projectDir.Length);
			} else {
				link = Path.GetFileName (path);
			}

			return link;
		}
	}
}

