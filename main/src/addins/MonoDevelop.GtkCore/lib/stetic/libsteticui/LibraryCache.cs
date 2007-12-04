
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Stetic
{
	static class LibraryCache
	{
		static string cachePath;
		static XmlDocument indexDoc;
		
		static LibraryCache ()
		{
			cachePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".config");
			cachePath = Path.Combine (cachePath, "stetic");
			cachePath = Path.Combine (cachePath, "desc-cache");
		}
		
		static XmlDocument GetIndex ()
		{
			if (indexDoc != null)
				return indexDoc;
			
			string index = Path.Combine (cachePath, "index.xml");
			if (!File.Exists (index))
				return null;
			
			try {
				XmlDocument doc = new XmlDocument ();
				doc.Load (index);
				return doc;
			} catch {
				return null;
			}
		}
		
		static void SaveIndex (XmlDocument doc)
		{
			if (!Directory.Exists (cachePath))
				Directory.CreateDirectory (cachePath);
			
			// Purge the file list
			
			ArrayList todel = new ArrayList ();
			foreach (XmlElement elem in doc.DocumentElement.SelectNodes ("lib")) {
				string file = elem.GetAttribute ("file");
				if (!File.Exists (file)) {
					todel.Add (elem);
					string path = elem.GetAttribute ("cached");
					foreach (string cf in Directory.GetFiles (cachePath, path + "*")) {
						try {
							File.Delete (cf);
						} catch (Exception ex) {
							// Ignore exception
							Console.WriteLine (ex);
						}
					}
				}
			}
			
			foreach (XmlElement elem in todel)
				doc.DocumentElement.RemoveChild (elem);
			
			string index = Path.Combine (cachePath, "index.xml");
			doc.Save (index);
			indexDoc = doc;
		}
		
		public static string GetCachedFilePath (string assemblyPath)
		{
			assemblyPath = Path.GetFullPath (assemblyPath);
			
			XmlDocument doc = GetIndex ();
			if (doc == null)
				return null;
				
			try {
				XmlElement elem = (XmlElement) doc.SelectSingleNode ("index/lib[@file='" + assemblyPath + "']");
				if (elem == null)
					return null;
				
				DateTime ts = XmlConvert.ToDateTime (elem.GetAttribute ("timestamp"), XmlDateTimeSerializationMode.Local);
				DateTime lastWrite = File.GetLastWriteTime (assemblyPath);
				if (ts != lastWrite)
					return null;
				
				return Path.Combine (cachePath, elem.GetAttribute ("cached"));
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				return null;
			}
		}
		
		public static string UpdateCachedFile (string assemblyPath)
		{
			assemblyPath = Path.GetFullPath (assemblyPath);
			
			XmlDocument doc = GetIndex ();
			if (doc == null) {
				doc = new XmlDocument ();
				doc.AppendChild (doc.CreateElement ("index"));
			}
			
			try {
				XmlElement elem = (XmlElement) doc.DocumentElement.SelectSingleNode ("lib[@file='" + assemblyPath + "']");
				if (elem == null) {
					elem = doc.CreateElement ("lib");
					elem.SetAttribute ("file", assemblyPath);
					doc.DocumentElement.AppendChild (elem);
				}
				
				DateTime lastWrite = File.GetLastWriteTime (assemblyPath);
				elem.SetAttribute ("timestamp", XmlConvert.ToString (lastWrite, XmlDateTimeSerializationMode.Local));
				
				string s = doc.DocumentElement.GetAttribute ("cindex");
				if (s.Length == 0) s = "0";
				else s = (int.Parse (s) + 1).ToString ();
				doc.DocumentElement.SetAttribute ("cindex", s);
				
				string cached = elem.GetAttribute ("cached");
				if (cached.Length == 0) {
					cached = Path.GetFileNameWithoutExtension (assemblyPath) + "_" + s;
					elem.SetAttribute ("cached", cached);
				}
				
				SaveIndex (doc);
				return Path.Combine (cachePath, cached);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				return null;
			}
		}
	}
}
