//
// DomCecilType.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Cecil;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.AssemblyBrowser.Dom
{
	public class DomCecilType : MonoDevelop.Projects.Dom.DomType
	{
		TypeDefinition typeDefinition;
		
		static ClassType GetClassType (TypeDefinition typeDefinition)
		{
			if (typeDefinition.IsInterface)
				return ClassType.Interface;
			if (typeDefinition.IsEnum)
				return ClassType.Enum;
			if (typeDefinition.IsValueType)
				return ClassType.Struct;
			// Todo: Delegates
			return ClassType.Class;
		}
		
		public DomCecilType (TypeDefinition typeDefinition)
		{
			this.typeDefinition = typeDefinition;
			this.classType      = GetClassType (typeDefinition);
			this.name           = typeDefinition.Name;
			this.namesp         = typeDefinition.Namespace;
			this.modifiers      = GetModifiers (typeDefinition.Attributes);
			if (typeDefinition.BaseType != null)
				this.baseType = new DomCecilReturnType (typeDefinition.BaseType);
			
			foreach (TypeReference interfaceReference in typeDefinition.Interfaces) {
				this.implementedInterfaces.Add (new DomCecilReturnType (interfaceReference));
			}
			
			members = null;
		}
		
		public override IEnumerable<IMember> Members {
			get {
				if (base.members == null) {
					base.members = new List<IMember> ();
					foreach (FieldDefinition fieldDefinition in typeDefinition.Fields)
						base.members.Add (new DomCecilField (this, fieldDefinition));
					foreach (MethodDefinition methodDefinition in typeDefinition.Methods)
						base.members.Add (new DomCecilMethod (this, methodDefinition));
					foreach (PropertyDefinition propertyDefinition in typeDefinition.Properties)
						base.members.Add (new DomCecilProperty (this, propertyDefinition));
					foreach (EventDefinition eventDefinition in typeDefinition.Events)
						base.members.Add (new DomCecilEvent (this, eventDefinition));
				}
				return members;
			}
		}

		public TypeDefinition TypeDefinition {
			get {
				return typeDefinition;
			}
		}
				
		
		public static MonoDevelop.Projects.Dom.Modifiers GetModifiers (Mono.Cecil.TypeAttributes attr)
		{
			MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
			if ((attr & TypeAttributes.Abstract) == TypeAttributes.Abstract)
				result |= Modifiers.Abstract;
			if ((attr & TypeAttributes.Sealed) == TypeAttributes.Sealed)
				result |= Modifiers.Sealed;
			
			if ((attr & TypeAttributes.Public) == TypeAttributes.Public) {
				result |= Modifiers.Public;
			} else if ((attr & TypeAttributes.NestedFamANDAssem) == TypeAttributes.NestedFamANDAssem) {
				result |= Modifiers.ProtectedAndInternal;
			} else if ((attr & TypeAttributes.NestedFamORAssem) == TypeAttributes.NestedFamORAssem) {
				result |= Modifiers.ProtectedOrInternal;
			} else if ((attr & TypeAttributes.NestedFamily) == TypeAttributes.NestedFamily) {
				result |= Modifiers.Protected;
			} else if ((attr & TypeAttributes.NestedAssembly) == TypeAttributes.NestedAssembly) {
				result |= Modifiers.Internal;
			} else {
				result |= Modifiers.Private;
			}
			return result;
		}
		
		public static MonoDevelop.Projects.Dom.Modifiers GetModifiers (Mono.Cecil.FieldAttributes attr)
		{
			MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
			
			if ((attr & FieldAttributes.Literal) == FieldAttributes.Literal) 
				result |= Modifiers.Literal;
			if ((attr & FieldAttributes.Static) == FieldAttributes.Static) 
				result |= Modifiers.Static;
			if ((attr & FieldAttributes.SpecialName) == FieldAttributes.SpecialName) 
				result |= Modifiers.SpecialName;
			if ((attr & FieldAttributes.InitOnly) == FieldAttributes.InitOnly) 
				result |= Modifiers.Readonly;
			
			if ((attr & FieldAttributes.Public) == FieldAttributes.Public) {
				result |= Modifiers.Public;
			} else if ((attr & FieldAttributes.FamANDAssem) == FieldAttributes.FamANDAssem) {
				result |= Modifiers.ProtectedAndInternal;
			} else if ((attr & FieldAttributes.FamORAssem) == FieldAttributes.FamORAssem) {
				result |= Modifiers.ProtectedOrInternal;
			} else if ((attr & FieldAttributes.Family) == FieldAttributes.Family) {
				result |= Modifiers.Protected;
			} else if ((attr & FieldAttributes.Assembly) == FieldAttributes.Assembly) {
				result |= Modifiers.Internal;
			} else {
				result |= Modifiers.Private;
			}
			return result;
		}
		
		public static MonoDevelop.Projects.Dom.Modifiers GetModifiers (Mono.Cecil.EventAttributes attr)
		{
			MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
			if ((attr & EventAttributes.SpecialName) == EventAttributes.SpecialName) 
				result |= Modifiers.SpecialName;
			return result;
		}
		
		public static MonoDevelop.Projects.Dom.Modifiers GetModifiers (Mono.Cecil.MethodAttributes attr)
		{
			MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
			
			if ((attr & MethodAttributes.Static) == MethodAttributes.Static) 
				result |= Modifiers.Static;
			if ((attr & MethodAttributes.Abstract) == MethodAttributes.Abstract) 
				result |= Modifiers.Static;
			if ((attr & MethodAttributes.Virtual) == MethodAttributes.Virtual) 
				result |= Modifiers.Virtual;
			if ((attr & MethodAttributes.Final) == MethodAttributes.Final) 
				result |= Modifiers.Final;
			if ((attr & MethodAttributes.SpecialName) == MethodAttributes.SpecialName) 
				result |= Modifiers.SpecialName;
			
			if ((attr & MethodAttributes.Public) == MethodAttributes.Public) {
				result |= Modifiers.Public;
			} else if ((attr & MethodAttributes.FamANDAssem) == MethodAttributes.FamANDAssem) {
				result |= Modifiers.ProtectedAndInternal;
			} else if ((attr & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem) {
				result |= Modifiers.ProtectedOrInternal;
			} else if ((attr & MethodAttributes.Family) == MethodAttributes.Family) {
				result |= Modifiers.Protected;
			} else if ((attr & MethodAttributes.Assem) == MethodAttributes.Assem) {
				result |= Modifiers.Internal;
			} else {
				result |= Modifiers.Private;
			}
			
			return result;
		}
		
		public static MonoDevelop.Projects.Dom.Modifiers GetModifiers (Mono.Cecil.PropertyAttributes attr)
		{
			MonoDevelop.Projects.Dom.Modifiers result = MonoDevelop.Projects.Dom.Modifiers.None;
			if ((attr & PropertyAttributes.SpecialName) == PropertyAttributes.SpecialName) 
				result |= Modifiers.SpecialName;
			return result;
		}
	}
}
