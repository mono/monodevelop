// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.IO;
using System.Reflection;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public sealed class PersistentProperty : AbstractProperty
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
			pro.FullyQualifiedName = source.FullyQualifiedName;
			pro.Documentation = source.Documentation;
			pro.modifiers = source.Modifiers;
			pro.returnType = PersistentReturnType.Resolve (source.ReturnType, typeResolver);
			pro.canGet = source.CanGet;
			pro.canSet = source.CanSet;
			pro.region = source.Region;
			return pro;
		}
		
		public static PersistentProperty Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentProperty pro = new PersistentProperty ();
			pro.FullyQualifiedName = PersistentHelper.ReadString (reader, nameTable);
			pro.Documentation = PersistentHelper.ReadString (reader, nameTable);
			uint m = reader.ReadUInt32();
			pro.modifiers = (ModifierEnum)(m & (canGetFlag - 1));
			pro.canGet = (m & canGetFlag) == canGetFlag;
			pro.canSet = (m & canSetFlag) == canSetFlag;
			pro.returnType = PersistentReturnType.Read (reader, nameTable);
			pro.region = PersistentRegion.Read (reader, nameTable);
			return pro;
		}
		
		public static void WriteTo (IProperty p, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (p.FullyQualifiedName, writer, nameTable);
			PersistentHelper.WriteString (p.Documentation, writer, nameTable);
			writer.Write((uint)p.Modifiers + (p.CanGet ? canGetFlag : 0) + (p.CanSet ? canSetFlag : 0));
			PersistentReturnType.WriteTo (p.ReturnType, writer, nameTable);
			PersistentRegion.WriteTo (p.Region, writer, nameTable);
		}
	}
}
