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
	internal sealed class PersistentParameter
	{
		public static DefaultParameter Resolve (IParameter source, ITypeResolver typeResolver)
		{
			DefaultParameter par = new DefaultParameter ();
			par.Name = source.Name;
			par.Documentation = source.Documentation;
			par.Modifier = source.Modifier;
			par.ReturnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			return par;
		}
		
		public static DefaultParameter Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultParameter par = new DefaultParameter ();
			par.Name = PersistentHelper.ReadString (reader, nameTable);
			par.Documentation = PersistentHelper.ReadString (reader, nameTable);
			par.Modifier = (ParameterModifier)reader.ReadByte();
			par.ReturnType = PersistentReturnType.Read (reader, nameTable);
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
