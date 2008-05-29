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

//#define CHECK_STRINGS

using System;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using System.Reflection;
/*
namespace MonoDevelop.Projects.Dom
{
	
	internal class CodeCompletionDatabase: IDisposable
	{
		static protected readonly int MAX_ACTIVE_COUNT = 100;
		static protected readonly int MIN_ACTIVE_COUNT = 10;
		static protected readonly int FORMAT_VERSION = 27;
		
		NamespaceEntry rootNamespace;
		protected ArrayList references;
		protected Hashtable files;
		protected ParserDatabase parserDatabase;
		protected Hashtable headers;
		
		BinaryReader datareader;
		FileStream datafile;
		int currentGetTime = 0;
		bool modified;
		bool disposed;
		
		string basePath;
		string dataFile;
		
		// This table is a cache of instantiated generic types. It is not stored
		// in disk, it's created under demand when a specific type is requested.
		Hashtable instantiatedGenericTypes = new Hashtable ();
		
		// This table stores type->subclasses relations for types which are not
		// known in this database. For example, types declared in other databases.
		// For known types, the type->subclasses relation is stored in the corresponding
		// ClassEntry object, not here. Inner classes don't have a class entry, so their
		// relations are also stored here.
		// The key of the hashtable is the full name of a type. The value is an ArrayList
		// which can contain ClassEntry objects, or other full type names (this second case
		// is only used for inner classes).
		Hashtable unresolvedSubclassTable = new Hashtable ();
		
		protected Object rwlock = new Object ();
		
		public CodeCompletionDatabase (ParserDatabase parserDatabase)
		{
			this.parserDatabase = parserDatabase;
			rootNamespace = new NamespaceEntry (null, null);
			files = new Hashtable ();
			references = new ArrayList ();
			headers = new Hashtable ();
			
			PropertyService.PropertyChanged += new EventHandler<PropertyChangedEventArgs> (OnPropertyUpdated);	
		}
		
		public virtual void Dispose ()
		{
			PropertyService.PropertyChanged -= new EventHandler<PropertyChangedEventArgs> (OnPropertyUpdated);
			disposed = true;
		}
		
		public string DataFile
		{
			get { return dataFile; }
		}
		
		public bool Modified {
			get { return modified; }
			set { modified = value; }
		}
		
		public bool Disposed {
			get { return disposed; }
		}
		
		public virtual SolutionItem SourceEntry {
			get { return null; }
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
					FileService.MoveFile (oldDataFile, dataFile);
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
					
					LoggingService.LogDebug ("Reading " + dataFile);
					
					ifile = new FileStream (dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
					BinaryFormatter bf = new BinaryFormatter ();
					
					// Read the headers
					headers = (Hashtable) bf.Deserialize (ifile);
					int ver = (int) headers["Version"];
					if (ver != FORMAT_VERSION)
						throw new OldPidbVersionException (ver, FORMAT_VERSION);
					
					// Move to the index offset and read the index
					BinaryReader br = new BinaryReader (ifile);
					long indexOffset = br.ReadInt64 ();
					ifile.Position = indexOffset;
					
					object[] data = (object[]) bf.Deserialize (ifile);
					Queue dataQueue = new Queue (data);
					references = (ArrayList) dataQueue.Dequeue ();
					rootNamespace = (NamespaceEntry)  dataQueue.Dequeue ();
					files = (Hashtable)  dataQueue.Dequeue ();
					unresolvedSubclassTable = (Hashtable) dataQueue.Dequeue ();
					DeserializeData (dataQueue);

					ifile.Close ();
				}
				catch (Exception ex)
				{
					if (ifile != null) ifile.Close ();
					OldPidbVersionException opvEx = ex as OldPidbVersionException;
					if (opvEx != null)
						LoggingService.LogWarning ("PIDB file '{0}' could not be loaded. Expected version {1}, found version {2}'. The file will be recreated.", dataFile, opvEx.ExpectedVersion, opvEx.FoundVersion);
					else
						LoggingService.LogError ("PIDB file '{0}' could not be loaded: '{1}'. The file will be recreated.", dataFile, ex.Message);
					rootNamespace = new NamespaceEntry (null, null);
					files = new Hashtable ();
					references = new ArrayList ();
					headers = new Hashtable ();
					unresolvedSubclassTable = new Hashtable ();
				}
			}
			
			// Notify read comments
			foreach (FileEntry fe in files.Values)
			{
				if (! fe.IsAssembly && fe.CommentTasks != null)
					parserDatabase.UpdatedCommentTasks (fe);
			}
			
			// Update comments if needed...
			PropertyChangedEventArgs args = new PropertyChangedEventArgs ("Monodevelop.TaskListTokens", LastValidTaskListTokens, PropertyService.Get ("Monodevelop.TaskListTokens", ""));
			this.OnPropertyUpdated (null, args);
		}
		
		private class OldPidbVersionException : Exception
		{
			public int FoundVersion;
			public int ExpectedVersion;
			
			public OldPidbVersionException (int foundVersion, int expectedVersion)
			{
				FoundVersion = foundVersion;
				ExpectedVersion = expectedVersion;
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
				headers["LastValidTaskListTokens"] = (string)PropertyService.Get ("Monodevelop.TaskListTokens", "");

				LoggingService.LogDebug ("Writing " + dataFile);
				
				string tmpDataFile = dataFile + ".tmp";
				FileStream dfile = new FileStream (tmpDataFile, FileMode.Create, FileAccess.Write, FileShare.Write);
				
				BinaryFormatter bf = new BinaryFormatter ();
				BinaryWriter bw = new BinaryWriter (dfile);
				
				try {
					// The headers are the first thing to write, so they can be read
					// without deserializing the whole file.
					bf.Serialize (dfile, headers);
					
					// The position of the index will be written here
					long indexOffsetPos = dfile.Position;
					bw.Write ((long)0);
					
					MemoryStream buffer = new MemoryStream ();
					BinaryWriter bufWriter = new BinaryWriter (buffer);
					
					// Write all class data
					foreach (ClassEntry ce in GetAllClasses ()) 
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
							
							// Sanity check to avoid allocating huge byte arrays if something
							// goes wrong when reading the file contents
							if (len > 1024*1024*10 || len < 0)
								throw new InvalidOperationException ("pidb file corrupted: " + dataFile);

							data = new byte[len];
							int nr = 0;
							while (nr < len)
								nr += datafile.Read (data, nr, len - nr);
						}
						else {
							buffer.Position = 0;
							PersistentClass.WriteTo (c, bufWriter, parserDatabase.DefaultNameEncoder);
							bufWriter.Flush ();
							data = buffer.GetBuffer ();
							len = (int)buffer.Position;
						}
						
						ce.Position = dfile.Position;
						bw.Write (len);
						bw.Write (data, 0, len);
					}
					
					bw.Flush ();
					
					// Write the index
					long indexOffset = dfile.Position;
					
					Queue dataQueue = new Queue ();
					dataQueue.Enqueue (references);
					dataQueue.Enqueue (rootNamespace);
					dataQueue.Enqueue (files);
					dataQueue.Enqueue (unresolvedSubclassTable);
					SerializeData (dataQueue);
					bf.Serialize (dfile, dataQueue.ToArray ());
					
					dfile.Position = indexOffsetPos;
					bw.Write (indexOffset);
					
					bw.Close ();
					dfile.Close ();
					dfile = null;
					
					CloseReader ();
					
					if (File.Exists (dataFile))
						FileService.DeleteFile (dataFile);
						
					FileService.MoveFile (tmpDataFile, dataFile);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					if (dfile != null)
						dfile.Close ();
					if (File.Exists (tmpDataFile))
						FileService.DeleteFile (tmpDataFile);
				}
			}
			
#if CHECK_STRINGS
			StringNameTable.PrintTop100 ();
#endif
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
		
		protected IEnumerable<FileEntry> GetAllFiles ()
		{
			foreach (FileEntry fe in files.Values)
				yield return fe;
		}

		internal IEnumerable<ClassEntry> GetAllClasses ()
		{
			return rootNamespace.GetAllClasses ();
		}
		
		public void Flush ()
		{
			// Saves the database if it has too much information
			// in memory. A parser database can't have more
			// MAX_ACTIVE_COUNT classes loaded in memory at the
			// same time.

			int activeCount = 0;
			
			foreach (ClassEntry ce in GetAllClasses ()) {
				if (ce.Class != null)
					activeCount++;
			}
			
			if (activeCount <= MAX_ACTIVE_COUNT) return;
			
			Write ();
			
			foreach (ClassEntry ce in GetAllClasses ()) {
				if (ce.LastGetTime < currentGetTime - MIN_ACTIVE_COUNT)
					ce.Class = null;
			}
		}
		
		internal IClass ReadClass (ClassEntry ce)
		{
			lock (rwlock)
			{
				if (datareader == null) {
					datafile = new FileStream (dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
					datareader = new BinaryReader (datafile);
				}
				datafile.Position = ce.Position;
				datareader.ReadInt32 ();	// Length of data

				DefaultClass cls = PersistentClass.Read (datareader, parserDatabase.DefaultNameDecoder);
				cls.SourceProject = SourceEntry;
				return cls;
			}
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
			rootNamespace = new NamespaceEntry (null, null);
			files = new Hashtable ();
			references = new ArrayList ();
			headers = new Hashtable ();
		}
		
		public IClass GetClass (string typeName, ReturnTypeList genericArguments, bool caseSensitive)
		{
			lock (rwlock)
			{
				if (genericArguments != null && genericArguments.Count > 0) {
					IClass templateClass = GetClass (typeName, null, caseSensitive);
					if (templateClass == null)
						return null;

					if (templateClass.GenericParameters == null || (templateClass.GenericParameters.Count != genericArguments.Count))
						return null;
			
					string tname = DefaultClass.GetInstantiatedTypeName (templateClass.FullyQualifiedName, genericArguments);
					IClass res = (IClass) instantiatedGenericTypes [tname];
					if (res == null) {
						res = DefaultClass.CreateInstantiatedGenericType (templateClass, genericArguments);
						instantiatedGenericTypes [tname] = res;
					}
					return res;
				}
				
				// It may be an instantiated generic type 
				IClass igt = (IClass) instantiatedGenericTypes [typeName];
				if (igt != null)
					return igt;
				
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
		
		internal IClass GetClass (ClassEntry ce)
		{
			ce.LastGetTime = currentGetTime++;
			if (ce.Class != null)
				return ce.Class;
			else
				return new ClassWrapper (this, ce);
		}
		
		public IEnumerable GetSubclasses (string fullName, string[] namespaces)
		{
			ArrayList nsubs = (ArrayList) unresolvedSubclassTable [fullName];
			ArrayList csubs = null;
			IList nsList = namespaces;
			
			ClassEntry ce = FindClassEntry (fullName);
			if (ce != null)
				csubs = ce.Subclasses;

			foreach (ArrayList subs in new object[] { nsubs, csubs }) {
				if (subs == null)
					continue;
				foreach (object ob in subs) {
					if (ob is ClassEntry) {
						string ns = ((ClassEntry) ob).NamespaceRef.FullName;
						if (namespaces == null || nsList.Contains (ns))
							yield return GetClass ((ClassEntry)ob);
					}
					else {
						// It's a full class name
						IClass cls = this.GetClass ((string)ob, null, true);
						if (cls != null && (namespaces == null || nsList.Contains (cls.Namespace)))
							yield return cls;
					}
				}
			}
		}
		
		void OnPropertyUpdated (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "Monodevelop.TaskListTokens")
			{
				// Update LastValidTagComments
				headers["LastValidTagComments"] = (string)e.NewValue;
				
				List<string> oldTokensList = new List<string> ();
				if (e.OldValue != null)
				{
					string[] tokens = ((string)e.OldValue).Split (';');
					foreach (string token in tokens)
					{
						int pos = token.IndexOf (':');
						if (pos != -1)
							oldTokensList.Add (token.Substring (0, pos));
					}
				}
				List<string> newTokensList = new List<string> ();
				if (e.NewValue != null)
				{
					string[] tokens = ((string)e.NewValue).Split (';');
					foreach (string token in tokens)
					{
						int pos = token.IndexOf (':');
						if (pos != -1)
							newTokensList.Add (token.Substring (0, pos));
					}
				}
				
				// Check if tokens just reordered or are the same
				if (oldTokensList.Count == newTokensList.Count)
				{
					bool tokensFound = true;
					foreach (string token in newTokensList)
					{	
						if (oldTokensList.Contains (token)) continue;
						tokensFound = false;
						break;
					}
					if (tokensFound) return;
				}
				
				// Check if some token(s) just removed
				if (oldTokensList.Count >= newTokensList.Count)
				{
					bool newTokenFound = false;
					foreach (string token in newTokensList)
					{	
						if (oldTokensList.Contains (token)) continue;
						newTokenFound = true;
						break;
					}
					if (!newTokenFound)
					{
						List<string> removedTokensList = new List<string> ();
						foreach (string token in oldTokensList)
						{	
							if (!newTokensList.Contains (token))
								removedTokensList.Add (token);
						}
						
						// Remove them from FileEntry data
						foreach (string token in removedTokensList)
							RemoveSpecialCommentTag (token);
	                	return;
					}
				}
				
				QueueAllFilesForParse ();
			}
		}
	
		public TagCollection GetSpecialComments (string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = files[fileName] as FileEntry;
				return fe != null ? fe.CommentTasks : null;
			}
		}
		
		public void UpdateTagComments (TagCollection tags, string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = files[fileName] as FileEntry;
				if (fe != null)
					fe.CommentTasks = tags;
			}
		}
		
		void RemoveSpecialCommentTag (string token)
		{
			foreach (FileEntry fe in files.Values)
			{
				if (fe.CommentTasks != null) {
					List<Tag> markedTags = new List<Tag> ();
					foreach (Tag tag in fe.CommentTasks)
						if (tag.Key == token) markedTags.Add (tag);
					foreach (Tag tag in markedTags)
						fe.CommentTasks.Remove (tag);
					parserDatabase.UpdatedCommentTasks (fe);
				}
			}
		}
		
		string LastValidTaskListTokens
		{
			get
			{
				return (string)headers["LastValidTaskListTokens"];
			}
		}
		
		public void UpdateDatabase ()
		{
			ArrayList list = GetModifiedFileEntries ();
			foreach (FileEntry file in list)
				ParseFile (file.FileName, null);
		}

		public virtual void CheckModifiedFiles ()
		{
			ArrayList list = GetModifiedFileEntries ();
			foreach (FileEntry file in list)
				QueueParseJob (file);
		}
		
		protected ArrayList GetModifiedFileEntries ()
		{
			ArrayList list = new ArrayList ();
			lock (rwlock)
			{
				foreach (FileEntry file in files.Values) {
					if (IsFileModified (file))
						list.Add (file);
				}
			}
			return list;
		}
		
		protected virtual bool IsFileModified (FileEntry file)
		{
			if (!File.Exists (file.FileName))
				return false;
			FileInfo fi = new FileInfo (file.FileName);
			return ((fi.LastWriteTime > file.LastParseTime || file.ParseErrorRetries > 0) && !file.DisableParse);
		}
		
		protected void QueueParseJob (FileEntry file)
		{
			if (file.InParseQueue)
				return;

			file.InParseQueue = true;
			parserDatabase.QueueParseJob (this, new JobCallback (ParseCallback), file.FileName);
		}
		
		protected void QueueAllFilesForParse ()
		{
			lock (rwlock)
			{
				foreach (FileEntry file in files.Values)
					file.LastParseTime = DateTime.MinValue;
			}
			CheckModifiedFiles ();
		}
		
		void ParseCallback (object ob, IProgressMonitor monitor)
		{
			string fileName = (string) ob;
			ParseFile (fileName, monitor);
			lock (rwlock) {
				FileEntry file = GetFile (fileName);
				if (file != null) {
					file.InParseQueue = false;
					FileInfo fi = new FileInfo (fileName);
					file.LastParseTime = fi.LastWriteTime;
				}
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
				// Create a new list because the reference list is accessible through a public property
				ReferenceEntry re = new ReferenceEntry (uri);
				ArrayList list = (ArrayList) references.Clone ();
				list.Add (re);
				references = list;
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
						ArrayList list = (ArrayList) references.Clone ();
						list.RemoveAt (n);
						references = list;
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
		
		public FileEntry AddFile (string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = new FileEntry (fileName);
				files [fileName] = fe;
				modified = true;
				return fe;
			}
		}
		
		public void RemoveFile (string fileName)
		{
			lock (rwlock)
			{
				ClassUpdateInformation classInfo = new ClassUpdateInformation ();
				
				FileEntry fe = files [fileName] as FileEntry;
				if (fe == null) return;
				
				foreach (ClassEntry ce in fe.ClassEntries) {
					if (ce.Class == null) ce.Class = ReadClass (ce);
					IClass c = CompoundClass.RemoveFile (ce.Class, fileName);
					if (c == null) {
						classInfo.Removed.Add (ce.Class);
						RemoveSubclassReferences (ce);
						UnresolveSubclasses (ce);
						ce.NamespaceRef.Remove (ce.Name);
					} else
						ce.Class = c;
				}
				
				files.Remove (fileName);
				modified = true;

				OnFileRemoved (fileName, classInfo);
			}
		}
		
		protected virtual void OnFileRemoved (string fileName, ClassUpdateInformation classInfo)
		{
		}
		
		public ClassUpdateInformation UpdateClassInformation (ClassCollection newClasses, string fileName)
		{
			lock (rwlock)
			{
				ClassUpdateInformation res = new ClassUpdateInformation ();
				
				FileEntry fe = files [fileName] as FileEntry;
				if (fe == null) return null;
				
				// Get the namespace entry for each class
				
				bool[] added = new bool [newClasses.Count];
				NamespaceEntry[] newNss = new NamespaceEntry [newClasses.Count];
				for (int n=0; n<newClasses.Count; n++) {
					string[] path = newClasses[n].Namespace.Split ('.');
					((DefaultClass)newClasses[n]).SourceProject = SourceEntry;
					newNss[n] = GetNamespaceEntry (path, path.Length, true, true);
				}
				
				ArrayList newFileClasses = new ArrayList ();
				
				if (fe != null)
				{
					foreach (ClassEntry ce in fe.ClassEntries)
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
							// Class already in the database, update it
							if (ce.Class == null) ce.Class = ReadClass (ce);
							RemoveSubclassReferences (ce);
							ce.Class = CompoundClass.MergeClass (ce.Class, CopyClass (newClass));
							AddSubclassReferences (ce);							
							
							ce.LastGetTime = currentGetTime++;
							newFileClasses.Add (ce);
							res.Modified.Add (ce.Class);
						}
						else {
							// Database class not found in the new class list, it has to be deleted
							IClass c = ce.Class;
							if (c == null) {
								ce.Class = ReadClass (ce);
								c = ce.Class;
							}
							IClass removed = CompoundClass.RemoveFile (c, fileName);
							if (removed != null) {
								// It's still a compound class
								ce.Class = removed;
								AddSubclassReferences (ce);							
								res.Modified.Add (removed);
							} else {
								// It's not a compoudnd class. Remove it.
								RemoveSubclassReferences (ce);
								UnresolveSubclasses (ce);
								res.Removed.Add (c);
								ce.NamespaceRef.Remove (ce.Name);
							}
						}
					}
				}
				
				if (fe == null) {
					fe = new FileEntry (fileName);
					files [fileName] = fe;
				}
				
				for (int n=0; n<newClasses.Count; n++) {
					if (!added[n]) {
						IClass c = CopyClass (newClasses[n]);
						
						// A ClassEntry may already exist if part of the class is defined in another file
						ClassEntry ce = newNss[n].GetClass (c.Name, true);
						if (ce != null) {
							// The entry exists, just update it
							if (ce.Class == null) ce.Class = ReadClass (ce);
							RemoveSubclassReferences (ce);
							ce.Class = CompoundClass.MergeClass (ce.Class, c);
							res.Modified.Add (ce.Class);
						} else {
							// It's a new class
							ce = new ClassEntry (c, newNss[n]);
							newNss[n].Add (c.Name, ce);
							res.Added.Add (c);
							ResolveSubclasses (ce);
						}
						AddSubclassReferences (ce);							
						newFileClasses.Add (ce);
						ce.LastGetTime = currentGetTime++;
					}
				}
				
				fe.SetClasses (newFileClasses);
				rootNamespace.Clean ();
				fe.LastParseTime = DateTime.Now;
				modified = true;
				
				return res;
			}
		}
		
		void ResolveSubclasses (ClassEntry ce)
		{
			// If this type is registered in the unresolved subclass table, now those subclasses
			// can properly be assigned.
			ArrayList subs = (ArrayList) unresolvedSubclassTable [ce.Class.FullyQualifiedName];
			if (subs != null) {
				ce.Subclasses = subs;
				unresolvedSubclassTable.Remove (ce.Class.FullyQualifiedName);
			}
		}
		
		void UnresolveSubclasses (ClassEntry ce)
		{
			// Called when a ClassEntry is removed. If there are registered subclass, add them
			// to the unresolved subclass table
			if (ce.Subclasses != null)
				unresolvedSubclassTable [ce.Class.FullyQualifiedName] = ce.Subclasses;
		}
		
		void AddSubclassReferences (ClassEntry ce)
		{
			foreach (IReturnType type in ce.Class.BaseTypes) {
				string bt = type.FullName;
				if (bt == "System.Object")
					continue;
				ClassEntry sup = FindClassEntry (bt);
				if (sup != null)
					sup.RegisterSubclass (ce);
				else {
					ArrayList subs = (ArrayList) unresolvedSubclassTable [bt];
					if (subs == null) {
						subs = new ArrayList ();
						unresolvedSubclassTable [bt] = subs;
					}
					subs.Add (ce);
				}
			}
			foreach (IClass cls in ce.Class.InnerClasses)
				AddInnerSubclassReferences (cls);
		}
		
		void AddInnerSubclassReferences (IClass cls)
		{
			foreach (IReturnType type in cls.BaseTypes) {
				string bt = type.FullName;
				if (bt == "System.Object")
					continue;
				ArrayList subs = (ArrayList) unresolvedSubclassTable [bt];
				if (subs == null) {
					subs = new ArrayList ();
					unresolvedSubclassTable [bt] = subs;
				}
				subs.Add (cls.FullyQualifiedName);
			}
			foreach (IClass ic in cls.InnerClasses)
				AddInnerSubclassReferences (ic);
		}
		
		void RemoveSubclassReferences (ClassEntry ce)
		{
			foreach (IReturnType type in ce.Class.BaseTypes) {
				ClassEntry sup = FindClassEntry (type.FullName);
				if (sup != null)
					sup.UnregisterSubclass (ce);
					
				ArrayList subs = (ArrayList) unresolvedSubclassTable [type.FullName];
				if (subs != null) {
					subs.Remove (ce);
					if (subs.Count == 0)
						unresolvedSubclassTable.Remove (type.FullName);
				}
			}
			foreach (IClass cls in ce.Class.InnerClasses)
				RemoveInnerSubclassReferences (cls);
		}
		
		void RemoveInnerSubclassReferences (IClass cls)
		{
			foreach (IReturnType type in cls.BaseTypes) {
				ArrayList subs = (ArrayList) unresolvedSubclassTable [type.FullName];
				if (subs != null)
					subs.Remove (type.FullName);
			}
			foreach (IClass ic in cls.InnerClasses)
				RemoveInnerSubclassReferences (ic);
		}
		
		ClassEntry FindClassEntry (string fullName)
		{
			string[] path = fullName.Split ('.');
			int len = path.Length - 1;
			NamespaceEntry nst;
			int nextPos;
			
			if (GetBestNamespaceEntry (path, len, false, true, out nst, out nextPos)) 
			{
				ClassEntry ce = nst.GetClass (path[len], true);
				if (ce == null) return null;
				return ce;
			}
			return null;
		}
		
		public void GetNamespaceContents (LanguageItemCollection list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock)
			{
				string[] path = subNameSpace.Split ('.');
				NamespaceEntry tns = GetNamespaceEntry (path, path.Length, false, caseSensitive);
				if (tns == null) return;
				
				foreach (DictionaryEntry en in tns.Contents) {
					if (en.Value is NamespaceEntry)
						list.Add (new Namespace ((string)en.Key));
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
				foreach (ClassEntry ce in GetAllClasses ()) {
					list.Add (GetClass (ce));
				}
				return (IClass[]) list.ToArray (typeof(IClass));
			}
		}
		
		public IEnumerable<IClass> GetClassList (bool includeInner, string[] namespaces)
		{
			lock (rwlock)
			{
				IList nsList = namespaces;
				ArrayList list = new ArrayList ();
				foreach (ClassEntry ce in GetAllClasses ()) {
					IClass cls = GetClass (ce);
					if (nsList != null && !nsList.Contains (cls.Namespace))
						continue;
					list.Add (cls);
					if (includeInner && ((ce.ContentFlags & ContentFlags.HasInnerClasses) != 0))
						GetAllInnerClassesRec (list, cls);
				}
				return (IClass[]) list.ToArray (typeof(IClass));
			}
		}
		
		void GetAllInnerClassesRec (ArrayList list, IClass cls)
		{
			foreach (IClass ic in cls.InnerClasses) {
				list.Add (ic);
				GetAllInnerClassesRec (list, ic);
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
			foreach (ClassEntry ce in fe.ClassEntries) {
				classes.Add (GetClass (ce));
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
			DefaultClass ret = PersistentClass.Read (br, parserDatabase.DefaultNameDecoder);
			ret.SourceProject = cls.SourceProject;
			return ret;
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
						
						nh = new NamespaceEntry (lastEntry, path[n]);
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
}*/

namespace MonoDevelop.Projects.Dom
{
	internal interface INameEncoder
	{
		int GetStringId (string text);
	}
	
	internal interface INameDecoder
	{
		string GetStringValue (int id);
	}
	
	
	internal class StringNameTable: INameEncoder, INameDecoder
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
#if CHECK_STRINGS
			count++;
			object ob = all [text];
			if (ob != null)
				all [text] = ((int)ob) + 1;
			else
				all [text] = 1;
#endif

			int i = Array.BinarySearch (table, text);
			if (i >= 0) return i;
			else return -1;
		}

#if CHECK_STRINGS
		static Hashtable all = new Hashtable ();
		static int count;
		
		public static void PrintTop100 ()
		{
			string[] ss = new string [all.Count];
			int[] nn = new int [all.Count];
			int n = 0;
			foreach (DictionaryEntry e in all) {
				ss [n] = (string) e.Key;
				nn [n] = (int) e.Value;
				n++;
			}
			Array.Sort (nn, ss);
			n=0;
			Console.WriteLine ("{0} total strings", count);
			Console.WriteLine ("{0} unique strings", nn.Length);
			for (int i = nn.Length - 1; i > nn.Length - 101 && i >= 0; i--) {
				Console.WriteLine ("\"{1}\", // {2}", n, ss[i], nn[i]);
			}
		}
#endif
	}
}
