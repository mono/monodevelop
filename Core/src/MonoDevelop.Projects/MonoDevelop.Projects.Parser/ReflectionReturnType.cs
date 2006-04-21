// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionReturnType : AbstractReturnType
	{
		public ReflectionReturnType(TypeReference type)
		{
			string fullyQualifiedName = type.FullName.Replace("+", ".").Trim('&');
			
			while (fullyQualifiedName.EndsWith("[") ||
			       fullyQualifiedName.EndsWith("]") ||
			       fullyQualifiedName.EndsWith(",") ||
			       fullyQualifiedName.EndsWith("*")) {
				fullyQualifiedName = fullyQualifiedName.Substring(0, fullyQualifiedName.Length - 1);
			}
			base.FullyQualifiedName = fullyQualifiedName;
			
			SetPointerNestingLevel(type);
			SetArrayDimensions(type);
			arrayDimensions = (int[])arrays.ToArray(typeof(int));
		}
		
		ArrayList arrays = new ArrayList();
		void SetArrayDimensions(TypeReference type)
		{
			if (type is ArrayType) {
				ArrayType at = (ArrayType) type;
				SetArrayDimensions (at.ElementType);
				arrays.Insert(0, at.Rank);
			}
		}
		
		void SetPointerNestingLevel(TypeReference type)
		{
			if (type is PointerType) {
				PointerType pt = (PointerType) type;
				SetPointerNestingLevel (pt.ElementType);
				++pointerNestingLevel;
			}
		}
	}
}
