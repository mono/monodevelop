//  PersistentEvent.cs
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
