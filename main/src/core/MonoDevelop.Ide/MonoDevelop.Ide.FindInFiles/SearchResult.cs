//
// SearchResult.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using Mono.TextEditor.Highlighting;
using MonoDevelop.Projects;
using System.Collections.Generic;

namespace MonoDevelop.Ide.FindInFiles
{
	public class SearchResult
	{
		public virtual FileProvider FileProvider { get; private set; }
		
		public int Offset { get; set; }
		public int Length { get; set; }

		public virtual string FileName {
			get {
				return FileProvider.FileName;
			}
		}

		#region Cached data
		public int LineNumber {
			get;
			set;
		}

		public string Markup {
			get; 
			set;
		}

		public uint StartIndex {
			get;
			set;
		}

		public uint EndIndex {
			get;
			set;
		}

		public Xwt.Drawing.Image FileIcon {
			get;
			set;
		}

		public Xwt.Drawing.Image ProjectIcon {
			get;
			set;
		}

		private List<Project> projects;
		public List<Project> Projects {
			get {
				if (projects == null) {
					projects = new List<Project> (IdeApp.Workspace.GetProjectsContainingFile (FileName));
				}
				return projects;
			}
		}
		#endregion


		protected SearchResult (int offset, int length)
		{
			Offset = offset;
			Length = length;
		}
		
		public SearchResult (FileProvider fileProvider, int offset, int length)
		{
			FileProvider = fileProvider;
			Offset = offset;
			Length = length;
		}
		
		public override string ToString ()
		{
			return string.Format("[SearchResult: FileProvider={0}, Offset={1}, Length={2}]", FileProvider, Offset, Length);
		}

		public virtual AmbientColor GetBackgroundMarkerColor (ColorScheme style)
		{
			return style.SearchResult;
		}
	}
}
