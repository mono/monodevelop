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
			
			return met;
		}
		
		public static DefaultMethod Read (BinaryReader reader, INameDecoder nameTable)
		{
			DefaultMethod met = new DefaultMethod ();
			met.Name = PersistentHelper.ReadString (reader, nameTable);
			met.Documentation = PersistentHelper.ReadString (reader, nameTable);
			
			met.Modifiers = (ModifierEnum)reader.ReadUInt32();
			met.ReturnType = PersistentReturnType.Read (reader, nameTable);
			
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
