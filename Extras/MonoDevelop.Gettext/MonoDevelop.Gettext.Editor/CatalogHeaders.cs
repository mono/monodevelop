//
// CatalogHeaders.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 1999-2006 Vaclav Slavik (Code and design inspiration - poedit.org)
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
using System.Text;

namespace MonoDevelop.Gettext.Editor
{
	public class CatalogHeaders
	{
		static readonly string version;
		const string AddinName = "MonoDevelop.Gettext";
		
		Dictionary<string, string> entries;
		Catalog owner;

		// Parsed values
		public string Project = String.Empty, CreationDate = String.Empty,
					  RevisionDate = String.Empty, Translator = String.Empty,
					  TranslatorEmail = String.Empty, Team = String.Empty,
					  TeamEmail = String.Empty, Charset = String.Empty,
					  Language = String.Empty, Country = String.Empty, // lang + country not yet used
					  Comment = String.Empty;

		static CatalogHeaders ()
		{
			foreach (Mono.Addins.Addin addin in Mono.Addins.AddinRegistry.GetGlobalRegistry ().GetAddins ())
			{
				if (addin.Id == AddinName)
				{
					version = addin.Version;
					break;
				}
			}
		}
		
		public CatalogHeaders (Catalog owner)
		{
			this.owner = owner;
			this.entries = new Dictionary<string, string> ();
		}

		// converts \n into newline character and \\ into \:
		static string UnescapeCEscapes (string source)
		{
			StringBuilder sb = new StringBuilder ();
			int pos;

			if (String.IsNullOrEmpty (source))
				return source;

			for (pos = 0; pos < source.Length - 1; pos++)
			{
				if (source[pos] == '\\')
				{
					switch (source[pos + 1])
					{
						case 'n':
							sb.Append ('\n');
							pos++;
							break;
						case '\\':
							sb.Append ('\\');
							pos++;
							break;
						default:
							sb.Append ('\\');
							break;
					}
				} else
				{
					sb.Append (source[pos]);
				}
			}

			// last character
			if (pos < source.Length)
				sb.Append (source[pos]);

			return sb.ToString ();
		}
		
		// Initializes the headers from string that is in msgid "" format (i.e. list of key:value\n entries).
		public void FromString (string headers)
		{
			string hdr = CatalogHeaders.UnescapeCEscapes (headers);
			string[] tokens = hdr.Split ('\n');
			entries.Clear ();

			foreach (string token in tokens)
			{
				if (token != String.Empty)
				{
					int pos = token.IndexOf (':');
					if (pos == -1)
					{
						throw new Exception (String.Format ("Malformed header: '{0}'", token));
					} else
					{
						string key = token.Substring (0, pos).Trim ();
						string value = token.Substring (pos + 1).Trim ();
						entries[key] = value;
					}
				}
			}
			ParseDict ();
		}

		// Converts the header into string representation that can be directly written to .po file as msgid ""
		public string ToString (string lineDelimeter)
		{
			UpdateDict ();
			StringBuilder sb = new StringBuilder ();

			foreach (string key in entries.Keys)
			{
				string value = String.Empty;
				if (entries[key] != null)
					value = entries[key].Replace ("\\", "\\\\");
				sb.AppendFormat ("\"{0}: {1}\\n\"{2}", key, value, lineDelimeter);

			}
			return sb.ToString ();
		}

		public override string ToString ()
		{
			return ToString (Environment.NewLine);
		}

		// Updates headers list from parsed values entries below
		public void UpdateDict ()
		{
			SetHeader ("Project-Id-Version", Project);
			SetHeader ("POT-Creation-Date", CreationDate);
			SetHeader ("PO-Revision-Date", RevisionDate);

			if (String.IsNullOrEmpty (TranslatorEmail))
			{
				SetHeader ("Last-Translator", Translator);
			} else
			{
				SetHeader ("Last-Translator", String.Format ("{0} <{1}>", Translator, TranslatorEmail));
			}
			if (String.IsNullOrEmpty (TeamEmail))
			{
				SetHeader ("Language-Team", Team);
			} else
			{
				SetHeader ("Language-Team", String.Format ("{0} <{1}>", Team, TeamEmail));
			}

			SetHeader ("MIME-Version", "1.0");
			SetHeader ("Content-Type", "text/plain; charset=" + Charset);
			SetHeader ("Content-Transfer-Encoding", "8bit");
			
			SetHeader ("X-Generator", String.Format ("{0} {1}", AddinName, version));
		}

		// Reverse operation to UpdateDict
		void ParseDict ()
		{
			string dummy;

			Project = GetHeader ("Project-Id-Version");
			CreationDate = GetHeader ("POT-Creation-Date");
			RevisionDate = GetHeader ("PO-Revision-Date");

			dummy = GetHeader ("Last-Translator");
			if (!String.IsNullOrEmpty (dummy))
			{
				string[] tokens = dummy.Split ('<', '>');
				if (tokens.Length < 2)
				{
					Translator = dummy;
					TranslatorEmail = String.Empty;
				} else
				{
					Translator = tokens[0].Trim ();
					TranslatorEmail = tokens[1];
				}
			}

			dummy = GetHeader ("Language-Team");
			if (!String.IsNullOrEmpty (dummy))
			{
				string[] tokens = dummy.Split ('<', '>');
				if (tokens.Length < 2)
				{
					Team = dummy;
					TeamEmail = String.Empty;
				} else
				{
					Team = tokens[0].Trim ();
					TeamEmail = tokens[1];
				}
			}

			string ctype = GetHeader ("Content-Type");
			int pos = ctype.IndexOf ("; charset=");
			if (pos != -1)
			{
				Charset = ctype.Substring (pos + "; charset=".Length).Trim ();
				;
			} else
			{
				Charset = "iso-8859-1";
			}
		}

		// Returns value of header or empty string if missing.
		public string GetHeader (string key)
		{
			if (entries.ContainsKey (key))
				return entries[key];
			else
				return String.Empty;
		}

		// Returns true if given key is present in the header.
		public bool HasHeader (string key)
		{
			return entries.ContainsKey (key);
		}

		// Sets header to given value. Overwrites old value if present, appends to header values otherwise.
		public void SetHeader (string key, string value)
		{
			entries[key] = value;
			
			if (key == "Plural-Forms" && owner != null)
				owner.UpdatePluralsCount ();
		}

		// Like SetHeader, but deletes the header if value is empty
		public void SetHeaderNotEmpty (string key, string value)
		{
			if (String.IsNullOrEmpty (value))
				DeleteHeader (key);
			else
				SetHeader (key, value);
			
			if (key == "Plural-Forms" && owner != null)
				owner.UpdatePluralsCount ();
		}

		// Removes given header entry
		public void DeleteHeader (string key)
		{
			if (HasHeader (key))
			{
				entries.Remove (key);
			}
		}
		
		public string CommentForGui
		{
			get
			{
				if (String.IsNullOrEmpty (Comment))
					return string.Empty;
				
				StringBuilder sb = new StringBuilder ();
				bool first = true;
				foreach (string line in Comment.Split ('\n'))
				{
					if (! first)
						sb.Append ('\n');
					else
						first = false;
					
					if (line.StartsWith ("#"))
						sb.Append (line.Substring (1).TrimStart (' ', '\t'));
					else
						sb.Append (line.TrimStart (' ', '\t'));
				}
				return sb.ToString ();
			}
			set
			{
				if (String.IsNullOrEmpty (value))
				{
					Comment = String.Empty;
					return;
				}
				
				StringBuilder sb = new StringBuilder ();
				bool first = true;
				foreach (string line in value.Split ('\n'))
				{
					if (! first)
						sb.Append ('\n');
					else
						first = false;
					sb.Append ("# " + line);
				}
				Comment = sb.ToString ();
			}
		}
		
		public Catalog Owner
		{
			get { return owner; }
		}
	}
}
