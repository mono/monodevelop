// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Xml;

using MonoDevelop.Services;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.PE;
using SharpAssembly_=MonoDevelop.SharpAssembly.Assembly.SharpAssembly;
using AssemblyReader=MonoDevelop.SharpAssembly.Assembly.AssemblyReader;
using SharpCustomAttribute=MonoDevelop.SharpAssembly.Assembly.SharpCustomAttribute;

namespace MonoDevelop.Internal.Parser {
	
	[Serializable]
	public class SharpAssemblyProperty : AbstractProperty
	{
		public SharpAssemblyProperty(SharpAssembly_ asm, Property[] propertyTable, SharpAssemblyClass declaringtype, uint index)
		{
			if (asm == null) {
				throw new System.ArgumentNullException("asm");
			}
			if (propertyTable == null) {
				throw new System.ArgumentNullException("propertyTable");
			}
			if (declaringtype == null) {
				throw new System.ArgumentNullException("declaringType");
			}
			if (index > propertyTable.GetUpperBound(0) || index < 1) {
				throw new System.ArgumentOutOfRangeException("index", index, String.Format("must be between 1 and {0}!", propertyTable.GetUpperBound(0)));
			}
			
			AssemblyReader assembly = asm.Reader;
			declaringType = declaringtype;
			
			Property property = asm.Tables.Property[index];
			string name = assembly.GetStringFromHeap(property.Name);
			FullyQualifiedName = String.Concat(declaringType.FullyQualifiedName, ".", name);
			
			MethodSemantics[] sem = (MethodSemantics[])assembly.MetadataTable.Tables[MethodSemantics.TABLE_ID];
			Method[] method       = (Method[])assembly.MetadataTable.Tables[Method.TABLE_ID];
			
			uint getterMethodIndex = 0; // used later for parameters
			
			if (sem == null) goto nosem;
			
			for (int i = 1; i <= sem.GetUpperBound(0); ++i) {
				uint table = sem[i].Association & 1;
				uint ident = sem[i].Association >> 1;
				
				if (table == 1 && ident == index) {  // table: Property
					modifiers = ModifierEnum.None;
					Method methodDef = method[sem[i].Method];
			
					if (methodDef.IsFlagSet(Method.FLAG_STATIC)) {
						modifiers |= ModifierEnum.Static;
					}
					
					if (methodDef.IsFlagSet(Method.FLAG_ABSTRACT)) {
						modifiers |= ModifierEnum.Abstract;
					}
					
					if (methodDef.IsFlagSet(Method.FLAG_VIRTUAL)) {
						modifiers |= ModifierEnum.Virtual;
					}
					
					if (methodDef.IsFlagSet(Method.FLAG_FINAL)) {
						modifiers |= ModifierEnum.Final;
					}
					
					if (methodDef.IsMaskedFlagSet(Method.FLAG_PRIVATE, Method.FLAG_MEMBERACCESSMASK)) { // I assume that private is used most and public last (at least should be)
						modifiers |= ModifierEnum.Private;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMILY, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Protected;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_PUBLIC, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Public;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_ASSEM, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Internal;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMORASSEM, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.ProtectedOrInternal;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMANDASSEM, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Protected;
						modifiers |= ModifierEnum.Internal;
					}

					
					if ((sem[i].Semantics & MethodSemantics.SEM_GETTER) == MethodSemantics.SEM_GETTER) {
						getterRegion = new DefaultRegion(0, 0, 0, 0);
						getterMethod = new SharpAssemblyMethod(asm, method, declaringtype, sem[i].Method);
						getterMethodIndex = sem[i].Method;
					}
					
					if ((sem[i].Semantics & MethodSemantics.SEM_SETTER) == MethodSemantics.SEM_SETTER) {
						setterRegion = new DefaultRegion(0, 0, 0, 0);
						setterMethod = new SharpAssemblyMethod(asm, method, declaringtype, sem[i].Method);
					}
				}
				
			}
			
		nosem:
			
			// Attributes
			ArrayList attrib = asm.Attributes.Property[index] as ArrayList;
			if (attrib == null) goto noatt;
			
			AbstractAttributeSection sect = new AbstractAttributeSection();
			
			foreach(SharpCustomAttribute customattribute in attrib) {
				sect.Attributes.Add(new SharpAssemblyAttribute(asm, customattribute));
			}
			
			attributes.Add(sect);
		
		noatt:
			
			if ((property.Flags & Property.FLAG_SPECIALNAME) == Property.FLAG_SPECIALNAME) modifiers |= ModifierEnum.SpecialName;
			
			uint offset = property.Type;
			assembly.LoadBlob(ref offset);
			offset += 1; // skip calling convention
			int paramCount = assembly.LoadBlob(ref offset);
			
			returnType = new SharpAssemblyReturnType(asm, ref offset);
	
			IReturnType[] returnTypes = new IReturnType[paramCount];
			for (int i = 0; i < returnTypes.Length; ++i) {
				returnTypes[i] = new SharpAssemblyReturnType(asm, ref offset);
			}
			
			if (getterMethodIndex != 0) {
				AddParameters(asm, asm.Tables.Method, getterMethodIndex, returnTypes);
			} else {
				AddParameters(asm, returnTypes);
			}
		}
		
		void AddParameters(SharpAssembly_ asm, Method[] methodTable, uint index, IReturnType[] returnTypes)
		{
			Param[] paramTable = asm.Tables.Param;
			if (paramTable == null) return;
			
			uint paramIndexStart = methodTable[index].ParamList;
			
			// 0 means no parameters
			if (paramIndexStart > paramTable.GetUpperBound(0) || paramIndexStart == 0) {
				return;
			}
			
			uint paramIndexEnd   = (uint)paramTable.GetUpperBound(0);
			if (index < methodTable.GetUpperBound(0)) {
				paramIndexEnd = methodTable[index + 1].ParamList;
			}
			
			if (paramTable[paramIndexStart].Sequence == 0) paramIndexStart++;
			
			for (uint i = paramIndexStart; i < paramIndexEnd; ++i) {
				uint j = (i - paramIndexStart);
				parameters.Add(new SharpAssemblyParameter(asm, paramTable, i, j < returnTypes.Length ? returnTypes[j] : null));
			}
		}
		
		void AddParameters(SharpAssembly_ asm, IReturnType[] returnTypes)
		{
			for (uint i = 0; i < returnTypes.GetUpperBound(0); ++i) {
				parameters.Add(new SharpAssemblyParameter(asm, "param_" + i, returnTypes[i]));
			}
		}
		
		public override string ToString()
		{
			return FullyQualifiedName;
		}
	}
}
