//  ReflectionParameter.cs
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
