//  PersistentField.cs
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
