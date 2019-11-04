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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	/// <summary>
	/// Return any type that doesn't have one of the filtered base classes, or one of the filtered assembly
	/// public keys
	/// </summary>
	class ModelVisitor : SymbolVisitor
	{
		static readonly string [] _filteredBaseClasses = new string [] {
			"System.Web.WebPages.WebPageExecutingBase",     // Base class for Razor views
			"System.Web.UI.TemplateControl",                // Base class for ASPX views
			"System.Web.HttpApplication",                   // Base class for Global.asax
			"System.Web.Mvc.Controller",                    // Base class for MVC controllers
			"System.Web.Http.ApiController"                 // Base class for API controllers
		};

		static readonly string [] _filteredPublicKeys = new string [] {
			"b77a5c561934e089",     // CLR types (mscorlib, System.dll, System.Core.dll, etc.)
			"b03f5f7f11d50a3a",     // System.Web and friends
			"31bf3856ad364e35",     // System.ComponentModel.DataAnnotations and friends
			"89845dcd8080cc91",     // System.Data.SqlServerCE
			"30ad4fe6b2a6aeed",     // Newtonsoft.Json
			"2780ccd10d57b246",     // DotNetOpenAuth
		};

		static readonly Dictionary<AssemblyIdentity, bool> _assemblyKeyFilteredCache = new Dictionary<AssemblyIdentity, bool> ();

		public static List<ITypeSymbol> FindModelTypes (IAssemblySymbol assembly)
		{
			var visitor = new ModelVisitor ();
			visitor.Visit (assembly);
			return visitor._types;
		}

		readonly List<ITypeSymbol> _types;

		ModelVisitor ()
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
			foreach (var type in symbol.GetTypeMembers ()) {
				Visit (type);
			}

			if (IncludeTypeInAddViewModelClassDropdown (symbol))
				_types.Add (symbol);
		}

		public static bool IncludeTypeInAddViewModelClassDropdown (INamedTypeSymbol symbol)
		{
			return symbol.DeclaredAccessibility == Accessibility.Public
				   && !symbol.IsStatic
				   && !symbol.IsGenericType
				   && !symbol.IsImplicitClass
				   && !symbol.IsAnonymousType
				   && !symbol.GetAttributes ().OfType<CompilerGeneratedAttribute> ().Any ()
				   && !IsSignedWithFilteredPublicKey (symbol)
				   && !IsDerivedFromFilteredBaseClass (symbol);
		}

		static bool IsDerivedFromFilteredBaseClass (INamedTypeSymbol t)
		{
			if (_filteredBaseClasses.Any (baseClass => t.GetFullMetadataName ().Equals (baseClass, StringComparison.Ordinal))) {
				return true;
			}
			if (t.BaseType != null) {
				return IsDerivedFromFilteredBaseClass (t.BaseType);
			}
			return false;
		}

		static bool IsSignedWithFilteredPublicKey (INamedTypeSymbol t)
		{
			var assembly = t.ContainingAssembly;
			if (!_assemblyKeyFilteredCache.TryGetValue (assembly.Identity, out var isFilteredKey)) {
				string publicKeyToken = BitConverter.ToString (assembly.Identity.PublicKeyToken.ToArray ()).Replace ("-", string.Empty);
				isFilteredKey = _filteredPublicKeys.Any (key => publicKeyToken.Equals (key, StringComparison.OrdinalIgnoreCase));
				_assemblyKeyFilteredCache [assembly.Identity] = isFilteredKey;
			}
			return isFilteredKey;
		}
	}
}
