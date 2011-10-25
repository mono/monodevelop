// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a type parameter that inherits its constraints from the overridden method in the base class.
	/// </summary>
	[Serializable]
	public sealed class TypeParameterWithInheritedConstraints : AbstractTypeParameter
	{
		readonly IMethod parentMethod;
		
		public TypeParameterWithInheritedConstraints(IMethod parentMethod, int index, string name)
			: base(EntityType.Method, index, name)
		{
			if (parentMethod == null)
				throw new ArgumentNullException("parentMethod");
			this.parentMethod = parentMethod;
		}
		
		ITypeParameter ResolveBaseTypeParameter(ITypeResolveContext context)
		{
			IMethod baseMethod = null;
			if (parentMethod.IsOverride) {
				foreach (IMethod m in InheritanceHelper.GetBaseMembers(parentMethod, context, false).OfType<IMethod>()) {
					if (!m.IsOverride) {
						baseMethod = m;
						break;
					}
				}
			} else if (parentMethod.InterfaceImplementations.Count == 1) {
				IType interfaceType = parentMethod.InterfaceImplementations[0].InterfaceType.Resolve(context);
				baseMethod = InheritanceHelper.GetMatchingMethodInBaseType(parentMethod, interfaceType, context);
			}
			if (baseMethod != null && this.Index < baseMethod.TypeParameters.Count)
				return baseMethod.TypeParameters[this.Index];
			else
				return null;
		}
		
		public override bool? IsReferenceType(ITypeResolveContext context)
		{
			ITypeParameter baseTp = ResolveBaseTypeParameter(context);
			if (baseTp == null)
				return null;
			bool? result = baseTp.IsReferenceType(context);
			if (result != null)
				return result;
			IType effectiveBaseClass = baseTp.GetEffectiveBaseClass(context);
			return IsReferenceTypeHelper(effectiveBaseClass);
		}
		
		public override IEnumerable<IType> GetEffectiveInterfaceSet(ITypeResolveContext context)
		{
			ITypeParameter baseTp = ResolveBaseTypeParameter(context);
			if (baseTp == null)
				return EmptyList<IType>.Instance;
			else
				return baseTp.GetEffectiveInterfaceSet(context);
		}
		
		public override IType GetEffectiveBaseClass(ITypeResolveContext context)
		{
			ITypeParameter baseTp = ResolveBaseTypeParameter(context);
			if (baseTp == null)
				return SharedTypes.UnknownType;
			else
				return baseTp.GetEffectiveBaseClass(context);
		}
		
		public override ITypeParameterConstraints GetConstraints(ITypeResolveContext context)
		{
			ITypeParameter baseTp = ResolveBaseTypeParameter(context);
			if (baseTp == null)
				return DefaultTypeParameterConstraints.Empty;
			else
				return baseTp.GetConstraints(context);
		}
	}
}
