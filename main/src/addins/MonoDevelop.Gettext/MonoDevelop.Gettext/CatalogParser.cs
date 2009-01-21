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

namespace MonoDevelop.Gettext
{
	public abstract class CatalogParser
	{
		//string fileName;
		string loadedFile;
		string[] fileLines;
		string newLine;
		int lineNumber = 0;
		
		public CatalogParser (string fileName, Encoding encoding)
		{
			//this.fileName = fileName;
			loadedFile = File.ReadAllText (fileName, encoding);
			fileLines = CatalogParser.GetLines (loadedFile, out newLine);
		}
		
		// Returns new line constant used in file
		public string NewLine
		{
			get { return newLine; }
		}
		
		static string[] GetLines (string fileStr, out string newLine)
		{
			// TODO: probe first new line...
			int posLN = fileStr.IndexOf ('\n');
			int posCR = fileStr.IndexOf ('\r');
			string useNewLineForParsing = String.Empty;
			newLine = String.Empty;
			
			if (posLN != -1) {
				if (posCR - 1 == posLN) {
					newLine = "\r\n"; //CRLF
				} else if (posCR == -1) {
					newLine = "\n"; //LF
				}
			} else if (posCR != -1 && posLN == -1) {
				newLine = "\r"; //LF
			} else {
				newLine = Environment.NewLine; //mixed for writing use system one
				int countLN = 0;
				int start = 0;
				while ((start = fileStr.IndexOf ('\n', start)) != -1)
					countLN++;
				
				int countCR = 0;
				start = 0;
				while ((start = fileStr.IndexOf ('\r', start)) != -1)
					countCR++;
					
				// for parsing use one with more occurences
				useNewLineForParsing = countCR > countLN ? "\r" : "\n";
			}
			
			if (useNewLineForParsing == String.Empty)
				useNewLineForParsing = newLine;
			
			List<string> lines = new List<string> ();
			
			foreach (string line in fileStr.Split (new string[] {useNewLineForParsing}, StringSplitOptions.None)) {
				lines.Add (line);
			}
			return lines.ToArray ();
		}
		
		// If input begins with pattern, fill output with end of input (without
		// pattern; strips trailing spaces) and return true.  Return false otherwise
		// and don't touch output
		static bool ReadParam (string input, string pattern, out string output)
		{
			output = String.Empty;
			input = input.TrimStart (' ', '\t');
			if (input.Length < pattern.Length)
				return false;
			
			if (! input.StartsWith (pattern))
				return false;
			
			output = StringEscaping.FromGettextFormat (input.Substring (pattern.Length).TrimEnd (' ', '\t'));
			return true;
		}
		
		string ParseMessage (ref string line, ref string dummy, ref int lineNumber)
		{
			StringBuilder result = new StringBuilder (dummy.Substring (0, dummy.Length - 1));
			
			while ((line = fileLines[lineNumber++]) != String.Empty) {
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
			if (fileLines.Length == 0)
				return false;
			
			string line, dummy;
			string mflags = String.Empty;
			string mstr = String.Empty;
			string msgidPlural = String.Empty;
			string mcomment = String.Empty;
			List<string> mrefs = new List<string> ();
			List<string> mautocomments = new List<string> ();
			List<string> mtranslations = new List<string> ();
			bool hasPlural = false;
			
			line = fileLines[lineNumber++];
			if (line == String.Empty)
				line = fileLines[lineNumber++];
			
			while (line != String.Empty)
			{
				// ignore empty special tags (except for automatic comments which we
				// DO want to preserve):
				while (line == "#," || line == "#:")
					line = fileLines[lineNumber++];
				
				// flags:
				// Can't we have more than one flag, now only the last is kept ...
				if (CatalogParser.ReadParam (line, "#, ", out dummy))
				{
					mflags = dummy; //"#, " +
					line = fileLines[lineNumber++];
				}
				
				// auto comments:
				if (CatalogParser.ReadParam (line, "#. ", out dummy) || CatalogParser.ReadParam (line, "#.", out dummy)) // second one to account for empty auto comments
				{
					mautocomments.Add (dummy);
					line = fileLines[lineNumber++];
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
						
						mrefs.Add (dummy.Substring (0, i));
						dummy = dummy.Substring (i).Trim ();
					}
					
					line = fileLines[lineNumber++];
				}
				
				// msgid:
				else if (CatalogParser.ReadParam (line, "msgid \"", out dummy) ||
				         CatalogParser.ReadParam (line, "msgid\t\"", out dummy))
				{
					mstr = ParseMessage (ref line, ref dummy, ref lineNumber);
				}
				
				// msgid_plural:
				else if (CatalogParser.ReadParam (line, "msgid_plural \"", out dummy) ||
				         CatalogParser.ReadParam (line, "msgid_plural\t\"", out dummy))
				{
					msgidPlural = ParseMessage (ref line, ref dummy, ref lineNumber);
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
					
					
					string str = ParseMessage (ref line, ref dummy, ref lineNumber);
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
						
						while ((line = fileLines[lineNumber++]) != String.Empty) {
							if (line[0] == '\t')
								line = line.Substring (1);
							if (line[0] == '"' && line[line.Length - 1] == '"') {
								str.Append (line.Substring (1, line.Length - 2));
							} else {
								if (ReadParam (line, "msgstr[", out dummy)) {
									pos = dummy.IndexOf (']');
									idx = dummy.Substring (pos - 1, 1);
									label = "msgstr[" + idx + "]";
								}
								break;
							}
						}
						mtranslations.Add (str.ToString ());
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
					while ((line = fileLines[lineNumber++]) != String.Empty) {
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
				} else if (line[0] == '#') {
					// comment:
					
					while (line != String.Empty &&
					       ((line[0] == '#' && line.Length < 2) ||
						   (line[0] == '#' && line[1] != ',' && line[1] != ':' && line[1] != '.')))
					{
						mcomment += mcomment.Length > 0 ? '\n' + line : line;
						line = fileLines[lineNumber++];
					}
				} else {
					line = fileLines[lineNumber++];
					
				}
				
				while (line == String.Empty && lineNumber < fileLines.Length)
					line = fileLines[lineNumber++];
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