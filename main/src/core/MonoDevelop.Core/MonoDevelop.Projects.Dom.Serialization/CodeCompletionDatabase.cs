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
using System.Linq;
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
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class SerializationCodeCompletionDatabase : IDisposable
	{
		static protected readonly int MAX_ACTIVE_COUNT = 100;
		static protected readonly int MIN_ACTIVE_COUNT = 10;
		static protected readonly int FORMAT_VERSION   = 85;
		
		Dictionary<string, ClassEntry> typeEntries = new Dictionary<string, ClassEntry> ();
		Dictionary<string, ClassEntry> typeEntriesIgnoreCase = new Dictionary<string, ClassEntry> (StringComparer.InvariantCultureIgnoreCase);
		
		Dictionary<string, List<ClassEntry>> classEntries = new Dictionary<string, List<ClassEntry>> ();
		Dictionary<string, List<ClassEntry>> classEntriesIgnoreCase = new Dictionary<string, List<ClassEntry>> (StringComparer.InvariantCultureIgnoreCase);

		Dictionary<string, List<Namespace>> namespaceEntries = new Dictionary<string, List<Namespace>> ();
		Dictionary<string, List<Namespace>> namespaceEntriesIgnoreCase = new Dictionary<string, List<Namespace>> (StringComparer.InvariantCultureIgnoreCase);
		// TODO: Table for inner types. Problem A.Inner could be B.Inner if B inherits from A.
		
		List<AttributeEntry> globalAttributes;
		long globalAttributesPosition = -1;
		
		List<ReferenceEntry> references;
		protected Dictionary<string, FileEntry> files;
		protected Hashtable headers;
		ParserDatabase pdb;
		
		BinaryReader datareader;
		FileStream dataFileStream;
		int currentGetTime = 0;
		
		bool disposed;
		bool handlesCommentTags;
		
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

		void UpdateClassEntries ()
		{
			// it's not really worth micro-updating the tables speed-wise.
			typeEntriesIgnoreCase.Clear ();
			foreach (var pair in typeEntries) {
				typeEntriesIgnoreCase [pair.Key] = pair.Value;
			}

			classEntries.Clear ();
			classEntriesIgnoreCase.Clear ();
			namespaceEntries.Clear ();
			namespaceEntriesIgnoreCase.Clear ();
			namespaceEntriesIgnoreCase[""] = namespaceEntries[""] = new List<Namespace> ();
			foreach (ClassEntry ce in typeEntries.Values) {
				if (!classEntries.ContainsKey (ce.Namespace))
					classEntriesIgnoreCase[ce.Namespace] = classEntries[ce.Namespace] = new List<ClassEntry> ();
				classEntries[ce.Namespace].Add (ce);
				AddNamespace (ce.Namespace);
			}
		}
		
		internal ProjectDomStats GetStats ()
		{
			ProjectDomStats stats = new ProjectDomStats ();
			stats.ClassEntries = typeEntries.Count;
			
			int typesWithUnshared = 0;
			StatsVisitor v = new StatsVisitor (stats);
			v.SharedTypes = SourceProjectDom.GetSharedReturnTypes ().ToArray ();
			List<string> detail = new List<string> ();
			foreach (ClassEntry ce in typeEntries.Values) {
				if (ce.Class != null) {
					stats.LoadedClasses++;
					v.Reset ();
					v.Visit (ce.Class, "");
					if (v.Failures.Count > 0) {
						stats.UnsharedReturnTypes += v.Failures.Count;
						stats.ClassesWithUnsharedReturnTypes++;
						if (typesWithUnshared++ < 10) {
							detail.Add (" * " + ce.Class.FullName + ": RTs:" + v.ReturnTypeCount + ", non shared:" + v.Failures.Count);
							foreach (var s in v.Failures)
								detail.Add ("    - " + s);
						}
					}
				}
			}
			
			stats.UnsharedReturnTypesDetail = detail;
			return stats;
		}
		
		public virtual string GetDocumentation (IMember member)
		{
			return member != null ? member.Documentation : null;
		}

		public void AddNamespace (string nsName)
		{
			if (namespaceEntries.ContainsKey (nsName))
				return;
			namespaceEntriesIgnoreCase[nsName] = namespaceEntries[nsName] = new List<Namespace> ();
			
			int idx = nsName.LastIndexOf ('.');
			if (idx < 0) {
				namespaceEntries[""].Add (new Namespace (nsName));
				return;
			}
			string parent = nsName.Substring (0, idx);
			string name   = nsName.Substring (idx + 1);
			AddNamespace (parent);
			namespaceEntries[parent].Add (new Namespace (name));
		}

		public SerializationCodeCompletionDatabase (ParserDatabase pdb, bool handlesCommentTags)
		{
			Counters.LiveDatabases++;
			this.handlesCommentTags = handlesCommentTags;
			
			files = new Dictionary<string, FileEntry> ();
			references = new List<ReferenceEntry> ();
			headers = new Hashtable ();
			this.pdb = pdb;
			
			if (handlesCommentTags)
				ProjectDomService.SpecialCommentTagsChanged += OnSpecialTagsChanged;	
		}
		
		public virtual void Dispose ()
		{
			if (disposed)
				return;
			
			Clear ();
			
			if (dataFileStream != null)
				dataFileStream.Close ();
			if (tempDataFile != null) {
				File.Delete (tempDataFile);
				tempDataFile = null;
			}
			if (handlesCommentTags)
				ProjectDomService.SpecialCommentTagsChanged -= OnSpecialTagsChanged;
			disposed = true;
			Counters.LiveDatabases--;
			
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
			get;
			set;
		}
		
		public bool Disposed {
			get { return disposed; }
		}

		public virtual DatabaseProjectDom SourceProjectDom {
			get { return sourceProjectDom; }
			set { sourceProjectDom = value; }
		}
		
		public virtual Project Project {
			get { return sourceProjectDom != null ? sourceProjectDom.Project : null; }
		}
		
		protected void SetLocation (string basePath, string name)
		{
			dataFile = Path.Combine (basePath, name + ".pidb");
		}
		
		protected void SetFile (string file)
		{
			dataFile = file;
		}
		
		protected internal virtual void ForceUpdateBROKEN ()
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
		
		public virtual void Read ()
		{
			Read (false);
		}
		
		void Read (bool verify)
		{
			if (!File.Exists (dataFile)) return;
			ITimeTracker timer = Counters.DatabasesRead.BeginTiming ("Reading Parser Database " + dataFile);
				
			lock (rwlock)
			{
				timer.Trace ("Clearing");
				Clear ();
			
				CloseReader ();

				try {
					timer.Trace ("Opening file");
					dataFileStream = OpenForWrite ();
					datareader = new BinaryReader (dataFileStream);
				} catch (Exception ex) {
					LoggingService.LogError ("PIDB file '{0}' could not be loaded: '{1}'. The file will be recreated.", dataFile, ex);
					timer.End ();
					return;
				}
					
				try 
				{
					Modified = false;
					currentGetTime = 0;
					
					BinaryFormatter bf = new BinaryFormatter ();
					
					timer.Trace ("Read headers");
					
					// Read the headers
					headers = (Hashtable) bf.Deserialize (dataFileStream);
					int ver = (int) headers["Version"];
					if (ver != FORMAT_VERSION)
						throw new OldPidbVersionException (ver, FORMAT_VERSION);
					
					timer.Trace ("Read index");
					
					// Move to the index offset and read the index
					BinaryReader br = new BinaryReader (dataFileStream);
					long indexOffset = br.ReadInt64 ();
					dataFileStream.Position = indexOffset;

					object oo = bf.Deserialize (dataFileStream);
					object[] data = (object[]) oo;
					Queue dataQueue = new Queue (data);
					references = (List<ReferenceEntry>) dataQueue.Dequeue () ?? new List<ReferenceEntry> ();
					typeEntries = (Dictionary<string, ClassEntry>) dataQueue.Dequeue () ?? new Dictionary<string, ClassEntry> ();
					files = (Dictionary<string, FileEntry>) dataQueue.Dequeue () ?? new Dictionary<string, FileEntry> ();
					unresolvedSubclassTable = (Hashtable) dataQueue.Dequeue () ?? new Hashtable ();
					
					// Read the global attributes position
					globalAttributesPosition = br.ReadInt64 ();
					
					DeserializeData (dataQueue);
					UpdateClassEntries ();
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
			
			try {
				timer.Trace ("Notify read comments");
				
				// Notify read comments
				foreach (FileEntry fe in files.Values) {
					if (!fe.IsAssembly && fe.CommentTasks != null) {
						ProjectDomService.UpdatedCommentTasks (fe.FileName, fe.CommentTasks, Project);
					}
				}
				
				int totalEntries = typeEntries.Count;
				Counters.TypeIndexEntries.Inc (totalEntries);
				
				if (verify) {
					// Read all information from the database to ensure everything is in place
					HashSet<ClassEntry> classes = new HashSet<ClassEntry> ();
					foreach (ClassEntry ce in GetAllClasses ()) {
						classes.Add (ce);
						try {
							LoadClass (ce);
						} catch (Exception ex) {
							LoggingService.LogWarning ("PIDB file verification failed. Class '" + ce.Name + "' could not be deserialized: " + ex.Message);
						}
					}
	/*				foreach (FileEntry fe in files.Values) {
						foreach (ClassEntry ce in fe.ClassEntries) {
							if (!classes.Contains (ce))
								LoggingService.LogWarning ("PIDB file verification failed. Class '" + ce.Name + "' from file '" + fe.FileName + "' not found in main index.");
							else
								classes.Remove (ce);
						}
					}
					foreach (ClassEntry ce in classes)
						LoggingService.LogWarning ("PIDB file verification failed. Class '" + ce.Name + "' not found in file index.");
	*/			}
				
				timer.Trace ("Notify tag changes");
				// Update comments if needed...
				CommentTagSet lastTags = new CommentTagSet (LastValidTaskListTokens);
				if (!lastTags.Equals (ProjectDomService.SpecialCommentTags))
					OnSpecialTagsChanged (null, null);
			}
			finally {
				timer.End ();
			}
		}

		FileStream OpenForWrite ()
		{
			if (tempDataFile != null) {
				// Already a temp file.
				return new FileStream (tempDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			}
			
			try {
				return new FileStream (dataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			}
			catch (IOException) {
				// If the file could not be opened nor created, and the file doesn't exist,
				// then it must be some write permission issue. Rethrow the exception.
				if (!File.Exists (dataFile))
					throw;
			}
			
			// The file is locked, so it can't be opened. The solution is to make
			// a copy of the file and open the copy. The copy will later be discarded,
			// and this is not a problem because if the main file is locked it means
			// that it is being updated by another MD instance.

			tempDataFile = Path.GetTempFileName ();
			File.Copy (dataFile, tempDataFile, true);
			return new FileStream (tempDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
		}
		
		protected int ResolveTypes (ICompilationUnit unit, IEnumerable<IType> types, IEnumerable<IAttribute> attributes, out List<IType> result, out List<IAttribute> resultAtrtibutes)
		{
			return ProjectDomService.ResolveTypes (SourceProjectDom, unit, types, attributes, out result, out resultAtrtibutes);
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
		
		public virtual bool Write ()
		{
			lock (rwlock)
			{
				if (!Modified) return false;
				
				ITimeTracker timer = Counters.DatabasesWritten.BeginTiming ("Writing Parser Database " + dataFile);
				
				Modified = false;
				headers["Version"] = FORMAT_VERSION;
				headers["LastValidTaskListTokens"] = ProjectDomService.SpecialCommentTags.ToString ();

				LoggingService.LogDebug ("Writing " + dataFile);
				
				try {
					if (dataFileStream == null) {
						timer.Trace ("Opening file");
						dataFileStream = OpenForWrite ();
						datareader = new BinaryReader (dataFileStream);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not write parser database.", ex);
					timer.End ();
					return false;
				}

				MemoryStream tmpStream = new MemoryStream ();
				BinaryFormatter bf = new BinaryFormatter ();
				BinaryWriter bw = new BinaryWriter (tmpStream);
				long attributesOffset;
				
				try {
					timer.Trace ("Serializing headers");
					
					// The headers are the first thing to write, so they can be read
					// without deserializing the whole file.
					bf.Serialize (tmpStream, headers);
					
					// The position of the index will be written here
					long indexOffsetPos = tmpStream.Position;
					bw.Write ((long)0);
					
					MemoryStream buffer = new MemoryStream ();
					BinaryWriter bufWriter = new BinaryWriter (buffer);
					
					timer.Trace ("Writing class data");
					
					INameEncoder nameEncoder = pdb.CreateNameEncoder ();
					
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
							while (nr < len) {
								var read = dataFileStream.Read (data, nr, len - nr);
								if (read <= 0)
									throw new InvalidOperationException (len + " bytes could not be read from pidb file : " +  dataFile);
								nr += read;
							}
						}
						else {
							buffer.Position = 0;
							DomPersistence.Write (bufWriter, nameEncoder, c);
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
					
					timer.Trace ("Writing index");
					
					// Write global attributes
					attributesOffset = tmpStream.Position;
					WriteGlobalAttribtues (bw);
					bw.Flush ();
					
					// Write the index
					long indexOffset = tmpStream.Position;
					
					Queue dataQueue = new Queue ();
					dataQueue.Enqueue (references);
					dataQueue.Enqueue (typeEntries);
					dataQueue.Enqueue (files);
					dataQueue.Enqueue (unresolvedSubclassTable);
					SerializeData (dataQueue);
					bf.Serialize (tmpStream, dataQueue.ToArray ());
					
					// Write the global index position
					bw.Write (attributesOffset);
					
					tmpStream.Position = indexOffsetPos;
					bw.Write (indexOffset);
					
					// Save to file
					
					timer.Trace ("Saving to file");
					
					dataFileStream.SetLength (0);
					dataFileStream.Position = 0;

					byte[] dataDump = tmpStream.ToArray ();
					dataFileStream.Write (dataDump, 0, dataDump.Length);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					dataFileStream.Close ();
					dataFileStream = null;
					return false;
				} finally {
					timer.End ();
				}
				
				globalAttributesPosition = attributesOffset;
			}
			
			return true;
		}
		
		protected virtual void SerializeData (Queue dataQueue)
		{
		}
		
		protected virtual void DeserializeData (Queue dataQueue)
		{
		}
		
		void LoadGlobalAttributes ()
		{
			if (globalAttributes != null)
				return;
			globalAttributes = new List<AttributeEntry> ();
			if (globalAttributesPosition == -1)
				return;
			
			dataFileStream.Position = globalAttributesPosition;
			INameDecoder nd = pdb.CreateNameDecoder ();
			DomPersistence.ReadAttributeEntryList (datareader, nd, sourceProjectDom);
		}
		
		void WriteGlobalAttribtues (BinaryWriter writer)
		{
			LoadGlobalAttributes ();
			INameEncoder nd = pdb.CreateNameEncoder ();
			DomPersistence.WriteAttributeEntryList (writer, nd, globalAttributes);
		}
		
		internal protected FileEntry GetFile (string name)
		{
			FileEntry result;
			files.TryGetValue (name, out result);
			return result;
		}
		
		protected IEnumerable<FileEntry> GetAllFiles ()
		{
			return files.Values;
		}

		internal IEnumerable<ClassEntry> GetAllClasses ()
		{
			// ensure that the GetAllClasses methods is save of type entry changes
			return this.typeEntries.Values.Where (ce => ce != null).ToArray ();
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
				if (ce.Class != null && ce.LastGetTime < currentGetTime - MIN_ACTIVE_COUNT && ce.Saved) {
					ce.Class = null;
					Counters.LiveTypeObjects--;
				}
			}
		}
		
		internal IType LoadClass (ClassEntry ce)
		{
			lock (rwlock) {
				if (ce.Class != null)
					return ce.Class;
				if (ce.Position < 0) // position not initialized/db error
					return null;
				dataFileStream.Position = ce.Position;
				datareader.ReadInt32 ();// Length of data
				DomType cls = DomPersistence.ReadType (datareader, pdb.CreateNameDecoder (), SourceProjectDom);
				cls.SourceProjectDom = SourceProjectDom;
				cls.Resolved = true;
				ce.Class = cls;
				Counters.LiveTypeObjects++;
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
			int tcl = 0;
			int tce = 0;
			foreach (ClassEntry ce in GetAllClasses ()) {
				tce++;
				if (ce.Class != null)
					tcl++;
			}
			
			Counters.TypeIndexEntries.Dec (tce);
			Counters.LiveTypeObjects.Dec (tcl);
			classEntries = new Dictionary<string, List<ClassEntry>> ();
			classEntriesIgnoreCase = new Dictionary<string, List<ClassEntry>> (StringComparer.InvariantCultureIgnoreCase);
			files = new Dictionary<string, FileEntry> ();
			typeEntries.Clear ();
			typeEntriesIgnoreCase.Clear ();
			references.Clear ();
			namespaceEntries.Clear ();
			namespaceEntriesIgnoreCase.Clear ();
			headers = new Hashtable ();
			unresolvedSubclassTable = new Hashtable ();
			globalAttributes = null;
		}
		
		public IType GetClass (string typeName, IList<IReturnType> genericArguments, bool caseSensitive)
		{
			int genericArgumentCount = ProjectDom.ExtractGenericArgCount (ref typeName);
			if (genericArguments != null)
				genericArgumentCount = genericArguments.Count;
			string fullName = ParserDatabase.GetDecoratedName (typeName, genericArgumentCount);
			lock (rwlock) {
				ClassEntry ce;
				if (!(caseSensitive ? typeEntries : typeEntriesIgnoreCase).TryGetValue (fullName, out ce)) {
					int idx = typeName.LastIndexOf ('.');
					if (idx < 0)
						return null;
					IType type = GetClass (typeName.Substring (0, idx), null, caseSensitive);
					if (type != null) {
						IType inner = SourceProjectDom.SearchInnerType (type, typeName.Substring (idx + 1), genericArgumentCount, caseSensitive);
						if (inner != null)
							return genericArguments == null || genericArguments.Count == 0 ? inner : sourceProjectDom.CreateInstantiatedGenericType (inner, genericArguments);
					}
					return null;
				}
				var result = GetClass (ce);
				if (genericArguments != null && genericArguments.Count > 0)
					result = sourceProjectDom.CreateInstantiatedGenericType (result, genericArguments);
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
						string ns = ((ClassEntry) ob).Namespace;
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
					if (parTypeName == pname || par.Name == pname) {
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
				FileEntry fe;
				if (!files.TryGetValue (fileName, out fe))
					return null;
				return fe.CommentTasks;
			}
		}
		
		public void UpdateTagComments (IList<Tag> tags, string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe;
				if (files.TryGetValue (fileName, out fe))
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
					ProjectDomService.UpdatedCommentTasks (fe.FileName, fe.CommentTasks, Project);
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
					if (IsFileModified (file)) {
						list.Add (file);
					}
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
			ProjectDomService.QueueParseJob (SourceProjectDom, ParseCallback, file.FileName);
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
		
		internal void AddReference (string uri)
		{
			lock (rwlock)
			{
				// Create a new list because the reference list is accessible through a public property
				var list = new List<ReferenceEntry>(references);
				list.Add (new ReferenceEntry (uri));
				references = list;
				Modified = true;
			}
		}
		
		internal void RemoveReference (string uri)
		{
			lock (rwlock)
			{
				for (int n=0; n<references.Count; n++)
				{
					if (references[n].Uri == uri) {
						var list = new List<ReferenceEntry>(references);
						list.RemoveAt (n);
						references = list;
						Modified = true;
						return;
					}
				}
			}
		}
		
		internal bool HasReference (string uri)
		{
			return references.Any (r => r.Uri == uri);
		}
		
		public FileEntry AddFile (string fileName)
		{
			lock (rwlock)
			{
				FileEntry fe = new FileEntry (fileName);
				files[fileName] = fe;
				Modified = true;
				return fe;
			}
		}
		
		public void RemoveFile (string fileName)
		{
			lock (rwlock)
			{
				TypeUpdateInformation classInfo = new TypeUpdateInformation ();
				
				FileEntry fe;
				if (!files.TryGetValue (fileName, out fe)) return;
				
				int te=0, tc=0;
				
				foreach (ClassEntry ce in fe.ClassEntries) {
					LoadClass (ce);
					tc++;
					IType c = CompoundType.RemoveFile (ce.Class, fileName);
					if (c == null) {
						classInfo.Removed.Add (ce.Class);
						RemoveSubclassReferences (ce);
						UnresolveSubclasses (ce);
						typeEntries.Remove (ce.Class.DecoratedFullName);
						typeEntriesIgnoreCase.Remove (ce.Class.DecoratedFullName);
						te++;
					} else
						ce.Class = c;
				}
				
				Counters.LiveTypeObjects.Dec (tc);
				Counters.TypeIndexEntries.Dec (te);
				
				files.Remove (fileName);
				Modified = true;

				OnFileRemoved (fileName, classInfo);
			}
		}
		
		protected virtual void OnFileRemoved (string fileName, TypeUpdateInformation classInfo)
		{
		}
		
		public TypeUpdateInformation UpdateTypeInformation (IList<IType> newClasses, IEnumerable<IAttribute> fileAttributes, string fileName)
		{
			lock (rwlock)
			{
				TypeUpdateInformation res = new TypeUpdateInformation ();
				FileEntry fe;
				if (!files.TryGetValue (fileName, out fe))
					return null;
				
				// Update global attributes
				
				LoadGlobalAttributes ();
				globalAttributes.RemoveAll (e => e.File == fileName);
				
				if (fileAttributes != null) {
					globalAttributes.AddRange (
						fileAttributes.Select (a => new AttributeEntry () { Attribute = a, File = fileName })
					);
				}
				
				// Update types
				
				bool[] added = new bool [newClasses.Count];
				for (int n = 0; n < newClasses.Count; n++) {
					((DomType)newClasses[n]).SourceProjectDom = sourceProjectDom;
				}
				List<ClassEntry> newFileClasses = new List<ClassEntry> ();
				if (fe != null)
				{
					foreach (ClassEntry ce in fe.ClassEntries)
					{
						IType newClass = null;
						for (int n=0; n<newClasses.Count && newClass == null; n++) {
							IType uc = newClasses [n];
							if (uc.Name == ce.Name  && uc.TypeParameters.Count == ce.TypeParameterCount && uc.Namespace == ce.Namespace) {
								newClass = newClass == null ? uc : CompoundType.Merge (newClass, uc);
								added[n] = true;
							}
						}
						
						if (newClass != null) {
							// Class already in the database, update it
							LoadClass (ce);
							RemoveSubclassReferences (ce);
							IType tp = CompoundType.RemoveFile (ce.Class, fileName);
//							newClass = CopyClass (newClass); //required ? - the classes given to the db don't get changed by md
							ce.Class = tp != null ? CompoundType.Merge (tp, newClass) : newClass;
							AddSubclassReferences (ce);
							string name = ce.Class.DecoratedFullName;
							typeEntriesIgnoreCase[name] = typeEntries[name] = ce;
							ce.LastGetTime = currentGetTime++;
							newFileClasses.Add (ce);
							res.Modified.Add (ce.Class);
							SourceProjectDom.ResetInstantiatedTypes (ce.Class);
						} else {
							// Database class not found in the new class list, it has to be deleted
							IType c = LoadClass (ce);
							if (c != null) {
								IType removed = CompoundType.RemoveFile (c, fileName);
								if (removed != null) {
									// It's still a compound class
									ce.Class = removed;
									AddSubclassReferences (ce);
									res.Modified.Add (removed);
								} else {
									// It's not a compoudnd class. Remove it.
									Counters.LiveTypeObjects--;
									Counters.TypeIndexEntries--;
									RemoveSubclassReferences (ce);
									UnresolveSubclasses (ce);
									res.Removed.Add (c);
									string name = c.DecoratedFullName;
									typeEntries.Remove (name);
									typeEntriesIgnoreCase.Remove (name);
								}
								SourceProjectDom.ResetInstantiatedTypes (c);
							} else {
								// c == null -> error in db - ignore this entry it'll get removed.
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
						// A ClassEntry may already exist if part of the class is defined in another file
						ClassEntry ce;
						string name = newClasses[n].DecoratedFullName;
						typeEntries.TryGetValue (name, out ce);
						if (ce != null) {
							// The entry exists, just update it
							LoadClass (ce);
							RemoveSubclassReferences (ce);
							ce.Class = CompoundType.Merge (ce.Class, newClasses[n]);
							res.Modified.Add (ce.Class);
						} else {
							// It's a new class
							ce = new ClassEntry (newClasses[n]);
							typeEntriesIgnoreCase[name] = typeEntries[name] = ce;
							res.Added.Add (newClasses[n]);
							ResolveSubclasses (ce);
							Counters.LiveTypeObjects++;
							Counters.TypeIndexEntries++;
						}
						AddSubclassReferences (ce);
						newFileClasses.Add (ce);
						ce.LastGetTime = currentGetTime++;
					}
				}
				
				fe.SetClasses (newFileClasses);
//				rootNamespace.Clean ();
				try {
					FileInfo fi = new FileInfo (fe.FileName);
					fe.LastParseTime = fi.LastWriteTime;
				} catch {
					fe.LastParseTime = DateTime.Now;
				}
				
				Modified = true;
				this.UpdateClassEntries ();
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
				foreach (IReturnType rt in baseType.GenericArguments) {
					pars.Remove (rt.FullName);
					pars.Remove (subType.FullName + "." + rt.FullName);
				}
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
			ClassEntry ce;
			this.typeEntries.TryGetValue (ParserDatabase.GetDecoratedName (fullName, genericArgumentCount), out ce);
			return ce;
		}

		public void GetNamespaceContents (List<IMember> list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock) {
				List<ClassEntry> classList;
				if ((caseSensitive ? classEntries : classEntriesIgnoreCase).TryGetValue (subNameSpace, out classList))
					list.AddRange (from c in classList where c.Name.IndexOfAny (new char[] { '<', '>' }) < 0 select (IMember)GetClass (c));
				List<Namespace> namespaceList;
				if ((caseSensitive ? namespaceEntries : namespaceEntriesIgnoreCase).TryGetValue (subNameSpace, out namespaceList))
					list.AddRange (namespaceList.Cast<IMember> ());
			}
		}

		public IEnumerable<IAttribute> GetGlobalAttributes ()
		{
			lock (rwlock)
			{
				List<IAttribute> list = new List<IAttribute> ();
				LoadGlobalAttributes ();
				list.AddRange (globalAttributes.Select (a => a.Attribute));
				return list;
			}
		}
		
		public void GetClassList (ArrayList list, string subNameSpace, bool caseSensitive)
		{
			lock (rwlock)
			{
				List<ClassEntry> entries;
				if (!(caseSensitive ? classEntries : classEntriesIgnoreCase).TryGetValue (subNameSpace, out entries))
					return;
				foreach (ClassEntry ce in entries) {
					if (!list.Contains (ce.Name))
						list.Add (ce.Name);
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

		/*
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
		}*/

		public bool NamespaceExists (string name, bool caseSensitive)
		{
			lock (rwlock) {
				return (caseSensitive ? namespaceEntries : namespaceEntriesIgnoreCase).ContainsKey (name);
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
		
//		IType CopyClass (IType cls)
//		{
//			CopyDomVisitor<object> copier = new CopyDomVisitor<object> ();
//			return (IType) copier.Visit (cls, null);
//		}
		/*
		bool GetBestNamespaceEntry (string[] path, int length, bool createPath, bool caseSensitive, out NamespaceEntry lastEntry, out int numMatched)
		{
			lastEntry = rootNamespace;-

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
		}*/

        public void RunWithLock<T> (Action<T> act, T data) 
        { 
            lock (rwlock) 
            { 
                act (data); 
            } 
        }
	}
}

namespace MonoDevelop.Projects.Dom
{
	public interface INameEncoder
	{
		int GetStringId (string text, out bool isNew);
		void Reset ();
	}
	
	public interface INameDecoder
	{
		string GetStringValue (int id);
		void RegisterString (int id, string str);
		void Reset ();
	}
	
	public class StringNameTable: INameEncoder, INameDecoder
	{
		string[] table;
		Dictionary<string,int> stringToId = new Dictionary<string, int> ();
		Dictionary<int,string> idToString = new Dictionary<int, string> ();
		int ci;
		
		public StringNameTable (string[] names)
		{
			table = names;
			Reset ();
		}
		
		public void Reset ()
		{
			stringToId.Clear ();
			idToString.Clear ();
			ci = table.Length + 1;
		}
		
		public void RegisterString (int id, string str)
		{
			idToString [id] = str;
		}
		
		public string GetStringValue (int id)
		{
			if (id > table.Length) {
				string res;
				if (idToString.TryGetValue (id, out res))
					return res;
				return null;
			}
			if (id < 0 || id >= table.Length)
				return "Invalid id:" + id;
			return table [id];
		}
		
		public int GetStringId (string text, out bool isNew)
		{
#if CHECK_STRINGS
			count++;
			object ob = all [text];
			if (ob != null)
				all [text] = ((int)ob) + 1;
			else
				all [text] = 1;
#endif
			isNew = false;
			int i = Array.BinarySearch (table, text);
			if (i >= 0) return i;

			if (stringToId.TryGetValue (text, out i))
				return i;
			
			isNew = true;
			stringToId.Add (text, ++ci);
			return ci;
		}

#if CHECK_STRINGS
		static Hashtable all = new Hashtable ();
		static int count;
#pragma warning disable 0414
		static TablePrinter printer = new TablePrinter ();
#pragma warning restore 0414
		
		class TablePrinter {
			~TablePrinter () {
				StringNameTable.PrintTop200 ();
			}
		}

		public static void PrintTop200 ()
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
			for (int i = nn.Length - 1; i > nn.Length - 201 && i >= 0; i--) {
				Console.WriteLine ("\"{1}\", // {2}", n, ss[i], nn[i]);
			}
		}
#endif
	}
}
