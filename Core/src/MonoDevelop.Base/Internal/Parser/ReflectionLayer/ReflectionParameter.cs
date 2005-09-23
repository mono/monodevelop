// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class ReflectionParameter : AbstractParameter
	{
		public ReflectionParameter(ParameterInfo parameterInfo, XmlNode methodNode)
		{
			name       = parameterInfo.Name;
			returnType = new ReflectionReturnType(parameterInfo.ParameterType);
			
			if (parameterInfo.IsOut) {
				modifier |= ParameterModifier.Out;
			}
			
			Type type = parameterInfo.ParameterType;
			if (type.IsArray && type != typeof(Array) && Attribute.IsDefined(parameterInfo, typeof(ParamArrayAttribute), true)) {
				modifier |= ParameterModifier.Params;
			}
			
			// seems there is no other way to determine a ref parameter
			if (type.Name.EndsWith("&")) {
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
