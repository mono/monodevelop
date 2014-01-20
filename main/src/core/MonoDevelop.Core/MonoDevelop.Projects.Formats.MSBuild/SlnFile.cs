using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Collections;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class SlnFile
	{
		SlnProjectCollection projects = new SlnProjectCollection ();
		SlnSectionCollection sections = new SlnSectionCollection ();
		SlnPropertySet metadata = new SlnPropertySet (true);

		public string FormatVersion { get; set; }
		public string ProductDescription { get; set; }

		public string VisualStudioVersion {
			get { return metadata.GetValue ("VisualStudioVersion"); }
			set { metadata.SetValue ("VisualStudioVersion", value); }
		}

		public string MinimumVisualStudioVersion {
			get { return metadata.GetValue ("MinimumVisualStudioVersion"); }
			set { metadata.SetValue ("MinimumVisualStudioVersion", value); }
		}

		public SlnFile ()
		{
		}

		/// <summary>
		/// The directory to be used as base for converting absolute paths to relative
		/// </summary>
		public FilePath BaseDirectory {
			get;
			set;
		}

		public SlnPropertySet SolutionConfigurationsSection {
			get { return sections.GetOrCreateSection ("SolutionConfigurationPlatforms", "preSolution").Properties; }
		}

		public SlnPropertySetCollection ProjectConfigurationsSection {
			get { return sections.GetOrCreateSection ("ProjectConfigurationPlatforms", "postSolution").NestedPropertySets; }
		}

		public SlnSectionCollection Sections {
			get { return sections; }
		}

		public SlnProjectCollection Projects {
			get { return projects; }
		}

		public void Read (string file)
		{
			using (var sr = new StreamReader (file))
				Read (sr);
		}

		public void Read (TextReader reader)
		{
			string line;
			int curLineNum = 0;
			bool globalFound = false;
			bool productRead = false;

			while ((line = reader.ReadLine ()) != null) {
				curLineNum++;
				line = line.Trim ();
				if (line.StartsWith ("Microsoft Visual Studio Solution File")) {
					int i = line.LastIndexOf (' ');
					if (i == -1)
						throw new InvalidSolutionFormatException (curLineNum);
					FormatVersion = line.Substring (i + 1);
				}
				if (line.StartsWith ("# ")) {
					if (!productRead) {
						productRead = true;
						ProductDescription = line.Substring (2);
					}
				} else if (line.StartsWith ("Project")) {
					SlnProject p = new SlnProject ();
					p.Read (reader, line, ref curLineNum);
					projects.Add (p);
				} else if (line == "Global") {
					if (globalFound)
						throw new InvalidSolutionFormatException (curLineNum, "Global section specified more than once");
					globalFound = true;
					while ((line = reader.ReadLine ()) != null) {
						curLineNum++;
						line = line.Trim ();
						if (line == "EndGlobal") {
							break;
						} else if (line.StartsWith ("GlobalSection")) {
							var sec = new SlnSection ();
							sec.Read (reader, line, ref curLineNum);
							sections.Add (sec);
						} else
							throw new InvalidSolutionFormatException (curLineNum);
					}
					if (line == null)
						throw new InvalidSolutionFormatException (curLineNum, "Global section not closed");
				} else if (line.IndexOf ('=') != -1) {
					metadata.ReadLine (line, curLineNum);
				}
			}
			if (FormatVersion == null)
				throw new InvalidSolutionFormatException (curLineNum, "File header is missing");
		}

		public void Write (string file)
		{
			using (var sw = new StreamWriter (file))
				Write (sw);
		}

		public void Write (TextWriter writer)
		{
			writer.NewLine = "\r\n";
			writer.WriteLine ("\r\nMicrosoft Visual Studio Solution File, Format Version " + FormatVersion);
			writer.WriteLine ("# " + ProductDescription);

			metadata.Write (writer);

			foreach (var p in projects)
				p.Write (writer);

			writer.WriteLine ("Global");
			foreach (SlnSection s in sections)
				s.Write (writer, "GlobalSection");
			writer.WriteLine ("EndGlobal");
		}
	}

	public class SlnProject
	{
		SlnSectionCollection sections = new SlnSectionCollection ();

		public string Id { get; set; }
		public string TypeGuid { get; set; }
		public string Name { get; set; }
		public string FilePath { get; set; }
		public int Line { get; private set; }
		internal bool Processed { get; set; }

		public SlnSectionCollection Sections {
			get { return sections; }
		}

		internal void Read (TextReader reader, string line, ref int curLineNum)
		{
			Line = curLineNum;

			int n = 0;
			FindNext (curLineNum, line, ref n, '(');
			n++;
			FindNext (curLineNum, line, ref n, '"');
			int n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			TypeGuid = line.Substring (n + 1, n2 - n - 1);

			n = n2 + 1;
			FindNext (curLineNum, line, ref n, ')');
			FindNext (curLineNum, line, ref n, '=');

			FindNext (curLineNum, line, ref n, '"');
			n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			Name = line.Substring (n + 1, n2 - n - 1);

			n = n2 + 1;
			FindNext (curLineNum, line, ref n, ',');
			FindNext (curLineNum, line, ref n, '"');
			n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			FilePath = line.Substring (n + 1, n2 - n - 1);

			n = n2 + 1;
			FindNext (curLineNum, line, ref n, ',');
			FindNext (curLineNum, line, ref n, '"');
			n2 = n + 1;
			FindNext (curLineNum, line, ref n2, '"');
			Id = line.Substring (n + 1, n2 - n - 1);

			while ((line = reader.ReadLine ()) != null) {
				curLineNum++;
				line = line.Trim ();
				if (line == "EndProject") {
					return;
				}
				if (line.StartsWith ("ProjectSection")) {
					if (sections == null)
						sections = new SlnSectionCollection ();
					var sec = new SlnSection ();
					sections.Add (sec);
					sec.Read (reader, line, ref curLineNum);
				}
			}

			throw new InvalidSolutionFormatException (curLineNum, "Project section not closed");
		}

		void FindNext (int ln, string line, ref int i, char c)
		{
			i = line.IndexOf (c, i);
			if (i == -1)
				throw new InvalidSolutionFormatException (ln);
		}

		public void Write (TextWriter writer)
		{
			writer.Write ("Project(\"");
			writer.Write (TypeGuid);
			writer.Write ("\") = \"");
			writer.Write (Name);
			writer.Write ("\", \"");
			writer.Write (FilePath);
			writer.Write ("\", \"");
			writer.Write (Id);
			writer.WriteLine ("\"");
			if (sections != null) {
				foreach (SlnSection s in sections)
					s.Write (writer, "ProjectSection");
			}
			writer.WriteLine ("EndProject");
		}
	}

	public class SlnSection
	{
		SlnPropertySetCollection nestedPropertySets;
		SlnPropertySet properties;
		List<string> sectionLines;
		int baseIndex;

		public string Id { get; set; }
		public int Line { get; private set; }
		internal bool Processed { get; set; }

		public SlnPropertySet Properties {
			get {
				if (properties == null) {
					properties = new SlnPropertySet ();
					if (sectionLines != null) {
						foreach (var line in sectionLines)
							properties.ReadLine (line, Line);
						sectionLines = null;
					}
				}
				return properties;
			}
		}

		public SlnPropertySetCollection NestedPropertySets {
			get {
				if (nestedPropertySets == null) {
					nestedPropertySets = new SlnPropertySetCollection ();
					if (sectionLines != null)
						LoadPropertySets ();
				}
				return nestedPropertySets;
			}
		}

		public string SectionType { get; set; }

		internal void Read (TextReader reader, string line, ref int curLineNum)
		{
			Line = curLineNum;
			int k = line.IndexOf ('(');
			if (k == -1)
				throw new InvalidSolutionFormatException (curLineNum, "Section id missing");
			var tag = line.Substring (0, k).Trim ();
			var k2 = line.IndexOf (')', k);
			if (k2 == -1)
				throw new InvalidSolutionFormatException (curLineNum);
			Id = line.Substring (k + 1, k2 - k - 1);

			k = line.IndexOf ('=', k2);
			SectionType = line.Substring (k + 1).Trim ();

			var endTag = "End" + tag;

			sectionLines = new List<string> ();
			baseIndex = ++curLineNum;
			while ((line = reader.ReadLine()) != null) {
				curLineNum++;
				line = line.Trim ();
				if (line == endTag)
					break;
				sectionLines.Add (line);
			}
			if (line == null)
				throw new InvalidSolutionFormatException (curLineNum, "Closing section tag not found");
		}

		void LoadPropertySets ()
		{
			if (sectionLines != null) {
				SlnPropertySet curSet = null;
				for (int n = 0; n < sectionLines.Count; n++) {
					var line = sectionLines [n];
					var i = line.IndexOf ('.');
					if (i == -1)
						throw new InvalidSolutionFormatException (baseIndex + i);
					var id = line.Substring (0, i);
					if (curSet == null || id != curSet.Id) {
						curSet = new SlnPropertySet (id);
						nestedPropertySets.Add (curSet);
					}
					curSet.ReadLine (line.Substring (i + 1), baseIndex + i);
				}
				sectionLines = null;
			}
		}

		internal void Write (TextWriter writer, string sectionTag)
		{
			writer.Write ("\t");
			writer.Write (sectionTag);
			writer.Write ('(');
			writer.Write (Id);
			writer.Write (") = ");
			writer.WriteLine (SectionType);
			if (sectionLines != null) {
				foreach (var l in sectionLines)
					writer.WriteLine ("\t\t" + l);
			} else if (properties != null)
				properties.Write (writer);
			else if (nestedPropertySets != null) {
				foreach (var ps in nestedPropertySets)
					ps.Write (writer);
			}
			writer.WriteLine ("\tEnd" + sectionTag);
		}
	}

	public class SlnPropertySet: IDictionary<string,string>
	{
		OrderedDictionary values = new OrderedDictionary ();
		bool isMetadata;

		internal bool Processed { get; set; }
		public int Line { get; private set; }

		public SlnPropertySet ()
		{
		}

		public SlnPropertySet (string id)
		{
			Id = id;
		}

		internal SlnPropertySet (bool isMetadata)
		{
			this.isMetadata = isMetadata;
		}

		internal void ReadLine (string line, int currentLine)
		{
			if (Line == 0)
				Line = currentLine;
			line = line.Trim ();
			int k = line.IndexOf ('=');
			if (k != -1) {
				var name = line.Substring (0, k).Trim ();
				var val = line.Substring (k + 1).Trim ();
				values.Add (name, val);
			} else {
				values.Add (line, null);
			}
		}

		internal void Write (TextWriter writer)
		{
			foreach (DictionaryEntry e in values) {
				if (!isMetadata)
					writer.Write ("\t\t");
				if (Id != null)
					writer.Write (Id + ".");
				writer.WriteLine (e.Key + " = " + e.Value);
			}
		}

		public string Id { get; private set; }

		public string GetValue (string key)
		{
			return (string) values [key];
		}

		public void SetValue (string key, string value)
		{
			values [key] = value;
		}

		public void Add (string key, string value)
		{
			SetValue (key, value);
		}

		public bool ContainsKey (string key)
		{
			return values.Contains (key);
		}

		public bool Remove (string key)
		{
			var wasThere = values.Contains (key);
			values.Remove (key);
			return wasThere;
		}

		public bool TryGetValue (string key, out string value)
		{
			value = (string) values [key];
			return value != null;
		}

		public string this [string index] {
			get {
				return (string) values [index];
			}
			set {
				values [index] = value;
			}
		}

		public ICollection<string> Values {
			get {
				return values.Values.Cast<string>().ToList ();
			}
		}

		public ICollection<string> Keys {
			get { return values.Keys.Cast<string> ().ToList (); }
		}

		public void Add (KeyValuePair<string, string> item)
		{
			SetValue (item.Key, item.Value);
		}

		public void Clear ()
		{
			values.Clear ();
		}

		internal void ClearExcept (HashSet<string> keys)
		{
			foreach (var k in values.Keys.Cast<string>().Except (keys).ToArray ())
				values.Remove (k);
		}

		public bool Contains (KeyValuePair<string, string> item)
		{
			var val = GetValue (item.Key);
			return val == item.Value;
		}

		public void CopyTo (KeyValuePair<string, string>[] array, int arrayIndex)
		{
			foreach (DictionaryEntry de in values)
				array [arrayIndex++] = new KeyValuePair<string, string> ((string)de.Key, (string)de.Value);
		}

		public bool Remove (KeyValuePair<string, string> item)
		{
			if (Contains (item)) {
				Remove (item.Key);
				return true;
			} else
				return false;
		}

		public int Count {
			get {
				return values.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator ()
		{
			foreach (DictionaryEntry de in values)
				yield return new KeyValuePair<string,string> ((string)de.Key, (string)de.Value);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			foreach (DictionaryEntry de in values)
				yield return new KeyValuePair<string,string> ((string)de.Key, (string)de.Value);
		}
	}

	public class SlnProjectCollection: Collection<SlnProject>
	{
		public SlnProject GetProject (string id)
		{
			return this.FirstOrDefault (s => s.Id == id);
		}

		public SlnProject GetOrCreateProject (string id)
		{
			var p = this.FirstOrDefault (s => s.Id.Equals (id, StringComparison.OrdinalIgnoreCase));
			if (p == null) {
				p = new SlnProject { Id = id };
				Add (p);
			}
			return p;
		}
	}

	public class SlnSectionCollection: Collection<SlnSection>
	{
		public SlnSection GetSection (string id)
		{
			return this.FirstOrDefault (s => s.Id == id);
		}

		public SlnSection GetOrCreateSection (string id, string sectionType)
		{
			var sec = this.FirstOrDefault (s => s.Id == id);
			if (sec == null) {
				sec = new SlnSection { Id = id };
				sec.SectionType = sectionType;
				Add (sec);
			}
			return sec;
		}

		public void RemoveSection (string id)
		{
			var s = GetSection (id);
			if (s != null)
				Remove (s);
		}
	}

	public class SlnPropertySetCollection: Collection<SlnPropertySet>
	{
		public SlnPropertySet GetPropertySet (string id, bool ignoreCase = false)
		{
			var sc = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return this.FirstOrDefault (s => s.Id.Equals (id, sc));
		}

		public SlnPropertySet GetOrCreatePropertySet (string id, bool ignoreCase = false)
		{
			var ps = GetPropertySet (id, ignoreCase);
			if (ps == null) {
				ps = new SlnPropertySet (id);
				Add (ps);
			}
			return ps;
		}
	}

	class InvalidSolutionFormatException: Exception
	{
		public InvalidSolutionFormatException (int line): base ("Invalid format in line " + line)
		{
		}

		public InvalidSolutionFormatException (int line, string msg): base ("Invalid format in line " + line + ": " + msg)
		{
		}
	}
}

