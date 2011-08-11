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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Reference to a qualified type or namespace name.
	/// </summary>
	public sealed class MemberTypeOrNamespaceReference : ITypeOrNamespaceReference
	{
		readonly ITypeOrNamespaceReference target;
		readonly ITypeDefinition parentTypeDefinition;
		readonly UsingScope parentUsingScope;
		readonly string identifier;
		readonly IList<ITypeReference> typeArguments;
		
		public MemberTypeOrNamespaceReference(ITypeOrNamespaceReference target, string identifier, IList<ITypeReference> typeArguments, ITypeDefinition parentTypeDefinition, UsingScope parentUsingScope)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.target = target;
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
			this.parentTypeDefinition = parentTypeDefinition;
			this.parentUsingScope = parentUsingScope;
		}
		
		/// <summary>
		/// Adds a suffix to the identifier.
		/// Does not modify the existing type reference, but returns a new one.
		/// </summary>
		public MemberTypeOrNamespaceReference AddSuffix(string suffix)
		{
			return new MemberTypeOrNamespaceReference(target, identifier + suffix, typeArguments, parentTypeDefinition, parentUsingScope);
		}
		
		public ResolveResult DoResolve(ITypeResolveContext context)
		{
			ResolveResult targetRR = target.DoResolve(context);
			if (targetRR.IsError)
				return targetRR;
			CSharpResolver r = new CSharpResolver(context);
			r.CurrentTypeDefinition = parentTypeDefinition;
			r.UsingScope = parentUsingScope;
			IType[] typeArgs = new IType[typeArguments.Count];
			for (int i = 0; i < typeArgs.Length; i++) {
				typeArgs[i] = typeArguments[i].Resolve(context);
			}
			return r.ResolveMemberType(targetRR, identifier, typeArgs);
		}
		
		public NamespaceResolveResult ResolveNamespace(ITypeResolveContext context)
		{
			// TODO: use resolve context for original project, if possible
			return DoResolve(context) as NamespaceResolveResult;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			// TODO: use resolve context for original project, if possible; then map the result type into the new context
			TypeResolveResult rr = DoResolve(context) as TypeResolveResult;
			return rr != null ? rr.Type : SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (typeArguments.Count == 0)
				return target.ToString() + "." + identifier;
			else
				return target.ToString() + "." + identifier + "<" + DotNet35Compat.StringJoin(",", typeArguments) + ">";
		}
	}
}
