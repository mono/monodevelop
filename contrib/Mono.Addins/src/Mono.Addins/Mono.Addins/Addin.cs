//
// Addin.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Specialized;
using Mono.Addins.Description;
using Mono.Addins.Database;

namespace Mono.Addins
{
	public class Addin
	{
		AddinInfo addin;
		string configFile;
		string sourceFile;
		WeakReference desc;
		AddinDatabase database;
		
		internal Addin (AddinDatabase database, string file)
		{
			this.database = database;
			configFile = file;
		}
		
		public string Id {
			get {
				if (configFile != null)
					return Path.GetFileNameWithoutExtension (configFile);
				return this.AddinInfo.Id; 
			}
		}
		
		public string Namespace {
			get { return this.AddinInfo.Namespace; }
		}
		
		public string LocalId {
			get { return this.AddinInfo.LocalId; }
		}
		
		public string Version {
			get { return this.AddinInfo.Version; }
		}
		
		public string Name {
			get { return this.AddinInfo.Name; }
		}
		
		internal string PrivateDataPath {
			get { return Path.Combine (database.AddinPrivateDataPath, Path.GetFileNameWithoutExtension (Description.FileName)); }
		}
		
		public bool SupportsVersion (string version)
		{
			return AddinInfo.SupportsVersion (version);
		}
		
		public override string ToString ()
		{
			return Id;
		}
		
		internal AddinInfo AddinInfo {
			get {
				if (addin == null) {
					try {
						addin = AddinInfo.ReadFromDescription (Description);
					} catch (Exception ex) {
						throw new InvalidOperationException ("Could not read add-in file: " + configFile, ex);
					}
				}
				return addin;
			}
		}
		
		public bool Enabled {
			get { return AddinInfo.IsRoot ? true : database.IsAddinEnabled (Description.Domain, AddinInfo.Id, true); }
			set {
				if (value)
					database.EnableAddin (Description.Domain, AddinInfo.Id, true);
				else
					database.DisableAddin (Description.Domain, AddinInfo.Id);
			}
		}
		
		public bool IsUserAddin {
			get { return configFile.StartsWith (Environment.GetFolderPath (Environment.SpecialFolder.Personal)); }
		}
		
		public string AddinFile {
			get {
				if (sourceFile == null && addin == null)
					LoadAddinInfo ();
				return sourceFile;
			}
		}
		
		void LoadAddinInfo ()
		{
			if (addin == null) {
				try {
					AddinDescription m = Description;
					sourceFile = m.AddinFile;
					addin = AddinInfo.ReadFromDescription (m);
				} catch (Exception ex) {
					throw new InvalidOperationException ("Could not read add-in file: " + configFile, ex);
				}
			}
		}
		
		public AddinDescription Description {
			get {
				if (desc != null) {
					AddinDescription d = desc.Target as AddinDescription;
					if (d != null)
						return d;
				}

				AddinDescription m;
				database.ReadAddinDescription (null, configFile, out m);
				
				if (m == null)
					throw new InvalidOperationException ("Could not read add-in description");
				if (addin == null) {
					addin = AddinInfo.ReadFromDescription (m);
					sourceFile = m.AddinFile;
				}
				desc = new WeakReference (m);
				return m;
			}
		}
			
		// returns -1 if v1 > v2
		public static int CompareVersions (string v1, string v2)
		{
			string[] a1 = v1.Split ('.');
			string[] a2 = v2.Split ('.');
			
			for (int n=0; n<a1.Length; n++) {
				if (n >= a2.Length)
					return -1;
				if (a1[n].Length == 0) {
					if (a2[n].Length != 0)
						return 1;
					continue;
				}
				try {
					int n1 = int.Parse (a1[n]);
					int n2 = int.Parse (a2[n]);
					if (n1 < n2)
						return 1;
					else if (n1 > n2)
						return -1;
				} catch {
					return 1;
				}
			}
			if (a2.Length > a1.Length)
				return 1;
			return 0;
		}
		
		public static string GetFullId (string ns, string id, string version)
		{
			string res;
			if (id.StartsWith ("::"))
				res = id.Substring (2);
			else if (ns != null && ns.Length > 0)
				res = ns + "." + id;
			else
				res = id;
			
			if (version != null && version.Length > 0)
				return res + "," + version;
			else
				return res;
		}
		
		public static string GetIdName (string addinId)
		{
			int i = addinId.IndexOf (',');
			if (i != -1)
				return addinId.Substring (0, i);
			else
				return addinId;
		}
		
		public static string GetIdVersion (string addinId)
		{
			int i = addinId.IndexOf (',');
			if (i != -1)
				return addinId.Substring (i + 1).Trim ();
			else
				return string.Empty;
		}
		
		public static void GetIdParts (string addinId, out string name, out string version)
		{
			int i = addinId.IndexOf (',');
			if (i != -1) {
				name = addinId.Substring (0, i);
				version = addinId.Substring (i+1).Trim ();
			} else {
				name = addinId;
				version = string.Empty;
			}
		}
	}
}
