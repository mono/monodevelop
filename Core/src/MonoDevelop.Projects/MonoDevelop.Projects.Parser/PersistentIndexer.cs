//  PersistentIndexer.cs
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
	internal sealed class PersistentIndexer
	{
		public static DefaultIndexer Resolve (IIndexer source, ITypeResolver typeResolver)
		{
			DefaultIndexer ind = new DefaultIndexer ();
			ind.Name = source.Name;
			ind.Documentation = source.Documentation;
			ind.Modifiers = source.Modifiers;
			ind.ReturnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);

			foreach (IParameter p in source.Parameters)
				ind.Parameters.Add (PersistentParameter.Resolve (p, (IMember) ind, typeResolver));

			ind.Region = source.Region;
			ind.Attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			ind.ExplicitDeclaration = PersistentReturnType.Resolve (source.ExplicitDeclaration, typeResolver);
			return ind;
		}
		
		public static DefaultIndexer Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultIndexer ind = new DefaultIndexer ();
			ind.Name = PersistentHelper.ReadString (reader, nameTable);
			ind.Documentation = PersistentHelper.ReadString (reader, nameTable);
			ind.Modifiers = (ModifierEnum)reader.ReadUInt32();
			ind.ReturnType = PersistentReturnType.Read (reader, nameTable);
			ind.ExplicitDeclaration = PersistentReturnType.Read (reader, nameTable);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				ind.Parameters.Add (PersistentParameter.Read (reader, (IMember) ind, nameTable));
			}
			ind.Region = PersistentRegion.Read (reader, nameTable);
			ind.Attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return ind;
		}
		
		public static void WriteTo (IIndexer ind, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (ind.Name, writer, nameTable);
			PersistentHelper.WriteString (ind.Documentation, writer, nameTable);
			
			writer.Write((uint)ind.Modifiers);
			PersistentReturnType.WriteTo (ind.ReturnType, writer, nameTable);
			PersistentReturnType.WriteTo (ind.ExplicitDeclaration, writer, nameTable);
			
			writer.Write ((uint)ind.Parameters.Count);
			foreach (IParameter p in ind.Parameters) {
				PersistentParameter.WriteTo (p, writer, nameTable);
			}
			PersistentRegion.WriteTo (ind.Region, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (ind.Attributes, writer, nameTable);
		}
	}
}
