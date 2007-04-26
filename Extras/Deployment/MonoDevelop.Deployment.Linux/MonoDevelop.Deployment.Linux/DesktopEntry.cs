
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;

namespace MonoDevelop.Deployment.Linux
{
	public class DesktopEntry
	{
		ListDictionary entries = new ListDictionary ();
		Hashtable lists = new Hashtable ();
		string currentLocale;
		bool isLoadedFile;
		
		IList knownKeys = new string[] {
			"Type", "Name", "GenericName", "NoDisplay", "Comment", "Icon", "OnlyShowIn", "NotShowIn", "TryExec",
			"Exec", "Path", "Terminal", "MimeType", "Categories", "URL"
		};
		
		public DesktopEntry()
		{
		}
		
		public string CurrentLocale {
			get { return currentLocale; }
			set { currentLocale = value; }
		}
		
		public DesktopEntryType Type {
			get {
				switch (GetEntry ("Type")) {
				case "Application": return DesktopEntryType.Application;
				case "Link": return DesktopEntryType.Link;
				case "Directory": return DesktopEntryType.Directory;
				default: return DesktopEntryType.Application;
				}
			}
			set {
				SetEntry ("Type", value.ToString ());
			}
		}
		
		public string Name {
			get { return GetEntry ("Name", true); }
			set { SetEntry ("Name", value, true); }
		}
		
		public string GenericName {
			get { return GetEntry ("GenericName", true); }
			set { SetEntry ("GenericName", value, true); }
		}
		
		public bool NoDisplay {
			get { return GetBoolEntry ("NoDisplay"); }
			set { SetBoolEntry ("NoDisplay", value); }
		}
		
		public string Comment {
			get { return GetEntry ("Comment", true); }
			set { SetEntry ("Comment", value, true); }
		}
		
		public string Icon {
			get { return GetEntry ("Icon", true); }
			set { SetEntry ("Icon", value, true); }
		}
		
		public StringCollection OnlyShowIn {
			get { return GetStringsEntry ("OnlyShowIn"); }
		}
		
		public StringCollection NotShowIn {
			get { return GetStringsEntry ("NotShowIn"); }
		}
		
		public string TryExec {
			get { return GetEntry ("TryExec"); }
			set { SetEntry ("TryExec", value); }
		}
		
		public string Exec {
			get { return GetEntry ("Exec"); }
			set { SetEntry ("Exec", value); }
		}
		
		public string Path {
			get { return GetEntry ("Path"); }
			set { SetEntry ("Path", value); }
		}
		
		public bool Terminal {
			get { return GetBoolEntry ("Terminal"); }
			set { SetBoolEntry ("Terminal", value); }
		}
		
		public StringCollection MimeTypes {
			get { return GetStringsEntry ("MimeType"); }
		}
		
		public StringCollection Categories {
			get { return GetStringsEntry ("Categories"); }
		}
		
		public StringCollection GetUnknownEntries ()
		{
			StringCollection col = new StringCollection ();
			foreach (string s in entries.Keys) {
				if (s.StartsWith ("_ "))
					continue;
				string k = s;
				int i = s.IndexOf ('[');
				if (i != -1)
					k = s.Substring (0, i);
				if (!knownKeys.Contains (k))
					col.Add (s);
			}
			return col;
		}
		
		public string Url {
			get { return GetEntry ("URL"); }
			set { SetEntry ("URL", value); }
		}
		
		public string[] GetLocales ()
		{
			ArrayList col = new ArrayList ();
			foreach (string s in entries.Keys) {
				if (s.StartsWith ("_ "))
					continue;
				int j = s.IndexOf ('[');
				if (j == -1)
					continue;
				string loc = s.Substring (j + 1, s.Length - j - 2);
				if (!col.Contains (loc))
					col.Add (loc);
			}
			col.Sort ();
			return (string[]) col.ToArray (typeof(string));
		}
		
		public void RemoveEntry (string key)
		{
			entries.Remove (key);
		}
		
		public string GetEntry (string key)
		{
			return (string) entries [key];
		}
		
		public string GetEntry (string key, bool localized)
		{
			string s;
			if (localized && !string.IsNullOrEmpty (currentLocale)) {
				s = (string) entries [key + "[" + currentLocale + "]"];
				if (s == null)
					s = (string) entries [key];
			}
			else
				s = (string) entries [key];
			return s != null ? s : string.Empty;
		}
		
		public void SetEntry (string key, string value)
		{
			if (value.Length == 0)
				entries.Remove (key);
			else
				entries [key] = value;
		}
		
		public void SetEntry (string key, string value, bool localized)
		{
			if (localized && !string.IsNullOrEmpty (currentLocale))
				entries [key + "[" + currentLocale + "]"] = value;
			else
				entries [key] = value;
		}
		
		bool GetBoolEntry (string key)
		{
			string s = GetEntry (key);
			if (string.IsNullOrEmpty (s))
				return false;
			else if (s == "true")
				return true;
			else
				return false;
		}
		
		void SetBoolEntry (string key, bool v)
		{
			SetEntry (key, v ? "true" : "false");
		}
		
		StringCollection GetStringsEntry (string key)
		{
			StringCollection col = (StringCollection) lists [key];
			if (col != null)
				return col;
			
			col = new StringCollection ();
			lists [key] = col;
			
			string s = GetEntry (key);
			if (string.IsNullOrEmpty (s))
				return col;

			int lasti = 0;
			int i = s.IndexOf (';');
			while (i != -1) {
				if (i == 0 || s[i-1]!='\\') {
					string e = s.Substring (lasti, i - lasti);
					col.Add (e.Replace ("\\;",";"));
					lasti = i+1;
				}
				i = s.IndexOf (';', i+1);
			}
			if (lasti < s.Length) {
				string r = s.Substring (lasti, s.Length - lasti);
				col.Add (r.Replace ("\\;",";"));
			}
			return col;
		}
		
		public void Load (string file)
		{
			using (StreamReader sr = new StreamReader (file))
			{
				string line;
				entries.Clear ();
				lists.Clear ();
				
				string group = null;
				int sepc = 0;
				
				while ((line = sr.ReadLine ()) != null) {
					
					bool skip = false;
					
					if (line.Length == 0 || line[0] == '#')
						skip = true;
					
					if (!skip && line[0] == '[') {
						group = line.Substring (1, line.Length - 2);
						skip = true;
					}
					if (!skip && group != "Desktop Entry")
						skip = true;
					
					if (!skip) {
						int i = line.IndexOf ('=');
						if (i != -1) {
							string key = line.Substring (0, i);
							string val = line.Substring (i+1);
							entries.Add (key, val);
							continue;
						}
					}
					
					entries.Add ("_ " + sepc, line);
					sepc++;
				}
			}
			isLoadedFile = true;
		}
		
		public void Save (string file)
		{
			foreach (DictionaryEntry e in lists) {
				StringCollection col = (StringCollection) e.Value;
				string s = "";
				foreach (string v in col)
					s += v.Replace (";", "\\;") + ";";
				if (s.Length > 0)
					entries [e.Key] = s;
				else
					entries.Remove (e.Key);
			}
			
			using (StreamWriter sw = new StreamWriter (file)) {
				if (!isLoadedFile) {
					sw.WriteLine ("[Desktop Entry]");
				}
				foreach (DictionaryEntry e in entries) {
					string key = (string) e.Key;
					string val = (string) e.Value;
					if (key.StartsWith ("_ "))
						sw.WriteLine (val);
					else
						sw.WriteLine (key + "=" + val);
				}
			}
		}
		
		static XmlDocument desktopInfo;
		
		public static XmlDocument GetDesktopInfo ()
		{
			if (desktopInfo != null)
				return desktopInfo;
			
			Stream s = typeof(DesktopEntry).Assembly.GetManifestResourceStream ("DesktopInfo.xml");
			desktopInfo = new XmlDocument ();
			desktopInfo.Load (s);
			s.Close ();
			
			return desktopInfo;
		}
	}
	
	public enum DesktopEntryType
	{
		Application,
		Link,
		Directory
	}
}
