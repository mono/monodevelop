// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class ReflectionMethod : AbstractMethod 
	{
/*		string GetParamList(MethodBase methodBase)
		{
			StringBuilder propertyName = new StringBuilder("(");
			ParameterInfo[] p = methodBase.GetParameters();
			if (p.Length == 0) {
				return String.Empty;
			}
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
		XmlNode FindMatch (XmlNodeList nodes, MethodBase methodBase)
		{
			ParameterInfo[] p = methodBase.GetParameters ();
			string s = "";
			foreach (XmlNode node in nodes) {
				XmlNodeList paramList = node.SelectNodes ("Parameters/*");
				s += paramList.Count + " - " + p.Length + "\n";
				if (p.Length == 0 && paramList.Count == 0) return node;
				if (p.Length != paramList.Count) continue;
				bool matched = true;
				for (int i = 0; i < p.Length; i++) {
					if (p[i].ParameterType.ToString () != paramList[i].Attributes["Type"].Value) {
						matched = false;
					}
				}
				if (matched)
					return node;
			}
			return null;
		}
		
		public ReflectionMethod(MethodBase methodBase, XmlDocument docs)
		{
			string name = methodBase.Name;
			
			if (methodBase is ConstructorInfo) {
				name = ".ctor";
			}
			FullyQualifiedName = String.Concat(methodBase.DeclaringType.FullName, ".", name);
			
			XmlNode node = null;

			if (docs != null) {
				XmlNodeList nodes = docs.SelectNodes ("/Type/Members/Member[@MemberName='" + name + "']");
				if (nodes != null && nodes.Count > 0) {
					if (nodes.Count == 1) {
						node = nodes[0];
					} else {
						node = FindMatch (nodes, methodBase);
					}
					if (node != null) {
						XmlNode docNode = node.SelectSingleNode ("Docs/summary");
						if (docNode != null) {
							Documentation = docNode.InnerXml;
						}
					}
				}
			}	
			
			modifiers = ModifierEnum.None;
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
			
			foreach (ParameterInfo paramInfo in methodBase.GetParameters()) {
				parameters.Add(new ReflectionParameter(paramInfo, node));
			}
			
			if (methodBase is MethodInfo) {
				returnType = new ReflectionReturnType(((MethodInfo)methodBase).ReturnType);
			} else {
				returnType = null;
			}
		}
	}
}
