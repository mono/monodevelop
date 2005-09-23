// Assembly.cs
// Copyright (C) 2003 Georg Brandl
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.IO;
using System.Collections;
using MonoDevelop.SharpAssembly.Assembly;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using Rows = MonoDevelop.SharpAssembly.Metadata.Rows;
using MSjogren.Fusion.Native;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MonoDevelop.SharpAssembly.Assembly
{
	public class SharpAssembly
	{
		// #Assembly maintains a "reference pool" that avoids loading assemblies twice
		// This is similar to what the Reflection API does, but
		// the assembly files are not locked because they are not opened permanently
		// so the locking problem is solved
		static Hashtable fullNamePool = new Hashtable();
		static Hashtable shortNamePool = new Hashtable();
		
		// initialize; load mscorlib (important, but HACK)
		static SharpAssembly() {
			Console.WriteLine("#Assembly: Initializing");
		
			string mscorlibasm = typeof(System.Object).Assembly.Location;
			/*SharpAssembly mscorlib = */LoadFrom(mscorlibasm);  // the constructor adds the assembly to the pool
		}
		
		Hashtable references = new Hashtable();
		
		public static SharpAssembly LoadFrom(string filename)
		{
			return LoadFrom(filename, false);
		}
		
		public static SharpAssembly LoadFrom(string filename, bool fromGAC)
		{
			// lock the assembly object in case that the preload thread is conflicting with another
			lock (typeof(SharpAssembly)) {
				AssemblyReader read = new AssemblyReader();
				read.Load(filename);
				
				SharpAssembly asm = new SharpAssembly(read, fromGAC);
				asm.LoadReferences();
				asm.LoadNestedTypeTable();
				asm.LoadAttributeTable();
				asm.LoadFieldConstants();
				
				return asm;
			}
		}
		
		public static SharpAssembly Load(string name, string lookInDir)
		{
			if (name.IndexOf(',') == -1) {
				if (shortNamePool.ContainsKey(name)) {
					return (SharpAssembly)shortNamePool[name];
				}
			} else {
				if (fullNamePool.ContainsKey(name)) {
					return (SharpAssembly)fullNamePool[name];
				}
			}
			
			bool fromGAC = false;
			
			string filename = GetAssemblyLocation(name);
				string[] nameParts = name.Split(',');
			
			if (filename != "") {
				fromGAC = true;
			} else {
				string possibleFilename = Path.Combine(lookInDir, nameParts[0] + ".dll");
				if (System.IO.File.Exists(possibleFilename)) filename = possibleFilename;
			}
			
			// HACK : try loading mscorlib from 1.0 sdk
			if (filename == "" && name == "mscorlib, Version=1.0.3300.0, Culture=neutral") {  
               	RegistryKey regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\.NETFramework");
                string cmd = (string)regKey.GetValue("InstallRoot");
				if (cmd != null) {
					string possFilename = Path.Combine(cmd, "v1.0.3705\\mscorlib.dll");
					if (System.IO.File.Exists(possFilename)) filename = possFilename;
				}
			}
			
			if (filename == "") throw new AssemblyNameNotFoundException(name);
			
			return LoadFrom(filename, fromGAC);
		}
		
		public static SharpAssembly Load(string name)
		{
			return Load(name, Environment.CurrentDirectory);
		}
		
		public static SharpAssembly Load(SharpAssemblyName name)
		{
			return Load(name.FullName, Environment.CurrentDirectory);
		}
		
		private static string GetAssemblyLocation(string assemblyName)
		{
			IAssemblyCache cache;
						
			FusionApi.CreateAssemblyCache(out cache, 0);
			ASSEMBLY_INFO info = new ASSEMBLY_INFO();
			
			cache.QueryAssemblyInfo(3, assemblyName, ref info);
			if (info.cchBuf != 0)
			{
		 		info.pszCurrentAssemblyPathBuf = new string(new char[info.cchBuf]);
				cache.QueryAssemblyInfo(3, assemblyName, ref info);
				return info.pszCurrentAssemblyPathBuf; 
			}
			return "";
			
		}
		
		internal SharpAssembly(AssemblyReader Reader, bool FromGac)
		{
			reader = Reader;
			tables = new MetadataTables(reader);
			
			// cache Name object
			internalName = GetAssemblyName();
			fromGAC = FromGac;
			
			fullNamePool[this.FullName] = this;
			if (!shortNamePool.ContainsKey(this.Name)) {
				shortNamePool[this.Name]    = this;
			}
		}
		
		
		AssemblyReader reader;
		MetadataTables tables;
		
		public AssemblyReader Reader {
			get {
				return reader;
			}
		}
		
		public string Location {
			get {
				return reader.FileName;
			}
		}
		
		public MetadataTables Tables {
			get {
				return tables;
			}
		}
		
		public string[] GetManifestResourceNames()
		{
			if (Tables.ManifestResource == null) return new string[0];
			string[] ret = new string[Tables.ManifestResource.GetUpperBound(0)];
			
			for (int i = 0; i <= ret.GetUpperBound(0); ++i) {
				ret[i] = reader.GetStringFromHeap(Tables.ManifestResource[i+1].Name);
			}
			
			return ret;
		}
		
		public int GetManifestResourceSize(string name)
		{
			if (Tables.ManifestResource == null) return 0;
			
			for (int i = 1; i <= Tables.ManifestResource.GetUpperBound(0); ++i) {
				if (reader.GetStringFromHeap(Tables.ManifestResource[i].Name) == name) {
					if (Tables.ManifestResource[i].Implementation != 0) {  // not in current file
						throw new System.NotImplementedException("resource in different file");
					}
					try {
						Stream fs = new System.IO.MemoryStream(reader.RawSectionData);
						fs.Seek(reader.LookupRVA(reader.CliHeader.Resources) + Tables.ManifestResource[i].Offset, SeekOrigin.Begin);
						BinaryReader binaryReader = new BinaryReader(fs);
						
						int size = (int)binaryReader.ReadUInt32();
						return size;
					} catch {
						return 0;
					}
				}
			}
			
			return 0;
		}
		
		public byte[] GetManifestResource(string name)
		{
			if (Tables.ManifestResource == null) return new byte[0];
			
			for (int i = 1; i <= Tables.ManifestResource.GetUpperBound(0); ++i) {
				if (reader.GetStringFromHeap(Tables.ManifestResource[i].Name) == name) {
					if (Tables.ManifestResource[i].Implementation != 0) {  // not in current file
						throw new System.NotImplementedException("resource in different file");
					}
					try {
						Stream fs = new System.IO.MemoryStream(reader.RawSectionData);
						fs.Seek(reader.LookupRVA(reader.CliHeader.Resources) + Tables.ManifestResource[i].Offset, SeekOrigin.Begin);
						BinaryReader binaryReader = new BinaryReader(fs);
						
						int size = (int)binaryReader.ReadUInt32();
						return binaryReader.ReadBytes(size);
					} catch {
						return new byte[0];
					}

				}
			}
			
			return new byte[0];
			
		}
		
		private void LoadReferences()
		{
			// load references
			SharpAssemblyName[] names = GetReferencedAssemblies();
			
			foreach(SharpAssemblyName name in names) {
				try {
					if (fullNamePool.ContainsKey(name.FullName)) {
						references.Add((int)name.RefId, fullNamePool[name.FullName]);
					} else {
						references.Add((int)name.RefId, Load(name));
					}
				} catch {
					Console.WriteLine("LoadReferences: Error loading reference " + name.FullName);
				}
			}
		}
			
		public SharpAssemblyName[] GetReferencedAssemblies()
		{
			if (Tables.AssemblyRef == null) return new SharpAssemblyName[0];
			SharpAssemblyName[] ret = new SharpAssemblyName[Tables.AssemblyRef.GetUpperBound(0)];
			
			for (int i = 1; i <= Tables.AssemblyRef.GetUpperBound(0); ++i) {
				AssemblyRef aref = Tables.AssemblyRef[i];
				ret[i-1]           = new SharpAssemblyName();
				ret[i-1].Name      = reader.GetStringFromHeap(aref.Name);
				ret[i-1].Version   = new Version(aref.Major, aref.Minor, aref.Build, aref.Revision);
				ret[i-1].Culture   = reader.GetStringFromHeap(aref.Culture);
				ret[i-1].Flags     = aref.Flags;
				ret[i-1].PublicKey = reader.GetBlobFromHeap(aref.PublicKeyOrToken);
				ret[i-1].RefId     = i;
			}
			
			return ret;
		}
		
		public SharpAssembly GetReference(int index)
		{
			if (!references.ContainsKey((int)index)) {
				Console.Write("GetReference: No such assembly ref index: " + index + " in assembly " + Name);
				Console.WriteLine("; ReferenceCount = " + references.Count);
				foreach (DictionaryEntry de in references) {
					Console.Write (de.Key.GetType().ToString() + " ");
				}
			}
			return (SharpAssembly)references[(int)index];
		}
		
		public SharpAssembly GetReference(string name)
		{
			bool fullName = false;
			if (name.IndexOf(',') != -1) fullName = true;
			
			foreach(DictionaryEntry de in references) {
				SharpAssembly assembly = (SharpAssembly)de.Value;
				string compare = (fullName ? assembly.FullName : assembly.Name);
				if (compare == name) return (SharpAssembly)de.Value;
			}
			return null;
		}
		
		public SharpAssembly GetRefAssemblyFor(uint typeRefIndex)
		{
			if (Tables.TypeRef == null) return null;
			
			uint val = Tables.TypeRef[typeRefIndex].ResolutionScope;
			int table = reader.GetCodedIndexTable(CodedIndex.ResolutionScope, ref val);
			
			switch (table) {
				case 2: // AssemblyRef
					return GetReference((int)val);
				case 3: // TypeRef -- nested type
					return GetRefAssemblyFor(val);
				case 0:
					Console.WriteLine("GetRefAssemblyFor: Unsupported ResolutionScope [Module]");
					return null;
				case 1:
					Console.WriteLine("GetRefAssemblyFor: Unsupported ResolutionScope [ModuleRef]");
					return null;
				default: // other token - not supported
					Console.WriteLine("GetRefAssemblyFor: Unsupported ResolutionScope [" + table + "] for assembly " + Name);
					return null;
			}
		}
		
		public SharpAssemblyName GetAssemblyName()
		{
			SharpAssemblyName name = new SharpAssemblyName();

			if (Tables.Assembly == null) return name;
			
			Rows.Assembly arow = Tables.Assembly[1];
			
			name.Name      = reader.GetStringFromHeap(arow.Name);
			name.Version   = new Version(arow.MajorVersion, arow.MinorVersion, arow.BuildNumber, arow.RevisionNumber);
			name.Culture   = reader.GetStringFromHeap(arow.Culture);
			name.Flags     = arow.Flags;
			name.PublicKey = reader.GetBlobFromHeap(arow.PublicKey);
			
			return name;
		}
		
		public static SharpAssemblyName GetAssemblyName(AssemblyReader asm)
		{
			SharpAssemblyName name = new SharpAssemblyName();

			if ((Rows.Assembly[])asm.MetadataTable.Tables[Rows.Assembly.TABLE_ID] == null) return name;
			
			Rows.Assembly arow = ((Rows.Assembly[])asm.MetadataTable.Tables[Rows.Assembly.TABLE_ID])[1];
			
			name.Name      = asm.GetStringFromHeap(arow.Name);
			name.Version   = new Version(arow.MajorVersion, arow.MinorVersion, arow.BuildNumber, arow.RevisionNumber);
			name.Culture   = asm.GetStringFromHeap(arow.Culture);
			name.Flags     = arow.Flags;
			name.PublicKey = asm.GetBlobFromHeap(arow.PublicKey);
			
			return name;
		}
		
		public static SharpAssemblyName GetNameOfReference(AssemblyReader asm, uint AsmRefIndex)
		{
			SharpAssemblyName name = new SharpAssemblyName();

			if ((AssemblyRef[])asm.MetadataTable.Tables[AssemblyRef.TABLE_ID] == null) return name;
			
			AssemblyRef aref = ((AssemblyRef[])asm.MetadataTable.Tables[AssemblyRef.TABLE_ID])[AsmRefIndex];
			
			name.Name      = asm.GetStringFromHeap(aref.Name);
			name.Version   = new Version(aref.Major, aref.Minor, aref.Build, aref.Revision);
			name.Culture   = asm.GetStringFromHeap(aref.Culture);
			name.Flags     = aref.Flags;
			name.PublicKey = asm.GetBlobFromHeap(aref.PublicKeyOrToken);
			
			return name;
		}
		
		SharpAssemblyName internalName = new SharpAssemblyName();
		
		public string Name {
			get {
				return internalName.Name;
			}
		}
		
		public string FullName {
			get {
				return internalName.FullName;
			}
		}
		
		void DebugMessage(string msg, Exception e)
		{
			//MessageBox.Show(msg + "\n\n" + e.ToString());
		}
		
		uint[] nestedType;
		
		void LoadNestedTypeTable()
		{
			if (Tables.TypeDef == null) {
				nestedType = new uint[0];
			}
			
			nestedType = new uint[Tables.TypeDef.GetUpperBound(0) + 1];
			
			if (Tables.NestedClass == null) {
				return;
			}
			
			for (uint i = 1; i <= Tables.NestedClass.GetUpperBound(0); ++i) {
				nestedType[Tables.NestedClass[i].NestedClassIndex] = Tables.NestedClass[i].EnclosingClass;
			}
		}
		
		public uint GetNestedTypeParent(uint index)
		{
			try {
				return nestedType[index];
			} catch {
				return 0; // not nested!
			}
		}
		
		// to store objects that are associated with TypeDef/TypeRef items
		Hashtable typeRefObjects = new Hashtable();
		Hashtable typeDefObjects = new Hashtable();
		
		public Hashtable TypeRefObjects {
			get {
				return typeRefObjects;
			}
		}
		
		public Hashtable TypeDefObjects {
			get {
				return typeDefObjects;
			}
		}
		
		bool fromGAC;
		
		public bool FromGAC {
			get {
				return fromGAC;
			}
		}
		
		Hashtable constantTable = new Hashtable();
		
		public Hashtable FieldConstantTable {
			get {
				return constantTable;
			}
		}
		
		void LoadFieldConstants()
		{
			if (Tables.Constant == null) return;
			
			for (uint i = 1; i <= Tables.Constant.GetUpperBound(0); ++i) {
				uint cst = Tables.Constant[i].Parent;
				int tbl  = reader.GetCodedIndexTable(CodedIndex.HasConstant, ref cst);
				if (tbl != 0) continue;
				
				constantTable[cst] = Tables.Constant[i];
			}
		}
		
		// to store attribute definitions
		CustomAttributeTable attributes = new CustomAttributeTable();
		
		public CustomAttributeTable Attributes {
			get {
				return attributes;
			}
		}
		
		void LoadAttributeTable()
		{
			CustomAttribute[] attrTable = Tables.CustomAttribute;
			if (attrTable == null) return;
			
			uint pval = 0;
			int table = 0;
			
			for (uint i = 1; i <= attrTable.GetUpperBound(0); ++i) {
				pval  = attrTable[i].Parent;
				table = reader.GetCodedIndexTable(CodedIndex.HasCustomAttribute, ref pval);
				
				Hashtable hashtable;
				
				switch(table) {
					case 0: hashtable = attributes.Method; break;
					case 1: hashtable = attributes.Field; break;
					case 2: hashtable = attributes.TypeRef; break;
					case 3: hashtable = attributes.TypeDef; break;
					case 4: hashtable = attributes.Param; break;
					case 9: hashtable = attributes.Property; break;
					case 10: hashtable = attributes.Event; break;
					case 14: hashtable = attributes.Assembly; break;
					default:
						continue;
				}
				
				AddAttribute(hashtable, pval, new SharpCustomAttribute(this, attrTable[i].Type, attrTable[i].Val));
			}
		}
		
		void AddAttribute(Hashtable table, uint index, object attribute)
		{
			if (table[index] == null) {
				table[index] = new ArrayList();
			}
			
			ArrayList list = (ArrayList)table[index];
			
			list.Add(attribute);
		}
	}
	
	public class SharpCustomAttribute
	{
		uint memberIndex;
		bool isMemberRef = false;
		uint valueIndex;
				SharpAssembly assembly;
		
		public SharpCustomAttribute(SharpAssembly Assembly, uint TypeIndex, uint ValueIndex)
		{
			assembly = Assembly;
			valueIndex = ValueIndex;
			
			memberIndex = TypeIndex;
			int table = assembly.Reader.GetCodedIndexTable(CodedIndex.CustomAttributeType, ref memberIndex);
			
			if (table == 3) isMemberRef = true;
		}
		
		public uint MemberIndex {
			get {
				return memberIndex;
			}
		}
		
		public uint ValueIndex {
			get {
				return valueIndex;
			}
		}
		
		public bool IsMemberRef {
			get {
				return isMemberRef;
			}
		}
		
		public override string ToString()
		{
			return "CustomAttribute: Index " + memberIndex + " (IsMemberRef: " + isMemberRef + ") -> " + valueIndex;
		}
	}
	
	public class CustomAttributeTable
	{
		public Hashtable Method;
		public Hashtable Field;
		public Hashtable TypeRef;
		public Hashtable TypeDef;
		public Hashtable Param;
		public Hashtable Property;
		public Hashtable Event;
		public Hashtable Assembly;
		
		public CustomAttributeTable()
		{
			Method   = new Hashtable();
			Field    = new Hashtable();
			TypeRef  = new Hashtable();
			TypeDef  = new Hashtable();
			Param    = new Hashtable();
			Property = new Hashtable();
			Event    = new Hashtable();
			Assembly = new Hashtable();
		}
	}

}
 
