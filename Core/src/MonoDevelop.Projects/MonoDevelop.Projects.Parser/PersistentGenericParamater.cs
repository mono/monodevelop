//
// PersistentGenericParamater.cs: A utility class that handles serialization of
//   generic parameters.
//
// Author:
//   Matej Urbas (matej.urbas@gmail.com)
//
// (C) 2006 Matej Urbas
// 

using System;
using System.IO;
using System.Reflection;

namespace MonoDevelop.Projects.Parser
{
	internal sealed class PersistentGenericParamater
	{
		private PersistentGenericParamater()
		{
		}
		
		public static GenericParameter Resolve(GenericParameter gp,
		                                       ITypeResolver typeResolver)
		{
			GenericParameter tmp = new GenericParameter();
			tmp.Name = gp.Name;
			tmp.SpecialConstraints = gp.SpecialConstraints;
			if (gp.BaseTypes != null && gp.BaseTypes.Count > 0) {
				tmp.BaseTypes = new ReturnTypeList();
				foreach (IReturnType rt in gp.BaseTypes) {
					tmp.BaseTypes.Add(PersistentReturnType.Resolve(rt, typeResolver));
				}
			}
			
			return tmp;
		}
		
		public static GenericParameter Read (BinaryReader reader, INameDecoder nameTable)
		{
			GenericParameter gp = new GenericParameter();
			gp.Name = PersistentHelper.ReadString(reader, nameTable);
			gp.SpecialConstraints = (GenericParameterAttributes) reader.ReadInt16();
			uint count = reader.ReadUInt32();
			if (count > 0) {
				gp.BaseTypes = new ReturnTypeList();
				for (uint j = 0; j < count; ++j) {
					gp.BaseTypes.Add(PersistentReturnType.Read(reader, nameTable));
				}
			}
			return gp;
		}

		public static void WriteTo (GenericParameter gp, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString(gp.Name, writer, nameTable);
			writer.Write((short)gp.SpecialConstraints);
			if (gp.BaseTypes == null)
				writer.Write((uint)0);
			else {
				writer.Write((uint)gp.BaseTypes.Count);
				foreach (IReturnType rt in gp.BaseTypes) {
					PersistentReturnType.WriteTo(rt, writer, nameTable);
				}
			}
		}
	}
}
