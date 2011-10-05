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
	/// Represents a simple C# name. (a single non-qualified identifier with an optional list of type arguments)
	/// </summary>
	public sealed class SimpleTypeOrNamespaceReference : ITypeOrNamespaceReference
	{
		readonly ITypeDefinition parentTypeDefinition;
		readonly UsingScope parentUsingScope;
		readonly string identifier;
		readonly IList<ITypeReference> typeArguments;
		readonly SimpleNameLookupMode lookupMode;
		
		public SimpleTypeOrNamespaceReference(string identifier, IList<ITypeReference> typeArguments, ITypeDefinition parentTypeDefinition, UsingScope parentUsingScope, SimpleNameLookupMode lookupMode = SimpleNameLookupMode.Type)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
			this.parentTypeDefinition = parentTypeDefinition;
			this.parentUsingScope = parentUsingScope;
			this.lookupMode = lookupMode;
		}
		
		/// <summary>
		/// Adds a suffix to the identifier.
		/// Does not modify the existing type reference, but returns a new one.
		/// </summary>
		public SimpleTypeOrNamespaceReference AddSuffix(string suffix)
		{
			return new SimpleTypeOrNamespaceReference(identifier + suffix, typeArguments, parentTypeDefinition, parentUsingScope, lookupMode);
		}
		
		public ResolveResult DoResolve(ITypeResolveContext context)
		{
			CSharpResolver r = new CSharpResolver(context);
			r.CurrentTypeDefinition = parentTypeDefinition;
			r.UsingScope = parentUsingScope;
			IType[] typeArgs = new IType[typeArguments.Count];
			for (int i = 0; i < typeArgs.Length; i++) {
				typeArgs[i] = typeArguments[i].Resolve(context);
			}
			return r.LookupSimpleNameOrTypeName(identifier, typeArgs, lookupMode);
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
				return identifier;
			else
				return identifier + "<" + DotNet35Compat.StringJoin(",", typeArguments) + ">";
		}
	}
}
