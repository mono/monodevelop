// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class ReflectionProperty : AbstractProperty 
	{
		public ReflectionProperty(PropertyInfo propertyInfo, XmlDocument docs)
		{
			FullyQualifiedName = String.Concat(propertyInfo.DeclaringType.FullName, ".", propertyInfo.Name);
			
			// show the abstract layer that we have getter & setters
			if (propertyInfo.CanRead) {
				getterRegion = new DefaultRegion(0, 0, 0, 0);
			} else {
				getterRegion = null;
			}
			
			if (propertyInfo.CanWrite) {
				setterRegion = new DefaultRegion(0, 0, 0, 0);
			} else {
				setterRegion = null;
			}

			if (docs != null) {
				XmlNode node = docs.SelectSingleNode ("/Type/Members/Member[@MemberName='" + propertyInfo.Name + "']/Docs/summary");
				if (node != null) {
					Documentation = node.InnerXml;
				}
			}

			returnType = new ReflectionReturnType(propertyInfo.PropertyType);
			
			MethodInfo methodBase = null;
			try {
				methodBase = propertyInfo.GetGetMethod(true);
			} catch (Exception) {}
			
			if (methodBase == null) {
				try {
					methodBase = propertyInfo.GetSetMethod(true);
				} catch (Exception) {}
			}
			
			if (methodBase != null) {
				if (methodBase.IsStatic) {
					modifiers |= ModifierEnum.Static;
				}
				
				if (methodBase.IsAssembly) {
					modifiers |= ModifierEnum.Internal;
				}
				
				if (methodBase.IsPrivate) { // I assume that private is used most and public last (at least should be)
					modifiers |= ModifierEnum.Private;
				} else if (methodBase.IsFamily) {
					modifiers |= ModifierEnum.Protected;
				} else if (methodBase.IsPublic) {
					modifiers |= ModifierEnum.Public;
				} else if (methodBase.IsFamilyOrAssembly) {
					modifiers |= ModifierEnum.ProtectedOrInternal;
				} else if (methodBase.IsFamilyAndAssembly) {
					modifiers |= ModifierEnum.Protected;
					modifiers |= ModifierEnum.Internal;
				}
				
				if (methodBase.IsVirtual) {
					modifiers |= ModifierEnum.Virtual;
				}
				if (methodBase.IsAbstract) {
					modifiers |= ModifierEnum.Abstract;
				}
				
			} else { // assume public property, if no methodBase could be get.
				modifiers = ModifierEnum.Public;
			}
			
		}
	}
}
