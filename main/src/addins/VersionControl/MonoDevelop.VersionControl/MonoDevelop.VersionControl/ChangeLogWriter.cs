//
// ChangeLogWriter.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Text;

using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	class ChangeLogWriter
	{
		private Dictionary<string, List<string>> messages = new Dictionary<string, List<string>> ();
		private string changelog_path;
		AuthorInformation uinfo;
	
		public ChangeLogWriter (string path, AuthorInformation uinfo)
		{
			changelog_path = Path.GetDirectoryName (path);
			this.uinfo = uinfo;
		}
	
		public void AddFile (string message, string path)
		{
			if (SkipEmpty && string.IsNullOrEmpty (message)) {
				return;
			}
			
			string relative_path = GetRelativeEntryPath (path);
			if (relative_path == null) {
				return;
			}
			
			List<string> path_list;
			if (!messages.TryGetValue (message, out path_list)) {
				path_list = new List<string> ();
				messages.Add (message, path_list);
			}
			
			if (!path_list.Contains (relative_path)) {
				path_list.Add (relative_path);
			}
		}
		
		private string GetRelativeEntryPath (string path)
		{
			if (!path.StartsWith (changelog_path)) {
				return null;
			}
			
			return path == changelog_path ? "." : path.Substring (changelog_path.Length + 1);
		}
		
		public override string ToString ()
		{
			if (messages.Count == 0)
				return string.Empty;

			CommitMessageStyle message_style = MessageFormat.Style;
			
			TextFormatter formatter = new TextFormatter ();
			formatter.MaxColumns = MessageFormat.MaxColumns;
			formatter.TabWidth = MessageFormat.TabWidth;
			formatter.TabsAsSpaces = MessageFormat.TabsAsSpaces;
			
			if (message_style.Header.Length > 0) {
				string [,] tags = new string[,] { {"AuthorName", uinfo.Name}, {"AuthorEmail", uinfo.Email} };
				formatter.Append (StringParserService.Parse (message_style.Header, tags));
			}
			
			formatter.IndentString = message_style.Indent;
			
			int m_i = 0;
			
			string fileSeparator1 = message_style.FileSeparator;
			string fileSeparator2 = string.Empty;
			
			int si = message_style.FileSeparator.IndexOf ('\n');
			if (si != -1 && si < message_style.FileSeparator.Length - 1) {
				fileSeparator1 = message_style.FileSeparator.Substring (0, si + 1);
				fileSeparator2 = message_style.FileSeparator.Substring (si + 1);
			}
			
			formatter.Wrap = WrappingType.Word;
			formatter.LeftMargin = message_style.LineAlign;
			formatter.ParagraphStartMargin = 0;
			
			foreach (KeyValuePair<string, List<string>> message in messages) {
				List<string> paths = message.Value;
				paths.Sort ((a, b) => a.Length.CompareTo (b.Length));
				
				formatter.BeginWord ();
				
				formatter.Append (message_style.FirstFilePrefix);
				for (int i = 0, n = paths.Count; i < n; i++) {
					if (i > 0) {
						formatter.Append (fileSeparator1);
						formatter.EndWord ();
						formatter.BeginWord ();
						formatter.Append (fileSeparator2);
					}
					formatter.Append (paths [i]);
				}
				
				formatter.Append (message_style.LastFilePostfix);
				formatter.EndWord ();
				formatter.Append (message.Key);
				
				if (m_i++ < messages.Count - 1) {
					formatter.AppendLine ();
					for (int n=0; n < message_style.InterMessageLines; n++)
						formatter.AppendLine ();
				}
			}
			
			for (int i = 0; i < MessageFormat.AppendNewlines; i++)
				formatter.AppendLine ();
			
			return formatter.ToString ();
		}
		
		public bool SkipEmpty { get; set; }
		
		public CommitMessageFormat MessageFormat { get; set; }
	}
}
