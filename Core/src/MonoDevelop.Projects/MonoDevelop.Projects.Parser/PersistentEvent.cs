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
	internal sealed class PersistentEvent
	{
		public static DefaultEvent Resolve (IEvent source, ITypeResolver typeResolver)
		{
			DefaultEvent ev = new DefaultEvent ();
			ev.Name = source.Name;
			ev.Documentation = source.Documentation;
			ev.Modifiers = source.Modifiers;
			ev.ReturnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			ev.Region = source.Region;
			ev.Attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			ev.ExplicitDeclaration = PersistentReturnType.Resolve (source.ExplicitDeclaration, typeResolver);
			return ev;
		}
		
		public static DefaultEvent Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultEvent ev = new DefaultEvent ();
			ev.Name = PersistentHelper.ReadString (reader, nameTable);
			ev.Documentation = PersistentHelper.ReadString (reader, nameTable);
			ev.Modifiers = (ModifierEnum)reader.ReadUInt32();
			ev.ReturnType = PersistentReturnType.Read (reader, nameTable);
			ev.ExplicitDeclaration = PersistentReturnType.Read (reader, nameTable);
			ev.Region = PersistentRegion.Read (reader, nameTable);
			ev.Attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return ev;
		}

		public static void WriteTo (IEvent ev, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (ev.Name, writer, nameTable);
			PersistentHelper.WriteString (ev.Documentation, writer, nameTable);
			writer.Write ((uint)ev.Modifiers);
			PersistentReturnType.WriteTo (ev.ReturnType, writer, nameTable);
			PersistentReturnType.WriteTo (ev.ExplicitDeclaration, writer, nameTable);
			PersistentRegion.WriteTo (ev.Region, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (ev.Attributes, writer, nameTable);
		}
	}
}
