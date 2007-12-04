//  PersistentClass.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Binary;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class PersistentClass
	{
		public static DefaultClass Resolve (IClass sclass, ITypeResolver typeResolver)
		{
			DefaultClass cls = new DefaultClass ();
			
			cls.Name = sclass.Name;
			cls.Namespace = sclass.Namespace;
			cls.Documentation = sclass.Documentation;
			
			cls.Modifiers          = sclass.Modifiers;
			cls.ClassType          = sclass.ClassType;

			foreach (IReturnType t in sclass.BaseTypes)
			{
				cls.BaseTypes.Add (PersistentReturnType.Resolve(t, typeResolver));
			}
			
			foreach (IClass c in sclass.InnerClasses) {
				DefaultClass pc = PersistentClass.Resolve (c, typeResolver);
				pc.DeclaredIn = cls;
				cls.InnerClasses.Add (pc);
			}

			foreach (IField f in sclass.Fields) {
				DefaultField pf = PersistentField.Resolve (f, typeResolver);
				cls.Fields.Add (pf);
			}

			foreach (IProperty p in sclass.Properties) {
				DefaultProperty pp = PersistentProperty.Resolve (p, typeResolver);
				cls.Properties.Add (pp);
			}

			foreach (IMethod m in sclass.Methods) {
				DefaultMethod pm = PersistentMethod.Resolve (m, typeResolver);
				cls.Methods.Add (pm);
			}

			foreach (IEvent e in sclass.Events) {
				DefaultEvent pe = PersistentEvent.Resolve (e, typeResolver);
				cls.Events.Add (pe);
			}

			foreach (IIndexer i in sclass.Indexer) {
				DefaultIndexer pi = PersistentIndexer.Resolve (i, typeResolver);
				cls.Indexer.Add (pi);
			}
			
			if (sclass.GenericParameters != null && sclass.GenericParameters.Count > 0) {
				cls.GenericParameters = new GenericParameterList();
				foreach (GenericParameter gp in sclass.GenericParameters) {
					cls.GenericParameters.Add(PersistentGenericParamater.Resolve(gp, typeResolver));
				}
			}
			
			cls.Region = sclass.Region;
			cls.BodyRegion = sclass.BodyRegion;
			cls.Attributes = PersistentAttributeSectionCollection.Resolve (sclass.Attributes, typeResolver);
			return cls;
		}
		
		public static DefaultClass Read (BinaryReader reader, INameDecoder nameTable)
		{
			uint classCount = reader.ReadUInt32();
			if (classCount > 1) {
				// It's a compound class
				CompoundClass ccls = new CompoundClass ();
				for (uint i = 0; i < classCount; i++)
					ccls.AddClass (Read (reader, nameTable));
				ccls.UpdateInformationFromParts ();
				return ccls;
			}
			
			DefaultClass cls = new DefaultClass ();
			
			cls.Name = PersistentHelper.ReadString (reader, nameTable);
			cls.Namespace = PersistentHelper.ReadString (reader, nameTable);
			cls.Documentation = PersistentHelper.ReadString (reader, nameTable);
			
			cls.Modifiers          = (ModifierEnum)reader.ReadUInt32();
			cls.ClassType          = (ClassType)reader.ReadInt16();

			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				cls.BaseTypes.Add (PersistentReturnType.Read (reader, nameTable));
			}
			
			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				DefaultClass c = PersistentClass.Read (reader, nameTable);
				c.DeclaredIn = cls;
				cls.InnerClasses.Add (c);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				DefaultField f = PersistentField.Read (reader, nameTable);
				cls.Fields.Add (f);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				DefaultProperty p = PersistentProperty.Read (reader, nameTable);
				cls.Properties.Add (p);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				DefaultMethod m = PersistentMethod.Read (reader, nameTable);
				cls.Methods.Add(m);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				DefaultEvent e = PersistentEvent.Read (reader, nameTable);
				cls.Events.Add (e);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				DefaultIndexer ind = PersistentIndexer.Read (reader, nameTable);
				cls.Indexer.Add (ind);
			}
			
			// Read the generic parameters
			count = reader.ReadUInt32();
			if (count > 0) {
				cls.GenericParameters = new GenericParameterList();
				// Add the generic parameters one by one
				for (uint i = 0; i < count; ++i) {
					cls.GenericParameters.Add(PersistentGenericParamater.Read(reader, nameTable));
				}
				// All the generic parameters have been added...
			}
			
			cls.Region = PersistentRegion.Read (reader, nameTable);
			cls.BodyRegion = PersistentRegion.Read (reader, nameTable);
			cls.Attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return cls;
		}

		public static void WriteTo (IClass cls, BinaryWriter writer, INameEncoder nameTable)
		{
			if (cls is CompoundClass) {
				// If it is a compound class, write each child class
				CompoundClass comp = (CompoundClass) cls;
				IClass[] parts = comp.Parts;
				writer.Write ((uint) parts.Length);
				foreach (IClass cc in parts)
					WriteTo (cc, writer, nameTable);
				return;
			}
			
			// Not a compound class
			writer.Write ((uint)1);
			
			PersistentHelper.WriteString (cls.Name, writer, nameTable);
			PersistentHelper.WriteString (cls.Namespace, writer, nameTable);
			PersistentHelper.WriteString (cls.Documentation, writer, nameTable);
				
			writer.Write((uint)cls.Modifiers);
			writer.Write((short)cls.ClassType);
				
			writer.Write((uint)(cls.BaseTypes.Count));
			foreach (IReturnType baseType in cls.BaseTypes)
				PersistentReturnType.WriteTo(baseType, writer, nameTable);

			writer.Write((uint)cls.InnerClasses.Count);
			foreach (IClass innerClass in cls.InnerClasses) {
				PersistentClass.WriteTo (innerClass, writer, nameTable);
			}

			writer.Write((uint)cls.Fields.Count);
			foreach (IField field in cls.Fields) {
				PersistentField.WriteTo (field, writer, nameTable);
			}

			writer.Write((uint)cls.Properties.Count);
			foreach (IProperty property in cls.Properties) {
				PersistentProperty.WriteTo (property, writer, nameTable);
			}

			writer.Write((uint)cls.Methods.Count);
			foreach (IMethod method in cls.Methods) {
				PersistentMethod.WriteTo (method, writer, nameTable);
			}

			writer.Write((uint)cls.Events.Count);
			foreach (IEvent e in cls.Events) {
				PersistentEvent.WriteTo (e, writer, nameTable);
			}

			writer.Write((uint)cls.Indexer.Count);
			foreach (IIndexer ind in cls.Indexer) {
				PersistentIndexer.WriteTo (ind, writer, nameTable);
			}
			
			// Write the generic parameters to the database file
			if (cls.GenericParameters == null || cls.GenericParameters.Count < 1)
				writer.Write((uint)0);
			else {
				writer.Write((uint)cls.GenericParameters.Count);
				foreach (GenericParameter gp in cls.GenericParameters) {
					PersistentGenericParamater.WriteTo(gp, writer, nameTable);
				}
			}
			
			
			PersistentRegion.WriteTo (cls.Region, writer, nameTable);
			PersistentRegion.WriteTo (cls.BodyRegion, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (cls.Attributes, writer, nameTable);
		}
	}
	
	internal class PersistentRegion: DefaultRegion
	{
		public PersistentRegion (): base (-1,-1)
		{
		}
		
		public static PersistentRegion Read (BinaryReader reader, INameDecoder nameTable)
		{
			if (PersistentHelper.ReadNull (reader)) return null;
			
			PersistentRegion reg = new PersistentRegion ();
			reg.FileName = PersistentHelper.ReadString (reader, nameTable);
			reg.beginLine = reader.ReadInt32 ();
			reg.endLine = reader.ReadInt32 ();
			reg.beginColumn = reader.ReadInt32 ();
			reg.endColumn = reader.ReadInt32 ();
			return reg;
		}
		
		public static void WriteTo (IRegion reg, BinaryWriter writer, INameEncoder nameTable)
		{
			if (PersistentHelper.WriteNull (reg, writer)) return;
			
			PersistentHelper.WriteString (reg.FileName, writer, nameTable);
			writer.Write (reg.BeginLine);
			writer.Write (reg.EndLine);
			writer.Write (reg.BeginColumn);
			writer.Write (reg.EndColumn);
		}
	}
	
	internal class PersistentHelper
	{
		static BinaryFormatter formatter = new BinaryFormatter ();
		
		public static void WriteString (string s, BinaryWriter writer, INameEncoder nameTable)
		{
			if (s == null)
				writer.Write (-2);
			else {
				int id = nameTable.GetStringId (s);
				writer.Write (id);
				if (id == -1)
					writer.Write (s);
			}
		}
		
		public static string ReadString (BinaryReader reader, INameDecoder nameTable)
		{
			int id = reader.ReadInt32 ();
			if (id == -1)
				return reader.ReadString ();
			else if (id == -2)
				return null;
			else
				return nameTable.GetStringValue (id);
		}
		
		public static bool WriteNull (object ob, BinaryWriter writer)
		{
			writer.Write (ob==null);
			return ob==null;
		}
		
		public static bool ReadNull (BinaryReader reader)
		{
			return reader.ReadBoolean ();
		}
		
		public static void SerializeObject (BinaryWriter writer, object obj)
		{
			if (obj == null)
				writer.Write (0);
			else {
				MemoryStream ms = new MemoryStream ();
				formatter.Serialize (ms, obj);
				byte[] data = ms.ToArray ();
				writer.Write (data.Length);
				writer.Write (data);
			}
		}
		
		public static object DeserializeObject (BinaryReader reader)
		{
			int len = reader.ReadInt32 ();
			if (len == 0) return null;
			byte[] data = reader.ReadBytes (len);
			MemoryStream ms = new MemoryStream (data);
			return formatter.Deserialize (ms);
		}
	}
}
