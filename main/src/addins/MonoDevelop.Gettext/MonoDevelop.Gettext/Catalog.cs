//
// Catalog.cs
//
// Author:
//   David Makovsk <yakeen@sannyas-on.net>
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 1999-2006 Vaclav Slavik (Code and design inspiration - poedit.org)
// Copyright (C) 2007 David Makovsk
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

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Gettext
{
	class Catalog : IEnumerable<CatalogEntry>
	{
		IDictionary<string, CatalogEntry> entriesDict;
		List<CatalogEntry>        entriesList;
		List<CatalogDeletedEntry> deletedEntriesList;
		bool isOk;
		string fileName;
		string originalNewLine = Environment.NewLine;
		bool isDirty;
		int nplurals = 0;
		TranslationProject parentProj;
		
		public bool IsDirty {
			get { return this.isDirty; }
			set {
				this.isDirty = value;
				OnDirtyChanged (EventArgs.Empty);
			}
		}
		
		public string FileName {
			get {
				return fileName;
			}
		}
		
		/// <value>
		/// The number of strings/translations in the catalog.
		/// </value>
		public int Count {
			get { 
				return entriesList.Count; 
			}
		}
		
		
		/// <value>
		/// Gets n-th item in the catalog (read-write access).
		/// </value>
		public CatalogEntry this[int index] {
			get {
				return index >= 0 && index < entriesList.Count ? entriesList[index] : null;
			}
		}
		
		/// <value>
		/// Returns plural forms count: taken from Plural-Forms header if present, 0 otherwise
		/// </value>
		public int PluralFormsCount {
			get {
				if (nplurals == 0)
					UpdatePluralsCount ();
				return nplurals;
			}
		}
		
		/// <summary>
		/// Creates empty catalog
		/// </summary>
		public Catalog (TranslationProject project)
		{
			parentProj = project;
			entriesDict = new Dictionary<string, CatalogEntry> ();
			entriesList = new List<CatalogEntry> ();
			deletedEntriesList = new List<CatalogDeletedEntry> ();
			isOk = true;
			this.CreateNewHeaders (project);
		}
		
		
		static string GetDateTimeRfc822Format ()
		{
			return DateTime.Now.ToString ("yyyy-MM-dd HH':'mm':'sszz00"); //rfc822 format
		}
		
		//escapes string and lays it out to 80 cols
		static void FormatMessageForFile (StreamWriter sw, string prefix, string message)
		{
			string escaped = StringEscaping.ToGettextFormat (message);
			
			//format to 80 cols
			//first the simple case: does it fit one one line, with the prefix, and contain no newlines?
			if (prefix.Length + escaped.Length < 77  && !escaped.Contains ("\\n")) {
				sw.Write (prefix);
				sw.Write (" \"");
				sw.Write (escaped);
				sw.Write ("\"");
				sw.WriteLine ();
				return;
			}
						
			//not the simple case.
			
			// first line is typically: prefix ""
			sw.Write (prefix);
			sw.Write (" \"\"");
			sw.WriteLine ();
			
			//followed by 80-col width break on spaces
			int possibleBreak = -1;
			int currLineLen = 0;
			int lastBreakAt = 0;
			bool forceBreak = false;
			
			int pos = 0;
			while (pos < escaped.Length) {
				char c = escaped[pos];
				
				//handle escapes			
				if (c == '\\' && pos+1 < escaped.Length) {
					pos++;
					currLineLen++;
					
					char c2 = escaped[pos];
					if (c2 == 'n') {
						possibleBreak = pos+1;
						forceBreak = true;
					} else if (c2 == 't') {
						possibleBreak = pos+1;
					}
				}
							
				if (c == ' ')
					possibleBreak = pos + 1;
				
				if (forceBreak || (currLineLen >= 77 && possibleBreak != -1)) {
					sw.Write ("\"");
					sw.Write (escaped.Substring (lastBreakAt, possibleBreak - lastBreakAt));
					sw.Write ("\"");
					sw.WriteLine ();
					
					//reset state for new line
					currLineLen = 0;
					lastBreakAt = possibleBreak;
					possibleBreak = -1;
					forceBreak = false;
				}
				pos++;
				currLineLen++;
			}
			string remainder = escaped.Substring (lastBreakAt);
			if (remainder.Length > 0) {
				sw.Write ("\"");
				sw.Write (remainder);
				sw.Write ("\"");
				sw.WriteLine ();
			}
			return;
		}
		
		// Clears the catalog, removes all entries from it.
		void Clear ()
		{
			entriesDict.Clear ();
			entriesList.Clear ();
			deletedEntriesList.Clear ();
			isOk = true;
		}
		
		/// <summary>
		/// Loads catalog from .po file.
		/// </summary>
		public bool Load (ProgressMonitor monitor, string poFile)
		{
			Clear ();
			isOk = false;
			fileName = poFile;

			// Load the .po file:
			try {
				CharsetInfoFinder charsetFinder = new CharsetInfoFinder (parentProj, poFile);
				charsetFinder.Parse ();
				Charset = charsetFinder.Charset;
				originalNewLine = charsetFinder.NewLine;
			} catch (Exception e) {
				string msg = "Error during getting charset of file '" + poFile + "'.";
				LoggingService.LogInternalError (msg, e);
				if (monitor != null)
					monitor.ReportError (msg, e);
				return false;
			}

			LoadParser parser = new LoadParser (this, poFile, Catalog.GetEncoding (this.Charset));
			if (!parser.Parse ()) {
				string msg = string.Format ("Error during parsing '{0}' file, file is probably corrupted.", poFile);
				LoggingService.LogError (msg);
				if (monitor != null)
					monitor.ReportError (msg, null);
				return false;
			}

			isOk = true;
			IsDirty = false;
			return true;
		}
		
		// Ensures that the end lines of text are the same as in the reference string.
		static string EnsureCorrectEndings (string reference, string text)
		{
			if (text.Length == 0)
				return "";
			
			int numEndings = 0;
			for (int i = text.Length - 1; i >= 0 && text[i] == '\n'; i--, numEndings++)
				;
			StringBuilder sb = new StringBuilder (text, 0, text.Length - numEndings, text.Length + reference.Length - numEndings);
			for (int i = reference.Length - 1; i >= 0 && reference[i] == '\n'; i--) {
				sb.Append ('\n');
			}
			return sb.ToString ();
		}
		
		// Saves catalog to file.
		//FIXME: escape all the values that the parser unescapes
		public bool Save (string poFile)
		{
			
			StreamWriter sw;
			
			// Update information about last modification time:
			RevisionDate = Catalog.GetDateTimeRfc822Format ();
			
			// Save .po file
			if (String.IsNullOrEmpty (Charset) || Charset == "CHARSET")
				Charset = "utf-8";
			if (!CanEncodeToCharset (Charset)) {
				// TODO: log that we don't support such encoding, utf-8 would be used
				Charset = "utf-8";
			}
			
			Encoding encoding = Catalog.GetNoBOMEncoding (Charset);
			
			int fileCounter = 0;
			string tempFileName = "";
			
			// get a temp file in same directory as original
			try {
				do  {
					fileCounter++;
					tempFileName = poFile + fileCounter.ToString();
				} while (File.Exists(tempFileName));
				
				sw = new StreamWriter(tempFileName,false,encoding);
				
			}
			catch (Exception ex) {
			
				LoggingService.LogError ("Unhandled error creating temp file while saving Gettext catalog '{0}': {1}", tempFileName, ex);
				return false;
			}
			
			using (sw) // careful not to use sw outside using block
			{
				sw.NewLine = originalNewLine;

				if (!String.IsNullOrEmpty (Comment))
					Catalog.SaveMultiLines (sw, Comment);
			
				sw.WriteLine ("msgid \"\"");
				sw.WriteLine ("msgstr \"\"");
			
				string pohdr = GetHeaderString (originalNewLine);
				pohdr = pohdr.Substring (0, pohdr.Length - 1);
				Catalog.SaveMultiLines (sw, pohdr);
				sw.WriteLine ();
			
				foreach (CatalogEntry data in entriesList) {
					if (data.Comment != String.Empty)
						SaveMultiLines (sw, data.Comment);
					foreach (string autoComment in data.AutoComments) {
						if (String.IsNullOrEmpty (autoComment))
							sw.WriteLine ("#.");
						else
							sw.WriteLine ("#. {0}", autoComment);
					}
					foreach (string reference in data.References) {
						//store paths as Unix-type paths, but internally use native style
						string r = reference;
						if (Platform.IsWindows)
							r = r.Replace ('\\', '/');
						sw.WriteLine ("#: {0}", r);
					}
					if (! String.IsNullOrEmpty (data.Flags)) {
						sw.WriteLine (data.Flags);
					}
					FormatMessageForFile (sw, "msgid", data.String);
					if (data.HasPlural) {
						FormatMessageForFile (sw, "msgid_plural", data.PluralString);
						for (int n = 0; n < data.NumberOfTranslations; n++) {
							string hdr = String.Format ("msgstr[{0}]", n);
							
							FormatMessageForFile (sw, hdr, EnsureCorrectEndings (data.String, data.GetTranslation (n)));
						}
					} else {
						FormatMessageForFile (sw, "msgstr", EnsureCorrectEndings (data.String, data.GetTranslation (0)));
					}
					sw.WriteLine ();
				}
			
				// Write back deleted items in the file so that they're not lost
				foreach (CatalogDeletedEntry deletedItem in deletedEntriesList) {
					if (deletedItem.Comment != String.Empty)
						SaveMultiLines (sw, deletedItem.Comment);
					foreach (string autoComment in deletedItem.AutoComments) {
						sw.WriteLine ("#. {0}", autoComment);
					}
					foreach (string reference in deletedItem.References) { // note references arnt read in during parse
						sw.WriteLine ("#: {0}", reference);
					}
					string flags = deletedItem.Flags;
					if (! String.IsNullOrEmpty (flags)) {
						sw.WriteLine (flags);
					}
					foreach (string deletedLine in deletedItem.DeletedLines){
						sw.WriteLine ("{0}", deletedLine);
					}
					sw.WriteLine ();
				}
			}
			
			//try to replace original file
			bool saved = false;
			try {
				File.Copy(tempFileName, poFile, true);
				saved = true;
			}
			catch (Exception ex){
				LoggingService.LogError ("Unhandled error saving Gettext Catalog to '{0}': {1}", poFile, ex);
				saved = false;
			}	
			finally {
				File.Delete(tempFileName);
			}
			
			if (!saved)
				return false;
						
			fileName = poFile;
			IsDirty = false;
			return true;
						
		}
		
		static void SaveMultiLines (StreamWriter sw, string text)
		{
			if (text != null) {
				foreach (string line in text.Split (CatalogParser.LineSplitStrings, StringSplitOptions.None)) {
					sw.WriteLine (line);
				}
			}
		}
		
		static bool CanEncodeToCharset (string charset)
		{
			foreach (EncodingInfo info in Encoding.GetEncodings ()) {
				try {
					if (string.Equals (info.Name, charset, StringComparison.OrdinalIgnoreCase))
						return true;
				} catch (Exception) {
					// info.Name may fail.
				}
			}
			return false;
		}
		
		static Encoding GetNoBOMEncoding(string charset)
		{
			// unicode Encoding types provide BOMs by default
			// so manually check for these and specify otherwise in constructors
			switch (charset.ToLower ())
			{
			case "utf-32":
				return new UTF32Encoding(false, false);
			case "utf-32be":
				return new UTF32Encoding(true, false);
			case "utf-8":
				return new UTF8Encoding(false);
			case "utf-16":
				return new UnicodeEncoding(false, false);
			case "unicodefffe":
				return new UnicodeEncoding(true, false);
			default:
				return GetEncoding(charset);				
			}
		}
		
		static Encoding GetEncoding (string charset)
		{
			foreach (EncodingInfo info in Encoding.GetEncodings ()) {
				try {
					if (string.Equals (info.Name, charset, StringComparison.OrdinalIgnoreCase))
						return info.GetEncoding ();
				} catch (Exception) {
					// info.Name may fail.
				}
			}
			return null;
		}
		
		// Updates the catalog from POT file.
		public bool UpdateFromPOT (ProgressMonitor mon, string potFile, bool summary)
		{
			if (! isOk)
				return false;

			Catalog newCat = new Catalog (parentProj);
			newCat.Load (mon, potFile);

			if (!newCat.IsOk)
				return false;

			// TODO: add some interactivity
			//if (! summary) //|| ShowMergeSummary (newcat)
				return Merge (mon, newCat);
			//else
			//	return false;
		}
		
		// Adds translation into the catalog. Returns true on success or false
		// if such key does not exist in the catalog
		public bool Translate (string key, string translation)
		{
			CatalogEntry d = FindItem (key);
			if (d == null)
			{
				return false;
			} else
			{
				d.SetTranslation (translation, 0);
				return true;
			}
		}
		
		// Returns catalog item with key or null if such key is not available.
		public CatalogEntry FindItem (string key)
		{
			return entriesDict.ContainsKey (key) ? this.entriesDict[key] : null;
		}
		
		// Adds an item to the catalog if it isn't already there
		public CatalogEntry AddItem (string original, string plural)
		{
			CatalogEntry result;
			if (!entriesDict.TryGetValue (original, out result)) {
				result = new CatalogEntry (this, original, plural);
				if (!String.IsNullOrEmpty (plural))
					result.SetTranslations (new string[]{"", ""});
				AddItem (result);
			}
			return result;
		}

		// Returns number of all, fuzzy, badtokens and untranslated items.
		public void GetStatistics (out int all, out int fuzzy, out int missing, out int badtokens, out int untranslated)
		{
			all = fuzzy = missing = badtokens = untranslated = 0;
			for (int i = 0; i < this.Count; i++) {
				all++;
				if (this[i].IsFuzzy)
					fuzzy++;
				if (this[i].References.Length == 0)
					missing++;
				if (this[i].DataValidity == CatalogEntry.Validity.Invalid)
					badtokens++;
				if (! this[i].IsTranslated)
					untranslated++;
			}
		}
		
		internal void UpdatePluralsCount ()
		{
			if (HasHeader ("Plural-Forms")) {
				// e.g. "Plural-Forms: nplurals=3; plural=(n%10==1 && n%100!=11 ?
				//       0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2);\n"

				string form = GetHeader ("Plural-Forms");
				int pos = form.IndexOf (';');
				if (pos != -1)
				{
					form = form.Substring (0, pos);
					pos = form.IndexOf ('=');
					if (pos != -1)
					{
						if (form.Substring (0, pos) == "nplurals")
						{
							int val;
							if (Int32.TryParse (form.Substring (pos + 1), out val))
							{
								nplurals = val;
								return;
							}
						}
					}
				}
			}
			nplurals = 2;
		}

		public string[] PluralFormsDescriptions {
			get {
				List<string> descriptions = new List<string> ();
				
				if (!HasHeader ("Plural-Forms")) {
					descriptions.Add (GettextCatalog.GetString ("Singular"));
					descriptions.Add (GettextCatalog.GetString ("Plural"));
					return descriptions.ToArray ();
				}

				PluralFormsCalculator calc = PluralFormsCalculator.Make (GetHeader ("Plural-Forms"));
				int cnt = PluralFormsCount;
				for (int i = 0; i < cnt; i++) {
					// find example number that would use this plural form:
					int example = 0;
					if (calc != null) {
						for (example = 1; example < 1000; example++) {
							if (calc.Evaluate (example) == i)
								break;
						}
						// we prefer non-zero values, but if this form is for zero only,
						// use zero:
						if (example == 1000 && calc.Evaluate (0) == i)
							example = 0;
					} else
						example = 1000;

					string desc;
					if (example == 1000)
						desc = String.Format (GettextCatalog.GetString ("Form {0}"), i + 1);
					else
						desc = String.Format (GettextCatalog.GetString ("Form {0} (e.g. \"{1}\")"), i + 1, example);
					descriptions.Add (desc);
				}
				return descriptions.ToArray ();
			}
		}

		// Returns status of catalog object: true if ok, false if damaged (i.e. constructor or Load failed).
		public bool IsOk
		{
			get { return isOk; }
		}

		// Appends content of catalog to this catalog.
		public void Append (Catalog catalog)
		{
			CatalogEntry dt, myDt;

			for (int i = 0; i < catalog.Count; i++)
			{
				dt = catalog[i];
				myDt = FindItem (dt.String);
				if (myDt == null)
				{
					myDt = new CatalogEntry (this, dt);
					entriesDict.Add (dt.String, myDt);
					entriesList.Add (myDt);
				} else
				{
					for (uint j = 0; j < dt.References.Length; j++)
						myDt.AddReference (dt.References[j]);
					if (! String.IsNullOrEmpty (dt.GetTranslation (0)))
						myDt.SetTranslation (dt.GetTranslation (0), 0);
					if (dt.IsFuzzy)
						myDt.IsFuzzy = true;
				}
			}
			IsDirty = true;
		}

		// Returns xx_YY ISO code of catalog's language.
		public string LocaleCode{
			get {
				string lang = String.Empty;

				// was the language explicitly specified?
				if (!String.IsNullOrEmpty (Language)) {
					lang = IsoCodes.LookupLanguageCode (Language).Name;
					if (!String.IsNullOrEmpty (Country)){
						lang += '_';
						lang += IsoCodes.LookupCountryCode (Country);
					}
				}
				
				// if not, can we deduce it from filename?
				if (String.IsNullOrEmpty (lang) && ! String.IsNullOrEmpty (fileName)) {
					string name = Path.GetFileNameWithoutExtension (fileName);
					if (name.Length == 2)
					{
						if (IsoCodes.IsKnownLanguageCode (name))
						    lang = name;
					} else if (name.Length == 5 && name[2] == '_')
					{
						if (IsoCodes.IsKnownLanguageCode (name.Substring (0, 2)) &&
						    IsoCodes.IsKnownCountryCode (name.Substring (3, 2)))
						{
							lang = name;
						}
					}
				}
				return lang;
			}
		}

		// Adds entry to the catalog (the catalog will take ownership of the object).
		public void AddItem (CatalogEntry data)
		{
			if (this.entriesDict.ContainsKey (data.String))
			{
				LoggingService.LogWarning ("Duplicate message id '{0}' in po file, ignoring it to achieve validity", data.String);
			} else
			{
				this.entriesDict.Add (data.String, data);
				entriesList.Add (data);
			}
		}
		
		public void RemoveItem (CatalogEntry data)
		{
			if (this.entriesDict.ContainsKey (data.String))
				this.entriesDict.Remove (data.String);
			if (this.entriesList.Contains (data))
				this.entriesList.Remove (data);
		}
		
		// Adds entry to the catalog (the catalog will take ownership of the object).
		public void AddDeletedItem (CatalogDeletedEntry data)
		{
			deletedEntriesList.Add (data);
		}

		// Returns true if the catalog contains obsolete entries (~.*)
		public bool HasDeletedItems
		{
			get { return deletedEntriesList.Count > 0; }
		}

		// Removes all obsolete translations from the catalog
		public void RemoveDeletedItems ()
		{
			deletedEntriesList.Clear ();
		}

		// Merges the catalog with reference catalog
		// (in the sense of msgmerge -- this catalog is old one with
		// translations, \a refcat is reference catalog created by Update().)
		// return true if the merge was successfull, false otherwise.
		public bool Merge (ProgressMonitor mon, Catalog refCat)
		{
			// TODO: implement via monitor, not in a GUI thread...
			// But mind about it as it would be used during build.
			// Or do we want such a feature also for invoking in gui
			// for po files not in project?
			string oldName = fileName;

			string tmpDir = Path.GetTempPath ();
			
			string filePrefix = Path.GetFileNameWithoutExtension (Path.GetTempFileName ());
			
			string tmp1 = tmpDir + Path.DirectorySeparatorChar + filePrefix + ".ref.pot";
			string tmp2 = tmpDir + Path.DirectorySeparatorChar + filePrefix + ".input.po";
			string tmp3 = tmpDir + Path.DirectorySeparatorChar + filePrefix + ".output.po";

			refCat.Save (tmp1);
			this.Save (tmp2);

			System.Diagnostics.Process process = new System.Diagnostics.Process ();
			process.StartInfo.FileName = "msgmerge";
			process.StartInfo.Arguments = "--force-po -o \"" + tmp3 + "\" \"" + tmp2 + "\" \"" + tmp1 + "\"";
			//Console.WriteLine ("--force-po -o \"" + tmp3 + "\" \"" + tmp2 + "\" \"" + tmp1 + "\"");
			process.Start ();
			process.WaitForExit ();
			bool succ = process.ExitCode == 0;
			if (succ)
			{
				Catalog c = new Catalog (parentProj);
				c.Load (mon, tmp3);
				Clear ();
				Append (c);
			}

			File.Delete (tmp1);
			File.Delete (tmp2);
			File.Delete (tmp3);

			fileName = oldName;
			IsDirty = true;
			return succ;
		}

		// Returns list of strings that are new in reference catalog
		// (compared to this one) and that are not present in  reference
		// catalog (i.e. are obsoleted).
		public void GetMergeSummary (Catalog refCat, out string[] newEntries, out string[] obsoleteEntries)
		{
			List<string> newEnt = new List<string> ();
			List<string> obsoleteEnt = new List<string> ();
			int i;

			for (i = 0; i < this.Count; i++)
				if (refCat.FindItem(this[i].String) == null)
					obsoleteEnt.Add (this[i].String);

			for (i = 0; i < refCat.Count; i++)
				if (FindItem (refCat[i].String) == null)
					newEnt.Add (refCat[i].String);
			
			newEntries = newEnt.ToArray ();
			obsoleteEntries = obsoleteEnt.ToArray ();
		}
		
		protected virtual void OnDirtyChanged (EventArgs e)
		{
			if (DirtyChanged != null)
				DirtyChanged (this, e);
		}
		
		public event EventHandler DirtyChanged;
		
		#region Header
		Dictionary<string, string> headerEntries = new Dictionary<string, string> ();
		
		// Parsed values
		public string Project = String.Empty, 
		              CreationDate = String.Empty,
		              RevisionDate = String.Empty, 
		              Translator = String.Empty,
		              TranslatorEmail = String.Empty, 
		              Team = String.Empty,
		              TeamEmail = String.Empty, 
		              Charset = String.Empty,
		              Language = String.Empty,
		              Country = String.Empty, // lang + country not yet used
					  Comment = String.Empty;
		
		// Creates new, empty header. Sets Charset to something meaningful ("UTF-8", currently).
		void CreateNewHeaders (TranslationProject project)
		{
			if (project == null)
				return;
			RevisionDate = CreationDate = Catalog.GetDateTimeRfc822Format ();
			
			Language = Country = Project = Team = TeamEmail = "";
			
			Charset = "utf-8";
			AuthorInformation userInfo = project.AuthorInformation;
			Translator = userInfo.Name;
			TranslatorEmail = userInfo.Email;
			
			//dt.SourceCodeCharset = String.Empty;
			UpdateHeaderDict ();
		}
		
		// Initializes the headers from string that is in msgid "" format (i.e. list of key:value\n entries).
		public void ParseHeaderString (string headers)
		{
			string hdr = StringEscaping.FromGettextFormat (headers);
			string[] tokens = hdr.Split ('\n');
			headerEntries.Clear ();
			
			foreach (string token in tokens) {
				if (token != String.Empty) {
					int pos = token.IndexOf (':');
					if (pos == -1){
						throw new Exception (String.Format ("Malformed header: '{0}'", token));
					} else {
						string key = token.Substring (0, pos).Trim ();
						string value = token.Substring (pos + 1).Trim ();
						headerEntries[key] = value;
					}
				}
			}
			ParseHeaderDict ();
		}

		// Converts the header into string representation that can be directly written to .po file as msgid ""
		public string GetHeaderString (string lineDelimeter)
		{
			UpdateHeaderDict ();
			StringBuilder sb = new StringBuilder ();

			foreach (string key in headerEntries.Keys)
			{
				string value = String.Empty;
				if (headerEntries[key] != null)
					value = StringEscaping.ToGettextFormat (headerEntries[key]);
				sb.AppendFormat ("\"{0}: {1}\\n\"{2}", key, value, lineDelimeter);

			}
			return sb.ToString ();
		}

		public string GetHeaderString ()
		{
			return GetHeaderString (Environment.NewLine);
		}

		// Updates headers list from parsed values entries below
		public void UpdateHeaderDict ()
		{
			SetHeader ("Project-Id-Version", Project);
			SetHeader ("POT-Creation-Date", CreationDate);
			SetHeader ("PO-Revision-Date", RevisionDate);

			if (String.IsNullOrEmpty (TranslatorEmail)) {
				SetHeader ("Last-Translator", Translator);
			} else {
				SetHeader ("Last-Translator", String.Format ("{0} <{1}>", Translator, TranslatorEmail));
			}
			
			if (String.IsNullOrEmpty (TeamEmail)) {
				SetHeader ("Language-Team", Team);
			} else {
				SetHeader ("Language-Team", String.Format ("{0} <{1}>", Team, TeamEmail));
			}

			SetHeader ("MIME-Version", "1.0");
			SetHeader ("Content-Type", "text/plain; charset=" + Charset);
			SetHeader ("Content-Transfer-Encoding", "8bit");
			
			SetHeader ("X-Generator", "MonoDevelop Gettext addin");
		}

		// Reverse operation to UpdateDict
		void ParseHeaderDict ()
		{
			string dummy;
			
			Project = GetHeader ("Project-Id-Version");
			CreationDate = GetHeader ("POT-Creation-Date");
			RevisionDate = GetHeader ("PO-Revision-Date");
			
			dummy = GetHeader ("Last-Translator");
			if (!String.IsNullOrEmpty (dummy)) {
				string[] tokens = dummy.Split ('<', '>');
				if (tokens.Length < 2) {
					Translator = dummy;
					TranslatorEmail = String.Empty;
				} else {
					Translator = tokens[0].Trim ();
					TranslatorEmail = tokens[1].Trim ();
				}
			}

			dummy = GetHeader ("Language-Team");
			if (!String.IsNullOrEmpty (dummy)) {
				string[] tokens = dummy.Split ('<', '>');
				if (tokens.Length < 2) {
					Team = dummy;
					TeamEmail = String.Empty;
				} else {
					Team = tokens[0].Trim ();
					TeamEmail = tokens[1].Trim ();
				}
			}

			string ctype = GetHeader ("Content-Type");
			int pos = ctype.IndexOf ("; charset=");
			if (pos != -1) {
				Charset = ctype.Substring (pos + "; charset=".Length).Trim ();
			} else {
				Charset = "iso-8859-1";
			}
		}
		
		// Returns value of header or empty string if missing.
		public string GetHeader (string key)
		{
			if (headerEntries.ContainsKey (key))
				return headerEntries[key];
			return String.Empty;
		}
		
		// Returns true if given key is present in the header.
		public bool HasHeader (string key)
		{
			return headerEntries.ContainsKey (key);
		}
		
		// Sets header to given value. Overwrites old value if present, appends to header values otherwise.
		public void SetHeader (string key, string value)
		{
			headerEntries[key] = value;
			
			if (key == "Plural-Forms")
				UpdatePluralsCount ();
		}
		
		// Like SetHeader, but deletes the header if value is empty
		public void SetHeaderNotEmpty (string key, string value)
		{
			if (String.IsNullOrEmpty (value))
				DeleteHeader (key);
			else
				SetHeader (key, value);
			
			if (key == "Plural-Forms")
				UpdatePluralsCount ();
		}
		
		// Removes given header entry
		public void DeleteHeader (string key)
		{
			if (HasHeader (key)) {
				headerEntries.Remove (key);
			}
		}
		
		public string CommentForGui {
			get {
				if (String.IsNullOrEmpty (Comment))
					return string.Empty;
				
				StringBuilder sb = new StringBuilder ();
				bool first = true;
				foreach (string line in Comment.Split ('\n')) {
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
			set {
				if (String.IsNullOrEmpty (value)) {
					Comment = String.Empty;
					return;
				}
				
				StringBuilder sb = new StringBuilder ();
				foreach (string line in value.Split (new string[] {Environment.NewLine}, StringSplitOptions.None)) {
					if (sb.Length != 0)
						sb.AppendLine ();
					sb.Append ("# ");
					sb.Append (line);
				}
				this.Comment = sb.ToString ();
			}
		}
		#endregion
		
		#region IEnumerable<CatalogEntry> Members
		public IEnumerator<CatalogEntry> GetEnumerator ()
		{
			return entriesList.GetEnumerator ();
		}
		#endregion

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return entriesList.GetEnumerator ();
		}
		#endregion
	}
}
