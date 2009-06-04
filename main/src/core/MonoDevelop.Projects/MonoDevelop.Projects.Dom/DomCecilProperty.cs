//
// DomCecilProperty.cs
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
	public class DomCecilProperty : MonoDevelop.Projects.Dom.DomProperty
	{
		PropertyDefinition propertyDefinition;
		
		public PropertyDefinition PropertyDefinition {
			get {
				return propertyDefinition;
			}
		}
		
		public void CleanCecilDefinitions ()
		{
			propertyDefinition = null;
		}

		public const string GetMethodPrefix = "get_";
		public const string SetMethodPrefix = "set_";
		
		public IMethod GetMethod {
			get {
				return LookupSpecialMethod (GetMethodPrefix);
			}
		}
		
		public IMethod SetMethod {
			get {
				return LookupSpecialMethod (SetMethodPrefix);
			}
		}
		public override bool HasGet {
			get {
				return GetMethod != null;
			}
		}
		
		public override bool HasSet {
			get {
				return SetMethod != null;
			}
		}
		
		public DomCecilProperty (MonoDevelop.Projects.Dom.IType declaringType, bool keepDefinitions, PropertyDefinition propertyDefinition)
		{
			this.declaringType      = declaringType;
			if (keepDefinitions)
				this.propertyDefinition = propertyDefinition;
			base.name = propertyDefinition.Name;
			if (propertyDefinition.Parameters.Count > 0) {
				this.PropertyModifier |= PropertyModifier.IsIndexer;
				foreach (ParameterDefinition paramDef in propertyDefinition.Parameters) {
					Add (new DomCecilParameter (paramDef));
				}
			}
			if (propertyDefinition.GetMethod != null) 
				this.PropertyModifier |= PropertyModifier.HasGet;
			if (propertyDefinition.SetMethod != null) 
				this.PropertyModifier |= PropertyModifier.HasSet;
			
			base.Modifiers          = DomCecilType.GetModifiers ((propertyDefinition.GetMethod != null ? propertyDefinition.GetMethod : propertyDefinition.SetMethod));
			if (!propertyDefinition.IsSpecialName)
				base.Modifiers &= ~MonoDevelop.Projects.Dom.Modifiers.SpecialName;
			base.returnType         = DomCecilMethod.GetReturnType (propertyDefinition.PropertyType);
			DomCecilMethod.AddAttributes (this, propertyDefinition.CustomAttributes);
		}
	}
}
