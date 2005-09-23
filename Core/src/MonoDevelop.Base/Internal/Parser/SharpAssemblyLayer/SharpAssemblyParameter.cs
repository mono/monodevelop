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
	public class SharpAssemblyParameter : AbstractParameter
	{
		public SharpAssemblyParameter(SharpAssembly_ asm, Param[] paramTable, uint index, IReturnType type)
		{
			if (asm == null) {
				throw new System.ArgumentNullException("asm");
			}
			if (paramTable == null) {
				throw new System.ArgumentNullException("paramTable");
			}
			if (index > paramTable.GetUpperBound(0) || index < 1) {
				throw new System.ArgumentOutOfRangeException("index", index, String.Format("must be between 1 and {0}!", paramTable.GetUpperBound(0)));
			}
			AssemblyReader assembly = asm.Reader;
			
			Param param = asm.Tables.Param[index];
			
			name = assembly.GetStringFromHeap(param.Name);
			if (param.IsFlagSet(Param.FLAG_OUT)) {
				modifier |= ParameterModifier.Out;
			}
			
			
			// Attributes
			ArrayList attrib = asm.Attributes.Param[index] as ArrayList;
			if (attrib == null) goto noatt;
			
			foreach(SharpCustomAttribute customattribute in attrib) {
				SharpAssemblyAttribute newatt = new SharpAssemblyAttribute(asm, customattribute);
				if (newatt.Name == "System.ParamArrayAttribute") modifier |= ParameterModifier.Params;
				attributeCollection.Add(newatt);
			}
		
		noatt:
			
			if (type == null) {
				returnType = new SharpAssemblyReturnType("PARAMETER_UNKNOWN");
			} else {
				if (type.Name.EndsWith("&")) {
					modifier |= ParameterModifier.Ref;
				}
				returnType = type;
			}
			
		}
		
		public SharpAssemblyParameter(SharpAssembly_ asm, string paramName, IReturnType type)
		{
			name = paramName;
			if (type.Name.EndsWith("&")) {
				modifier |= ParameterModifier.Ref;
			}
			returnType = type;
		}
		
		public override string ToString()
		{
			return "Parameter : " + returnType.FullyQualifiedName;
		}
	}
}
