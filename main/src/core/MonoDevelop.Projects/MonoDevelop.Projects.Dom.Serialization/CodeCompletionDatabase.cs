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
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Core;
using Mono.Addins;
using System.Reflection;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class SerializationCodeCompletionDatabase : IDisposable
	{
		static protected readonly int MAX_ACTIVE_COUNT = 100;
		static protected readonly int MIN_ACTIVE_COUNT = 10;
		static protected readonly int FORMAT_VERSION   = 63;
		
		NamespaceEntry rootNamespace;
		protected ArrayList references;
		protected Hashtable files;
		protected Hashtable headers;
		ParserDatabase pdb;
		
		BinaryReader datareader;
		FileStream dataFileStream;
		int currentGetTime = 0;
		bool modified;
		bool disposed;
		
		string dataFile;
		string tempDataFile;
		DatabaseProjectDom sourceProjectDom;
		
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
		
		public SerializationCodeCompletionDatabase (ParserDatabase pdb)
		{
			rootNamespace = new NamespaceEntry (null, null);
			files = new Hashtable ();
			references = new ArrayList ();
			headers = new Hashtable ();
			this.pdb = pdb;
			
			ProjectDomService.SpecialCommentTagsChanged += OnSpecialTagsChanged;	
		}
		
		public virtual void Dispose ()
		{
			if (dataFileStream != null)
				dataFileStream.Close ();
			if (tempDataFile != null) {
				File.Delete (tempDataFile);
				tempDataFile = null;
			}
			ProjectDomService.SpecialCommentTagsChanged -= OnSpecialTagsChanged;
			disposed = true;
		}

		~SerializationCodeCompletionDatabase ()
		{
			if (tempDataFile != null) {
				File.Delete (tempDataFile);
				tempDataFile = null;
			}
		}
		
		public string DataFile {
			get { return dataFile; }
		}

		// File where data is actually readen from of written to. It can be a temp file. 
		public string RealDataFile {
			get { return tempDataFile ?? dataFile; }
		}
		
		public bool Modified {
			get { return modified; }
			set { modified = value; }
		}
		
		public bool Disposed {
			get { return disposed; }
		}

		public virtual DatabaseProjectDom SourceProjectDom {
			get { return sourceProjectDom; }
			set { sourceProjectDom = value; }
		}
		
		protected void SetLocation (string basePath, string name)
		{
			dataFile = Path.Combine (basePath, name + ".pidb");
		}
		
		protected void SetFile (string file)
		{
			dataFile = file;
		}
		
		public virtual void Read ()
		{
			if (!File.Exists (dataFile)) return;
			
			lock (rwlock)
			{
				rootNamespace = new NamespaceEntry (null, null);
				files = new Hashtable ();
				references = new ArrayList ();
				headers = new Hashtable ();
				unresolvedSubclassTable = new Hashtable ();
			
				CloseReader ();

				try {
					dataFileStream = OpenForWrite ();
					datareader = new BinaryReader (dataFileStream);
				} catch (Exception ex) {
					LoggingService.LogError ("PIDB file '{0}' could not be loaded: '{1}'. The file will be recreated.", dataFile, ex);
					return;
				}
					
				try 
				{
					modified = false;
					currentGetTime = 0;
					
					BinaryFormatter bf = new BinaryFormatter ();
					
					// Read the headers
					headers = (Hashtable) bf.Deserialize (dataFileStream);
					int ver = (int) headers["Version"];
					if (ver != FORMAT_VERSION)
						throw new OldPidbVersionException (ver, FORMAT_VERSION);
					
					// Move to the index offset and read the index
					BinaryReader br = new BinaryReader (dataFileStream);
					long indexOffset = br.ReadInt64 ();
					dataFileStream.Position = indexOffset;

					object oo = bf.Deserialize (dataFileStream);
					object[] data = (object[]) oo;
					Queue dataQueue = new Queue (data);
					references = (ArrayList) dataQueue.Dequeue ();
					rootNamespace = (NamespaceEntry)  dataQueue.Dequeue ();
					files = (Hashtable)  dataQueue.Dequeue ();
					unresolvedSubclassTable = (Hashtable) dataQueue.Dequeue ();
					DeserializeData (dataQueue);
				}
				catch (Exception ex)
				{
					OldPidbVersionException opvEx = ex as OldPidbVersionException;
					if (opvEx != null)
						LoggingService.LogWarning ("PIDB file '{0}' could not be loaded. Expected version {1}, found version {2}'. The file will be recreated.", dataFile, opvEx.ExpectedVersion, opvEx.FoundVersion);
					else
						LoggingService.LogError ("PIDB file '{0}' could not be loaded: '{1}'. The file will be recreated.", dataFile, ex);
				}
			}

			// Notify read comments
			foreach (FileEntry fe in files.Values) {
				if (! fe.IsAssembly && fe.CommentTasks != null) {
					ProjectDomService.UpdatedCommentTasks (fe.FileName, fe.CommentTasks);
				}
			}
			
			// Update comments if needed...
			CommentTagSet lastTags = new CommentTagSet (LastValidTaskListTokens);
			if (!lastTags.Equals (ProjectDomService.SpecialCommentTags))
				OnSpecialTagsChanged (null, null);
		}

		FileStream OpenForWrite ()
		{
			if (tempDataFile != null) {
				// Already a temp file.
				return new FileStream (tempDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			}
			
			try {
				return new FileStream (dataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Write);
			}
			catch (IOException) {
			}

			// The file is locked, so it can be opened. The solution is to make
			// a copy of the file and opend the copy. The copy will later be discarded,
			// and this is not a problem because if the main file is locked it means
			// that it is being updated by another MD instance.

			tempDataFile = Path.GetTempFileName ();
			File.Copy (dataFile, tempDataFile, true);
			return new FileStream (tempDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
		}
		
		protected int ResolveTypes (ICompilationUnit unit, IList<IType> types, out List<IType> result)
		{
			return ProjectDomService.ResolveTypes (SourceProjectDom, unit, types, out result);
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
			using (FileStream ifile = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				BinaryFormatter bf = new BinaryFormatter ();
				Hashtable headers = (Hashtable) bf.Deserialize (ifile);
				return headers;
			}
		}
		
		public virtual void Write ()
		{
			lock (rwlock)
			{
				if (!modified) return;
				
				modified = false;
				headers["Version"] = FORMAT_VERSION;
				headers["LastValidTaskListTokens"] = ProjectDomService.SpecialCommentTags.ToString ();

				LoggingService.LogDebug ("Writing " + dataFile);
				
				try {
					if (dataFileStream == null) {
						dataFileStream = OpenForWrite ();
						datareader = new BinaryReader (dataFileStream);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not write parser database.", ex);
					return;
				}

				MemoryStream tmpStream = new MemoryStream ();
				BinaryFormatter bf = new BinaryFormatter ();
				BinaryWriter bw = new BinaryWriter (tmpStream);
				
				try {
					// The headers are the first thing to write, so they can be read
					// without deserializing the whole file.
					bf.Serialize (tmpStream, headers);
					
					// The position of the index will be written here
					long indexOffsetPos = tmpStream.Position;
					bw.Write ((long)0);
					
					MemoryStream buffer = new MemoryStream ();
					BinaryWriter bufWriter = new BinaryWriter (buffer);
					
					// Write all class data
					foreach (ClassEntry ce in GetAllClasses ()) 
					{
						IType c = ce.Class;
						byte[] data;
						int len;
						
						if (c == null) {
							// Copy the data from the source file
							dataFileStream.Position = ce.Position;
							len = datareader.ReadInt32 ();
							
							// Sanity check to avoid allocating huge byte arrays if something
							// goes wrong when reading the file contents
							if (len > 1024*1024*10 || len < 0)
								throw new InvalidOperationException ("pidb file corrupted: " + dataFile);

							data = new byte[len];
							int nr = 0;
							while (nr < len)
								nr += dataFileStream.Read (data, nr, len - nr);
						}
						else {
							buffer.Position = 0;
							DomPersistence.Write (bufWriter, pdb.DefaultNameEncoder, c);
							bufWriter.Flush ();
							data = buffer.GetBuffer ();
							len = (int)buffer.Position;
						}
						
						bw.Flush ();
						ce.Position = tmpStream.Position;
						bw.Write (len);
						bw.Write (data, 0, len);
					}
					
					bw.Flush ();
					
					// Write the index
					long indexOffset = tmpStream.Position;
					
					Queue dataQueue = new Queue ();
					dataQueue.Enqueue (references);
					dataQueue.Enqueue (rootNamespace);
					dataQueue.Enqueue (files);
					dataQueue.Enqueue (unresolvedSubclassTable);
					SerializeData (dataQueue);
					bf.Serialize (tmpStream, dataQueue.ToArray ());
					
					tmpStream.Position = indexOffsetPos;
					bw.Write (indexOffset);
					
					// Save to file
					
					dataFileStream.SetLength (0);
					dataFileStream.Position = 0;

					byte[] dataDump = tmpStream.ToArray ();
					dataFileStream.Write (dataDump, 0, dataDump.Length);
					
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					dataFileStream.Close ();
					dataFileStream = null;
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
		
		internal protected FileEntry GetFile (string name)
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
		
		internal IType ReadClass (ClassEntry ce)
		{
			lock (rwlock) {
				dataFileStream.Position = ce.Position;
				datareader.ReadInt32 ();// Length of data
				
				DomType cls = DomPersistence.ReadType (datareader, pdb.DefaultNameDecoder);
				cls.SourceProjectDom = SourceProjectDom;
				cls.Resolved = true;
				return cls;
			}
		}

		protected void UnlockDatabaseFile ()
		{
			CloseReader ();
		}
		
		void CloseReader ()
		{
			if (datareader != null) {
				datareader = null;
				dataFileStream.Close ();
				dataFileStream = null;
			}
		}
		
		public void Clear ()
		{
			rootNamespace = new NamespaceEntry (null, null);
			files = new Hashtable ();
			references = new ArrayList ();
			headers = new Hashtable ();
		}
		
		public IType GetClass (string typeName, IList<IReturnType> genericArguments, bool caseSensitive)
		{
			int genericArgumentCount = ProjectDom.ExtractGenericArgCount (ref typeName);
			if (genericArguments != null)
				genericArgumentCount = genericArguments.Count;

			lock (rwlock)
			{
				string[] path = typeName.Split ('.');
				int len = path.Length - 1;
				
				NamespaceEntry nst;
				int nextPos;

				IType result = null;
				
				if (GetBestNamespaceEntry (path, len, false, caseSensitive, out nst, out nextPos)) 
				{
					ClassEntry ce = nst.GetClass (path[len], genericArgumentCount, caseSensitive);
					if (ce == null) return null;
					result = GetClass (ce);
				}
				else
				{
					// It may be an inner class
					string nextName = path[nextPos++];
					int partArgsCount = ProjectDom.ExtractGenericArgCount (ref nextName);
					ClassEntry ce = nst.GetClass (nextName, partArgsCount, caseSensitive);
					if (ce == null) return null;
					foreach (IType type in SourceProjectDom.GetInheritanceTree (GetClass (ce))) {
						result = SourceProjectDom.FindInnerType (type, path, nextPos, genericArgumentCount, caseSensitive);
						if (result != null)
							break;
					}
				}
				if (result != null && genericArguments != null && genericArguments.Count > 0)
					return sourceProjectDom.CreateInstantiatedGenericType (result, genericArguments);
				else
					return result;
			}
		}
		
		internal IType GetClass (ClassEntry ce)
		{
			ce.LastGetTime = currentGetTime++;
			if (ce.Class != null)
				return ce.Class;
			DomTypeProxy result = new DomTypeProxy (this, ce);
			result.SourceProjectDom = this.SourceProjectDom;
			result.Resolved = true;
			return result;
		}
		
		public IEnumerable<IType> GetSubclasses (IType btype, IList<string> namespaces)
		{
			InstantiatedType itype = btype as InstantiatedType;
			if (itype != null) {
				foreach (IType t in GetSubclassesInternal (itype.UninstantiatedType, namespaces)) {
					IType sub = GetCompatibleSubclass (itype, t);
					if (sub != null)
						yield return sub;
				}
			} else {
				// We don't support getting the subclasses of generic non-instantiated types.
				if (btype.TypeParameters.Count > 0)
					yield break;
				foreach (IType t in GetSubclassesInternal (btype, namespaces))
					yield return t;
			}
		}
		
		IEnumerable<IType> GetSubclassesInternal (IType btype, IList<string> namespaces)
		{
			ArrayList nsubs = (ArrayList) unresolvedSubclassTable [ParserDatabase.GetDecoratedName (btype)];
			ArrayList csubs = null;
			
			ClassEntry ce = FindClassEntry (btype.FullName, btype.TypeParameters.Count);
			if (ce != null)
				csubs = ce.Subclasses;

			foreach (ArrayList subs in new object[] { nsubs, csubs }) {
				if (subs == null)
					continue;
				foreach (object ob in subs) {
					if (ob is ClassEntry) {
						string ns = ((ClassEntry) ob).NamespaceRef.FullName;
						if (namespaces == null || namespaces.Contains (ns)) {
							IType t = GetClass ((ClassEntry)ob);
							if (t != null && t != btype)
								yield return t;
						}
					}
					else {
						// It's a full class name
						IType cls = this.GetClass ((string)ob, null, true);
						if (cls != null && (namespaces == null || namespaces.Contains (cls.Namespace))) {
							if (cls != btype)
								yield return cls;
						}
					}
				}
			}
		}

		IType GetCompatibleSubclass (InstantiatedType baseType, IType subType)
		{
			// No generic type inferring involved
			if (subType.TypeParameters.Count == 0 && baseType.GenericParameters.Count == 0)
				return subType;

			// The subclass is compatible and can be returned if all type parameters
			// can be inferred from the base class

			// Find the IReturnType that is relating the subtype with the base type

			IReturnType baseRetType = null;
			foreach (IReturnType rt in subType.BaseTypes) {
				if (rt.FullName == baseType.UninstantiatedType.FullName && rt.GenericArguments.Count == baseType.GenericParameters.Count) {
					baseRetType = rt;
					break;
				}
			}
			if (baseRetType == null)
				return null; // Something went wrong. Not compatible.

			ReadOnlyCollection<IReturnType> bparams = baseRetType.GenericArguments;
			bool[] paramsMatched = new bool [bparams.Count];
			
			List<IReturnType> args = new List<IReturnType> ();
			foreach (TypeParameter par in subType.TypeParameters) {
				int pos = -1;
				string parTypeName = subType.FullName + "." + par.Name;
				for (int n=0; n < bparams.Count; n++) {
					string pname = bparams [n].FullName;
					if (parTypeName == pname) {
						paramsMatched [n] = true;
						pos = n;
						break;
					}
				}
				if (pos != -1)
					args.Add (baseType.GenericParameters [pos]);
				else
					return null; // Something went wrong. Not compatible.
			}

			// Parameter which are instantiated must match the ones in the
			// instantiated base class
			for (int n=0; n < bparams.Count; n++) {
				if (paramsMatched [n])
					continue;
				if (!bparams [n].Equals (baseType.GenericParameters [n]))
					return null;
			}
			return sourceProjectDom.CreateInstantiatedGenericType (subType, args);
		}
		
		void OnSpecialTagsChanged (object sender, EventArgs e)
		{
			// Update LastValidTagComments
			
			string oldTokens = (string) headers["LastValidTagComments"];
			headers["LastValidTagComments"] = ProjectDomService.SpecialCommentTags.ToString ();
			
			CommentTagSet oldTags = new CommentTagSet (oldTokens);
			foreach (string tag in oldTags.GetNames ()) {
				// Remove them from FileEntry data
				if (!ProjectDomService.SpecialCommentTags.ContainsTag (tag))
					RemoveSpecialCommentTag (tag);
			}
			QueueAllFilesForParse ();
		}
	
		public IList<Tag> GetSpecialComments (string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = files[fileName] as FileEntry;
				return fe != null ? fe.CommentTasks : null;
			}
		}
		
		public void UpdateTagComments (IList<Tag> tags, string fileName)
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
					ProjectDomService.UpdatedCommentTasks (fe.FileName, fe.CommentTasks);
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
		
		public virtual void UpdateDatabase ()
		{
			ArrayList list = GetModifiedFileEntries ();
			foreach (FileEntry file in list) {
				ParseFile (file.FileName, null);
				try {
					FileInfo fi = new FileInfo (file.FileName);
					file.LastParseTime = fi.LastWriteTime;
				} catch {
					// Ignore
				}
			}
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
			return file.IsModified;
		}
		
		protected void QueueParseJob (FileEntry file)
		{
			if (file.InParseQueue)
				return;
			file.InParseQueue = true;
			ProjectDomService.QueueParseJob (SourceProjectDom, new JobCallback (ParseCallback), file.FileName);
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
				foreach (FileEntry fe in files.Values)  {
					ParseFile (fe.FileName, null);
					try {
						FileInfo fi = new FileInfo (fe.FileName);
						fe.LastParseTime = fi.LastWriteTime;
					} catch {
						// Ignore
					}
				}
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
				TypeUpdateInformation classInfo = new TypeUpdateInformation ();
				
				FileEntry fe = files [fileName] as FileEntry;
				if (fe == null) return;
				
				foreach (ClassEntry ce in fe.ClassEntries) {
					if (ce.Class == null) ce.Class = ReadClass (ce);
					IType c = CompoundType.RemoveFile (ce.Class, fileName);
					if (c == null) {
						classInfo.Removed.Add (ce.Class);
						RemoveSubclassReferences (ce);
						UnresolveSubclasses (ce);
						ce.NamespaceRef.Remove (ce);
					} else
						ce.Class = c;
				}
				
				files.Remove (fileName);
				modified = true;

				OnFileRemoved (fileName, classInfo);
			}
		}
		
		protected virtual void OnFileRemoved (string fileName, TypeUpdateInformation classInfo)
		{
		}
		
		public TypeUpdateInformation UpdateTypeInformation (IList<IType> newClasses, string fileName)
		{
			lock (rwlock)
			{
				TypeUpdateInformation res = new TypeUpdateInformation ();
				
				FileEntry fe = files [fileName] as FileEntry;
				if (fe == null) return null;
				
				// Get the namespace entry for each class

				bool[] added = new bool [newClasses.Count];
				NamespaceEntry[] newNss = new NamespaceEntry [newClasses.Count];
				for (int n = 0; n < newClasses.Count; n++) {
					string[] path = newClasses[n].Namespace.Split ('.');
					((DomType)newClasses[n]).SourceProjectDom = sourceProjectDom;
					newNss[n] = GetNamespaceEntry (path, path.Length, true, true);
				}
				
				ArrayList newFileClasses = new ArrayList ();
				
				if (fe != null)
				{
					foreach (ClassEntry ce in fe.ClassEntries)
					{
						IType newClass = null;
						for (int n=0; n<newClasses.Count && newClass == null; n++) {
							IType uc = newClasses [n];
							if (uc.Name == ce.Name  && uc.TypeParameters.Count == ce.TypeParameterCount && newNss[n] == ce.NamespaceRef) {
								if (newClass == null)
									newClass = uc;
								else
									newClass = CompoundType.Merge (newClass, uc);
								added[n] = true;
							}
						}
						
						if (newClass != null) {
							// Class already in the database, update it
							if (ce.Class == null) 
								ce.Class = ReadClass (ce);
							RemoveSubclassReferences (ce);
							
							IType tp = CompoundType.RemoveFile (ce.Class, fileName);
							if (tp != null)
								ce.Class = CompoundType.Merge (tp, CopyClass (newClass));
							else
								ce.Class = CopyClass (newClass);
							AddSubclassReferences (ce);
							
							ce.LastGetTime = currentGetTime++;
							newFileClasses.Add (ce);
							res.Modified.Add (ce.Class);
							SourceProjectDom.ResetInstantiatedTypes (ce.Class);
						} else {
							// Database class not found in the new class list, it has to be deleted
							IType c = ce.Class;
							if  (c == null) {
								ce.Class = ReadClass (ce);
								c = ce.Class;
							}
							IType removed = CompoundType.RemoveFile (c, fileName);
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
								ce.NamespaceRef.Remove (ce);
							}
							SourceProjectDom.ResetInstantiatedTypes (c);
						}
					}
				}
				
				if (fe == null) {
					fe = new FileEntry (fileName);
					files [fileName] = fe;
				}
				
				for (int n=0; n<newClasses.Count; n++) {
					if (!added[n]) {
						IType c = CopyClass (newClasses[n]);
						
						// A ClassEntry may already exist if part of the class is defined in another file
						ClassEntry ce = newNss[n].GetClass (c.Name, c.TypeParameters.Count , true);
						if (ce != null) {
							// The entry exists, just update it
							if (ce.Class == null) ce.Class = ReadClass (ce);
							RemoveSubclassReferences (ce);
							ce.Class = CompoundType.Merge (ce.Class, c);
							res.Modified.Add (ce.Class);
						} else {
							// It's a new class
							ce = new ClassEntry (c, newNss[n]);
							newNss[n].Add (ce);
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
			string name = ParserDatabase.GetDecoratedName (ce);
			ArrayList subs = (ArrayList) unresolvedSubclassTable [name];
			if (subs != null) {
				ce.Subclasses = subs;
				unresolvedSubclassTable.Remove (name);
			}
		}
		
		void UnresolveSubclasses (ClassEntry ce)
		{
			// Called when a ClassEntry is removed. If there are registered subclass, add them
			// to the unresolved subclass table
			if (ce.Subclasses != null)
				unresolvedSubclassTable [ParserDatabase.GetDecoratedName (ce)] = ce.Subclasses;
		}

		IEnumerable<IReturnType> GetAllBaseTypes (IType type)
		{
			if (type.BaseType != null)
				yield return type.BaseType;
			foreach (IReturnType rt in type.ImplementedInterfaces)
				yield return rt;
		}

		bool IsValidGenericSubclass (IType subType, IReturnType baseType)
		{
			// Subclass relations between generic types are only useful if we can infer
			// an instantiated subtype from a given instantiated base type
			// Examples of valid subclasses: 
			//    class Sub<T>: Base<T> { }
			//    class Sub<T1,T2>: Base<T1,T2> { }
			//    class Sub<T>: Base<T,T> { }
			//    class Sub<T>: Base<T,string> { }
			//    class Sub<A,B>: Base<B,A> { }
			// Examples of invalid subclasses: 
			//    class Sub<T>: Base { }
			//    class Sub<T,S>: Base<T> { }
			//    class Sub<T1,T2>: Base<T1, string> { }
			//    class Sub<T>: Base<string> { }
			
			if (baseType.GenericArguments.Count > 0) {
				if (subType.TypeParameters.Count == 0)
					return true;
				if (subType.TypeParameters.Count > baseType.GenericArguments.Count)
					return false;
				List<string> pars = new List<string> ();
				foreach (TypeParameter tpar in subType.TypeParameters)
					pars.Add (subType.FullName + "." + tpar.Name);
				foreach (IReturnType rt in baseType.GenericArguments)
					pars.Remove (rt.FullName);
				if (pars.Count > 0)
					return false;
			} else if (subType.TypeParameters.Count != 0)
				return false;
			return true;
		}
		
		void AddSubclassReferences (ClassEntry ce)
		{
			foreach (IReturnType type in GetAllBaseTypes (ce.Class)) {
				string bt = ParserDatabase.GetDecoratedName (type);
				if (bt == "System.Object")
					continue;
				if (!IsValidGenericSubclass (ce.Class, type))
					continue;
				ClassEntry sup = FindClassEntry (type.FullName, type.GenericArguments.Count);
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
			foreach (IType cls in ce.Class.InnerTypes)
				AddInnerSubclassReferences (cls);
		}
		
		void AddInnerSubclassReferences (IType cls)
		{
			foreach (IReturnType type in GetAllBaseTypes (cls)) {
				string bt = ParserDatabase.GetDecoratedName (type);
				if (bt == "System.Object")
					continue;
				if (!IsValidGenericSubclass (cls, type))
					continue;
				ArrayList subs = (ArrayList) unresolvedSubclassTable [bt];
				if (subs == null) {
					subs = new ArrayList ();
					unresolvedSubclassTable [bt] = subs;
				}
				subs.Add (ParserDatabase.GetDecoratedName (cls));
			}
			foreach (IType ic in cls.InnerTypes)
				AddInnerSubclassReferences (ic);
		}
		
		void RemoveSubclassReferences (ClassEntry ce)
		{
			foreach (IReturnType type in GetAllBaseTypes (ce.Class)) {
				ClassEntry sup = FindClassEntry (type.FullName, type.GenericArguments.Count);
				if (sup != null)
					sup.UnregisterSubclass (ce);
					
				ArrayList subs = (ArrayList) unresolvedSubclassTable [ParserDatabase.GetDecoratedName (type)];
				if (subs != null) {
					subs.Remove (ce);
					if (subs.Count == 0)
						unresolvedSubclassTable.Remove (ParserDatabase.GetDecoratedName (type));
				}
			}
			foreach (IType cls in ce.Class.InnerTypes)
				RemoveInnerSubclassReferences (cls);
		}
		
		void RemoveInnerSubclassReferences (IType cls)
		{
			foreach (IReturnType type in GetAllBaseTypes (cls)) {
				ArrayList subs = (ArrayList) unresolvedSubclassTable [ParserDatabase.GetDecoratedName (type)];
				if (subs != null)
					subs.Remove (ParserDatabase.GetDecoratedName (cls));
			}
			foreach (IType ic in cls.InnerTypes)
				RemoveInnerSubclassReferences (ic);
		}
		
		ClassEntry FindClassEntry (string fullName, int genericArgumentCount)
		{
			string[] path = fullName.Split ('.');
			int len = path.Length - 1;
			NamespaceEntry nst;
			int nextPos;
			
			if (GetBestNamespaceEntry (path, len, false, true, out nst, out nextPos)) 
			{
				ClassEntry ce = nst.GetClass (path[len], genericArgumentCount, true);
				if (ce == null) return null;
				return ce;
			}
			return null;
		}
		
		public void GetNamespaceContents (List<IMember> list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock) {
				string[] path = subNameSpace.Split ('.');
				NamespaceEntry tns = GetNamespaceEntry (path, path.Length, false, caseSensitive);
				if (tns == null) return;
				
				foreach (DictionaryEntry en in tns.Contents) {
					if (en.Value is NamespaceEntry) {
						list.Add (new Namespace ((string)en.Key));
					} else {
						IType type = GetClass ((ClassEntry)en.Value);
						
						if (type.Name.IndexOfAny (new char[] { '<', '>' }) >= 0)
							continue;
						list.Add (type);
					}
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
		
		public IType[] GetClassList ()
		{
			lock (rwlock)
			{
				ArrayList list = new ArrayList ();
				foreach (ClassEntry ce in GetAllClasses ()) {
					list.Add (GetClass (ce));
				}
				return (IType[]) list.ToArray (typeof(IType));
			}
		}
		
		public IEnumerable<IType> GetClassList (bool includeInner, IList<string> namespaces)
		{
			lock (rwlock)
			{
				ArrayList list = new ArrayList ();
				foreach (ClassEntry ce in GetAllClasses ()) {
					IType cls = GetClass (ce);
					if (namespaces != null && !namespaces.Contains (cls.Namespace))
						continue;
					list.Add (cls);
					if (includeInner && ((ce.ContentFlags & ContentFlags.HasInnerClasses) != 0))
						GetAllInnerClassesRec (list, cls);
				}
				return (IType[]) list.ToArray (typeof(IType));
			}
		}
		
		void GetAllInnerClassesRec (ArrayList list, IType cls)
		{
			foreach (IType ic in cls.InnerTypes) {
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
		
		public IType[] GetFileContents (string fileName)
		{
			FileEntry fe = GetFile (fileName);
			if (fe == null) return new IType [0];

			ArrayList classes = new ArrayList ();
			foreach (ClassEntry ce in fe.ClassEntries) {
				classes.Add (GetClass (ce));
			}
			return (IType[]) classes.ToArray (typeof(IType));
		}
		
		IType CopyClass (IType cls)
		{
			CopyDomVisitor<object> copier = new CopyDomVisitor<object> ();
			return (IType) copier.Visit (cls, null);
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
						lastEntry.Add (nh);
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
}

namespace MonoDevelop.Projects.Dom
{
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
			if (id < 0 || id >= table.Length)
				return "Invalid id:" + id;
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
