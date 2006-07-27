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
	internal sealed class PersistentMethod : AbstractMethod
	{
		public static PersistentMethod Resolve (IMethod source, ITypeResolver typeResolver)
		{
			PersistentMethod met = new PersistentMethod ();
			met.FullyQualifiedName = source.FullyQualifiedName;
			met.Documentation = source.Documentation;
			met.modifiers = source.Modifiers;
			met.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			
			foreach (IParameter p in source.Parameters)
				met.parameters.Add (PersistentParameter.Resolve (p, typeResolver));

			met.region = source.Region;
			met.bodyRegion = source.BodyRegion;
			met.attributes = PersistentAttributeSectionCollection.Resolve (source.Attributes, typeResolver);
			
			if (source.GenericParameters != null && source.GenericParameters.Count > 0) {
				met.GenericParameters = new GenericParameterList();
				foreach (GenericParameter gp in source.GenericParameters) {
					met.GenericParameters.Add(PersistentGenericParamater.Resolve(gp, typeResolver));
				}
			}
			
			return met;
		}
		
		public static PersistentMethod Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentMethod met = new PersistentMethod ();
			met.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			met.Documentation = PersistentHelper.ReadString (reader, nameTable);
			
			met.modifiers = (ModifierEnum)reader.ReadUInt32();
			met.returnType = PersistentReturnType.Read (reader, nameTable);
			
			uint count = reader.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				met.parameters.Add (PersistentParameter.Read (reader, nameTable));
			}
			met.region = PersistentRegion.Read (reader, nameTable);
			met.bodyRegion = PersistentRegion.Read (reader, nameTable);
			met.attributes = PersistentAttributeSectionCollection.Read (reader, nameTable);
			
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
			PersistentHelper.WriteString (met.FullyQualifiedName, writer, nameTable);
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
