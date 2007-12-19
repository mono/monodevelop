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
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MonoDevelop.Profiling.HeapShot
{
	public class ObjectMapReader 
	{
		const uint magic_number = 0x4eabfdd1;
		const int expected_log_version = 6;
		const int expected_summary_version = 2;
		const string log_file_label = "heap-shot logfile";
		
		bool terminated_normally = true;
		string name;
		DateTime timestamp;
		uint numTypes;
		uint numObjects;
		uint numReferences;
		uint numFields;
		uint totalMemory;
		uint objectCount;
		
		int curObject;
		int curType;
		int curField;
		int curRef;
		
		ObjectInfo[] objects;
		TypeInfo[] types;
		string[] fieldNames;
		int[] objectIndices;
		int[] typeIndices;
		int[] references;
		int[] inverseRefs;
		int[] fieldReferences;
		bool[] filteredObjects;
		
		uint[] referenceCodes;
		uint[] objectTypeCodes;
		uint[] fieldCodes;
		uint[] fieldReferenceCodes;
		uint[] objectCodes;
		
		internal ObjectMapReader ()
		{
		}
		
		public ObjectMapReader (string filename)
		{
			this.name = filename;
			
			Stream stream;
			stream = new FileStream (filename, FileMode.Open, FileAccess.Read);

			BinaryReader reader;
			reader = new BinaryReader (stream);
			
			ReadPreamble (reader);
			ReadLogFile (reader);
			
			reader.Close ();
			
			timestamp = File.GetLastWriteTime (filename);
		}
		
		public string Name {
			get { return name; }
		}
		
		public DateTime Timestamp {
			get { return timestamp; }
		}
		
		public uint TotalMemory {
			get { return totalMemory; }
		}
		
		public uint NumObjects {
			get { return objectCount; }
		}
		
		public static ObjectMapReader CreateProcessSnapshot (int pid)
		{
			string dumpFile = "/tmp/heap-shot-dump";
			if (File.Exists (dumpFile))
				File.Delete (dumpFile);
			System.Diagnostics.Process.Start ("kill", "-PROF " + pid);
			
			string fileName = null;
			int tries = 40;
			
			while (fileName == null) {
				if (--tries == 0)
					return null;

				System.Threading.Thread.Sleep (500);
				if (!File.Exists (dumpFile))
					continue;
					
				StreamReader freader = null;
				try {
					freader = new StreamReader (dumpFile);
					fileName = freader.ReadToEnd ();
					freader.Close ();
				} catch {
					if (freader != null)
						freader.Close ();
				}
			}
			return new ObjectMapReader (fileName);
		}

		///////////////////////////////////////////////////////////////////

		private void Spew (string format, params object [] args)
		{
			string message;
			message = String.Format (format, args);
			Console.WriteLine (message);
		}

		///////////////////////////////////////////////////////////////////

		private void ReadPreamble (BinaryReader reader)
		{
			uint this_magic;
			this_magic = reader.ReadUInt32 ();
			if (this_magic != magic_number) {
				string msg;
				msg = String.Format ("Bad magic number: expected {0}, found {1}",
						     magic_number, this_magic);
				throw new Exception (msg);
			}

			int this_version;
			this_version = reader.ReadInt32 ();

			string this_label;
			int expected_version;

			this_label = reader.ReadString ();
			if (this_label == log_file_label) {
				expected_version = expected_log_version;
			} else
				throw new Exception ("Unknown file label in heap-shot outfile");

			if (this_version != expected_version) {
				string msg;
				msg = String.Format ("Version error in {0}: expected {1}, found {2}",
						     this_label, expected_version, this_version);
				throw new Exception (msg);
			}
			numTypes = reader.ReadUInt32 ();
			numObjects = reader.ReadUInt32 ();
			numReferences = reader.ReadUInt32 ();
			numFields = reader.ReadUInt32 ();
			objectCount = numObjects;
		}

		//
		// Code to read the log files generated at runtime
		//

		// These need to agree w/ the definitions in outfile-writer.c
		const byte TAG_TYPE      = 0x01;
		const byte TAG_OBJECT    = 0x02;
		const byte TAG_EOS       = 0xff;

		private void ReadLogFile (BinaryReader reader)
		{
			int chunk_count = 0;
			
			objects = new ObjectInfo [numObjects];
			types = new TypeInfo [numTypes];
			objectTypeCodes = new uint [numObjects];
			referenceCodes = new uint [numReferences];
			fieldReferenceCodes = new uint [numReferences];
			fieldCodes = new uint [numFields];
			fieldNames = new string [numFields];

			try {
				while (ReadLogFileChunk (reader))
					++chunk_count;

			} catch (System.IO.EndOfStreamException) {
				// This means that the outfile was truncated.
				// In that case, just do nothing --- except if the file
				// claimed that things terminated normally.
				if (terminated_normally)
					throw new Exception ("The heap log did not contain TAG_EOS, "
							     + "but the outfile was marked as having been terminated normally, so "
							     + "something must be terribly wrong.");
			}
			BuildMap ();
			Spew ("Processed {0} chunks", chunk_count);
			
			objectTypeCodes = null;
			referenceCodes = null;
			fieldReferenceCodes = null;
			fieldCodes = null;
		}

		private bool ReadLogFileChunk (BinaryReader reader)
		{
			byte tag = reader.ReadByte ();

			switch (tag) {
			case TAG_TYPE:
				ReadLogFileChunk_Type (reader);
				break;
					
			case TAG_OBJECT:
				ReadLogFileChunk_Object (reader);
				break;
				
			case TAG_EOS:
				//Spew ("Found EOS");
				return false;

			default:
				throw new Exception ("Unknown tag! " + tag);
			}

			return true;
		}
		
		private void ReadLogFileChunk_Type (BinaryReader reader)
		{
			uint code = reader.ReadUInt32 ();
			string name = reader.ReadString ();
			
			types [curType].Code = code;
			types [curType].Name = name;
			types [curType].FieldsIndex = curField;
			
			int nf = 0;
			uint fcode;
			while ((fcode = reader.ReadUInt32 ()) != 0) {
				fieldCodes [curField] = fcode;
				fieldNames [curField] = reader.ReadString ();
				curField++;
				nf++;
			}
			types [curType].FieldsCount = nf;
			curType++;
		}
		
		private void ReadLogFileChunk_Object (BinaryReader reader)
		{
			objects [curObject].Code = reader.ReadUInt32 ();
			objectTypeCodes [curObject] = reader.ReadUInt32 ();
			objects [curObject].Size = reader.ReadUInt32 ();
			objects [curObject].RefsIndex = curRef;
			totalMemory += objects [curObject].Size;
			
			// Read referenceCodes
			
			int nr = 0;
			uint oref;
			while ((oref = reader.ReadUInt32 ()) != 0) {
				referenceCodes [curRef] = oref;
				fieldReferenceCodes [curRef] = reader.ReadUInt32 ();
				nr++;
				curRef++;
			}
			objects [curObject].RefsCount = nr;
			curObject++;
		}
		
		void BuildMap ()
		{
			// Build an array of object indices and sort it
			
			RefComparer objectComparer = new RefComparer ();
			objectComparer.objects = objects;
			
			objectIndices = new int [numObjects];
			for (int n=0; n < numObjects; n++)
				objectIndices [n] = n;
			Array.Sort<int> (objectIndices, objectComparer);
			// Sorted array of codes needed for the binary search
			objectCodes = new uint [numObjects];	
			for (int n=0; n < numObjects; n++)
				objectCodes [n] = objects [objectIndices[n]].Code;
			
			// Build an array of type indices and sort it
			
			TypeComparer typeComparer = new TypeComparer ();
			typeComparer.types = types;
			
			typeIndices = new int [numTypes];
			for (int n=0; n < numTypes; n++)
				typeIndices [n] = n;
			Array.Sort<int> (typeIndices, typeComparer);
			// Sorted array of codes needed for the binary search
			uint[] typeCodes = new uint [numTypes];	
			for (int n=0; n < numTypes; n++) {
				typeCodes [n] = types [typeIndices[n]].Code;
			}
			
			// Assign the type index to each object
			
			for (int n=0; n<numObjects; n++) {
				int i = Array.BinarySearch<uint> (typeCodes, objectTypeCodes [n]);
				if (i >= 0) {
					objects [n].Type = typeIndices [i];
					types [objects [n].Type].ObjectCount++;
					types [objects [n].Type].TotalSize += objects [n].Size;
				}
			}
			
			// Build the array of referenceCodes, but using indexes
			references = new int [numReferences];
			
			for (int n=0; n<numReferences; n++) {
				int i = Array.BinarySearch (objectCodes, referenceCodes[n]);
				if (i >= 0) {
					references[n] = objectIndices [i];
					objects [objectIndices [i]].InverseRefsCount++;
				} else
					references[n] = -1;
			}
			
			// Calculate the array index of inverse referenceCodes for each object
			
			int[] invPositions = new int [numObjects];	// Temporary array to hold reference positions
			int rp = 0;
			for (int n=0; n<numObjects; n++) {
				objects [n].InverseRefsIndex = rp;
				invPositions [n] = rp;
				rp += objects [n].InverseRefsCount;
			}
			
			// Build the array of inverse referenceCodes
			// Also calculate the index of each field name
			
			inverseRefs = new int [numReferences];
			fieldReferences = new int [numReferences];
			
			for (int ob=0; ob < numObjects; ob++) {
				int fi = types [objects [ob].Type].FieldsIndex;
				int nf = fi + types [objects [ob].Type].FieldsCount;
				int sr = objects [ob].RefsIndex;
				int er = sr + objects [ob].RefsCount;
				for (; sr<er; sr++) {
					int i = references [sr];
					if (i != -1) {
						inverseRefs [invPositions [i]] = ob;
						invPositions [i]++;
					}
					// If the reference is bound to a field, locate the field
					uint fr = fieldReferenceCodes [sr];
					if (fr != 0) {
						for (int k=fi; k<nf; k++) {
							if (fieldCodes [k] == fr) {
								fieldReferences [sr] = k;
								break;
							}
						}
					}
				}
			}
		}
		
		class RefComparer: IComparer <int> {
			public ObjectInfo[] objects;
			
			public int Compare (int x, int y) {
				return objects [x].Code.CompareTo (objects [y].Code);
			}
		}
		
		class TypeComparer: IComparer <int> {
			public TypeInfo[] types;
			
			public int Compare (int x, int y) {
				return types [x].Code.CompareTo (types [y].Code);
			}
		}
		
		public ReferenceNode GetReferenceTree (string typeName, bool inverse)
		{
			int type = GetTypeFromName (typeName);
			if (type != -1)
				return GetReferenceTree (type, inverse);
			else
				return new ReferenceNode (this, type, inverse);
		}
		
		public ReferenceNode GetReferenceTree (int type, bool inverse)
		{
			ReferenceNode nod = new ReferenceNode (this, type, inverse);
			nod.AddGlobalReferences ();
			nod.Flush ();
			return nod;
		}
		
		public List<List<int>> GetRoots (int type)
		{
			List<int> path = new List<int> ();
			Dictionary<int,List<int>> roots = new Dictionary<int,List<int>> ();
			Dictionary<int,int> visited = new Dictionary<int,int> ();
			
			foreach (int obj in GetObjectsByType (type)) {
				FindRoot (visited, path, roots, obj);
				visited.Clear ();
			}
			
			List<List<int>> res = new List<List<int>> ();
			res.AddRange (roots.Values);
			return res;
		}
		
		void FindRoot (Dictionary<int,int> visited, List<int> path, Dictionary<int,List<int>> roots, int obj)
		{
			if (visited.ContainsKey (obj))
				return;
			visited [obj] = obj;
			path.Add (obj);
			
			bool hasrefs = false;
			foreach (int oref in GetReferencers (obj)) {
				hasrefs = true;
				FindRoot (visited, path, roots, oref);
			}
			
			if (!hasrefs) {
				// A root
				if (!roots.ContainsKey (obj)) {
					roots [obj] = new List<int> (path);
				} else {
					List<int> ep = roots [obj];
					if (ep.Count > path.Count)
						roots [obj] = new List<int> (path);
				}
			}
			path.RemoveAt (path.Count - 1);
		}		
		
		public int GetTypeCount ()
		{
			return (int) numTypes;
		}
		
		public int GetTypeFromName (string name)
		{
			for (int n=0; n<numTypes; n++) {
				if (name == types [n].Name)
					return n;
			}
			return -1;
		}
		
		public IEnumerable<int> GetObjectsByType (int type)
		{
			for (int n=0; n<numObjects; n++) {
				if (objects [n].Type == type && (filteredObjects == null || !filteredObjects[n])) {
					yield return n;
				}
			}
		}
		
		public static ObjectMapReader GetDiff (ObjectMapReader oldMap, ObjectMapReader newMap)
		{
			ObjectMapReader dif = new ObjectMapReader ();
			dif.fieldNames = newMap.fieldNames;
			dif.fieldReferences = newMap.fieldReferences;
			dif.inverseRefs = newMap.inverseRefs;
			dif.numFields = newMap.numFields;
			dif.numObjects = newMap.numObjects;
			dif.numReferences = newMap.numReferences;
			dif.numTypes = newMap.numTypes;
			dif.objectCount = newMap.objectCount;
			dif.objectIndices = newMap.objectIndices;
			dif.objects = newMap.objects;
			dif.objectCodes = newMap.objectCodes;
			dif.references = newMap.references;
			dif.totalMemory = newMap.totalMemory;
			dif.typeIndices = newMap.typeIndices;
			dif.types = newMap.types;
			dif.RemoveData (oldMap);
			return dif;
		}
		
		public void RemoveData (ObjectMapReader otherReader)
		{
			types = (TypeInfo[]) types.Clone ();
			filteredObjects = new bool [numObjects];
			for (int n=0; n<otherReader.numObjects; n++) {
				int i = Array.BinarySearch (objectCodes, otherReader.objects[n].Code);
				if (i >= 0) {
					i = objectIndices [i];
					filteredObjects [i] = true;
					int t = objects[i].Type;
					types [t].ObjectCount--;
					types [t].TotalSize -= objects[i].Size;
					this.objectCount--;
					this.totalMemory -= objects[i].Size;
				}
			}
		}
		
		public IEnumerable<int> GetReferencers (int obj)
		{
			int n = objects [obj].InverseRefsIndex;
			int end = n + objects [obj].InverseRefsCount;
			for (; n<end; n++) {
				int ro = inverseRefs [n];
				if (filteredObjects == null || !filteredObjects [ro])
					yield return ro;
			}
		}
		
		public IEnumerable<int> GetReferences (int obj)
		{
			int n = objects [obj].RefsIndex;
			int end = n + objects [obj].RefsCount;
			for (; n<end; n++) {
				int ro = references [n];
				if (filteredObjects == null || !filteredObjects [ro])
					yield return ro;
			}
		}
		
		public string GetReferencerField (int obj, int refObj)
		{
			int n = objects [obj].RefsIndex;
			int end = n + objects [obj].RefsCount;
			for (; n<end; n++) {
				if (references [n] == refObj) {
					if (fieldReferences [n] != 0)
						return fieldNames [fieldReferences [n]];
					else
						return null;
				}
			}
			return null;
		}
		
		public string GetObjectTypeName (int obj)
		{
			return types [objects [obj].Type].Name;
		}
		
		public int GetObjectType (int obj)
		{
			return objects [obj].Type;
		}
		
		public uint GetObjectSize (int obj)
		{
			return objects [obj].Size;
		}
		
		public IEnumerable<int> GetTypes ()
		{
			for (int n=0; n<numTypes; n++)
				yield return n;
		}
		
		public string GetTypeName (int type)
		{
			return types [type].Name;
		}
		
		public int GetObjectCountForType (int type)
		{
			return types [type].ObjectCount;
		}
		
		public uint GetObjectSizeForType (int type)
		{
			return types [type].TotalSize;
		}
	}
}