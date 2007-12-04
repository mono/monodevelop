//
// AddinScanFolderInfo.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Serialization;

namespace Mono.Addins.Database
{
	class AddinScanFolderInfo: IBinaryXmlElement
	{
		Hashtable files = new Hashtable ();
		string folder;
		string fileName;
		string domain;
		bool sharedFolder = true;
		
		static BinaryXmlTypeMap typeMap = new BinaryXmlTypeMap (
			typeof(AddinScanFolderInfo),
			typeof(AddinFileInfo)
		);
		
		internal AddinScanFolderInfo ()
		{
		}
		
		public AddinScanFolderInfo (string folder)
		{
			this.folder = folder;
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public static AddinScanFolderInfo Read (FileDatabase filedb, string file)
		{
			AddinScanFolderInfo finfo = (AddinScanFolderInfo) filedb.ReadSharedObject (file, typeMap);
			if (finfo != null)
				finfo.fileName = file;
			return finfo;
		}
		
		public static AddinScanFolderInfo Read (FileDatabase filedb, string basePath, string folderPath)
		{
			string fileName;
			AddinScanFolderInfo finfo = (AddinScanFolderInfo) filedb.ReadSharedObject (basePath, GetDomain (folderPath), ".data", Util.GetFullPath (folderPath), typeMap, out fileName);
			if (finfo != null)
				finfo.fileName = fileName;
			return finfo;
		}
		
		static string GetDomain (string path)
		{
			path = Util.GetFullPath (path);
			string s = path.Replace (Path.DirectorySeparatorChar, '_');
			s = s.Replace (Path.AltDirectorySeparatorChar, '_');
			s = s.Replace (Path.VolumeSeparatorChar, '_');
			s = s.Trim ('_');
			if (s.Length > 200)
				s = s.Substring (s.Length - 200);
			return s;
		}
		
		public void Write (FileDatabase filedb, string basePath)
		{
			filedb.WriteSharedObject (basePath, GetDomain (folder), ".data", Util.GetFullPath (folder), fileName, typeMap, this);
		}
		
		public string GetExistingLocalDomain ()
		{
			foreach (AddinFileInfo info in files.Values) {
				if (info.Domain != null && info.Domain != AddinDatabase.GlobalDomain)
					return info.Domain;
			}
			return AddinDatabase.GlobalDomain;
		}
		
		public string Folder {
			get { return folder; }
		}

		public string Domain {
			get {
				if (sharedFolder)
					return AddinDatabase.GlobalDomain;
				else
					return domain;
			}
			set {
				domain = value;
				sharedFolder = true;
			}
		}
		
		public string RootsDomain {
			get {
				return domain;
			}
			set {
				domain = value;
			}
		}
		
		public string GetDomain (bool isRoot)
		{
			if (isRoot)
				return RootsDomain;
			else
				return Domain;
		}
		
		public bool SharedFolder {
			get {
				return sharedFolder;
			}
			set {
				sharedFolder = value;
			}
		}
		
		public DateTime GetLastScanTime (string file)
		{
			AddinFileInfo info = (AddinFileInfo) files [file];
			if (info == null)
				return DateTime.MinValue;
			else
				return info.LastScan;
		}
		
		public AddinFileInfo GetAddinFileInfo (string file)
		{
			return (AddinFileInfo) files [file];
		}
		
		public void SetLastScanTime (string file, string addinId, bool isRoot, DateTime time, bool scanError)
		{
			AddinFileInfo info = (AddinFileInfo) files [file];
			if (info == null) {
				info = new AddinFileInfo ();
				info.File = file;
				files [file] = info;
			}
			info.LastScan = time;
			info.AddinId = addinId;
			info.IsRoot = isRoot;
			info.ScanError = scanError;
			if (addinId != null)
				info.Domain = GetDomain (isRoot);
			else
				info.Domain = null;
		}
		
		public ArrayList GetMissingAddins ()
		{
			ArrayList missing = new ArrayList ();
			
			if (!Directory.Exists (folder)) {
				// All deleted
				foreach (AddinFileInfo info in files.Values) {
					if (info.IsAddin)
						missing.Add (info);
				}
				files.Clear ();
				return missing;
			}
			ArrayList toDelete = new ArrayList ();
			foreach (AddinFileInfo info in files.Values) {
				if (!File.Exists (info.File)) {
					if (info.IsAddin)
						missing.Add (info);
					toDelete.Add (info.File);
				}
				else if (info.IsAddin && info.Domain != GetDomain (info.IsRoot)) {
					missing.Add (info);
				}
			}
			foreach (string file in toDelete)
				files.Remove (file);
				
			return missing;
		}
		
		void IBinaryXmlElement.Write (BinaryXmlWriter writer)
		{
			if (files.Count == 0) {
				domain = null;
				sharedFolder = true;
			}
			writer.WriteValue ("folder", folder);
			writer.WriteValue ("files", files);
			writer.WriteValue ("domain", domain);
			writer.WriteValue ("sharedFolder", sharedFolder);
		}
		
		void IBinaryXmlElement.Read (BinaryXmlReader reader)
		{
			folder = reader.ReadStringValue ("folder");
			reader.ReadValue ("files", files);
			domain = reader.ReadStringValue ("domain");
			sharedFolder = reader.ReadBooleanValue ("sharedFolder");
		}
	}
	
	
	class AddinFileInfo: IBinaryXmlElement
	{
		public string File;
		public DateTime LastScan;
		public string AddinId;
		public bool IsRoot;
		public bool ScanError;
		public string Domain;
		
		public bool IsAddin {
			get { return AddinId != null && AddinId.Length != 0; }
		}
		
		void IBinaryXmlElement.Write (BinaryXmlWriter writer)
		{
			writer.WriteValue ("File", File);
			writer.WriteValue ("LastScan", LastScan);
			writer.WriteValue ("AddinId", AddinId);
			writer.WriteValue ("IsRoot", IsRoot);
			writer.WriteValue ("ScanError", ScanError);
			writer.WriteValue ("Domain", Domain);
		}
		
		void IBinaryXmlElement.Read (BinaryXmlReader reader)
		{
			File = reader.ReadStringValue ("File");
			LastScan = reader.ReadDateTimeValue ("LastScan");
			AddinId = reader.ReadStringValue ("AddinId");
			IsRoot = reader.ReadBooleanValue ("IsRoot");
			ScanError = reader.ReadBooleanValue ("ScanError");
			Domain = reader.ReadStringValue ("Domain");
		}
	}
}
