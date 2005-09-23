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
	public sealed class PersistentEvent : AbstractEvent
	{
		public static PersistentEvent Resolve (IEvent source, ITypeResolver typeResolver)
		{
			PersistentEvent ev = new PersistentEvent();
			ev.FullyQualifiedName = source.FullyQualifiedName;
			ev.Documentation = source.Documentation;
			ev.modifiers = source.Modifiers;
			ev.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			ev.region = source.Region;
			return ev;
		}
		
		public static PersistentEvent Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentEvent ev = new PersistentEvent();
			ev.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			ev.Documentation = PersistentHelper.ReadString (reader, nameTable);
			ev.modifiers = (ModifierEnum)reader.ReadUInt32();
			ev.returnType = PersistentReturnType.Read (reader, nameTable);
			ev.region = PersistentRegion.Read (reader, nameTable);
			return ev;
		}

		public static void WriteTo (IEvent ev, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (ev.FullyQualifiedName, writer, nameTable);
			PersistentHelper.WriteString (ev.Documentation, writer, nameTable);
			writer.Write ((uint)ev.Modifiers);
			PersistentReturnType.WriteTo (ev.ReturnType, writer, nameTable);
			PersistentRegion.WriteTo (ev.Region, writer, nameTable);
		}
	}
}
