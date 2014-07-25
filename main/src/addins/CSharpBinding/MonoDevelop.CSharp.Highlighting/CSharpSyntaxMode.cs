// 
// SyntaxMode.cs
//  
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Analysis;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Highlighting
{
	static class StringHelper
	{
		public static bool IsAt (this string str, int idx, string pattern)
		{
			if (idx + pattern.Length > str.Length)
				return false;

			for (int i = 0; i < pattern.Length; i++)
				if (pattern [i] != str [idx + i])
					return false;
			return true;
		}
	}

	class CSharpSyntaxMode : SemanticHighlighting
	{
		SemanticModel resolver;
		CancellationTokenSource src;

		public CSharpSyntaxMode (TextEditor editor, DocumentContext documentContext) : base (editor, documentContext)
		{
		}

		#region implemented abstract members of SemanticHighlighting

		protected override void DocumentParsed ()
		{
			if (src != null)
				src.Cancel ();
			resolver = null;
			src = new CancellationTokenSource ();
			var analysisDocument = documentContext.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var cancellationToken = src.Token;
			System.Threading.Tasks.Task.Factory.StartNew (delegate {
				var newResolverTask = analysisDocument.GetSemanticModelAsync (cancellationToken);
				if (newResolverTask == null)
					return;
				var newResolver = newResolverTask.Result;
				if (newResolver == null)
					return;
				if (!cancellationToken.IsCancellationRequested) {
					Gtk.Application.Invoke (delegate {
						if (cancellationToken.IsCancellationRequested)
							return;
						resolver = newResolver;
						UpdateSemanticHighlighting ();
					});
				}
			}, cancellationToken);
		}

		public override IEnumerable<ColoredSegment> GetColoredSegments (ISegment segment)
		{
			var result = new List<ColoredSegment> ();
			if (resolver == null)
				return result;
			var visitor = new HighlightingVisitior (resolver, result.Add, default (CancellationToken), segment);
			visitor.Visit (resolver.SyntaxTree.GetRoot ()); 
			return result;
		}
		#endregion
	}

	class HighlightingVisitior : SemanticHighlightingVisitor<string>
	{
		readonly Action<ColoredSegment> colorizeCallback;

		public HighlightingVisitior (SemanticModel resolver, Action<ColoredSegment> colorizeCallback, CancellationToken cancellationToken, ISegment textSpan) : base (resolver)
		{
			if (resolver == null)
				throw new ArgumentNullException ("resolver");
			this.cancellationToken = cancellationToken;
			this.colorizeCallback = colorizeCallback;
			this.region = new TextSpan (textSpan.Offset, textSpan.Length);
			Setup ();
		}
		
		void Setup ()
		{
			
			defaultTextColor = ColorScheme.PlainTextKey;
			referenceTypeColor = ColorScheme.UserTypesKey;
			valueTypeColor = ColorScheme.UserTypesValueTypesKey;
			interfaceTypeColor = ColorScheme.UserTypesInterfacesKey;
			enumerationTypeColor = ColorScheme.UserTypesEnumsKey;
			typeParameterTypeColor = ColorScheme.UserTypesTypeParametersKey;
			delegateTypeColor = ColorScheme.UserTypesDelegatesKey;

			methodCallColor = ColorScheme.UserMethodUsageKey;
			methodDeclarationColor = ColorScheme.UserMethodDeclarationKey;

			eventDeclarationColor = ColorScheme.UserEventDeclarationKey;
			eventAccessColor = ColorScheme.UserEventUsageKey;

			fieldDeclarationColor = ColorScheme.UserFieldDeclarationKey;
			fieldAccessColor = ColorScheme.UserFieldUsageKey;

			propertyDeclarationColor = ColorScheme.UserPropertyDeclarationKey;
			propertyAccessColor = ColorScheme.UserPropertyUsageKey;

			variableDeclarationColor = ColorScheme.UserVariableDeclarationKey;
			variableAccessColor = ColorScheme.UserVariableUsageKey;

			parameterDeclarationColor = ColorScheme.UserParameterDeclarationKey;
			parameterAccessColor = ColorScheme.UserParameterUsageKey;

			valueKeywordColor = ColorScheme.KeywordContextKey;
			externAliasKeywordColor = ColorScheme.KeywordNamespaceKey;
			varKeywordTypeColor = ColorScheme.KeywordTypesKey;

			parameterModifierColor = ColorScheme.KeywordParameterKey;
			inactiveCodeColor = ColorScheme.ExcludedCodeKey;
			syntaxErrorColor = ColorScheme.SyntaxErrorKey;

			stringFormatItemColor = ColorScheme.StringFormatItemsKey;
		}

		protected override void Colorize (TextSpan span, string color)
		{
			colorizeCallback (new ColoredSegment (span.Start, span.Length, color));
		}
	}
}
