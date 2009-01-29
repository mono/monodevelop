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

using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	class ChangeLogWriter
	{
		private Dictionary<string, List<string>> messages = new Dictionary<string, List<string>> ();
		private string changelog_path;
		UserInformation uinfo;
	
		public ChangeLogWriter (string path, UserInformation uinfo)
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
				
			StringBuilder builder = new StringBuilder ();
			
			CommitMessageStyle message_style = MessageFormat.Style;
			
			if (message_style.Header.Length > 0) {
				string [,] tags = new string[,] { {"UserName", uinfo.Name}, {"UserEmail", uinfo.Email} };
				builder.Append (StringParserService.Parse (message_style.Header, tags));
			}
			
			int m_i = 0;
			
			foreach (KeyValuePair<string, List<string>> message in messages) {
				List<string> paths = message.Value;
				paths.Sort ((a, b) => a.Length.CompareTo (b.Length));
				
				StringBuilder sb = new StringBuilder ();
				for (int i = 0, n = paths.Count; i < n; i++) {
					if (i == 0)
						sb.Append (message_style.FirstFilePrefix);
					else
						sb.Append (message_style.FileSeparator);
					sb.Append (paths [i]);
				}
				sb.Append (message_style.LastFilePostfix);
				sb.Append (message.Key);
				string files = sb.ToString ();
				int e = files.LastIndexOf ('\n');
				
				string fileListEnd;
				if (e != -1) {
					string fileList = files.Substring (0, e);
					fileListEnd = files.Substring (e + 1);
					fileList = message_style.Indent + fileList.Replace ("\n", "\n" + message_style.Indent);
					builder.Append (fileList).AppendLine ();
				} else
					fileListEnd = files;
				
				builder.Append (FormatText (fileListEnd, message_style.Indent, 0, message_style.LineAlign, MessageFormat.MaxColumns, MessageFormat.TabWidth, MessageFormat.TabsAsSpaces));
				
				if (m_i++ < messages.Count - 1) {
					builder.AppendLine ();
					for (int n=0; n < message_style.InterMessageLines; n++)
						builder.AppendLine ();
				}
			}
			
			return builder.ToString ();
		}
		
		static string FormatText (string text, string indentString, int initialLeftMargin, int leftMargin, int maxCols, int tabWidth, bool tabsAsSpaces)
		{
			if (text == "")
				return "";
			
			int indentWidth = 0;
			foreach (char c in indentString) {
				if (c == '\t') indentWidth += tabWidth;
				else indentWidth++;
			}
			
			int n = 0;
			int margin = initialLeftMargin;
			
			StringBuilder outs = new StringBuilder ();
			
			while (n < text.Length)
			{
				int col = margin + indentWidth;
				int lastWhite = -1;
				int sn = n;
				bool forcedLineBreak = false;
				while ((col < maxCols || lastWhite==-1) && n < text.Length) {
					if (char.IsWhiteSpace (text[n]))
						lastWhite = n;
					if (text[n] == '\n') {
						lastWhite = n;
						n++;
						forcedLineBreak = true;
						break;
					}
					col++;
					n++;
				}
				
				if ((lastWhite == -1 || col < maxCols) && !forcedLineBreak)
					lastWhite = n;
				else if (col >= maxCols)
					n = lastWhite + 1;
				
				string line = text.Substring (sn, lastWhite - sn);
				if (line.Length > 0 || n < text.Length) {
					if (outs.Length > 0) outs.Append ('\n');
					outs.Append (indentString + new String (' ', margin) + line);
				}
				margin = leftMargin;
			}
			if (tabsAsSpaces)
				outs.Replace ("\t", new string (' ', tabWidth));
			return outs.ToString ();
		}
		
		public bool SkipEmpty { get; set; }
		
		public CommitMessageFormat MessageFormat { get; set; }
	}
}
