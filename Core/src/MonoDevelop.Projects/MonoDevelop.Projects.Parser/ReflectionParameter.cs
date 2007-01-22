// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Xml;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionParameter : DefaultParameter
	{
		public ReflectionParameter (ParameterDefinition parameterInfo, XmlNode methodNode)
		{
			name       = parameterInfo.Name;
			returnType = new ReflectionReturnType(parameterInfo.ParameterType);
			
			TypeReference type = parameterInfo.ParameterType;
			if (type is ArrayType && type.FullName != "System.Array") {
				foreach (CustomAttribute att in parameterInfo.CustomAttributes)
					if (att.Constructor.DeclaringType.FullName == "System.ParamArrayAttribute") {
						modifier |= ParameterModifier.Params;
						break;
					}
			}
			
			if ((parameterInfo.Attributes & ParameterAttributes.Out) != 0) {
				modifier |= ParameterModifier.Out;
			} else if (returnType.ByRef) {
				// FIX: We should look at the return type of this parameter to
				// determine whether a parameter is 'ref'
				modifier |= ParameterModifier.Ref;
			}
			
			if (methodNode != null) {
				XmlNode paramDocu = methodNode.SelectSingleNode("Docs/param[@name='" + parameterInfo.Name + "']");
				if (paramDocu != null) {
					documentation = paramDocu.InnerXml;
				}
			}
		}
	}
}
