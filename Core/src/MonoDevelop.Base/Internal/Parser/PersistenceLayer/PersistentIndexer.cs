// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.IO;
using System.Reflection;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public sealed class PersistentIndexer : AbstractIndexer
	{
		public static PersistentIndexer Resolve (IIndexer source, ITypeResolver typeResolver)
		{
			PersistentIndexer ind = new PersistentIndexer();
			ind.FullyQualifiedName = source.FullyQualifiedName;
			ind.Documentation = source.Documentation;
			ind.modifiers = source.Modifiers;
			ind.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);

			foreach (IParameter p in source.Parameters)
				ind.parameters.Add (PersistentParameter.Resolve (p, typeResolver));

			ind.region = source.Region;
			return ind;
		}
		
		public static PersistentIndexer Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentIndexer ind = new PersistentIndexer();
			ind.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			ind.Documentation = PersistentHelper.ReadString (reader, nameTable);
			ind.modifiers = (ModifierEnum)reader.ReadUInt32();
			ind.returnType = PersistentReturnType.Read (reader, nameTable);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				ind.parameters.Add (PersistentParameter.Read (reader, nameTable));
			}
			ind.region = PersistentRegion.Read (reader, nameTable);
			return ind;
		}
		
		public static void WriteTo (IIndexer ind, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (ind.FullyQualifiedName, writer, nameTable);
			PersistentHelper.WriteString (ind.Documentation, writer, nameTable);
			
			writer.Write((uint)ind.Modifiers);
			PersistentReturnType.WriteTo (ind.ReturnType, writer, nameTable);
			
			writer.Write ((uint)ind.Parameters.Count);
			foreach (IParameter p in ind.Parameters) {
				PersistentParameter.WriteTo (p, writer, nameTable);
			}
			PersistentRegion.WriteTo (ind.Region, writer, nameTable);
		}
	}
}
