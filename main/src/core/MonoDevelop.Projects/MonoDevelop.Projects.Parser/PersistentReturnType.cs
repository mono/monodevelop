//  PersistentReturnType.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal sealed class PersistentReturnType
	{
		public static IReturnType Resolve (IReturnType source, ITypeResolver typeResolver)
		{
			if (source == null) return null;
			return typeResolver.Resolve (source);
		}

		public static IReturnType Read (BinaryReader reader, INameDecoder nameTable)
		{
			if (PersistentHelper.ReadNull (reader)) return null;
			
			DefaultReturnType rt = new DefaultReturnType ();
			rt.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			
			rt.ByRef = reader.ReadBoolean();

			rt.PointerNestingLevel = reader.ReadInt32();

			uint count = reader.ReadUInt32();
			int[] dims = new int[count];
			for (uint i = 0; i < count; ++i) {
				dims[i] = reader.ReadInt32();
			}
			rt.ArrayDimensions = dims;
			
			// Read the generic arguments
			count = reader.ReadUInt32();
			if (count > 0) {
				rt.GenericArguments = new ReturnTypeList();
				// Add the generic arguments one by one
				for (uint i = 0; i < count; ++i) {
					rt.GenericArguments.Add(PersistentReturnType.Read(reader, nameTable));
				}
				// All the generic arguments have been added...
			}
			
			return DefaultReturnType.GetSharedType (rt);
		}

		public static void WriteTo (IReturnType rt, BinaryWriter writer, INameEncoder nameTable)
		{
			if (PersistentHelper.WriteNull (rt, writer)) return;
			
			PersistentHelper.WriteString (rt.FullyQualifiedName, writer, nameTable);
			
			writer.Write(rt.ByRef);

			writer.Write (rt.PointerNestingLevel);
			if (rt.ArrayDimensions == null) {
				writer.Write((uint)0);
			} else {
				writer.Write((uint)rt.ArrayDimensions.Length);
				for (uint i = 0; i < rt.ArrayDimensions.Length; ++i) {
					writer.Write (rt.ArrayDimensions[i]);
				}
			}
			
			// Write generic arguments of this return type
			if (rt.GenericArguments == null || rt.GenericArguments.Count < 1)
				writer.Write((uint)0);
			else {
				writer.Write((uint)rt.GenericArguments.Count);
				foreach (IReturnType ga in rt.GenericArguments) {
					PersistentReturnType.WriteTo(ga, writer, nameTable);
				}
			}
		}
	}
}
