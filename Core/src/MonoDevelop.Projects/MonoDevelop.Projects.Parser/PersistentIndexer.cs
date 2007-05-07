// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.IO;
using System.Reflection;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal sealed class PersistentIndexer
	{
		public static DefaultIndexer Resolve (IIndexer source, ITypeResolver typeResolver)
		{
			DefaultIndexer ind = new DefaultIndexer ();
			ind.Name = source.Name;
			ind.Documentation = source.Documentation;
			ind.Modifiers = source.Modifiers;
			ind.ReturnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);

			foreach (IParameter p in source.Parameters)
				ind.Parameters.Add (PersistentParameter.Resolve (p, (IMember) ind, typeResolver));

			ind.Region = source.Region;
			ind.Attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			return ind;
		}
		
		public static DefaultIndexer Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultIndexer ind = new DefaultIndexer ();
			ind.Name = PersistentHelper.ReadString (reader, nameTable);
			ind.Documentation = PersistentHelper.ReadString (reader, nameTable);
			ind.Modifiers = (ModifierEnum)reader.ReadUInt32();
			ind.ReturnType = PersistentReturnType.Read (reader, nameTable);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				ind.Parameters.Add (PersistentParameter.Read (reader, (IMember) ind, nameTable));
			}
			ind.Region = PersistentRegion.Read (reader, nameTable);
			ind.Attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return ind;
		}
		
		public static void WriteTo (IIndexer ind, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (ind.Name, writer, nameTable);
			PersistentHelper.WriteString (ind.Documentation, writer, nameTable);
			
			writer.Write((uint)ind.Modifiers);
			PersistentReturnType.WriteTo (ind.ReturnType, writer, nameTable);
			
			writer.Write ((uint)ind.Parameters.Count);
			foreach (IParameter p in ind.Parameters) {
				PersistentParameter.WriteTo (p, writer, nameTable);
			}
			PersistentRegion.WriteTo (ind.Region, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (ind.Attributes, writer, nameTable);
		}
	}
}
