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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Helper class for the GetAllBaseTypes() implementation.
	/// </summary>
	sealed class BaseTypeCollector : List<IType>
	{
		readonly ITypeResolveContext context;
		readonly Stack<ITypeDefinition> activeTypeDefinitions = new Stack<ITypeDefinition>();
		
		/// <summary>
		/// If this option is enabled, the list will not contain interfaces when retrieving the base types
		/// of a class.
		/// </summary>
		internal bool SkipImplementedInterfaces;
		
		public BaseTypeCollector(ITypeResolveContext context)
		{
			this.context = context;
		}
		
		public void CollectBaseTypes(IType type)
		{
			ITypeDefinition def = type.GetDefinition();
			if (def != null) {
				// Maintain a stack of currently active type definitions, and avoid having one definition
				// multiple times on that stack.
				// This is necessary to ensure the output is finite in the presence of cyclic inheritance:
				// class C<X> : C<C<X>> {} would not be caught by the 'no duplicate output' check, yet would
				// produce infinite output.
				if (activeTypeDefinitions.Contains(def))
					return;
				activeTypeDefinitions.Push(def);
			}
			// Avoid outputting a type more than once - necessary for "diamond" multiple inheritance
			// (e.g. C implements I1 and I2, and both interfaces derive from Object)
			if (!this.Contains(type)) {
				this.Add(type);
				foreach (IType baseType in type.GetBaseTypes(context)) {
					if (SkipImplementedInterfaces && def != null && def.Kind != TypeKind.Interface) {
						if (baseType.Kind == TypeKind.Interface) {
							// skip the interface
							continue;
						}
					}
					CollectBaseTypes(baseType);
				}
			}
			if (def != null)
				activeTypeDefinitions.Pop();
		}
	}
}
