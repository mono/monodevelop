//
// DomCecilMethod.cs
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
using Mono.Cecil;

namespace MonoDevelop.Projects.Dom
{
	public class DomCecilMethod : MonoDevelop.Projects.Dom.DomMethod
	{
		MethodDefinition methodDefinition;
		
		public MethodDefinition MethodDefinition {
			get {
				return methodDefinition;
			}
		}
		
		public void CleanCecilDefinitions ()
		{
			methodDefinition = null;
			foreach (DomCecilParameter parameter in base.parameters) {
				parameter.CleanCecilDefinitions ();
			}
		}
		
		public static IReturnType GetReturnType (TypeReference typeReference)
		{
			if (typeReference == null)
				return DomReturnType.Void;
			if (typeReference.GenericParameters.Count > 0) {
				DomReturnType newType = new DomReturnType (typeReference.FullName);
				foreach (GenericParameter gp in typeReference.GenericParameters)
					newType.AddTypeParameter (GetReturnType (gp));
				return newType;
			}
			else
				return DomReturnType.GetSharedReturnType (DomCecilType.RemoveGenericParamSuffix (typeReference.FullName)); 
		}
		
		public static IReturnType GetReturnType (MethodReference methodReference)
		{
			if (methodReference == null)
				return DomReturnType.Void;
			return DomReturnType.GetSharedReturnType (DomCecilType.RemoveGenericParamSuffix (methodReference.DeclaringType.FullName));
		}
		
		public static void AddAttributes (AbstractMember member, CustomAttributeCollection attributes)
		{
			foreach (CustomAttribute customAttribute in attributes) {
				member.Add (new DomCecilAttribute (customAttribute));
			}
		}
				
		public DomCecilMethod (MonoDevelop.Projects.Dom.IType declaringType, bool keepDefinitions, MethodDefinition methodDefinition)
		{
			this.declaringType    = declaringType;
			if (keepDefinitions)
				this.methodDefinition = methodDefinition;
			
			if (methodDefinition.Name == ".ctor") {
				Name = declaringType.Name;
				methodModifier |= MethodModifier.IsConstructor;
			} else
				Name = methodDefinition.Name;
			AddAttributes (this, methodDefinition.CustomAttributes);
			base.modifiers  = DomCecilType.GetModifiers (methodDefinition.Attributes);
			base.returnType = DomCecilMethod.GetReturnType (methodDefinition.ReturnType.ReturnType);
			foreach (ParameterDefinition paramDef in methodDefinition.Parameters) {
				Add (new DomCecilParameter (paramDef));
			}

			if (this.IsStatic) {
				foreach (IAttribute attr in this.Attributes) {
					System.Console.WriteLine(attr.Name);
					if (attr.Name == "System.Runtime.CompilerServices.ExtensionAttribute") {
						System.Console.WriteLine("FOUND EXTENSION METHOD !!!!");
						methodModifier |= MethodModifier.IsExtension;
						break;
					}
				}
			}

		}
	}
}
