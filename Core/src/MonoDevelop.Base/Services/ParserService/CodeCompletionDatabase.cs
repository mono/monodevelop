//
// CodeCompletionDatabase.cs
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
using System.Threading;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Internal.Parser;
using System.Reflection;

namespace MonoDevelop.Services
{
	internal class CodeCompletionDatabase: IDisposable
	{
		static readonly int MAX_ACTIVE_COUNT = 100;
		static readonly int MIN_ACTIVE_COUNT = 50;
		static protected readonly int FORMAT_VERSION = 3;
		
		NamespaceEntry rootNamespace;
		protected ArrayList references;
		protected Hashtable files;
		protected ParserDatabase parserDatabase;
		protected Hashtable headers;
		
		BinaryReader datareader;
		FileStream datafile;
		int currentGetTime = 0;
		bool modified;
		
		string basePath;
		string dataFile;
		
		protected Object rwlock = new Object ();
		
		public CodeCompletionDatabase (ParserDatabase parserDatabase)
		{
			this.parserDatabase = parserDatabase;
			rootNamespace = new NamespaceEntry ();
			files = new Hashtable ();
			references = new ArrayList ();
			headers = new Hashtable ();
		}
		
		public virtual void Dispose ()
		{
		}
		
		public string DataFile
		{
			get { return dataFile; }
		}
		
		protected void SetLocation (string basePath, string name)
		{
			dataFile = Path.Combine (basePath, name + ".pidb");
			this.basePath = basePath;
		}
		
		public void Rename (string name)
		{
			lock (rwlock)
			{
				Flush ();
				string oldDataFile = dataFile;
				dataFile = Path.Combine (basePath, name + ".pidb");

				CloseReader ();
				
				if (File.Exists (oldDataFile))
					File.Move (oldDataFile, dataFile);
			}
		}
		
		public virtual void Read ()
		{
			if (basePath == null)
				throw new InvalidOperationException ("Location not set");
				
			if (!File.Exists (dataFile)) return;
			
			lock (rwlock)
			{
				FileStream ifile = null;
				try 
				{
					modified = false;
					currentGetTime = 0;
					CloseReader ();
					
					Runtime.LoggingService.Info ("Reading " + dataFile);
					ifile = new FileStream (dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
					BinaryFormatter bf = new BinaryFormatter ();
					
					// Read the headers
					headers = (Hashtable) bf.Deserialize (ifile);
					int ver = (int) headers["Version"];
					if (ver != FORMAT_VERSION)
						throw new Exception ("Expected version " + FORMAT_VERSION + ", found version " + ver);
					
					// Move to the index offset and read the index
					BinaryReader br = new BinaryReader (ifile);
					long indexOffset = br.ReadInt64 ();
					ifile.Position = indexOffset;
					
					object[] data = (object[]) bf.Deserialize (ifile);
					Queue dataQueue = new Queue (data);
					references = (ArrayList) dataQueue.Dequeue ();
					rootNamespace = (NamespaceEntry)  dataQueue.Dequeue ();
					files = (Hashtable)  dataQueue.Dequeue ();
					DeserializeData (dataQueue);

					ifile.Close ();
					
				}
				catch (Exception ex)
				{
					if (ifile != null) ifile.Close ();
					Runtime.LoggingService.Info ("PIDB file '" + dataFile + "' couldn not be loaded: '" + ex.Message + "'. The file will be recreated");
					rootNamespace = new NamespaceEntry ();
					files = new Hashtable ();
					references = new ArrayList ();
					headers = new Hashtable ();
				}
			}
		}
		
		public static Hashtable ReadHeaders (string baseDir, string name)
		{
			string file = Path.Combine (baseDir, name + ".pidb");
			FileStream ifile = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryFormatter bf = new BinaryFormatter ();
			Hashtable headers = (Hashtable) bf.Deserialize (ifile);
			ifile.Close ();
			return headers;
		}
		
		public virtual void Write ()
		{
			lock (rwlock)
			{
				if (!modified) return;
				modified = false;
				headers["Version"] = FORMAT_VERSION;
							
				Runtime.LoggingService.Info ("Writing " + dataFile);
				
				string tmpDataFile = dataFile + ".tmp";
				FileStream dfile = new FileStream (tmpDataFile, FileMode.Create, FileAccess.Write, FileShare.Write);
				
				BinaryFormatter bf = new BinaryFormatter ();
				BinaryWriter bw = new BinaryWriter (dfile);
				
				// The headers are the first thing to write, so they can be read
				// without deserializing the whole file.
				bf.Serialize (dfile, headers);
				
				// The position of the index will be written here
				long indexOffsetPos = dfile.Position;
				bw.Write ((long)0);
				
				MemoryStream buffer = new MemoryStream ();
				BinaryWriter bufWriter = new BinaryWriter (buffer);
				
				// Write all class data
				foreach (FileEntry fe in files.Values) 
				{
					ClassEntry ce = fe.FirstClass;
					while (ce != null)
					{
						IClass c = ce.Class;
						byte[] data;
						int len;
						
						if (c == null) {
							// Copy the data from the source file
							if (datareader == null) {
								datafile = new FileStream (dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
								datareader = new BinaryReader (datafile);
							}
							datafile.Position = ce.Position;
							len = datareader.ReadInt32 ();
							data = new byte[len];
							datafile.Read (data, 0, len);
						}
						else {
							buffer.Position = 0;
							PersistentClass.WriteTo (c, bufWriter, parserDatabase.DefaultNameEncoder);
							data = buffer.GetBuffer ();
							len = (int)buffer.Position;
						}
						
						ce.Position = dfile.Position;
						bw.Write (len);
						bw.Write (data, 0, len);
						ce = ce.NextInFile;
					}
				}
				
				// Write the index
				long indexOffset = dfile.Position;
				
				Queue dataQueue = new Queue ();
				dataQueue.Enqueue (references);
				dataQueue.Enqueue (rootNamespace);
				dataQueue.Enqueue (files);
				SerializeData (dataQueue);
				bf.Serialize (dfile, dataQueue.ToArray ());
				
				dfile.Position = indexOffsetPos;
				bw.Write (indexOffset);
				
				bw.Close ();
				dfile.Close ();
				
				CloseReader ();
				
				if (File.Exists (dataFile))
					File.Delete (dataFile);
					
				File.Move (tmpDataFile, dataFile);
			}
		}
		
		protected virtual void SerializeData (Queue dataQueue)
		{
		}
		
		protected virtual void DeserializeData (Queue dataQueue)
		{
		}
		
		protected FileEntry GetFile (string name)
		{
			return files [name] as FileEntry;
		}
				
		void Flush ()
		{
			int activeCount = 0;
			
			foreach (FileEntry fe in files.Values) {
				ClassEntry ce = fe.FirstClass;
				while (ce != null) { 
					if (ce.Class != null) activeCount++;
					ce = ce.NextInFile;
				}
			}
			
			if (activeCount <= MAX_ACTIVE_COUNT) return;
			
			Write ();
			
			foreach (FileEntry fe in files.Values) {
				ClassEntry ce = fe.FirstClass;
				while (ce != null) { 
					if (ce.LastGetTime < currentGetTime - MIN_ACTIVE_COUNT)
						ce.Class = null;
					ce = ce.NextInFile;
				}
			}
		}
		
		IClass ReadClass (ClassEntry ce)
		{
			if (datareader == null) {
				datafile = new FileStream (dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				datareader = new BinaryReader (datafile);
			}
			datafile.Position = ce.Position;
			datareader.ReadInt32 ();	// Length of data
			return PersistentClass.Read (datareader, parserDatabase.DefaultNameDecoder);
		}
		
		void CloseReader ()
		{
			if (datareader != null) {
				datareader.Close ();
				datareader = null;
			}
		}
		
		public void Clear ()
		{
			rootNamespace = new NamespaceEntry ();
			files = new Hashtable ();
			references = new ArrayList ();
			headers = new Hashtable ();
		}
		
		public IClass GetClass (string typeName, bool caseSensitive)
		{
			lock (rwlock)
			{
//				Runtime.LoggingService.Info ("GET CLASS " + typeName + " in " + dataFile);
				string[] path = typeName.Split ('.');
				int len = path.Length - 1;
				
				NamespaceEntry nst;
				int nextPos;
				
				if (GetBestNamespaceEntry (path, len, false, caseSensitive, out nst, out nextPos)) 
				{
					ClassEntry ce = nst.GetClass (path[len], caseSensitive);
					if (ce == null) return null;
					return GetClass (ce);
				}
				else
				{
					// It may be an inner class
					ClassEntry ce = nst.GetClass (path[nextPos++], caseSensitive);
					if (ce == null) return null;
					
					len++;	// Now include class name
					IClass c = GetClass (ce);
					
					while (nextPos < len) {
						IClass nextc = null;
						for (int n=0; n<c.InnerClasses.Count && nextc == null; n++) {
							IClass innerc = c.InnerClasses[n];
							if (string.Compare (innerc.Name, path[nextPos], !caseSensitive) == 0)
								nextc = innerc;
						}
						if (nextc == null) return null;
						c = nextc;
						nextPos++;
					}
					return c;
				}
			}
		}
		
		IClass GetClass (ClassEntry ce)
		{
			ce.LastGetTime = currentGetTime++;
			if (ce.Class != null) return ce.Class;
			
			// Read the class from the file
			
			ce.Class = ReadClass (ce);
			return ce.Class;
		}		
		
		public virtual void CheckModifiedFiles ()
		{
			lock (rwlock)
			{
				foreach (FileEntry file in files.Values)
				{
					if (!File.Exists (file.FileName)) continue;
					FileInfo fi = new FileInfo (file.FileName);
					if (fi.LastWriteTime > file.LastParseTime || file.ParseErrorRetries > 0) 
						QueueParseJob (file);
				}
			}
		}
		
		protected void QueueParseJob (FileEntry file)
		{
			// Change date now, to avoid reparsing if CheckModifiedFiles is called again
			// before the parse job is executed
			
			FileInfo fi = new FileInfo (file.FileName);
			file.LastParseTime = fi.LastWriteTime;
			parserDatabase.QueueParseJob (new JobCallback (ParseCallback), file.FileName);
		}
		
		void ParseCallback (object ob, IProgressMonitor monitor)
		{
			lock (rwlock)
			{
				ParseFile ((string)ob, monitor);
			}
		}
		
		protected virtual void ParseFile (string fileName, IProgressMonitor monitor)
		{
		}
		
		public void ParseAll ()
		{
			lock (rwlock)
			{
				foreach (FileEntry fe in files.Values) 
					ParseFile (fe.FileName, null);
			}
		}
		
		protected void AddReference (string uri)
		{
			lock (rwlock)
			{
				ReferenceEntry re = new ReferenceEntry (uri);
				references.Add (re);
				modified = true;
			}
		}
		
		protected void RemoveReference (string uri)
		{
			lock (rwlock)
			{
				for (int n=0; n<references.Count; n++)
				{
					if (((ReferenceEntry)references[n]).Uri == uri) {
						references.RemoveAt (n);
						modified = true;
						return;
					}
				}
			}
		}
		
		protected bool HasReference (string uri)
		{
			for (int n=0; n<references.Count; n++) {
				ReferenceEntry re = (ReferenceEntry) references[n];
				if (re.Uri == uri)
					return true;
			}
			return false;
		}
		
		public void AddFile (string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = new FileEntry (fileName);
				files [fileName] = fe;
				modified = true;
			}
		}
		
		public void RemoveFile (string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = files [fileName] as FileEntry;
				if (fe == null) return;
				
				ClassEntry ce = fe.FirstClass;
				while (ce != null) {
					ce.NamespaceRef.Remove (ce.Name);
					ce = ce.NextInFile;
				}
				
				files.Remove (fileName);
				modified = true;
			}
		}
		
		public ClassUpdateInformation UpdateClassInformation (ClassCollection newClasses, string fileName)
		{
			lock (rwlock)
			{
				ClassUpdateInformation res = new ClassUpdateInformation ();
				
				FileEntry fe = files [fileName] as FileEntry;
				if (fe == null) return null;
				
				bool[] added = new bool [newClasses.Count];
				NamespaceEntry[] newNss = new NamespaceEntry [newClasses.Count];
				for (int n=0; n<newClasses.Count; n++) {
					string[] path = newClasses[n].Namespace.Split ('.');
					newNss[n] = GetNamespaceEntry (path, path.Length, true, true);
				}
				
				ArrayList newFileClasses = new ArrayList ();
				
				if (fe != null)
				{
					ClassEntry ce = fe.FirstClass;
					while (ce != null)
					{
						IClass newClass = null;
						for (int n=0; n<newClasses.Count && newClass == null; n++) {
							IClass uc = newClasses [n];
							if (uc.Name == ce.Name && newNss[n] == ce.NamespaceRef) {
								newClass = uc;
								added[n] = true;
							}
						}
						
						if (newClass != null) {
							// Class found, replace it
							ce.Class = CopyClass (newClass);
							ce.LastGetTime = currentGetTime++;
							newFileClasses.Add (ce);
							res.Modified.Add (ce.Class);
						}
						else {
							// Class not found, it has to be deleted, unless it has
							// been added in another file
							if (ce.FileEntry == fe) {
								IClass c = ce.Class;
								if (c == null) c = ReadClass (ce);
								res.Removed.Add (c);
								ce.NamespaceRef.Remove (ce.Name);
							}
						}
						ce = ce.NextInFile;
					}
				}
				
				if (fe == null) {
					fe = new FileEntry (fileName);
					files [fileName] = fe;
				}
				
				for (int n=0; n<newClasses.Count; n++) {
					if (!added[n]) {
						IClass c = CopyClass (newClasses[n]);
						ClassEntry ce = new ClassEntry (c, fe, newNss[n]);
						ce.LastGetTime = currentGetTime++;
						newNss[n].Add (c.Name, ce);
						newFileClasses.Add (ce);
						res.Added.Add (c);
					}
				}
				
				fe.SetClasses (newFileClasses);
				rootNamespace.Clean ();
				fe.LastParseTime = DateTime.Now;
				modified = true;
				Flush ();
				
				return res;
			}
		}
		
		public void GetNamespaceContents (ArrayList list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock)
			{
				string[] path = subNameSpace.Split ('.');
				NamespaceEntry tns = GetNamespaceEntry (path, path.Length, false, caseSensitive);
				if (tns == null) return;
				
				foreach (DictionaryEntry en in tns.Contents) {
					if (en.Value is NamespaceEntry)
						list.Add (en.Key);
					else
						list.Add (GetClass ((ClassEntry)en.Value));
				}
			}
		}
		
		public void GetClassList (ArrayList list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock)
			{
				string[] path = subNameSpace.Split ('.');
				NamespaceEntry tns = GetNamespaceEntry (path, path.Length, false, caseSensitive);
				if (tns == null) return;
				
				foreach (DictionaryEntry en in tns.Contents) {
					if (en.Value is ClassEntry && !list.Contains (en.Key))
						list.Add (en.Key);
				}
			}
		}
		
		public IClass[] GetClassList ()
		{
			lock (rwlock)
			{
				ArrayList list = new ArrayList ();
				foreach (FileEntry fe in files.Values) {
					ClassEntry ce = fe.FirstClass;
					while (ce != null) {
						list.Add (GetClass (ce));
						ce = ce.NextInFile;
					}
				}
				return (IClass[]) list.ToArray (typeof(IClass));
			}
		}
		
		public void GetNamespaceList (ArrayList list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock)
			{
				string[] path = subNameSpace.Split ('.');
				NamespaceEntry tns = GetNamespaceEntry (path, path.Length, false, caseSensitive);
				if (tns == null) return;
				
				foreach (DictionaryEntry en in tns.Contents) {
					if (en.Value is NamespaceEntry && !list.Contains (en.Key))
						list.Add (en.Key);
				}
			}
		}
		
		public bool NamespaceExists (string name, bool caseSensitive)
		{
			lock (rwlock)
			{
				string[] path = name.Split ('.');
				NamespaceEntry tns = GetNamespaceEntry (path, path.Length, false, caseSensitive);
				return tns != null;
			}
		}
		
		public ICollection References
		{
			get { return references; }
		}
		
		public IClass[] GetFileContents (string fileName)
		{
			FileEntry fe = GetFile (fileName);
			if (fe == null) return new IClass [0];

			ArrayList classes = new ArrayList ();
			ClassEntry ce = fe.FirstClass;
			while (ce != null) {
				classes.Add (GetClass (ce));
				ce = ce.NextInFile;
			}
			return (IClass[]) classes.ToArray (typeof(IClass));
		}
		
		IClass CopyClass (IClass cls)
		{
			MemoryStream ms = new MemoryStream ();
			BinaryWriter bw = new BinaryWriter (ms);
			PersistentClass.WriteTo (cls, bw, parserDatabase.DefaultNameEncoder);
			bw.Flush ();
			ms.Position = 0;
			BinaryReader br = new BinaryReader (ms);
			return PersistentClass.Read (br, parserDatabase.DefaultNameDecoder);
		}
		
		bool GetBestNamespaceEntry (string[] path, int length, bool createPath, bool caseSensitive, out NamespaceEntry lastEntry, out int numMatched)
		{
			lastEntry = rootNamespace;

			if (length == 0 || (length == 1 && path[0] == "")) {
				numMatched = length;
				return true;
			}
			else
			{
				for (int n=0; n<length; n++) {
					NamespaceEntry nh = lastEntry.GetNamespace (path[n], caseSensitive);
					if (nh == null) {
						if (!createPath) {
							numMatched = n;
							return false;
						}
						
						nh = new NamespaceEntry ();
						lastEntry.Add (path[n], nh);
					}
					lastEntry = nh;
				}
				numMatched = length;
				return true;
			}
		}
		
		NamespaceEntry GetNamespaceEntry (string[] path, int length, bool createPath, bool caseSensitive)
		{
			NamespaceEntry nst;
			int matched;
			
			if (GetBestNamespaceEntry (path, length, createPath, caseSensitive, out nst, out matched))
				return nst;
			else
				return null;
		}
	}

	public interface INameEncoder
	{
		int GetStringId (string text);
	}
	
	public interface INameDecoder
	{
		string GetStringValue (int id);
	}
	
	
	public class StringNameTable: INameEncoder, INameDecoder
	{
		string[] table;
		
		public StringNameTable (string[] names)
		{
			table = names;
			Array.Sort (table);
		}
		
		public string GetStringValue (int id)
		{
			return table [id];
		}
		
		public int GetStringId (string text)
		{
			int i = Array.BinarySearch (table, text);
			if (i >= 0) return i;
			else return -1;
		}
	}
}
