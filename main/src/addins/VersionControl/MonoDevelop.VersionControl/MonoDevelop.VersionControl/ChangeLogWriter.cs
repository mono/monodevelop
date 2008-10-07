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

using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public class ChangeLogWriter
	{
		private Dictionary<string, List<string>> messages = new Dictionary<string, List<string>> ();
		private string changelog_path;
	
		public ChangeLogWriter (string path)
		{
			changelog_path = Path.GetDirectoryName (path);
		}
	
		public void AddFile (string message, string path)
		{
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
			StringBuilder builder = new StringBuilder ();
			
			if (WriteHeader) {
				builder.AppendFormat ("{0}  {1}  <{2}>", 
					DateTime.Now.ToString ("yyyy-MM-dd"), 
					FullName, EmailAddress);
				builder.AppendLine ();
				builder.AppendLine ();
			}
			
			string lead_indent = WriteHeader ? "\t" : string.Empty;
			string wrap_indent = string.Format ("{0}  ", lead_indent);
			
			foreach (KeyValuePair<string, List<string>> message in messages) {
				List<string> paths = message.Value;
				paths.Sort ((a, b) => a.Length.CompareTo (b.Length));
				
				for (int i = 0, n = paths.Count; i < n; i++) {
					if (i < n - 1) {
						builder.Append (lead_indent);
						builder.AppendFormat ("* {0}:", message.Value[i]);
						builder.AppendLine ();
						continue;
					}
					
					builder.Append (lead_indent);
					builder.Append ("* ");
					WrapAlign (builder, string.Format ("{0}: {1}", paths[i], message.Key), 70, wrap_indent, 1, true);
					builder.AppendLine ();
					builder.AppendLine ();
				}
			}
			
			return builder.ToString ().Trim ();
		}
		
		// Adapted from Hyena.CommandLine.Layout (Banshee)
		private static StringBuilder WrapAlign (StringBuilder builder, string str, int width, 
			string indent, int align, bool last)
		{
			bool did_wrap = false;
			
			for (int i = 0, b = 0; i < str.Length; i++, b++) {
				if (str[i] == ' ') {
					int word_length = 0;
					for (int j = i + 1; j < str.Length && str[j] != ' '; word_length++, j++);
					
					if (b + word_length >= width) {
						builder.AppendLine ();
						for (int k = 0; k < align; k++) {
							builder.Append (indent);
						}
						b = 0;
						did_wrap = true;
						continue;
					}
				}
				
				builder.Append (str[i]);
			}
			
			if (did_wrap && !last) {
				builder.AppendLine ();
			}
			
			return builder;
		}
				
		private string full_name;
		public string FullName {
			get { return full_name; }
			set { full_name = value; }
		}
		
		private string email_address;
		public string EmailAddress {
			get { return email_address; }
			set { email_address = value; }
		}
		
		private bool write_header;
		public bool WriteHeader {
			get { return write_header; }
			set { write_header = value; }
		}
	}
}
