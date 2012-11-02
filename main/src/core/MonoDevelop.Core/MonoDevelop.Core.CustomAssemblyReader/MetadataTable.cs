// 
// MetadataTable.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Relicensed from SharpAssembly (c) 2003 by Mike Krüger
//
// Copyright (c) 2012 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Core.CustomAssemblyReader
{
	class MetadataTable
	{
		uint reserved;
		byte majorVersion;
		byte minorVersion;
		byte heapSizes;
		ulong valid;
		ulong sorted;
		uint[] rows;
		
		Hashtable tableIndices  = new Hashtable();
		Hashtable tables        = new Hashtable();

		// map TABLE_ID to index in rows
		public Hashtable TableIndices {
			get {
				return tableIndices;
			}
		}
		
		public Hashtable Tables {
			get {
				return tables;
			}
		}
		
		public uint Reserved {
			get {
				return reserved;
			}
			set {
				reserved = value;
			}
		}
		public byte MajorVersion {
			get {
				return majorVersion;
			}
			set {
				majorVersion = value;
			}
		}
		public byte MinorVersion {
			get {
				return minorVersion;
			}
			set {
				minorVersion = value;
			}
		}
		public byte HeapSizes {
			get {
				return heapSizes;
			}
			set {
				heapSizes = value;
			}
		}
		public ulong Valid {
			get {
				return valid;
			}
			set {
				valid = value;
			}
		}
		public ulong Sorted {
			get {
				return sorted;
			}
			set {
				sorted = value;
			}
		}
		public uint[] Rows {
			get {
				return rows;
			}
			set {
				rows = value;
			}
		}
		
		public bool FourByteStringIndices {
			get {
				return (heapSizes & 1) == 1;
			}
		}
		public bool FourByteGUIDIndices {
			get {
				return (heapSizes & 2) == 2;
			}
		}
		public bool FourByteBlobIndices {
			get {
				return (heapSizes & 4) == 4;
			}
		}
		
		public AbstractRow[] LoadTable(BinaryReader binaryReader, Type tableType, uint count)
		{
			// rows start at 1, as the indices in the metadata do
			AbstractRow[] array = (AbstractRow[])Array.CreateInstance(tableType, count+1);
			for (int i = 1; i <= count; ++i) {
				array[i] = (AbstractRow)tableType.Assembly.CreateInstance(tableType.FullName);
				array[i].LoadRow(this, binaryReader);
			}
			return array;
		}
		
		public uint GetRowCount(int tableID)
		{
			object index = tableIndices[tableID];
			if (index is uint) {
				return rows[(uint)index];
			}
			return 0;
		}
		
		public uint GetMultipleRowCount(params int[] tableID)
		{
			uint count = 0;
			foreach (int id in tableID) {
				object index = tableIndices[id];
				if (index != null) {
					count += rows[(uint)index];
				}
			}
			return count;
		}
		
		public uint GetMaxRowCount(params int[] tableID)
		{
			uint maxcount = 0;
			foreach (int id in tableID) {
				object index = tableIndices[id];
				if (index is uint) {
					uint count = rows[(uint)index];
					if (count > maxcount) maxcount = count;
				}
			}
			return maxcount;
		}
		
		internal static int GetTableID(Type type)
		{
			return (int)type.InvokeMember("TABLE_ID", 
			                              BindingFlags.Static |
			                              BindingFlags.Public |
			                              BindingFlags.Instance |
			                              BindingFlags.GetField, null, null, null);
		}

		HashSet<int> storeTables = new HashSet<int> ();
		public void AddStoreTable (int id)
		{
			storeTables.Add (id);
		}
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			reserved     = binaryReader.ReadUInt32();
			majorVersion = binaryReader.ReadByte();
			minorVersion = binaryReader.ReadByte();
			heapSizes    = binaryReader.ReadByte();
			reserved     = binaryReader.ReadByte();
			valid        = binaryReader.ReadUInt64();
			sorted       = binaryReader.ReadUInt64();
			rows = new uint[CalculateNumberOfRows()];
			for (int i = 0; i < rows.Length; ++i) {
				rows[i] = binaryReader.ReadUInt32();
			}
			
			var types = new List<Type>();
			foreach (Type type in typeof(AbstractRow).Assembly.GetTypes()) {
				if (type.IsSubclassOf(typeof(AbstractRow))) {
					ulong tableBit = (ulong)1 << GetTableID(type);
					if ((valid & tableBit) == tableBit) {
						types.Add(type);
					} 
				}
			}
			types.Sort(new TypeComparer());


			for (int i = 0; i < types.Count; ++i) {
				tableIndices[GetTableID((Type)types[i])] = (uint)i;
			}

			foreach (Type type in types) {
				int id = GetTableID(type);
				var table = LoadTable(binaryReader, type, rows[(uint)tableIndices[id]]);
				if (storeTables.Contains (id))
					Tables[id] = table;
			}
		}
		
		int CalculateNumberOfRows()
		{
			int rows = 0;
			ulong v = valid;
			for (int i = 0; i < 64; ++i) {
				rows += (int)(v & 1);
				v /= 2;
			}
			return rows;
		}
		
		class TypeComparer : IComparer<Type>
		{
			public int Compare(Type o1, Type o2)
			{
				return GetTableID(o1).CompareTo(GetTableID(o2));
			}
		}
		

	}
}
