//
// CatalogParser.cs
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
using System.IO;
using System.Linq; //GB Added

namespace MonoDevelop.Gettext
{
	//FIXME: StreamReader has been implemeneted but not a real parser
	abstract class CatalogParser
	{
		internal static readonly string[] LineSplitStrings = { "\r\n", "\r", "\n" };
		
		string newLine;
		string fileName;
		Encoding encoding;
		
		public CatalogParser (string fileName, Encoding encoding)
		{
			
			newLine = GetNewLine (fileName,encoding);
			
			// parse command will open file later through streamreader
			this.fileName = fileName;
			this.encoding = encoding;
			
		}
		
		// Returns new line constant used in file
		public string NewLine
		{
			get { return newLine; }
		}
		
		// Detects the characters used for newline from the 1st newline present in file
		static string GetNewLine (string fileName, Encoding encoding) 
		{
			
			char foundchar = 'x';
			char[] curr = new char[] {'x'};
			
			using (TextReader tr = new StreamReader(File.Open(fileName,FileMode.Open),encoding))
			{
				while (tr.Read (curr,0,1) != 0)
				{
					if (curr[0] == '\n') {
						if (foundchar == '\r')
							return "\r\n";
						else 
							return "\n";
					} else if (curr[0] == '\r') {
						if (foundchar == 'x')
							foundchar = '\r';
						else if (foundchar == '\r')
							return  "\r";
					}
					else if (foundchar != 'x')
							return foundchar.ToString ();
				}
			}
			
			// only gets here if EOF reached	
			if (foundchar != 'x')
						return foundchar.ToString ();
			else
				return Environment.NewLine;
		}
		
		// If input begins with pattern, fill output with end of input (without
		// pattern; strips trailing spaces) and return true.  Return false otherwise
		// or if input is null
		static bool ReadParam (string input, string pattern, out string output)
		{
			output = String.Empty;
			
			if (input == null)
				return false;
			
			input = input.TrimStart (' ', '\t');
			if (input.Length < pattern.Length)
				return false;
			
			if (! input.StartsWith (pattern))
				return false;
			
			output = StringEscaping.FromGettextFormat (input.Substring (pattern.Length).TrimEnd (' ', '\t'));
			return true;
		}
		
		// returns value in dummy plus any trailing lines in sr enclosed in quotes
		// next line is ready for parsing by function end
		string ParseMessage (ref string line, ref string dummy, StreamReader sr)
		{
			StringBuilder result = new StringBuilder (dummy.Substring (0, dummy.Length - 1));
			
			while (!String.IsNullOrEmpty(line = sr.ReadLine ())) {
				if (line[0] == '\t') 
					line = line.Substring (1);
				
				if (line[0] == '"' && line[line.Length - 1] == '"') { 
					result.Append (StringEscaping.FromGettextFormat (line.Substring (1, line.Length - 2)));
				} else
					break;
			}
			return result.ToString ();
		}
		
		// Parses the entire file, calls OnEntry each time msgid/msgstr pair is found.
		// return false if parsing failed, true otherwise
		public bool Parse ()
		{
			
			string line, dummy;
			string mflags = String.Empty;
			string mstr = String.Empty;
			string msgidPlural = String.Empty;
			string mcomment = String.Empty;
			List<string> mrefs = new List<string> ();
			List<string> mautocomments = new List<string> ();
			List<string> mtranslations = new List<string> ();
			bool hasPlural = false;
			
			using (StreamReader sr = new StreamReader(fileName,encoding))
			{
				line = sr.ReadLine ();
				
				while (line == "")
					line = sr.ReadLine ();
				
				if (line == null)
					return false;
				
				while (line != null)
				{
					// ignore empty special tags (except for automatic comments which we
					// DO want to preserve):
					while (line == "#," || line == "#:")
						line = sr.ReadLine ();
					
					// flags:
					// Can't we have more than one flag, now only the last is kept ...
					if (CatalogParser.ReadParam (line, "#, ", out dummy))
					{
						mflags = dummy; //"#, " +
						line = sr.ReadLine ();
					}
					
					// auto comments:
					if (CatalogParser.ReadParam (line, "#. ", out dummy) || CatalogParser.ReadParam (line, "#.", out dummy)) // second one to account for empty auto comments
					{
						mautocomments.Add (dummy);
						line = sr.ReadLine ();
					}
					
					// references:
					else if (CatalogParser.ReadParam (line, "#: ", out dummy))
					{
						// A line may contain several references, separated by white-space.
						// Each reference is in the form "path_name:line_number"
						// (path_name may contain spaces)
						dummy = dummy.Trim ();
						while (dummy != String.Empty) {
							int i = 0;
							while (i < dummy.Length && dummy[i] != ':') {
								i++;
							}
							while (i < dummy.Length && ! Char.IsWhiteSpace (dummy[i])) {
								i++;
							}
							
							//store paths as Unix-type paths, but internally use native style
							string refpath = dummy.Substring (0, i);
							if (MonoDevelop.Core.Platform.IsWindows) {
								refpath = refpath.Replace ('/', '\\');
							}
							
							mrefs.Add (refpath);
							dummy = dummy.Substring (i).Trim ();
						}
						
						line = sr.ReadLine ();
					}
					
					// msgid:
					else if (CatalogParser.ReadParam (line, "msgid \"", out dummy) ||
					         CatalogParser.ReadParam (line, "msgid\t\"", out dummy))
					{
						mstr = ParseMessage (ref line, ref dummy, sr);
					}
					
					// msgid_plural:
					else if (CatalogParser.ReadParam (line, "msgid_plural \"", out dummy) ||
					         CatalogParser.ReadParam (line, "msgid_plural\t\"", out dummy))
					{
						msgidPlural = ParseMessage (ref line, ref dummy, sr);
						hasPlural = true;
					}
	
					// msgstr:
					else if (CatalogParser.ReadParam (line, "msgstr \"", out dummy) ||
					         CatalogParser.ReadParam (line, "msgstr\t\"", out dummy))
					{
						if (hasPlural) {
							// TODO: use logging
							Console.WriteLine ("Broken catalog file: singular form msgstr used together with msgid_plural");
							return false;
						}
						
						string str = ParseMessage (ref line, ref dummy, sr);
						mtranslations.Add (str);
						
						if (! OnEntry (mstr, String.Empty, false, mtranslations.ToArray (),
						               mflags, mrefs.ToArray (), mcomment,
						               mautocomments.ToArray ()))
						{
							return false;
						}
						
						mcomment = mstr = msgidPlural = mflags = String.Empty;
						hasPlural = false;
						mrefs.Clear ();
						mautocomments.Clear ();
						mtranslations.Clear ();
					} else if (CatalogParser.ReadParam (line, "msgstr[", out dummy)) {
						// msgstr[i]:
						if (!hasPlural){
							// TODO: use logging
							Console.WriteLine ("Broken catalog file: plural form msgstr used without msgid_plural");
							return false;
						}
						
						int pos = dummy.IndexOf (']');
						string idx = dummy.Substring (pos - 1, 1);
						string label = "msgstr[" + idx + "]";
						
						while (CatalogParser.ReadParam (line, label + " \"", out dummy) || CatalogParser.ReadParam (line, label + "\t\"", out dummy)) {
							StringBuilder str = new StringBuilder (dummy.Substring (0, dummy.Length - 1));
							
							while (!String.IsNullOrEmpty (line = sr.ReadLine ())) {
								if (line[0] == '\t')
									line = line.Substring (1);
								if (line[0] == '"' && line[line.Length - 1] == '"') {
									str.Append (line, 1, line.Length - 2);
								} else {
									if (ReadParam (line, "msgstr[", out dummy)) {
										pos = dummy.IndexOf (']');
										idx = dummy.Substring (pos - 1, 1);
										label = "msgstr[" + idx + "]";
									}
									break;
								}
							}
							mtranslations.Add (StringEscaping.FromGettextFormat (str.ToString ()));
						}
						
						if (! OnEntry (mstr, msgidPlural, true, mtranslations.ToArray (),
						               mflags, mrefs.ToArray (), mcomment,
						               mautocomments.ToArray ()))
						{
							return false;
						}
						
						mcomment = mstr = msgidPlural = mflags = String.Empty;
						hasPlural = false;
						mrefs.Clear ();
						mautocomments.Clear ();
						mtranslations.Clear ();
					}else if (CatalogParser.ReadParam (line, "#~ ", out dummy)) {
						// deleted lines:
						
						List<string> deletedLines = new List<string> ();
						deletedLines.Add (line);
						while (!String.IsNullOrEmpty (line = sr.ReadLine ())) {
							// if line does not start with "#~ " anymore, stop reading
							if (! ReadParam (line, "#~ ", out dummy))
								break;
	
							deletedLines.Add (line);
						}
						if (! OnDeletedEntry (deletedLines.ToArray (), mflags, null, mcomment, mautocomments.ToArray ())) 
							return false;
						
						mcomment = mstr = msgidPlural = mflags = String.Empty;
						hasPlural = false;
						mrefs.Clear ();
						mautocomments.Clear ();
						mtranslations.Clear ();
					} else if (line != null && line[0] == '#') {
						// comment:
						
						//  added line[1] != '~' check as deleted lines where being wrongly detected as comments
						while (!String.IsNullOrEmpty (line) &&
						       ((line[0] == '#' && line.Length < 2) ||
							   (line[0] == '#' && line[1] != ',' && line[1] != ':' && line[1] != '.' && line[1] != '~'))) 
						{
							mcomment += mcomment.Length > 0 ? '\n' + line : line;
							line = sr.ReadLine ();
						}
					} else {
						line = sr.ReadLine ();
						
					}
					
					while (line == String.Empty)
						line = sr.ReadLine ();
				}
			}
			return true;
		}
		
		// Called when new entry was parsed. Parsing continues
		// if returned value is true and is cancelled if it is false.
		protected abstract bool OnEntry (string msgid, string msgidPlural, bool hasPlural,
		                                 string[] translations, string flags,
		                                 string[] references, string comment,
		                                 string[] autocomments);

		// Called when new deleted entry was parsed. Parsing continues
		// if returned value is true and is cancelled if it
		// is false. Defaults to an empty implementation.
		protected virtual bool OnDeletedEntry (string[] deletedLines, string flags,
		                                       string[] references, string comment,
		                                       string[] autocomments)
		{
			return true;
		}
	}
	
	internal class CharsetInfoFinder : CatalogParser
	{
		string charset;
		TranslationProject project;
		
		// Expecting iso-8859-1 encoding
		public CharsetInfoFinder (TranslationProject project, string poFile)
			: base (poFile, Encoding.GetEncoding ("iso-8859-1"))
		{
			this.project = project;
			charset = "iso-8859-1";
		}
		
		public string Charset {
			get { 
				return charset; 
			}
		}
		
		protected override bool OnEntry (string msgid, string msgidPlural, bool hasPlural,
		                                 string[] translations, string flags,
		                                 string[] references, string comment,
		                                 string[] autocomments)
		{
			if (String.IsNullOrEmpty (msgid)) {
				// gettext header:
				Catalog headers = new Catalog (project);
				headers.ParseHeaderString (translations[0]);
				charset = headers.Charset;
				if (charset == "CHARSET")
					charset = "iso-8859-1";
				return false; // stop parsing
			}
			return true;
		}
	}
	
	internal class LoadParser : CatalogParser
	{
		Catalog catalog;
		bool headerParsed = false;
		
		public LoadParser (Catalog catalog, string poFile, Encoding encoding) : base (poFile, encoding)
		{
			this.catalog = catalog;
		}
		
		protected override bool OnEntry (string msgid, string msgidPlural, bool hasPlural,
		                                 string[] translations, string flags,
		                                 string[] references, string comment,
		                                 string[] autocomments)
		{
			if (String.IsNullOrEmpty (msgid) && ! headerParsed) {
				// gettext header:
				catalog.ParseHeaderString (translations[0]);
				catalog.Comment = comment;
				headerParsed = true;
			} else {
				CatalogEntry d = new CatalogEntry (catalog, String.Empty, String.Empty);
				if (! String.IsNullOrEmpty (flags))
					d.Flags = flags;
				d.SetString (msgid);
				if (hasPlural)
				    d.SetPluralString (msgidPlural);
				d.SetTranslations (translations);
				d.Comment = comment;
				for (uint i = 0; i < references.Length; i++) {
					d.AddReference (references[i]);
				}
				for (uint i = 0; i < autocomments.Length; i++) {
					d.AddAutoComment (autocomments[i]);
				}
				catalog.AddItem (d);
			}
			return true;
		}
		
		 protected override bool OnDeletedEntry (string[] deletedLines, string flags,
		                                        string[] references, string comment,
		                                        string[] autocomments)
		{
			CatalogDeletedEntry d = new CatalogDeletedEntry (new string[0]);
			if (!String.IsNullOrEmpty (flags))
				d.Flags = flags;
			d.SetDeletedLines (deletedLines);
			d.SetComment (comment);
			for (uint i = 0; i < autocomments.Length; i++) {
				d.AddAutoComments (autocomments[i]);
				
			}
			catalog.AddDeletedItem (d);
			return true;
		}
	}
}