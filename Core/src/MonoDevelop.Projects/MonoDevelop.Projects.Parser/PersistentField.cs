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
	internal sealed class PersistentField
	{
		public static DefaultField Resolve (IField source, ITypeResolver typeResolver)
		{
			DefaultField field = new DefaultField ();
			field.Name = source.Name;
			field.Documentation = source.Documentation;
			field.Modifiers = source.Modifiers;
			field.ReturnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			field.Region = source.Region;
			field.Attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			return field;
		}
		
		public static DefaultField Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultField field = new DefaultField ();
			field.Name = PersistentHelper.ReadString (reader, nameTable);
			field.Documentation = PersistentHelper.ReadString (reader, nameTable);
			field.Modifiers = (ModifierEnum)reader.ReadUInt32();
			field.ReturnType = PersistentReturnType.Read (reader, nameTable);
			field.Region = PersistentRegion.Read (reader, nameTable);
			field.Attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return field;
		}
		
		public static void WriteTo (IField field, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (field.Name, writer, nameTable);
			PersistentHelper.WriteString (field.Documentation, writer, nameTable);
			writer.Write ((uint)field.Modifiers);
			PersistentReturnType.WriteTo (field.ReturnType, writer, nameTable);
			PersistentRegion.WriteTo (field.Region, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (field.Attributes, writer, nameTable);
		}
	}
}
