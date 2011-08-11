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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type Reference used when the fully qualified type name is known.
	/// </summary>
	public sealed class GetClassTypeReference : ITypeReference, ISupportsInterning
	{
		string nameSpace, name;
		int typeParameterCount;
		//volatile CachedResult v_cachedResult;
		
		public GetClassTypeReference(string nameSpace, string name, int typeParameterCount)
		{
			if (nameSpace == null)
				throw new ArgumentNullException("nameSpace");
			if (name == null)
				throw new ArgumentNullException("name");
			this.nameSpace = nameSpace;
			this.name = name;
			this.typeParameterCount = typeParameterCount;
		}
		
		public GetClassTypeReference(string fullTypeName, int typeParameterCount)
		{
			if (fullTypeName == null)
				throw new ArgumentNullException("fullTypeName");
			int pos = fullTypeName.LastIndexOf('.');
			if (pos < 0) {
				nameSpace = string.Empty;
				name = fullTypeName;
			} else {
				nameSpace = fullTypeName.Substring(0, pos);
				name = fullTypeName.Substring(pos + 1);
			}
			this.typeParameterCount = typeParameterCount;
		}
		
		public string Namespace { get { return nameSpace; } }
		public string Name { get { return name; } }
		public int TypeParameterCount { get { return typeParameterCount; } }
		
		/*
		sealed class CachedResult
		{
			public readonly CacheManager CacheManager;
			public readonly IType Result;
			
			public CachedResult(CacheManager cacheManager, IType result)
			{
				this.CacheManager = cacheManager;
				this.Result = result;
			}
		}
		 */
		
		public IType Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
			/*  TODO PERF: caching disabled until we measure how much of an advantage it is
			 * (and whether other approaches like caching only the last N resolve calls in a thread-static cache would work better)
			 * Maybe even make a distinction between the really common type references (e.g. primitiveTypeReferences) and
			 * normal GetClassTypeReferences?
			CacheManager cacheManager = context.CacheManager;
			if (cacheManager != null) {
				CachedResult result = this.v_cachedResult;
				if (result != null && result.CacheManager == cacheManager)
					return result.Result;
				IType newResult = DoResolve(context);
				this.v_cachedResult = new CachedResult(cacheManager, newResult);
				cacheManager.Disposed += delegate { v_cachedResult = null; }; // maybe optimize this to use interface call instead of delegate?
				return newResult;
			} else {
				return DoResolve(context);
			}
			
		}
		
		IType DoResolve(ITypeResolveContext context)
		{
			 */
			return context.GetTypeDefinition(nameSpace, name, typeParameterCount, StringComparer.Ordinal) ?? SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (typeParameterCount == 0)
				return NamespaceDeclaration.BuildQualifiedName(nameSpace, name);
			else
				return NamespaceDeclaration.BuildQualifiedName(nameSpace, name) + "`" + typeParameterCount;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			nameSpace = provider.Intern(nameSpace);
			name = provider.Intern(name);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return nameSpace.GetHashCode() ^ name.GetHashCode() ^ typeParameterCount;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			GetClassTypeReference o = other as GetClassTypeReference;
			return o != null && name == o.name && nameSpace == o.nameSpace && typeParameterCount == o.typeParameterCount;
		}
	}
}
