//  PersistentMethod.cs
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
	internal sealed class PersistentMethod
	{
		public static DefaultMethod Resolve (IMethod source, ITypeResolver typeResolver)
		{
			DefaultMethod met = new DefaultMethod ();
			met.Name = source.Name;
			met.Documentation = source.Documentation;
			met.Modifiers = source.Modifiers;
			met.ReturnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			
			foreach (IParameter p in source.Parameters)
				met.Parameters.Add (PersistentParameter.Resolve (p, (IMember) met, typeResolver));

			met.Region = source.Region;
			met.BodyRegion = source.BodyRegion;
			met.Attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			
			if (source.GenericParameters != null && source.GenericParameters.Count > 0) {
				met.GenericParameters = new GenericParameterList();
				foreach (GenericParameter gp in source.GenericParameters) {
					met.GenericParameters.Add(PersistentGenericParamater.Resolve(gp, typeResolver));
				}
			}
			met.ExplicitDeclaration = PersistentReturnType.Resolve (source.ExplicitDeclaration, typeResolver);
			
			return met;
		}
		
		public static DefaultMethod Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultMethod met = new DefaultMethod ();
			met.Name = PersistentHelper.ReadString (reader, nameTable);
			met.Documentation = PersistentHelper.ReadString (reader, nameTable);
			
			met.Modifiers = (ModifierEnum)reader.ReadUInt32();
			met.ReturnType = PersistentReturnType.Read (reader, nameTable);
			met.ExplicitDeclaration = PersistentReturnType.Read (reader, nameTable);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				met.Parameters.Add (PersistentParameter.Read (reader, (IMember) met, nameTable));
			}
			met.Region = PersistentRegion.Read (reader, nameTable);
			met.BodyRegion = PersistentRegion.Read (reader, nameTable);
			met.Attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			
			// Read the generic parameters
			count = reader.ReadUInt32();
			if (count > 0) {
				met.GenericParameters = new GenericParameterList();
				// Add the generic parameters one by one
				for (uint i = 0; i < count; ++i) {
					met.GenericParameters.Add(PersistentGenericParamater.Read(reader, nameTable));
				}
				// All the generic parameters have been added...
			}
			return met;
		}
		
		public static void WriteTo (IMethod met, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (met.Name, writer, nameTable);
			PersistentHelper.WriteString (met.Documentation, writer, nameTable);
			
			writer.Write ((uint)met.Modifiers);
			PersistentReturnType.WriteTo (met.ReturnType, writer, nameTable);
			PersistentReturnType.WriteTo (met.ExplicitDeclaration, writer, nameTable);
			
			writer.Write (met.Parameters != null ? (uint)met.Parameters.Count : (uint)0);
			foreach (IParameter p in met.Parameters) {
				PersistentParameter.WriteTo (p, writer, nameTable);
			}
			PersistentRegion.WriteTo (met.Region, writer, nameTable);
			PersistentRegion.WriteTo (met.BodyRegion, writer, nameTable);
			PersistentAttributeSectionCollection.WriteTo (met.Attributes, writer, nameTable);
			
			// Write the generic parameters to the database file
			if (met.GenericParameters == null || met.GenericParameters.Count < 1)
				writer.Write((uint)0);
			else {
				writer.Write((uint)met.GenericParameters.Count);
				foreach (GenericParameter gp in met.GenericParameters) {
					PersistentGenericParamater.WriteTo(gp, writer, nameTable);
				}
			}
		}
	}
}
