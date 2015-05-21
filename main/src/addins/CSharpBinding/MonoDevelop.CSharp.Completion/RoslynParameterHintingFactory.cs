//
// RoslynParameterHintingFactory.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory6.CSharp.Completion;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using ICSharpCode.NRefactory6.CSharp;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynParameterHintingFactory : IParameterHintingDataFactory
	{
		#region IParameterHintingDataFactory implementation

		IParameterHintingData IParameterHintingDataFactory.CreateConstructorProvider (Microsoft.CodeAnalysis.IMethodSymbol constructor)
		{
			return new ParameterHintingData (constructor);
		}

		IParameterHintingData IParameterHintingDataFactory.CreateMethodDataProvider (Microsoft.CodeAnalysis.IMethodSymbol method)
		{
			return new ParameterHintingData (method);
		}

		IParameterHintingData IParameterHintingDataFactory.CreateDelegateDataProvider (Microsoft.CodeAnalysis.ITypeSymbol delegateType)
		{
			return new DelegateParameterHintingData (delegateType);
		}

		IParameterHintingData IParameterHintingDataFactory.CreateIndexerParameterDataProvider (Microsoft.CodeAnalysis.IPropertySymbol indexer, Microsoft.CodeAnalysis.SyntaxNode resolvedNode)
		{
			return new ParameterHintingData (indexer);
		}

		IParameterHintingData IParameterHintingDataFactory.CreateTypeParameterDataProvider (Microsoft.CodeAnalysis.INamedTypeSymbol type)
		{
			return new TypeParameterHintingData (type);
		}

		IParameterHintingData IParameterHintingDataFactory.CreateTypeParameterDataProvider (Microsoft.CodeAnalysis.IMethodSymbol method)
		{
			return new TypeParameterHintingData (method);
		}

		IParameterHintingData IParameterHintingDataFactory.CreateArrayDataProvider (Microsoft.CodeAnalysis.IArrayTypeSymbol arrayType)
		{
			return new ArrayParameterHintingData (arrayType);
		}

		#endregion

		class ParameterHintingData : MonoDevelop.Ide.CodeCompletion.ParameterHintingData, IParameterHintingData
		{
			public ParameterHintingData (IMethodSymbol symbol) : base (symbol)
			{
			}

			public ParameterHintingData (IPropertySymbol symbol) : base (symbol)
			{
			}
			public override Task<TooltipInformation> CreateTooltipInformation (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken cancelToken)
			{
				return CreateTooltipInformation (editor, ctx, Symbol, currentParameter, smartWrap, cancelToken);
			}
			
			internal static Task<TooltipInformation> CreateTooltipInformation (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, ISymbol sym, int currentParameter, bool smartWrap, CancellationToken cancelToken)
			{
				var tooltipInfo = new TooltipInformation ();
				var sig = new SignatureMarkupCreator (ctx, editor != null ? editor.CaretOffset : 0);
				sig.HighlightParameter = currentParameter;
				sig.BreakLineAfterReturnType = smartWrap;

				return Task.Run (() => {
					if (cancelToken.IsCancellationRequested)
						return null;
					try {
						tooltipInfo.SignatureMarkup = sig.GetMarkup (sym);
					} catch (Exception e) {
						LoggingService.LogError ("Got exception while creating markup for :" + sym, e);
						return new TooltipInformation ();
					}
					tooltipInfo.SummaryMarkup = Ambience.GetSummaryMarkup (sym) ?? "";

					if (cancelToken.IsCancellationRequested)
						return null;
					
					if (sym is IMethodSymbol) {
						var method = (IMethodSymbol)sym;
						if (method.IsExtensionMethod && method.ReducedFrom != null && method.ReducedFrom.ContainingType != null) {
							tooltipInfo.AddCategory (GettextCatalog.GetString ("Extension Method from"), method.ReducedFrom.ContainingType.Name);
						}
					}
					int paramIndex = currentParameter;

					//				if (Symbol is IMethodSymbol && ((IMethodSymbol)Symbol).IsExtensionMethod)
					//					paramIndex++;
					var list = GetParameterList (sym);
					paramIndex = Math.Min (list.Length - 1, paramIndex);

					var curParameter = paramIndex >= 0 && paramIndex < list.Length ? list [paramIndex] : null;
					if (curParameter != null) {

						string docText = Ambience.GetDocumentation (sym);
						if (!string.IsNullOrEmpty (docText)) {
							string text = docText;
							Regex paramRegex = new Regex ("(\\<param\\s+name\\s*=\\s*\"" + curParameter.Name + "\"\\s*\\>.*?\\</param\\>)", RegexOptions.Compiled);
							Match match = paramRegex.Match (docText);

							if (match.Success) {
								text = Ambience.GetDocumentationMarkup (sym, match.Groups [1].Value);
								if (!string.IsNullOrWhiteSpace (text))
									tooltipInfo.AddCategory (GettextCatalog.GetString ("Parameter"), text);
							}
						}
						if (curParameter.Type.TypeKind == TypeKind.Delegate)
							tooltipInfo.AddCategory (GettextCatalog.GetString ("Delegate Info"), sig.GetDelegateInfo (curParameter.Type));
					}
					return tooltipInfo;
				});
			}

			static ImmutableArray<IParameterSymbol> GetParameterList (ISymbol data)
			{
				var ms = data as IMethodSymbol;
				if (ms != null)
					return ms.Parameters;
				
				var ps = data as IPropertySymbol;
				if (ps != null)
					return ps.Parameters;

				return ImmutableArray<IParameterSymbol>.Empty;
			}

			public override string GetParameterName (int currentParameter)
			{
				var list = GetParameterList (Symbol);
				if (currentParameter < 0 || currentParameter >= list.Length)
					throw new ArgumentOutOfRangeException ("currentParameter");
				return list [currentParameter].Name;
			}

			public override int ParameterCount {
				get {
					return GetParameterList (Symbol).Length;
				}
			}

			public override bool IsParameterListAllowed {
				get {
					var param = GetParameterList (Symbol).LastOrDefault ();
					return param != null && param.IsParams;
				}
			}
		}

		class DelegateParameterHintingData : MonoDevelop.Ide.CodeCompletion.ParameterHintingData, IParameterHintingData
		{
			readonly IMethodSymbol invocationMethod;

			public DelegateParameterHintingData (ITypeSymbol symbol) : base (symbol)
			{
				this.invocationMethod = symbol.GetDelegateInvokeMethod ();
			}

			public override string GetParameterName (int currentParameter)
			{
				var list = invocationMethod.Parameters;
				if (currentParameter < 0 || currentParameter >= list.Length)
					throw new ArgumentOutOfRangeException ("currentParameter");
				return list [currentParameter].Name;
			}

			public override int ParameterCount {
				get {
					return invocationMethod.Parameters.Length;
				}
			}

			public override bool IsParameterListAllowed {
				get {
					var param = invocationMethod.Parameters.LastOrDefault ();
					return param != null && param.IsParams;
				}
			}

			public override Task<TooltipInformation> CreateTooltipInformation (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken cancelToken)
			{
				return ParameterHintingData.CreateTooltipInformation (editor, ctx, invocationMethod, currentParameter, smartWrap, cancelToken);
			}
		}

		class ArrayParameterHintingData : MonoDevelop.Ide.CodeCompletion.ParameterHintingData, IParameterHintingData
		{
			readonly IArrayTypeSymbol arrayType;

			public ArrayParameterHintingData (IArrayTypeSymbol arrayType) : base (arrayType)
			{
				this.arrayType = arrayType;
			}

			public override string GetParameterName (int currentParameter)
			{
				return null;
			}

			public override int ParameterCount {
				get {
					return arrayType.Rank;
				}
			}

			public override bool IsParameterListAllowed {
				get {
					return false;
				}
			}
			
			public override Task<TooltipInformation> CreateTooltipInformation (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, int currentParameter, bool smartWrap, CancellationToken cancelToken)
			{
				var sig = new SignatureMarkupCreator (ctx, editor != null ? editor.CaretOffset : 0) {
					HighlightParameter = currentParameter
				};
				return Task.FromResult (new TooltipInformation {
					SignatureMarkup = sig.GetArrayIndexerMarkup (arrayType)
				});
			}
			
		}

		class TypeParameterHintingData : MonoDevelop.Ide.CodeCompletion.ParameterHintingData, IParameterHintingData
		{
			public TypeParameterHintingData (IMethodSymbol symbol) : base (symbol)
			{
			}

			public TypeParameterHintingData (INamedTypeSymbol symbol) : base (symbol)
			{
			}

			static ImmutableArray<ITypeParameterSymbol> GetTypeParameterList (IParameterHintingData data)
			{
				var ms = data.Symbol as IMethodSymbol;
				if (ms != null)
					return ms.TypeParameters;
				
				var ps = data.Symbol as INamedTypeSymbol;
				if (ps != null)
					return ps.TypeParameters;

				return ImmutableArray<ITypeParameterSymbol>.Empty;
			}

			public override string GetParameterName (int currentParameter)
			{
				var list = GetTypeParameterList (this);
				if (currentParameter < 0 || currentParameter >= list.Length)
					throw new ArgumentOutOfRangeException ("currentParameter");
				return list [currentParameter].Name;
			}

			public override int ParameterCount {
				get {
					return GetTypeParameterList (this).Length;
				}
			}

			public override bool IsParameterListAllowed {
				get {
					return false;
				}
			}
		}
	}
}

