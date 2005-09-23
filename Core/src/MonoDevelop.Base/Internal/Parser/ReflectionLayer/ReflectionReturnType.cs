// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class ReflectionReturnType : AbstractReturnType
	{
		public ReflectionReturnType(Type type)
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
		void SetArrayDimensions(Type type)
		{
			if (type.IsArray && type != typeof(Array)) {
				SetArrayDimensions(type.GetElementType());
				arrays.Insert(0, type.GetArrayRank());
			}
		}
		
		void SetPointerNestingLevel(Type type)
		{
			if (type.IsPointer) {
				SetPointerNestingLevel(type.GetElementType());
				++pointerNestingLevel;
			}
		}
	}
}
