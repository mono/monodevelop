// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;

using MonoDevelop.Services;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.PE;
using SharpAssembly_=MonoDevelop.SharpAssembly.Assembly.SharpAssembly;
using SharpCustomAttribute=MonoDevelop.SharpAssembly.Assembly.SharpCustomAttribute;

namespace MonoDevelop.Internal.Parser {
	
	[Serializable]
	public class SharpAssemblyField : AbstractField
	{
		public SharpAssemblyField(SharpAssembly_ assembly, Field[] fieldTable, SharpAssemblyClass declaringtype, uint index)
		{
			if (assembly == null) {
				throw new System.ArgumentNullException("assembly");
			}
			if (fieldTable == null) {
				throw new System.ArgumentNullException("fieldTable");
			}
			if (declaringtype == null) {
				throw new System.ArgumentNullException("declaringtype");
			}
			if (index > fieldTable.GetUpperBound(0) || index < 0) {
				throw new System.ArgumentOutOfRangeException("index", index, String.Format("must be between 1 and {0}!", fieldTable.GetUpperBound(0)));
			}
			
			declaringType = declaringtype;
			
			Field field = fieldTable[index];
			string name = assembly.Reader.GetStringFromHeap(field.Name);
			FullyQualifiedName = String.Concat(declaringType.FullyQualifiedName, ".", name);
			
			// Attributes
			ArrayList attrib = assembly.Attributes.Field[index] as ArrayList;
			if (attrib == null) goto noatt;
			
			AbstractAttributeSection sect = new AbstractAttributeSection();
			
			foreach(SharpCustomAttribute customattribute in attrib) {
				sect.Attributes.Add(new SharpAssemblyAttribute(assembly, customattribute));
			}
			
			attributes.Add(sect);
		
		noatt:
			
			if (field.IsFlagSet(Field.FLAG_INITONLY)) {
				modifiers |= ModifierEnum.Readonly;
			}
			
			if (field.IsFlagSet(Field.FLAG_STATIC)) {
				modifiers |= ModifierEnum.Static;
			}
						
			if (field.IsMaskedFlagSet(Field.FLAG_PRIVATE, Field.FLAG_FIELDACCESSMASK)) { // I assume that private is used most and public last (at least should be)
				modifiers |= ModifierEnum.Private;
			} else if (field.IsMaskedFlagSet(Field.FLAG_FAMILY, Field.FLAG_FIELDACCESSMASK)) {
				modifiers |= ModifierEnum.Protected;
			} else if (field.IsMaskedFlagSet(Field.FLAG_PUBLIC, Field.FLAG_FIELDACCESSMASK)) {
				modifiers |= ModifierEnum.Public;
			} else if (field.IsMaskedFlagSet(Field.FLAG_ASSEMBLY, Field.FLAG_FIELDACCESSMASK)) {
				modifiers |= ModifierEnum.Internal;
			} else if (field.IsMaskedFlagSet(Field.FLAG_FAMORASSEM, Field.FLAG_FIELDACCESSMASK)) {
				modifiers |= ModifierEnum.ProtectedOrInternal;
			} else if (field.IsMaskedFlagSet(Field.FLAG_FAMANDASSEM, Field.FLAG_FIELDACCESSMASK)) {
				modifiers |= ModifierEnum.Protected;
				modifiers |= ModifierEnum.Internal;
			}
			
			if (field.IsFlagSet(Field.FLAG_LITERAL)) {
				modifiers |= ModifierEnum.Const;
			}
			
			if (field.IsFlagSet(Field.FLAG_SPECIALNAME)) {
				modifiers |= ModifierEnum.SpecialName;
			}
			
			// field return type
			uint sigOffset = field.Signature;
			assembly.Reader.LoadBlob(ref sigOffset);
			sigOffset++;  // skip field id
			returnType = new SharpAssemblyReturnType(assembly, ref sigOffset);
			
			// field constant value -- for enums
			Constant cst = (Constant)assembly.FieldConstantTable[index];
			if (declaringtype.ClassType == ClassType.Enum && cst != null) {
				try {
					DataType dt = (DataType)cst.Type;
					
					byte[] blob = assembly.Reader.GetBlobFromHeap(cst.Val);
					BinaryReader binReader = new BinaryReader(new MemoryStream(blob));
					
					switch (dt) {
						case DataType.Byte:
							initialValue = binReader.ReadByte();
							break;
						case DataType.Int16:
							initialValue = binReader.ReadInt16();
							break;
						case DataType.Int32:
							initialValue = binReader.ReadInt32();
							break;
						case DataType.Int64:
							initialValue = binReader.ReadInt64();
							break;
						case DataType.SByte:
							initialValue = binReader.ReadSByte();
							break;
						case DataType.UInt16:
							initialValue = binReader.ReadUInt16();
							break;
						case DataType.UInt32:
							initialValue = binReader.ReadUInt32();
							break;
						case DataType.UInt64:
							initialValue = binReader.ReadUInt64();
							break;
						default: // not supported
							break;
					}
					binReader.Close();
				} catch {
					Runtime.LoggingService.Info("SharpAssemblyField: Error reading constant value");
				}
			}
		}
		
		public override string ToString()
		{
			return FullyQualifiedName;
		}
		
		object initialValue;
		
		public object InitialValue {
			get {
				return initialValue;
			}
		}
	}
}
