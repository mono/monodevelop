//  ReflectionMethod.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Text;
using System.Collections;
using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MDGenericParameter = MonoDevelop.Projects.Parser.GenericParameter;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionMethod : DefaultMethod 
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
		XmlNode FindMatch (XmlNodeList nodes, MethodDefinition methodBase)
		{
			ParameterDefinitionCollection p = methodBase.Parameters;
			string s = "";
			foreach (XmlNode node in nodes) {
				XmlNodeList paramList = node.SelectNodes ("Parameters/*");
				s += paramList.Count + " - " + p.Count + "\n";
				if (p.Count == 0 && paramList.Count == 0) return node;
				if (p.Count != paramList.Count) continue;
				bool matched = true;
				for (int i = 0; i < p.Count; i++) {
					if (p[i].ParameterType.ToString () != paramList[i].Attributes["Type"].Value) {
						matched = false;
					}
				}
				if (matched)
					return node;
			}
			return null;
		}
		
		public ReflectionMethod (MethodDefinition methodBase, XmlDocument docs)
		{
			string name = methodBase.Name;
			if (methodBase.HasBody && methodBase.Body.Instructions != null && methodBase.Body.Instructions.Count > 0) {
				SequencePoint sp = methodBase.Body.Instructions[0].SequencePoint;
				if (sp != null) {
					Region = new DefaultRegion (sp.StartLine, sp.StartColumn);
					Region.FileName = sp.Document.Url;
				}
			}
			
			if (methodBase.IsConstructor) {
				name = ".ctor";
			}
			Name = name;
			
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
			
			modifiers = GetModifiers (methodBase.Attributes);
			
			foreach (ParameterDefinition paramInfo in methodBase.Parameters) {
				parameters.Add(new ReflectionParameter(paramInfo, node));
			}
			
			returnType = new ReflectionReturnType (methodBase.ReturnType.ReturnType);
			
			if (methodBase.GenericParameters != null && methodBase.GenericParameters.Count > 0) {
				GenericParameters = new GenericParameterList();
				foreach (Mono.Cecil.GenericParameter par in methodBase.GenericParameters) {
					// Fill out the type constraints for generic parameters 
					ReturnTypeList rtl = null;
					if (par.Constraints != null && par.Constraints.Count > 0) {
						rtl = new ReturnTypeList();
						foreach (Mono.Cecil.TypeReference typeRef in par.Constraints) {
							rtl.Add(new ReflectionReturnType(typeRef));
						}
					}
					// Add the parameter to the generic parameter list
					GenericParameters.Add(new MDGenericParameter(par.Name, rtl, (System.Reflection.GenericParameterAttributes)par.Attributes));
				}
			}
		}
		
		public static ModifierEnum GetModifiers (MethodAttributes attributes)
		{
			ModifierEnum modifiers = ModifierEnum.None;
			
			if ((attributes & MethodAttributes.Static) != 0)
				modifiers |= ModifierEnum.Static;
			if ((attributes & MethodAttributes.SpecialName) != 0)
				modifiers |= ModifierEnum.SpecialName;
			if ((attributes & MethodAttributes.Virtual) != 0)
				modifiers |= ModifierEnum.Virtual;
			if ((attributes & MethodAttributes.Abstract) != 0)
				modifiers |= ModifierEnum.Abstract;
			if ((attributes & MethodAttributes.Final) != 0)
				modifiers |= ModifierEnum.Sealed;
			
			MethodAttributes access = attributes & MethodAttributes.MemberAccessMask;
			
			if (access == MethodAttributes.Private) { // I assume that private is used most and public last (at least should be)
				modifiers |= ModifierEnum.Private;
			} else if (access == MethodAttributes.Family) {
				modifiers |= ModifierEnum.Protected;
			} else if (access == MethodAttributes.Public) {
				modifiers |= ModifierEnum.Public;
			} else if (access == MethodAttributes.Assem) {
				modifiers |= ModifierEnum.Internal;
			} else if (access == MethodAttributes.FamORAssem) {
				modifiers |= ModifierEnum.ProtectedOrInternal;
			} else if (access == MethodAttributes.FamANDAssem) {
				modifiers |= ModifierEnum.Protected | ModifierEnum.Internal;
			}
			
			return modifiers;
		}
	}
}
