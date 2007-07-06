//
// TranslationProjectInfo.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2007 David Makovský
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Gettext.Translator
{
	[DataItem ("TranslationInfo")]
	public class TranslationProjectInfo : ICloneable
	{
		Project owner;

		[ItemProperty ("ExcludedFiles")]
		[ProjectPathItemProperty ("File", Scope = 1)]
		List<string> excludedFiles = new List<string> ();

		public TranslationProjectInfo ()
		{
		}

		public Project Project
		{
			get { return owner; }
			set {
				if (value != null && owner != value)
				{
					ProjectFileEventHandler onFileRemovedhandler = delegate (object sender, ProjectFileEventArgs e)
					{
						// TODO: if we have such file in excluded files, remove it
					};

					value.FileRemovedFromProject += onFileRemovedhandler;

					value.FileRenamedInProject += delegate {
						// TODO: if we have such old named file in excluded files, update it
					};
				}
				owner = value;
			}
		}

		public bool IsFileExcluded (string fileName)
		{
			return excludedFiles.Contains (fileName);
		}

		public void SetFileExcluded (string fileName, bool exclude)
		{
			if (exclude)
				excludedFiles.Add (fileName);
			else
				excludedFiles.Remove (fileName);
		}

		public object Clone ()
		{
			TranslationProjectInfo info = new TranslationProjectInfo ();
			info.Project = this.Project;
			info.excludedFiles = new List<string> (this.excludedFiles);
			return info;
		}
	}
}
