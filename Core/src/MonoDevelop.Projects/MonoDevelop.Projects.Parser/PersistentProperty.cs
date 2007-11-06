//  PersistentProperty.cs
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
	internal sealed class PersistentProperty : DefaultProperty
	{
		const uint canGetFlag = (uint)(1 << 29);
		const uint canSetFlag = (uint)(1 << 30);
		
		bool canGet = false;
		bool canSet = false;
		
		public override bool CanGet {
			get {
				return canGet;
			}
		}
		
		public override bool CanSet {
			get {
				return canSet;
			}
		}
		
		public static PersistentProperty Resolve (IProperty source, ITypeResolver typeResolver)
		{
			PersistentProperty pro = new PersistentProperty ();
			pro.Name = source.Name;
			pro.Documentation = source.Documentation;
			pro.modifiers = source.Modifiers;
			pro.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			pro.canGet = source.CanGet;
			pro.canSet = source.CanSet;
			pro.region = source.Region;
			pro.bodyRegion = source.BodyRegion;
			pro.GetterRegion  = source.GetterRegion;
			pro.SetterRegion  = source.SetterRegion;
			pro.attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			pro.ExplicitDeclaration = PersistentReturnType.Resolve (source.ExplicitDeclaration, typeResolver);
			return pro;
		}
		
		public static PersistentProperty Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentProperty pro = new PersistentProperty ();
			pro.Name = PersistentHelper.ReadString (reader, nameTable);
			pro.Documentation = PersistentHelper.ReadString (reader, nameTable);
			uint m = reader.ReadUInt32();
			pro.modifiers = (ModifierEnum)(m & (canGetFlag - 1));
			pro.canGet = (m & canGetFlag) == canGetFlag;
			pro.canSet = (m & canSetFlag) == canSetFlag;
			pro.returnType = PersistentReturnType.Read (reader, nameTable);
			pro.ExplicitDeclaration = PersistentReturnType.Read (reader, nameTable);
			pro.region = PersistentRegion.Read (reader, nameTable);
			pro.bodyRegion = PersistentRegion.Read (reader, nameTable);
			pro.GetterRegion = PersistentRegion.Read (reader, nameTable);
			pro.SetterRegion = PersistentRegion.Read (reader, nameTable);
			pro.attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			return pro;
		}
		
		public static void WriteTo (IProperty p, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (p.Name, writer, nameTable);
			PersistentHelper.WriteString (p.Documentation, writer, nameTable);
			writer.Write((uint)p.Modifiers + (p.CanGet ? canGetFlag : 0) + (p.CanSet ? canSetFlag : 0));
			PersistentReturnType.WriteTo (p.ReturnType, writer, nameTable);
			PersistentReturnType.WriteTo (p.ExplicitDeclaration, writer, nameTable);
			PersistentRegion.WriteTo (p.Region, writer, nameTable);
			PersistentRegion.WriteTo (p.BodyRegion, writer, nameTable);
			PersistentRegion.WriteTo (p.GetterRegion, writer, nameTable);
			PersistentRegion.WriteTo (p.SetterRegion, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (p.Attributes, writer, nameTable);
		}
	}
}
