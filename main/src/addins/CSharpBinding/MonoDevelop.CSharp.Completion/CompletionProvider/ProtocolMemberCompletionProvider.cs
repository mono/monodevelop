//
// ProtocolMemberCompletionProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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

/*
 * 
 * //
// ProtocolMemberContextHandler.cs
//
// Author:
//       mkrueger <>
//
// Copyright (c) 2017 ${CopyrightHolder}
//
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Linq;
using Mono.Addins.Description;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.CSharp.Completion;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.IPhone.Editor
{
	[ExportCompletionProvider ("ProtocolMemberCompletionProvider", LanguageNames.CSharp)]
	class ProtocolMemberCompletionProvider : CommonCompletionProvider
	{/*
		RoslynCodeCompletionFactory factory;

		void IExtensionContextHandler.Init (RoslynCodeCompletionFactory factory)
		{
			this.factory = factory;
		}

		protected override IEnumerable<CompletionData> CreateCompletionData (CompletionEngine engine, SemanticModel semanticModel, int position, ITypeSymbol returnType, Accessibility seenAccessibility, SyntaxToken startToken, SyntaxToken tokenBeforeReturnType, bool afterKeyword, CancellationToken cancellationToken)
		{
			var result = new List<CompletionData> ();
			ISet<ISymbol> overridableMembers;
			if (!TryDetermineOverridableProtocolMembers (semanticModel, tokenBeforeReturnType, seenAccessibility, out overridableMembers, cancellationToken)) {
				return result;
			}
			if (returnType != null) {
				overridableMembers = FilterOverrides (overridableMembers, returnType);
			}
			var curType = semanticModel.GetEnclosingSymbolMD<INamedTypeSymbol> (startToken.SpanStart, cancellationToken);
			var declarationBegin = afterKeyword ? startToken.SpanStart : position - 1;
			foreach (var m in overridableMembers) {
				var data = new ProtocolCompletionData (this, factory, declarationBegin, curType, m, afterKeyword);
				result.Add (data);
			}
			return result;
		}

		static bool TryDetermineOverridableProtocolMembers(SemanticModel semanticModel, SyntaxToken startToken, Accessibility seenAccessibility, out ISet<ISymbol> overridableMembers, CancellationToken cancellationToken)
		{
			var result = new HashSet<ISymbol>();
			var containingType = semanticModel.GetEnclosingSymbolMD<INamedTypeSymbol>(startToken.SpanStart, cancellationToken);
			if (containingType != null && !containingType.IsScriptClass && !containingType.IsImplicitClass)
			{
				if (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct)
				{
					var baseTypes = containingType.GetBaseTypesMD().Reverse().Concat(containingType.AllInterfaces);
					foreach (var type in baseTypes)
					{
						cancellationToken.ThrowIfCancellationRequested();

						// Prefer overrides in derived classes
						RemoveOverriddenMembers(result, type, cancellationToken);

						// Retain overridable methods
						AddProtocolMembers(semanticModel, result, containingType, type, cancellationToken);
					}
					// Don't suggest already overridden members
					RemoveOverriddenMembers(result, containingType, cancellationToken);
				}
			}

			// Filter based on accessibility
			if (seenAccessibility != Accessibility.NotApplicable)
			{
				result.RemoveWhere(m => m.DeclaredAccessibility != seenAccessibility);
			}


			// Filter members that are already overriden - they're already part of 'override completion'
			ISet<ISymbol> realOverridableMembers;
			if (OverrideContextHandler.TryDetermineOverridableMembers (semanticModel, startToken, seenAccessibility, out realOverridableMembers, cancellationToken)) {
				result.RemoveWhere (m => realOverridableMembers.Any (m2 => IsEqualMember (m, m2)));
			}

			overridableMembers = result;
			return overridableMembers.Count > 0;
		}

		static bool IsEqualMember (ISymbol m, ISymbol m2)
		{
			return SignatureComparerMD.HaveSameSignature (m, m2, true);
		}

		static void AddProtocolMembers(SemanticModel semanticModel, HashSet<ISymbol> result, INamedTypeSymbol containingType, INamedTypeSymbol type, CancellationToken cancellationToken)
		{
			string name;
			if (!HasProtocolAttribute (type, out name))
				return;
			var protocolType = semanticModel.Compilation.GlobalNamespace.GetAllTypesMD (cancellationToken).FirstOrDefault (t => string.Equals (t.Name, name, StringComparison.OrdinalIgnoreCase));
			if (protocolType == null)
				return;

			foreach (var member in protocolType.GetMembers ().OfType<IMethodSymbol> ()) {
				if (member.ExplicitInterfaceImplementations.Length > 0 || member.IsAbstract || !member.IsVirtual)
					continue;
				if (member.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ()))) {
					result.Add (member);
				}

			}
			foreach (var member in protocolType.GetMembers ().OfType<IPropertySymbol> ()) {
				if (member.ExplicitInterfaceImplementations.Length > 0 || member.IsAbstract || !member.IsVirtual)
					continue;
				if (member.GetMethod != null && member.GetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ())) ||
					member.SetMethod != null && member.SetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ())))
					result.Add (member);
			}
		}
internal static bool IsFoundationNamespace (string ns)
{
	return (ns == "MonoTouch.Foundation" || ns == "Foundation");
}

internal static bool IsFoundationNamespace (INamespaceSymbol ns)
{
	return IsFoundationNamespace (ns.GetFullName ());
}

internal static bool HasProtocolAttribute (INamedTypeSymbol type, out string name)
{
	foreach (var baseType in type.GetAllBaseClassesAndInterfaces (true)) {
		foreach (var attrs in baseType.GetAttributes ()) {
			if (attrs.AttributeClass.Name == "ProtocolAttribute" && IsFoundationNamespace (attrs.AttributeClass.ContainingNamespace.GetFullName ())) {
				foreach (var na in attrs.NamedArguments) {
					if (na.Key != "Name")
						continue;
					name = na.Value.Value as string;
					if (name != null)
						return true;
				}
			}
		}
	}
	name = null;
	return false;
}
	}

	/* TODO: Add tests - note CompletionTestBase is part of MonoDevelop.CSharpBinding.Tests - needs to be copied as well :

	[TestFixture]
	class ProtocolMemberContextHandlerTests : CompletionTestBase
	{
		static readonly string Header = @"
using System;
using Foundation;

namespace Foundation
{
	public class ExportAttribute : Attribute
	{
		public ExportAttribute(string id) { }
	}

	public class ProtocolAttribute : Attribute
	{
		public string Name { get; set; }
		public ProtocolAttribute() { }
	}
}";

		internal override CompletionContextHandler CreateContextHandler ()
		{
			return new ProtocolMemberContextHandler ();
		}

		[Test]
		public void TestSimple ()
		{
			VerifyItemsExist (Header + @"

class MyProtocol
{
	[Export("":FooBar"")]
	public virtual void FooBar()
	{

	}
}


[Protocol(Name = ""MyProtocol"")]
class ProtocolClass
{

}


class FooBar : ProtocolClass
{
	override $$
}

", "FooBar");
		}

		/// <summary>
		/// Bug 39428 - [iOS] Override of protocol method shows 2 completions
		/// </summary>
		[Test]
		public void TestBug39428 ()
		{
			VerifyItemIsAbsent (Header + @"

class MyProtocol
{
	[Export("":FooBar"")]
	public virtual void FooBar()
	{

	}
}


[Protocol(Name = ""MyProtocol"")]
class ProtocolClass
{
	public virtual void FooBar()
	{
	}
}

class FooBar : ProtocolClass
{
	override $$
}

", "FooBar");
		}
	}	



}





//
// ProtocolCompletionData.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
/*using System;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.CodeGeneration;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using MonoDevelop.CSharp.Refactoring;
using System.Linq;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Completion;

namespace MonoDevelop.IPhone.Editor
{
	class ProtocolCompletionData : RoslynSymbolCompletionData
	{
		readonly int declarationBegin;
		readonly ITypeSymbol currentType;

		public bool GenerateBody { get; set; }

		static readonly SymbolDisplayFormat NameFormat;

		internal static readonly SymbolDisplayFormat overrideNameFormat;

		static ProtocolCompletionData ()
		{
			NameFormat = new SymbolDisplayFormat (
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
				propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
				memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
				parameterOptions:
				SymbolDisplayParameterOptions.IncludeParamsRefOut |
				SymbolDisplayParameterOptions.IncludeExtensionThis |
				SymbolDisplayParameterOptions.IncludeType |
				SymbolDisplayParameterOptions.IncludeName,
				miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			);

			overrideNameFormat = NameFormat.WithParameterOptions (
				SymbolDisplayParameterOptions.IncludeDefaultValue |
				SymbolDisplayParameterOptions.IncludeExtensionThis |
				SymbolDisplayParameterOptions.IncludeType |
				SymbolDisplayParameterOptions.IncludeName |
				SymbolDisplayParameterOptions.IncludeParamsRefOut
			);
		}

		string displayText;

		bool afterKeyword;

		public override string DisplayText {
			get {
				if (displayText == null) {
					if (factory == null) {
						displayText = Symbol.Name;
					} else {
						var model = ext.ParsedDocument.GetAst<SemanticModel> ();
						displayText = RoslynCompletionData.SafeMinimalDisplayString (base.Symbol, model, ext.Editor.CaretOffset, overrideNameFormat);
					}
					if (!afterKeyword)
						displayText = "override " + displayText;
				}

				return displayText;
			}
		}

		public override string CompletionText {
			get {
				return Symbol.Name;
			}
		}

		public override string GetDisplayTextMarkup ()
		{
			if (factory == null)
				return Symbol.Name;
			var model = ext.ParsedDocument.GetAst<SemanticModel> ();

			var result = RoslynCompletionData.SafeMinimalDisplayString (base.Symbol, model, declarationBegin, Ambience.LabelFormat) + " {...}";
			var idx = result.IndexOf (Symbol.Name);
			if (idx >= 0) {
				result =
					result.Substring (0, idx) +
						  "<b>" + Symbol.Name + "</b>" +
						  result.Substring (idx + Symbol.Name.Length);
			}

			if (!afterKeyword)
				result = "override " + result;

			return ApplyDiplayFlagsFormatting (result);
		}

		public ProtocolCompletionData (ICompletionDataKeyHandler keyHandler, RoslynCodeCompletionFactory factory, int declarationBegin, ITypeSymbol currentType, Microsoft.CodeAnalysis.ISymbol member, bool afterKeyword) : base (keyHandler, factory, member, member.ToDisplayString ())
		{
			this.afterKeyword = afterKeyword;
			this.currentType = currentType;
			this.declarationBegin = declarationBegin;
			this.GenerateBody = true;
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var editor = ext.Editor;
			bool isExplicit = false;
			//			if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
			//				foreach (var m in type.Members) {
			//					if (m.Name == member.Name && !m.ReturnType.Equals (member.ReturnType)) {
			//						isExplicit = true;
			//						break;
			//					}
			//				}
			//			}
			//			var resolvedType = type.Resolve (ext.Project).GetDefinition ();
			//			if (ext.Project != null)
			//				generator.PolicyParent = ext.Project.Policies;

			var result = CSharpCodeGenerator.CreateProtocolMemberImplementation (ext.DocumentContext, ext.Editor, currentType, currentType.Locations.First (), Symbol, isExplicit, factory.SemanticModel);
			string sb = result.Code.TrimStart ();
			int trimStart = result.Code.Length - sb.Length;
			sb = sb.TrimEnd ();

			var lastRegion = result.BodyRegions.LastOrDefault ();
			var region = lastRegion == null ? null
				: new CodeGeneratorBodyRegion (lastRegion.StartOffset - trimStart, lastRegion.EndOffset - trimStart);

			int targetCaretPosition;
			int selectionEndPosition = -1;
			if (region != null && region.IsValid) {
				targetCaretPosition = declarationBegin + region.StartOffset;
				if (region.Length > 0) {
					if (GenerateBody) {
						selectionEndPosition = declarationBegin + region.EndOffset;
					} else {
						//FIXME: if there are multiple regions, remove all of them
						sb = sb.Substring (0, region.StartOffset) + sb.Substring (region.EndOffset);
					}
				}
			} else {
				targetCaretPosition = declarationBegin + sb.Length;
			}

			editor.ReplaceText (declarationBegin, editor.CaretOffset - declarationBegin, sb);
			if (selectionEndPosition > 0) {
				editor.CaretOffset = selectionEndPosition;
				editor.SetSelection (targetCaretPosition, selectionEndPosition);
			} else {
				editor.CaretOffset = targetCaretPosition;
			}

			OnTheFlyFormatter.Format (editor, ext.DocumentContext, declarationBegin, declarationBegin + sb.Length);
		}
	}
}

*/
