//
// DomCecilParameter.cs
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
	public class DomCecilParameter : MonoDevelop.Projects.Dom.DomParameter
	{
		ParameterDefinition parameterDefinition;
		
		public ParameterDefinition ParameterDefinition {
			get {
				return parameterDefinition;
			}
		}
		
		public void CleanCecilDefinitions ()
		{
			parameterDefinition = null;
		}
		
		public DomCecilParameter (ParameterDefinition parameterDefinition)
		{
			this.parameterDefinition = parameterDefinition;
			base.Name                = parameterDefinition.Name;
//			base.modifiers           = DomCecilType.GetModifiers (parameterDefinition..Attributes);
			base.ReturnType          = DomCecilMethod.GetReturnType (parameterDefinition.ParameterType);
			
			if (parameterDefinition.ParameterType is Mono.Cecil.ReferenceType) {
				this.ParameterModifiers = (parameterDefinition.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out ? ParameterModifiers.Out : ParameterModifiers.Ref;
			} else {
				this.ParameterModifiers = ParameterModifiers.In;
			}
			if ((parameterDefinition.Attributes & ParameterAttributes.Optional) == ParameterAttributes.Optional) 
				this.ParameterModifiers |= ParameterModifiers.Optional;
			
			if ((parameterDefinition.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault) {
				this.DefaultValue = new System.CodeDom.CodePrimitiveExpression (parameterDefinition.Constant);
			}
			
			if (ReturnType.ArrayDimensions > 0) {
				foreach (CustomAttribute customAttribute in parameterDefinition.CustomAttributes) {
					if (customAttribute.Constructor.DeclaringType.FullName == "System.ParamArrayAttribute") 
						this.ParameterModifiers |= ParameterModifiers.Params;
				}
			}
		}
	}
}