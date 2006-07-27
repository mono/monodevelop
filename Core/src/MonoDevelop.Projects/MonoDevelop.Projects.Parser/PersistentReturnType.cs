// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal sealed class PersistentReturnType : AbstractReturnType
	{
		public static PersistentReturnType Resolve (IReturnType source, ITypeResolver typeResolver)
		{
			if (source == null) return null;
			
			PersistentReturnType rt = new PersistentReturnType ();
			rt.FullyQualifiedName = typeResolver.Resolve (source.FullyQualifiedName);
			rt.byRef = source.ByRef;
			rt.pointerNestingLevel = source.PointerNestingLevel;
			rt.arrayDimensions = source.ArrayDimensions;
			
			if (rt.GenericArguments != null && rt.GenericArguments.Count > 0) {
				rt.GenericArguments = new ReturnTypeList();
				foreach (IReturnType ga in rt.GenericArguments) {
					rt.GenericArguments.Add(PersistentReturnType.Resolve(ga, typeResolver));
				}
			}
			
			return rt;
		}

		public static PersistentReturnType Read (BinaryReader reader, INameDecoder nameTable)
		{
			if (PersistentHelper.ReadNull (reader)) return null;
			
			PersistentReturnType rt = new PersistentReturnType ();
			rt.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			
			rt.byRef = reader.ReadBoolean();

			rt.pointerNestingLevel = reader.ReadInt32();

			uint count = reader.ReadUInt32();
			rt.arrayDimensions = new int[count];
			for (uint i = 0; i < rt.arrayDimensions.Length; ++i) {
				rt.arrayDimensions[i] = reader.ReadInt32();
			}
			
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
			
			return rt;
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
