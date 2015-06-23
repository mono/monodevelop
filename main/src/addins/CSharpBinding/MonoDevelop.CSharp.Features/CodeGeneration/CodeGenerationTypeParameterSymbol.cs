//
// CodeGenerationTypeParameterSymbol.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ICSharpCode.NRefactory6.CSharp
{
	#if NR6
	public
	#endif
	class CodeGenerationTypeParameterSymbol
	{
		readonly static Type typeInfo;
		readonly object instance;

		internal object Instance {
			get {
				return instance;
			}
		}

		readonly static System.Reflection.PropertyInfo constraintTypesProperty;
		public ImmutableArray<ITypeSymbol> ConstraintTypes { 
			get {
				return (ImmutableArray<ITypeSymbol>)constraintTypesProperty.GetValue (instance);
			}
			internal set {
				constraintTypesProperty.SetValue (instance, value);
			}
		}


		static CodeGenerationTypeParameterSymbol ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationTypeParameterSymbol" + ReflectionNamespaces.WorkspacesAsmName, true);
			constraintTypesProperty = typeInfo.GetProperty ("ConstraintTypes");
		}

		public CodeGenerationTypeParameterSymbol(
			INamedTypeSymbol containingType,
			IList<AttributeData> attributes,
			VarianceKind varianceKind,
			string name,
			ImmutableArray<ITypeSymbol> constraintTypes,
			bool hasConstructorConstraint,
			bool hasReferenceConstraint,
			bool hasValueConstraint,
			int ordinal)
		{
			instance = Activator.CreateInstance (typeInfo, new object[] {
				containingType,
				attributes,
				varianceKind,
				name,
				constraintTypes,
				hasConstructorConstraint,
				hasReferenceConstraint,
				hasValueConstraint,
				ordinal
			});
		}



	}
}

