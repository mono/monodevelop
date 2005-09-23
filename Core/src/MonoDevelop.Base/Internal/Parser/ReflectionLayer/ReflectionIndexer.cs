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

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class ReflectionIndexer : AbstractIndexer
	{
/*		string GetIndexerName(PropertyInfo propertyInfo)
		{
			StringBuilder propertyName = new StringBuilder("Item(");
			ParameterInfo[] p = propertyInfo.GetIndexParameters();
			for (int i = 0; i < p.Length; ++i) {
				propertyName.Append(p[i].ParameterType.FullName);
				if (i + 1 < p.Length) {
					propertyName.Append(',');
				}
			}
			propertyName.Append(')');
			return propertyName.ToString();
		}
*/
		public ReflectionIndexer(PropertyInfo propertyInfo, XmlDocument docs)
		{
			// indexers does have the same name as the object that declare the indexers
			FullyQualifiedName = propertyInfo.DeclaringType.FullName;
			
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

			XmlNode node = null;
			if (docs != null) {
				node = docs.SelectSingleNode ("/Type/Members/Member[@MemberName='" + propertyInfo.Name + "']");
				if (node != null) {
					XmlNode docNode = node.SelectSingleNode ("Docs/summary");
					if (docNode != null) {
						Documentation = node.InnerXml;
					}
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
			} else { // assume public property, if no methodBase could be get.
				modifiers = ModifierEnum.Public;
			}
			
			ParameterInfo[] p = propertyInfo.GetIndexParameters();
			foreach (ParameterInfo parameterInfo in p) {
				parameters.Add(new ReflectionParameter(parameterInfo, node));
			}
		}
	}
}
