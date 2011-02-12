//
// DomCecilAttribute.cs
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
using System.CodeDom;

using Mono.Cecil;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom
{
	public class DomCecilAttribute : MonoDevelop.Projects.Dom.DomAttribute
	{
		/*CustomAttribute customAttribute;
		
		public CustomAttribute CustomAttribute {
			get {
				return customAttribute;
			}
		}*/
		
		public DomCecilAttribute (CustomAttribute customAttribute)
		{
			//this.customAttribute = customAttribute;
			base.AttributeType = DomCecilMethod.GetReturnType (customAttribute.Constructor);
			base.Name          = customAttribute.Constructor.DeclaringType.FullName;
			
			try {
				foreach (var argument in customAttribute.ConstructorArguments)
					AddPositionalArgument (CreateExpressionFor (argument));
			} catch (Exception e) {
				LoggingService.LogError ("Error reading attributes", e);
			}
			
			try {
				foreach (var namedArgument in customAttribute.Properties)
					AddNamedArgument (namedArgument.Name, CreateExpressionFor (namedArgument.Argument));
			} catch (Exception e) {
				LoggingService.LogError ("Error reading attributes", e);
			}
			
			try {
				foreach (var namedArgument in customAttribute.Fields)
					AddNamedArgument (namedArgument.Name, CreateExpressionFor (namedArgument.Argument));
			} catch (Exception e) {
				LoggingService.LogError ("Error reading attributes", e);
			}
		}

		static CodeExpression CreateExpressionFor (CustomAttributeArgument argument)
		{
			if (IsSystemType (argument.Type))
				return new CodeTypeOfExpression (GetCodeTypeReference ((TypeReference) argument.Value));

			return new CodePrimitiveExpression (argument.Value);
		}

		static bool IsSystemType (TypeReference type)
		{
			return type.FullName == "System.Type";
		}

		static CodeTypeReference GetCodeTypeReference (TypeReference type)
		{
			var array = type as ArrayType;
			if (array != null)
				return GetArrayCodeTypeReference (array);

			var instance = type as GenericInstanceType;
			if (instance != null)
				return GetGenericInstanceCodeTypeReference (instance);

			if (type.DeclaringType != null)
				return new CodeTypeReference (type.FullName.Replace ('/', '+'));

			return new CodeTypeReference (type.FullName);
		}

		static CodeTypeReference GetArrayCodeTypeReference (ArrayType array)
		{
			return new CodeTypeReference (GetCodeTypeReference (array.ElementType), array.Rank);
		}

		static CodeTypeReference GetGenericInstanceCodeTypeReference (GenericInstanceType instance)
		{
			var reference = GetCodeTypeReference (instance.ElementType);

			foreach (var argument in instance.GenericArguments)
				reference.TypeArguments.Add (GetCodeTypeReference (argument));

			return reference;
		}
	}
}
