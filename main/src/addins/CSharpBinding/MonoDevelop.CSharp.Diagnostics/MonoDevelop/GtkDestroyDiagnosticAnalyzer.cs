//
// GtkDestroyDiagnosticAnalyzer.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.CSharp.Diagnostics
{
	[DiagnosticAnalyzer (LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class GtkDestroyDiagnosticAnalyzer : DiagnosticAnalyzer
	{	
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			IDEDiagnosticIds.GtkDestroyDiagnosticId,
			"Do not override Gtk.Object.Destroy",
			"Override OnDestroyed rather than Destroy - the latter will not run from unmanaged destruction",
			DiagnosticCategory.Style,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		const string gtkObjectTypeName = "Gtk.Object";
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction (compilationContext => {
				// Limit search to compilations which reference the specific localizers.
				var compilation = compilationContext.Compilation;
				var gtktype = compilation.GetTypeByMetadataName (gtkObjectTypeName);
				if (gtktype == null)
					return;

				context.RegisterSymbolAction (operationContext => {
					if (!(operationContext.Symbol is INamedTypeSymbol symbol))
						return;

					if (symbol.Name == "Widget" && symbol.ContainingNamespace.Name == "Gtk")
						return;

					if (!IsGtkObjectDerived (symbol, gtktype))
						return;

					var members = symbol.GetMembers ("Destroy");
					foreach (var member in members) {
						if (!member.IsOverride)
							continue;

						var loc = member.Locations.FirstOrDefault (x => x.IsInSource);
						if (loc != null)
							operationContext.ReportDiagnostic (Diagnostic.Create (descriptor, loc));
					}


				}, SymbolKind.NamedType);
			});
		}

		static bool IsGtkObjectDerived (INamedTypeSymbol symbol, INamedTypeSymbol gtkType)
		{
			var type = symbol;
			while (type != null) {
				if (type == gtkType)
					return true;
				type = type.BaseType;
			}
			return false;
		}
	}
}
