//
// Scaffolder.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class DbSetModelVisitor : SymbolVisitor
	{
		const string DbSetTypeName = "System.Data.Entity.DbSet`1";
		const string EF7DbSetTypeName = "Microsoft.Data.Entity.DbSet`1";
		const string EFCDbSetTypeName = "Microsoft.EntityFrameworkCore.DbSet`1";

		public static List<ITypeSymbol> FindModelTypes (IAssemblySymbol assembly)
		{
			var visitor = new DbSetModelVisitor ();
			visitor.Visit (assembly);
			return visitor._types;
		}

		private readonly List<ITypeSymbol> _types;

		private DbSetModelVisitor ()
		{
			_types = new List<ITypeSymbol> ();
		}

		public override void VisitAssembly (IAssemblySymbol symbol)
		{
			Visit (symbol.GlobalNamespace);
		}

		public override void VisitNamespace (INamespaceSymbol symbol)
		{
			foreach (var type in symbol.GetTypeMembers ()) {
				Visit (type);
			}

			foreach (var @namespace in symbol.GetNamespaceMembers ()) {
				Visit (@namespace);
			}
		}

		public override void VisitNamedType (INamedTypeSymbol symbol)
		{
			foreach (var property in symbol.GetMembers ()) {
				Visit (property);
			}
		}

		public override void VisitProperty (IPropertySymbol symbol)
		{
			if (symbol.Type is INamedTypeSymbol namedTypeSymbol) {

				if (namedTypeSymbol.IsGenericType) {
					// for DbSet<MyModel>, return MyModel
					var unboundType = namedTypeSymbol.ConstructUnboundGenericType ();
					// TODO: check FQN
					if (unboundType.MetadataName == "DbSet`1") {//   DbSetTypeName || unboundName == EF7DbSetTypeName || unboundName == EFCDbSetTypeName) {
						_types.Add (namedTypeSymbol.TypeArguments.First ());
					}
				}
			}
		}
	}
}
