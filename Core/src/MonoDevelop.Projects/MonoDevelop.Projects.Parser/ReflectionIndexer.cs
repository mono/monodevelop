// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Text;
using System.Xml;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionIndexer : DefaultIndexer
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
		public ReflectionIndexer (PropertyDefinition propertyInfo, XmlDocument docs)
		{
			// indexers does have the same name as the object that declare the indexers
			Name = propertyInfo.DeclaringType.Name;
			
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
			} else { // assume public property, if no methodBase could be get.
				modifiers = ModifierEnum.Public;
			}
			
			ParameterDefinitionCollection p = propertyInfo.Parameters;
			foreach (ParameterDefinition parameterInfo in p) {
				parameters.Add(new ReflectionParameter(parameterInfo, node));
			}
		}
	}
}
