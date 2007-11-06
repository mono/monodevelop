//  PersistentParameter.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
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
