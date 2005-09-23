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
	public sealed class PersistentField : AbstractField
	{
		public static PersistentField Resolve (IField source, ITypeResolver typeResolver)
		{
			PersistentField field = new PersistentField ();
			field.FullyQualifiedName = source.FullyQualifiedName;
			field.Documentation = source.Documentation;
			field.modifiers = source.Modifiers;
			field.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			field.region = source.Region;
			return field;
		}
		
		public static PersistentField Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentField field = new PersistentField ();
			field.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			field.Documentation = PersistentHelper.ReadString (reader, nameTable);
			field.modifiers = (ModifierEnum)reader.ReadUInt32();
			field.returnType = PersistentReturnType.Read (reader, nameTable);
			field.region = PersistentRegion.Read (reader, nameTable);
			return field;
		}
		
		public static void WriteTo (IField field, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (field.FullyQualifiedName, writer, nameTable);
			PersistentHelper.WriteString (field.Documentation, writer, nameTable);
			writer.Write ((uint)field.Modifiers);
			PersistentReturnType.WriteTo (field.ReturnType, writer, nameTable);
			PersistentRegion.WriteTo (field.Region, writer, nameTable);
		}
	}
}
