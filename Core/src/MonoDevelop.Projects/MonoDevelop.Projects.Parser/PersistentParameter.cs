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
		public static DefaultParameter Resolve (IParameter source, IMember declaringMember, ITypeResolver typeResolver)
		{
			IReturnType returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			DefaultParameter par = new DefaultParameter (declaringMember, source.Name, returnType);
			par.Documentation = source.Documentation;
			par.Modifier = source.Modifier;
			
			return par;
		}
		
		public static DefaultParameter Read (BinaryReader reader, IMember declaringMember, INameDecoder nameTable)
		{
			string name = PersistentHelper.ReadString (reader, nameTable);
			string docs = PersistentHelper.ReadString (reader, nameTable);
			byte mod = reader.ReadByte ();
			IReturnType returnType = PersistentReturnType.Read (reader, nameTable);
			
			DefaultParameter param = new DefaultParameter (declaringMember, name, returnType);
			param.Modifier = (ParameterModifier) mod;
			param.Documentation = docs;
			
			return param;
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
