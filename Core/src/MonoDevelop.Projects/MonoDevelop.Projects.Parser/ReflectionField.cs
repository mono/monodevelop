//  ReflectionField.cs
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
using System.Collections;
using System.Diagnostics;
using System.Xml;
using Mono.Cecil;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal class ReflectionField : DefaultField
	{
		public ReflectionField (FieldDefinition fieldInfo, XmlDocument docs)
		{
			System.Diagnostics.Debug.Assert(fieldInfo != null);
			Name = fieldInfo.Name;

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
