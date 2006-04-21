// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionField : AbstractField
	{
		public ReflectionField(FieldDefinition fieldInfo, XmlDocument docs)
		{
			System.Diagnostics.Debug.Assert(fieldInfo != null);
			FullyQualifiedName = String.Concat(fieldInfo.DeclaringType.FullName, ".", fieldInfo.Name);

			if (docs != null) {
				XmlNode node = docs.SelectSingleNode ("/Type/Members/Member[@MemberName='" + fieldInfo.Name + "']/Docs/summary");
				if (node != null) {
					Documentation = node.InnerXml;
				}
			}
			
			if ((fieldInfo.Attributes & FieldAttributes.InitOnly) != 0) {
				modifiers |= ModifierEnum.Readonly;
			}
			
			if (fieldInfo.IsStatic) {
				modifiers |= ModifierEnum.Static;
			}
			
			modifiers |= GetModifiers (fieldInfo.Attributes);
			
			if (fieldInfo.IsLiteral) {
				modifiers |= ModifierEnum.Const;
			}
			
			returnType = new ReflectionReturnType (fieldInfo.FieldType);
		}
		
		public static ModifierEnum GetModifiers (FieldAttributes attributes)
		{
			FieldAttributes access = attributes & FieldAttributes.FieldAccessMask;
			
			if (access == FieldAttributes.Private) { // I assume that private is used most and public last (at least should be)
				return ModifierEnum.Private;
			} else if (access == FieldAttributes.Family) {
				return ModifierEnum.Protected;
			} else if (access == FieldAttributes.Public) {
				return ModifierEnum.Public;
			} else if (access == FieldAttributes.Assembly) {
				return ModifierEnum.Internal;
			} else if (access == FieldAttributes.FamORAssem) {
				return ModifierEnum.ProtectedOrInternal;
			} else if (access == FieldAttributes.FamANDAssem) {
				return ModifierEnum.Protected | ModifierEnum.Internal;
			}
			return ModifierEnum.None;
		}
	}
}
