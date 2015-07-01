// 
// PathCompletion.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Text.RegularExpressions;
using MonoDevelop.Ide;

namespace MonoDevelop.AspNet.Html
{
	public static class HtmlPathCompletion
	{

		public static CompletionDataList GetPathCompletion (Project proj, string pattern,
		                                                    FilePath fromFile,
		                                                    Func<ProjectFile,string> pathFunc)
		{
			var list = new CompletionDataList ();
			
			var regex = new Regex ("^" + 
				Regex.Escape (pattern).Replace ("\\*",".*").Replace ("\\?",".").Replace ("\\|","$|^") + "$",
				RegexOptions.Compiled);
			
			var dir = fromFile.ParentDirectory.ToString ();
			foreach (var pf in proj.Files) {
				string pathStr = pf.FilePath.ToString ();
				if (pf.Subtype == Subtype.Directory || !pathStr.StartsWith (dir, StringComparison.Ordinal))
					continue;
				
				int split = pathStr.LastIndexOf (System.IO.Path.DirectorySeparatorChar);
				if (split != dir.Length)
					continue;
				
				if (regex.IsMatch (pf.FilePath.FileName))
					list.Add (new FileCompletionData (pf, pathFunc));
			}
			
			list.Sort ();
			list.IsSorted = true;
			list.Add (new FilePickerCompletionData (proj, pattern, pathFunc));
			return list;
		}
		
		class FileCompletionData : CompletionData, IComparable<CompletionData>
		{
			ProjectFile file;
			Func<ProjectFile,string> pathFunc;
			
			public FileCompletionData (ProjectFile file, Func<ProjectFile, string> pathFunc)
			{
				this.file = file;
				this.pathFunc = pathFunc;
			}
			
			public override string DisplayText {
				get { return file.FilePath.FileName; }
			}
			
			public override string CompletionText {
				get { return pathFunc (file); }
			}
			
			public int CompareTo (CompletionData other)
			{
				return string.Compare (DisplayText, other.DisplayText, StringComparison.Ordinal);
			}
		}
		
		class FilePickerCompletionData : CompletionData
		{
			readonly Project proj;
			readonly string pattern;
			readonly Func<ProjectFile,string> pathFunc;
			
			public FilePickerCompletionData (Project proj, string pattern, Func<ProjectFile,string> pathFunc)
			{
				this.proj = proj;
				this.pattern = pattern;
				this.pathFunc = pathFunc;
			}
			
			public override string DisplayText {
				get { return GettextCatalog.GetString ("Choose file..."); }
			}
			
			public override string Description {
				get { return GettextCatalog.GetString ("Choose a file from the project."); }
			}
			
			public override string CompletionText {
				get { throw new InvalidOperationException (); }
			}
			
			public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
			{
				string text;
				var dialog = new MonoDevelop.Ide.Projects.ProjectFileSelectorDialog (proj, "", pattern);
				try {
					if (MessageService.RunCustomDialog (dialog) != (int)Gtk.ResponseType.Ok || dialog.SelectedFile == null)
						return;
					text = pathFunc (dialog.SelectedFile);
				}
				finally {
					dialog.Destroy ();
					dialog.Dispose ();
				}
				window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, "", text);
			}
		}
	}
}
