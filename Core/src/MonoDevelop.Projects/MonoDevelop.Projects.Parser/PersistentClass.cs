// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
	internal sealed class PersistentClass : AbstractClass
	{
		public override ICompilationUnit CompilationUnit {
			get {
				return null;
			}
		}

		public static PersistentClass Resolve (IClass sclass, ITypeResolver typeResolver)
		{
			PersistentClass cls = new PersistentClass ();
			
			cls.FullyQualifiedName = sclass.FullyQualifiedName;
			cls.Documentation = sclass.Documentation;
			
			cls.modifiers          = sclass.Modifiers;
			cls.classType          = sclass.ClassType;

			foreach (string t in sclass.BaseTypes)
				cls.baseTypes.Add (typeResolver.Resolve (t));
			
			foreach (IClass c in sclass.InnerClasses) {
				PersistentClass pc = PersistentClass.Resolve (c, typeResolver);
				pc.declaredIn = cls;
				cls.innerClasses.Add (pc);
			}

			foreach (IField f in sclass.Fields) {
				PersistentField pf = PersistentField.Resolve (f, typeResolver);
				pf.DeclaringType = cls;
				cls.fields.Add (pf);
			}

			foreach (IProperty p in sclass.Properties) {
				PersistentProperty pp = PersistentProperty.Resolve (p, typeResolver);
				pp.DeclaringType = cls;
				cls.properties.Add (pp);
			}

			foreach (IMethod m in sclass.Methods) {
				PersistentMethod pm = PersistentMethod.Resolve (m, typeResolver);
				pm.DeclaringType = cls;
				cls.methods.Add (pm);
			}

			foreach (IEvent e in sclass.Events) {
				PersistentEvent pe = PersistentEvent.Resolve (e, typeResolver);
				pe.DeclaringType = cls;
				cls.events.Add (pe);
			}

			foreach (IIndexer i in sclass.Indexer) {
				PersistentIndexer pi = PersistentIndexer.Resolve (i, typeResolver);
				pi.DeclaringType = cls;
				cls.indexer.Add (pi);
			}
			
			cls.region = sclass.Region;
			cls.bodyRegion = sclass.BodyRegion;
			cls.attributes = PersistentAttributeSectionCollection.Resolve (sclass.Attributes, typeResolver);
			return cls;
		}
		
		public static PersistentClass Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentClass cls = new PersistentClass ();
			
			cls.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			cls.Documentation = PersistentHelper.ReadString (reader, nameTable);
			
			cls.modifiers          = (ModifierEnum)reader.ReadUInt32();
			cls.classType          = (ClassType)reader.ReadInt16();

			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				cls.baseTypes.Add (PersistentHelper.ReadString (reader, nameTable));
			}
			
			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				PersistentClass c = PersistentClass.Read (reader, nameTable);
				c.declaredIn = cls;
				cls.innerClasses.Add (c);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				PersistentField f = PersistentField.Read (reader, nameTable);
				f.DeclaringType = cls;
				cls.fields.Add (f);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				PersistentProperty p = PersistentProperty.Read (reader, nameTable);
				p.DeclaringType = cls;
				cls.properties.Add (p);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				PersistentMethod m = PersistentMethod.Read (reader, nameTable);
				m.DeclaringType = cls;
				cls.methods.Add(m);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				PersistentEvent e = PersistentEvent.Read (reader, nameTable);
				e.DeclaringType = cls;
				cls.events.Add (e);
			}

			count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				PersistentIndexer ind = PersistentIndexer.Read (reader, nameTable);
				ind.DeclaringType = cls;
				cls.indexer.Add (ind);
			}
			
			cls.region = PersistentRegion.Read (reader, nameTable);
			cls.bodyRegion = PersistentRegion.Read (reader, nameTable);
			cls.attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return cls;
		}

		public static void WriteTo (IClass cls, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (cls.FullyQualifiedName, writer, nameTable);
			PersistentHelper.WriteString (cls.Documentation, writer, nameTable);
				
			writer.Write((uint)cls.Modifiers);
			writer.Write((short)cls.ClassType);

			writer.Write((uint)(cls.BaseTypes.Count));
			foreach (string baseType in cls.BaseTypes)
				PersistentHelper.WriteString (baseType, writer, nameTable);

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
