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
	public sealed class PersistentParameter : AbstractParameter
	{
		public static PersistentParameter Resolve (IParameter source, ITypeResolver typeResolver)
		{
			PersistentParameter par = new PersistentParameter ();
			par.name = source.Name;
			par.documentation = source.Documentation;
			par.modifier = source.Modifier;
			par.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			return par;
		}
		
		public static PersistentParameter Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentParameter par = new PersistentParameter ();
			par.name = PersistentHelper.ReadString (reader, nameTable);
			par.documentation = PersistentHelper.ReadString (reader, nameTable);
			par.modifier = (ParameterModifier)reader.ReadByte();
			par.returnType = PersistentReturnType.Read (reader, nameTable);
			return par;
		}
		
		public static void WriteTo (IParameter p, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (p.Name, writer, nameTable);
			PersistentHelper.WriteString (p.Documentation, writer, nameTable);
			writer.Write((byte)p.Modifier);
			PersistentReturnType.WriteTo (p.ReturnType, writer, nameTable);
		}
	}
}
