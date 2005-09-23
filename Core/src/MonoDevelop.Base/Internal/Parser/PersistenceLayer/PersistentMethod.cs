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
	public sealed class PersistentMethod : AbstractMethod
	{
		public static PersistentMethod Resolve (IMethod source, ITypeResolver typeResolver)
		{
			PersistentMethod met = new PersistentMethod ();
			met.FullyQualifiedName = source.FullyQualifiedName;
			met.Documentation = source.Documentation;
			met.modifiers = source.Modifiers;
			met.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			
			foreach (IParameter p in source.Parameters)
				met.parameters.Add (PersistentParameter.Resolve (p, typeResolver));

			met.region = source.Region;
			return met;
		}
		
		public static PersistentMethod Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentMethod met = new PersistentMethod ();
			met.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			met.Documentation = PersistentHelper.ReadString (reader, nameTable);
			
			met.modifiers = (ModifierEnum)reader.ReadUInt32();
			met.returnType = PersistentReturnType.Read (reader, nameTable);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				met.parameters.Add (PersistentParameter.Read (reader, nameTable));
			}
			met.region = PersistentRegion.Read (reader, nameTable);
			return met;
		}
		
		public static void WriteTo (IMethod met, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (met.FullyQualifiedName, writer, nameTable);
			PersistentHelper.WriteString (met.Documentation, writer, nameTable);
			
			writer.Write ((uint)met.Modifiers);
			PersistentReturnType.WriteTo (met.ReturnType, writer, nameTable);
			
			writer.Write (met.Parameters != null ? (uint)met.Parameters.Count : (uint)0);
			foreach (IParameter p in met.Parameters) {
				PersistentParameter.WriteTo (p, writer, nameTable);
			}
			PersistentRegion.WriteTo (met.Region, writer, nameTable);
		}
	}
}
