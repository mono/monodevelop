//
// CatalogDeletedEntry.cs
//
// Author:
//   David Makovsk� <yakeen@sannyas-on.net>
//
// Copyright (C) 1999-2006 Vaclav Slavik (Code and design inspiration - poedit.org)
// Copyright (C) 2007 David Makovsk�
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
using System.Text;

namespace MonoDevelop.Gettext
{
	// This class holds information about one particular deleted item.
	// This includes deleted lines, references, translation's status
	// (fuzzy, non translated, translated) and optional comment(s).
	class CatalogDeletedEntry
	{
		List<string> deletedLines;
		List<string> references;
		List<string> autocomments;
		string flags;
		string comment;
		
		// Initializes the object with original string and translation.
		public CatalogDeletedEntry (string[] deletedLines)
		{
			this.deletedLines = new List<string> (deletedLines);
			references = new List<string> ();
			autocomments = new List<string> ();
		}
		
		public CatalogDeletedEntry (CatalogDeletedEntry dt)
		{
			deletedLines = new List<string> (dt.deletedLines);
			references = new List<string> (dt.references);
			autocomments = new List<string> (dt.autocomments);
			flags = dt.flags;
			comment = dt.comment;
		}
		
		// Returns the deleted lines.
		public string[] DeletedLines
		{
			get { return deletedLines.ToArray (); }
		}
		
		// Returns array of all occurences of this string in source code.
		public string[] References
		{
			get { return references.ToArray (); }
		}
		
		// Returns comment added by the translator to this entry
		public string Comment
		{
			get { return comment; }
		}
		
		// Returns array of all auto comments.
		public string[] AutoComments
		{
			get { return autocomments.ToArray (); }
		}
		
		// Convenience function: does this entry has a comment?
		public bool HasComment
		{
			get { return ! String.IsNullOrEmpty (comment); }
		}
		
		// Adds new reference to the entry (used by SourceDigger).
		public void AddReference (string reference)
		{
			if (! references.Contains (reference))
				references.Add (reference);
		}
		
		// Clears references (used by SourceDigger).
		public void ClearReferences ()
		{
			references.Clear ();
		}
		
		// Sets the string.
		public void SetDeletedLines (string[] lines)
		{
			deletedLines = new List<string> (lines);
		}

		// Sets the comment.
		public void SetComment (string comment)
		{
			this.comment = comment;
		}

		// Sets gettext flags directly in string format. It may be
		// either empty string or "#, fuzzy", "#, c-format",
		// "#, fuzzy, c-format" or others (not understood by poEdit).
		public string Flags {
			get {
				if (String.IsNullOrEmpty (flags))
					return String.Empty;
				if (flags.StartsWith ("#,"))
					return flags;
				else
					return "#, " + flags;
			}
			set { flags = value; }
		}
		
		// Adds new autocomments (#. )
		public void AddAutoComments (string comment)
		{
			autocomments.Add (comment);
		}

		// Clears autocomments.
		public void ClearAutoComments ()
		{
			autocomments.Clear ();
		}
	}
}
