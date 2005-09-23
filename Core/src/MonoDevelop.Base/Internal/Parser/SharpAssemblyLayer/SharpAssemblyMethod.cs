// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Xml;

using MonoDevelop.Services;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.PE;
using SharpAssembly_=MonoDevelop.SharpAssembly.Assembly.SharpAssembly;
using SharpCustomAttribute=MonoDevelop.SharpAssembly.Assembly.SharpCustomAttribute;
using AssemblyReader=MonoDevelop.SharpAssembly.Assembly.AssemblyReader;

namespace MonoDevelop.Internal.Parser {
	
	[Serializable]
	public class SharpAssemblyMethod : AbstractMethod
	{
		public SharpAssemblyMethod(SharpAssembly_ asm, Method[] methodTable, SharpAssemblyClass declaringtype, uint index)
		{
			if (asm == null) {
				throw new System.ArgumentNullException("asm");
			}
			if (methodTable == null) {
				throw new System.ArgumentNullException("methodTable");
			}
			if (declaringtype == null) {
				throw new System.ArgumentNullException("declaringType");
			}
			if (index > methodTable.GetUpperBound(0) || index < 1) {
				throw new System.ArgumentOutOfRangeException("index", index, String.Format("must be between 1 and {0}!", methodTable.GetUpperBound(0)));
			}
			AssemblyReader assembly = asm.Reader;
			
			declaringType = declaringtype;
			
			Method methodDef = methodTable[index];
			string name = assembly.GetStringFromHeap(methodDef.Name);
			
			FullyQualifiedName = String.Concat(declaringType.FullyQualifiedName, ".", name);
			
			// Attributes
			ArrayList attrib = asm.Attributes.Method[index] as ArrayList;
			if (attrib == null) goto noatt;
			
			AbstractAttributeSection sect = new AbstractAttributeSection();
			
			foreach(SharpCustomAttribute customattribute in attrib) {
				sect.Attributes.Add(new SharpAssemblyAttribute(asm, customattribute));
			}
			
			attributes.Add(sect);
		
		noatt:
			
			modifiers = ModifierEnum.None;
			
			if (methodDef.IsFlagSet(Method.FLAG_STATIC)) {
				modifiers |= ModifierEnum.Static;
			}
			
			if (methodDef.IsMaskedFlagSet(Method.FLAG_PRIVATE, Method.FLAG_MEMBERACCESSMASK)) {
				modifiers |= ModifierEnum.Private;
			} else if (methodDef.IsMaskedFlagSet(Method.FLAG_PUBLIC, Method.FLAG_MEMBERACCESSMASK)) {
				modifiers |= ModifierEnum.Public;
			} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMILY, Method.FLAG_MEMBERACCESSMASK)) {
				modifiers |= ModifierEnum.Protected;
			} else if (methodDef.IsMaskedFlagSet(Method.FLAG_ASSEM, Method.FLAG_MEMBERACCESSMASK)) {
				modifiers |= ModifierEnum.Internal;
			} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMORASSEM, Method.FLAG_MEMBERACCESSMASK)) {
				modifiers |= ModifierEnum.ProtectedOrInternal;
			} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMANDASSEM, Method.FLAG_MEMBERACCESSMASK)) {
				modifiers |= ModifierEnum.Protected;
				modifiers |= ModifierEnum.Internal;
			}
			
			if (methodDef.IsFlagSet(Method.FLAG_VIRTUAL)) {
				modifiers |= ModifierEnum.Virtual;
			}
			
			if (methodDef.IsFlagSet(Method.FLAG_FINAL)) {
				modifiers |= ModifierEnum.Final;
			}
			
			if (methodDef.IsFlagSet(Method.FLAG_ABSTRACT)) {
				modifiers |= ModifierEnum.Abstract;
			}
			
			if (methodDef.IsFlagSet(Method.FLAG_SPECIALNAME)) {
				modifiers |= ModifierEnum.SpecialName;
			}
			
			uint offset = methodDef.Signature;
			assembly.LoadBlob(ref offset);
			offset += 1;  // skip calling convention
			int numReturnTypes = assembly.LoadBlob(ref offset);
					
			returnType = new SharpAssemblyReturnType(asm, ref offset);
			
			IReturnType[] returnTypes = new IReturnType[numReturnTypes];
			for (int i = 0; i < returnTypes.Length; ++i) {
				returnTypes[i] = new SharpAssemblyReturnType(asm, ref offset);
			}
			
			AddParameters(asm, methodTable, index, returnTypes);
		}
		
		void AddParameters(SharpAssembly_ asm, Method[] methodDefTable, uint index, IReturnType[] returnTypes)
		{
			Param[] paramTable = asm.Tables.Param;
			if (paramTable == null) return;
			
			uint paramIndexStart = methodDefTable[index].ParamList;
			
			// 0 means no parameters
			if (paramIndexStart > paramTable.GetUpperBound(0) || paramIndexStart == 0) {
				return;
			}
			
			uint paramIndexEnd   = (uint)paramTable.GetUpperBound(0);
			if (index < methodDefTable.GetUpperBound(0)) {
				paramIndexEnd = methodDefTable[index + 1].ParamList;
			}
			
			if (paramTable[paramIndexStart].Sequence == 0) paramIndexStart++;
			
			for (uint i = paramIndexStart; i < paramIndexEnd; ++i) {
				uint j = (i - paramIndexStart);
				parameters.Add(new SharpAssemblyParameter(asm, paramTable, i, j < returnTypes.Length ? returnTypes[j] : null));
			}
		}
		
		public override bool IsConstructor {
			get {
				return FullyQualifiedName.IndexOf("..") != -1;
			}
		}
		
		public override string ToString()
		{
			return FullyQualifiedName;
		}
	}
}

