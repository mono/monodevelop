// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Xml;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionProperty : DefaultProperty 
	{
		public ReflectionProperty (PropertyDefinition propertyInfo, XmlDocument docs)
		{
			Name = propertyInfo.Name;
			
			// show the abstract layer that we have getter & setters
			if (propertyInfo.GetMethod != null) {
				getterRegion = new DefaultRegion(0, 0, 0, 0);
			} else {
				getterRegion = null;
			}
			
			if (propertyInfo.SetMethod != null) {
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
			
			MethodDefinition methodBase = null;
			try {
				methodBase = propertyInfo.GetMethod;
			} catch (Exception) {}
			
			if (methodBase == null) {
				try {
					methodBase = propertyInfo.SetMethod;
				} catch (Exception) {}
			}
			
			if (methodBase != null) {
				modifiers |= ReflectionMethod.GetModifiers (methodBase.Attributes);
				
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
