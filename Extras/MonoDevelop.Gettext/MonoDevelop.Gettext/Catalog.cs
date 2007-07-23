//
// Catalog.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Gettext
{
	public class Catalog : IEnumerable<CatalogEntry>
	{
		IDictionary<string, CatalogEntry> entriesDict;
		List<CatalogEntry> entriesList;
		List<CatalogDeletedEntry> deletedEntriesList;
		bool isOk;
		string fileName;
		CatalogHeaders headers;
		string originalNewLine = Environment.NewLine;
		bool isDirty;
		int nplurals = 0;
		
		public event EventHandler OnDirtyChanged;
		
		 // Creates empty catalog, you have to call Load.
		public Catalog ()
		{
			entriesDict = new Dictionary<string, CatalogEntry> ();
			entriesList = new List<CatalogEntry> ();
			deletedEntriesList = new List<CatalogDeletedEntry> ();
			isOk = true;
			headers = new CatalogHeaders (this);
		}

		// Loads the catalog from po file.
		public Catalog (string poFile)
			: this ()
		{
			isOk = Load (poFile);
		}
		
		// Creates new, empty header. Sets Charset to something meaningful ("UTF-8", currently).
		public void CreateNewHeaders ()
		{
			CatalogHeaders dt = new CatalogHeaders (this);
			dt.CreationDate = Catalog.GetDateTimeRfc822Format ();
			dt.RevisionDate = dt.CreationDate;

			dt.Language = String.Empty;
			dt.Country = String.Empty;
			dt.Project = String.Empty;
			dt.Team = String.Empty;
			dt.TeamEmail = String.Empty;
			dt.Charset = "utf-8";
			// TODO: join to MD property
			dt.Translator = String.Empty;
			// TODO: join to MD property
			dt.TranslatorEmail = String.Empty;
			//dt.SourceCodeCharset = String.Empty;
			dt.UpdateDict ();
		}

		static string GetDateTimeRfc822Format ()
		{
			return DateTime.Now.ToString ("yyyy-MM-dd HH':'mm':'sszz00"); //rfc822 format
		}

		static string FormatStringForFile (string text)
		{
			StringBuilder sb = new StringBuilder ();
			uint n_cnt = 0;
			int len = text.Length;

			//s = new char[len + 16];
			// Scan the string up to len-2 because we don't want to account for the
			// very last \n on the line:
			//       "some\n string \n"
			//                      ^
			//                      |
			//                      \--- = len-2
			int i;
			if (text.Length > 0 && text[0] != '\n')
				sb.Append (text[0]);
			else
				n_cnt++;
			for (i = 1; i < len - 2; i++)
			{
				if (text[i] == '\\' && text[i + 1] == 'n')
				{
					n_cnt++;
					sb.Append ("\\n\"\n\"");
					i++;
				} else if (text[i] == '\n')
				{
					sb.Append ("\"\n\"");
					n_cnt++;
				} else
					sb.Append (text[i]);
			}
			// ...and add not yet processed characters to the string...
			for (; i < len; i++)
				sb.Append (text[i]);

			// normalize, remove "\n\"\"" lines
			sb.Replace ("\n\"\"", String.Empty);

			if (n_cnt >= 1 && sb.Length > 0)
				return "\"\n\"" + sb.ToString ();
			else
				return sb.ToString ();
		}

		// Clears the catalog, removes all entries from it.
		void Clear ()
		{
			entriesDict.Clear ();
			entriesList.Clear ();
			deletedEntriesList.Clear ();
			isOk = true;
		}

		// Loads catalog from .po file.
		public bool Load (string poFile)
		{
			Clear ();
			isOk = false;
			fileName = poFile;

			// Load the .po file:
			bool finished = false;
			try
			{
				CharsetInfoFinder charsetFinder = new CharsetInfoFinder (poFile);
				charsetFinder.Parse ();
				headers.Charset = charsetFinder.Charset;
				originalNewLine = charsetFinder.NewLine;
				finished = true;
			}
			catch (Exception e)
			{
				// TODO: use loging - GUI!
				Console.WriteLine ("Error during getting charset of '{0}' file, exception: {1}", poFile, e.ToString ());
			}
			if (! finished)
				return false;

			LoadParser parser = new LoadParser (this, poFile, Catalog.GetEncoding (this.Headers.Charset));
			if (!parser.Parse())
			{
				// TODO: use loging - GUI!
				Console.WriteLine ("Error during parsing '{0}' file, file is probably corrupted.", poFile);
				return false;
			}

			isOk = true;
			isDirty = false;
			return true;
		}

		// Saves catalog to file.
		public bool Save (string poFile)
		{
			string dummy;
			StringBuilder sb = new StringBuilder ();

			// TODO: check directory
			//if (! File.Exists (poFile))
			//{
			//    // TODO: log it
			//    return false;
			//}

			// Update information about last modification time:
			headers.RevisionDate = Catalog.GetDateTimeRfc822Format ();

			// Save .po file

			string charset = headers.Charset;
			if (String.IsNullOrEmpty (charset) || charset == "CHARSET")
				charset = "utf-8";

			if (! CanEncodeToCharset (charset))
			{
				// TODO: log that we don't support such encoding, utf-8 would be used
				charset = "utf-8";
			}
			headers.Charset = charset;
			Encoding encoding = Catalog.GetEncoding (charset);

			if (headers.Comment != String.Empty)
				Catalog.SaveMultiLines (sb, headers.Comment, originalNewLine);
			
			sb.AppendFormat ("msgid \"\"{0}", originalNewLine);
			sb.AppendFormat ("msgstr \"\"{0}", originalNewLine);

			string pohdr = headers.ToString (originalNewLine);
			pohdr = pohdr.Substring (0, pohdr.Length - 1);
			Catalog.SaveMultiLines (sb, pohdr, originalNewLine);
			sb.Append (originalNewLine);

			foreach (CatalogEntry data in entriesList)
			{
				if (data.Comment != String.Empty)
					SaveMultiLines (sb, data.Comment, originalNewLine);
				foreach (string autoComment in data.AutoComments)
				{
					if (String.IsNullOrEmpty (autoComment))
						sb.AppendFormat ("#.{0}", originalNewLine);
					else
						sb.AppendFormat ("#. {0}{1}", autoComment, originalNewLine);
				}
				foreach (string reference in data.References)
				{
					sb.AppendFormat ("#: {0}{1}", reference, originalNewLine);
				}
				dummy = data.Flags;
				if (! String.IsNullOrEmpty (dummy))
				{
					dummy += originalNewLine;
					sb.Append (dummy);
				}
				dummy = Catalog.FormatStringForFile (data.String);
				Catalog.SaveMultiLines (sb, "msgid \"" + dummy + "\"", originalNewLine);
				if (data.HasPlural)
				{
					dummy = Catalog.FormatStringForFile (data.PluralString);
					Catalog.SaveMultiLines (sb, "msgid_plural \"" + dummy + "\"", originalNewLine);

					for (int n = 0; n < data.NumberOfTranslations; n++)
					{
						dummy = Catalog.FormatStringForFile (data.GetTranslation (n));
						string hdr = String.Format ("msgstr[{0}] \"", n);
						SaveMultiLines (sb, hdr + dummy + "\"", originalNewLine);
					}
				} else
				{
					dummy = Catalog.FormatStringForFile (data.GetTranslation (0));
					SaveMultiLines (sb, "msgstr \"" + dummy + "\"", originalNewLine);
				}
				sb.Append (originalNewLine);
			}

			// Write back deleted items in the file so that they're not lost
			foreach (CatalogDeletedEntry deletedItem in deletedEntriesList)
			{
				if (deletedItem.Comment != String.Empty)
					SaveMultiLines (sb, deletedItem.Comment, originalNewLine);
				foreach (string autoComment in deletedItem.AutoComments)
				{
					sb.AppendFormat ("#. {0}{1}", autoComment, originalNewLine);
				}
				foreach (string reference in deletedItem.References)
				{
					sb.AppendFormat ("#: {0}{1}", reference, originalNewLine);
				}
				dummy = deletedItem.Flags;
				if (! String.IsNullOrEmpty (dummy))
				{
					dummy += originalNewLine;
					sb.Append (dummy);
				}
				foreach (string deletedLine in deletedItem.DeletedLines)
				{
					sb.AppendFormat ("{0}{1}", deletedLine, originalNewLine);
				}
				sb.Append (originalNewLine);
			}

			bool saved = false;
			try
			{
				// Write it as bytes, text writer includes BOF for utf-8,
				// getetext utils are refusing to work with this
				byte[] content = encoding.GetBytes (sb.ToString ());
				File.WriteAllBytes (poFile, content);
				saved = true;
			}
			catch (Exception)
			{
				// TODO: log it
			}
			if (! saved)
				return false;

			fileName = poFile;
			isDirty = false;
				return true;
		}

		static void SaveMultiLines (StringBuilder sb, string text, string newLine)
		{
			if (text != null)
			{
				foreach (string line in text.Split (new char[] { '\n', '\r'}, StringSplitOptions.None))
					sb.AppendFormat ("{0}{1}", line, newLine);
			}
		}

		static bool CanEncodeToCharset (string charset)
		{
			foreach (EncodingInfo info in Encoding.GetEncodings ())
			{
				if (info.Name.ToLower () == charset.ToLower ())
					return true;
			}
			return false;
		}
		
		static Encoding GetEncoding (string charset)
		{
			foreach (EncodingInfo info in Encoding.GetEncodings ())
			{
				if (info.Name.ToLower () == charset.ToLower ())
					return info.GetEncoding ();
			}
			return null;
		}

		// Updates the catalog from POT file.
		public bool UpdateFromPOT (string potFile, bool summary)
		{
			if (! isOk)
				return false;

			Catalog newCat = new Catalog (potFile);

			if (! newCat.IsOk)
			{
				// TODO: log - GUI!
				Console.WriteLine ("'{0}' is not a valid POT file.", potFile);
				return false;
			}

			// TODO: add some interactivity
			//if (! summary) //|| ShowMergeSummary (newcat)
				return Merge (newCat);
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
			if (entriesDict.ContainsKey (key))
				return this.entriesDict[key];
			else
				return null;
		}

		// Returns the number of strings/translations in the catalog.
		public int Count
		{
			get { return entriesList.Count; }
		}

		// Returns number of all, fuzzy, badtokens and untranslated items.
		public void GetStatistics (out int all, out int fuzzy, out int missing, out int badtokens, out int untranslated)
		{
			all = 0;
			fuzzy = 0;
			missing = 0;
			badtokens = 0;
			untranslated = 0;
			for (int i = 0; i < this.Count; i++)
			{
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

		// Gets n-th item in the catalog (read-write access).
		public CatalogEntry this[int index]
		{
			get
			{
				if (index >= 0 && index < entriesList.Count)
					return entriesList[index];
				else
					return null;
			}
		}

		// Gets catalog header.
		public CatalogHeaders Headers
		{
			get { return headers; }
		}

		// Returns plural forms count: taken from Plural-Forms header if present, 0 otherwise
		public int PluralFormsCount
		{
			get
			{
				if (nplurals == 0)
					UpdatePluralsCount ();
				return nplurals;
			}
		}

		internal void UpdatePluralsCount ()
		{
			if (headers.HasHeader ("Plural-Forms"))
			{
				// e.g. "Plural-Forms: nplurals=3; plural=(n%10==1 && n%100!=11 ?
				//       0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2);\n"

				string form = headers.GetHeader ("Plural-Forms");
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

		public string[] PluralFormsDescriptions
		{
			get
			{
				List<string> descriptions = new List<string> ();
				
				if (! Headers.HasHeader ("Plural-Forms"))
				{
					descriptions.Add (GettextCatalog.GetString ("Singular"));
					descriptions.Add (GettextCatalog.GetString ("Plural"));
					return descriptions.ToArray ();
				}

				PluralFormsCalculator calc = PluralFormsCalculator.Make (Headers.GetHeader ("Plural-Forms"));
				int cnt = PluralFormsCount;
				for (int i = 0; i < cnt; i++)
				{
					// find example number that would use this plural form:
					int example = 0;
					if (calc != null)
					{
						for (example = 1; example < 1000; example++)
						{
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
			isDirty = true;
		}

		// Returns xx_YY ISO code of catalog's language.
		public string LocaleCode
		{
			get
			{
				string lang = String.Empty;

				// was the language explicitly specified?
				if (! String.IsNullOrEmpty (headers.Language))
				{
					lang = IsoCodes.LookupLanguageCode (headers.Language).Name;
					if (! String.IsNullOrEmpty (headers.Country))
					{
						lang += '_';
						lang += IsoCodes.LookupCountryCode (headers.Country);
					}
				}

				// if not, can we deduce it from filename?
				if (String.IsNullOrEmpty (lang) && ! String.IsNullOrEmpty (fileName))
				{
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
				Runtime.LoggingService.WarnFormat ("Duplicate message id '{0}' in po file, ignoring it to achieve validity", data.String);
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
		public bool Merge (Catalog refCat)
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
			Console.WriteLine ("--force-po -o \"" + tmp3 + "\" \"" + tmp2 + "\" \"" + tmp1 + "\"");
			process.Start ();
			process.WaitForExit ();
			bool succ = process.ExitCode == 0;
			if (succ)
			{
				Catalog c = new Catalog (tmp3);
				Clear ();
				Append (c);
			}

			File.Delete (tmp1);
			File.Delete (tmp2);
			File.Delete (tmp3);

			fileName = oldName;
			isDirty = true;
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
		
		public bool IsDirty
		{
			get { return isDirty; }
		}
		
		public void MarkDirty (object sender)
		{
			isDirty = true;
			if (OnDirtyChanged != null)
				OnDirtyChanged (sender, EventArgs.Empty);
		}

		#region IEnumerable<CatalogEntry> Members
		public IEnumerator<CatalogEntry> GetEnumerator ()
		{
			return entriesList.GetEnumerator ();
		}
		#endregion

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator ()
		{
			throw new Exception ("This method has not been implemented.");
		}
		#endregion
	}
}
