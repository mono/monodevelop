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

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Provides helper methods for inheritance.
	/// </summary>
	public class InheritanceHelper
	{
		// TODO: maybe these should be extension methods?
		
		#region GetBaseMember
		/// <summary>
		/// Gets the base member that has the same signature.
		/// </summary>
		public static IMember GetBaseMember(IMember member, ITypeResolveContext context)
		{
			return GetBaseMembers(member, context, false).FirstOrDefault();
		}
		
		/// <summary>
		/// Gets all base members that have the same signature.
		/// </summary>
		/// <returns>
		/// List of base members with the same signature. The member from the derived-most base class is returned first.
		/// </returns>
		public static IEnumerable<IMember> GetBaseMembers(IMember member, ITypeResolveContext context, bool includeImplementedInterfaces)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			if (context == null)
				throw new ArgumentNullException("context");
			member = member.MemberDefinition;
			IMethod method = member as IMethod;
			IProperty property = member as IProperty;
			IEvent ev = member as IEvent;
			IField field = member as IField;
			using (var ctx = context.Synchronize()) {
				IEnumerable<IType> allBaseTypes;
				if (includeImplementedInterfaces) {
					allBaseTypes = member.DeclaringTypeDefinition.GetAllBaseTypes(ctx);
				} else {
					allBaseTypes = member.DeclaringTypeDefinition.GetNonInterfaceBaseTypes(ctx);
				}
				foreach (IType baseType in allBaseTypes.Reverse()) {
					if (baseType == member.DeclaringTypeDefinition)
						continue;
					
					if (method != null) {
						IMethod baseMethod = GetMatchingMethodInBaseType(method, baseType, ctx);
						if (baseMethod != null)
							yield return baseMethod;
					}
					if (property != null) {
						foreach (IProperty baseProperty in baseType.GetProperties(
							ctx, p => p.Name == property.Name && p.Parameters.Count == property.Parameters.Count,
							GetMemberOptions.IgnoreInheritedMembers))
						{
							if (ParameterListComparer.Compare(ctx, property, baseProperty))
								yield return baseProperty;
						}
					}
					if (ev != null) {
						IEvent baseEvent = baseType.GetEvents(ctx, e => e.Name == ev.Name, GetMemberOptions.IgnoreInheritedMembers).FirstOrDefault();
						if (baseEvent != null)
							yield return baseEvent;
					}
					// Fields can't be overridden, but we handle them anyways, just for consistence
					if (field != null) {
						IField baseField = baseType.GetFields(ctx, f => f.Name == field.Name, GetMemberOptions.IgnoreInheritedMembers).FirstOrDefault();
						if (baseField != null)
							yield return baseField;
					}
				}
			}
		}
		
		internal static IMethod GetMatchingMethodInBaseType(IMethod method, IType baseType, ITypeResolveContext context)
		{
			foreach (IMethod baseMethod in baseType.GetMethods(
				context, m => m.Name == method.Name && m.Parameters.Count == method.Parameters.Count && m.TypeParameters.Count == method.TypeParameters.Count,
				GetMemberOptions.IgnoreInheritedMembers))
			{
				if (ParameterListComparer.Compare(context, method, baseMethod))
					return baseMethod;
			}
			return null;
		}
		#endregion
		
		#region GetDerivedMember
		/// <summary>
		/// Finds the member declared in 'derivedType' that has the same signature (could override) 'baseMember'.
		/// </summary>
		public static IMember GetDerivedMember(IMember baseMember, ITypeDefinition derivedType, ITypeResolveContext context)
		{
			if (baseMember == null)
				throw new ArgumentNullException("baseMember");
			if (derivedType == null)
				throw new ArgumentNullException("derivedType");
			if (context == null)
				throw new ArgumentNullException("context");
			baseMember = baseMember.MemberDefinition;
			bool includeInterfaces = baseMember.DeclaringTypeDefinition.Kind == TypeKind.Interface;
			IMethod method = baseMember as IMethod;
			if (method != null) {
				using (var ctx = context.Synchronize()) {
					foreach (IMethod derivedMethod in derivedType.Methods) {
						if (derivedMethod.Name == method.Name && derivedMethod.Parameters.Count == method.Parameters.Count) {
							if (derivedMethod.TypeParameters.Count == method.TypeParameters.Count) {
								// The method could override the base method:
								if (GetBaseMembers(derivedMethod, ctx, includeInterfaces).Any(m => m.MemberDefinition == baseMember))
									return derivedMethod;
							}
						}
					}
				}
			}
			IProperty property = baseMember as IProperty;
			if (property != null) {
				using (var ctx = context.Synchronize()) {
					foreach (IProperty derivedProperty in derivedType.Properties) {
						if (derivedProperty.Name == property.Name && derivedProperty.Parameters.Count == property.Parameters.Count) {
							// The property could override the base property:
							if (GetBaseMembers(derivedProperty, ctx, includeInterfaces).Any(m => m.MemberDefinition == baseMember))
								return derivedProperty;
						}
					}
				}
			}
			if (baseMember is IEvent) {
				foreach (IEvent derivedEvent in derivedType.Events) {
					if (derivedEvent.Name == baseMember.Name)
						return derivedEvent;
				}
			}
			if (baseMember is IField) {
				foreach (IField derivedField in derivedType.Fields) {
					if (derivedField.Name == baseMember.Name)
						return derivedField;
				}
			}
			return null;
		}
		#endregion
	}
}
