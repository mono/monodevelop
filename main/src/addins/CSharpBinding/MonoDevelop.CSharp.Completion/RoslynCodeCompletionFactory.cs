// 
// CSharpCompletionTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory6.CSharp.Completion;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCodeCompletionFactory : ICompletionDataFactory
	{
		readonly CSharpCompletionTextEditorExtension ext;
		readonly SemanticModel semanticModel;

		public CSharpCompletionTextEditorExtension Ext {
			get {
				return this.ext;
			}
		}

		public SemanticModel SemanticModel {
			get {
				return this.semanticModel;
			}
		}

		public RoslynCodeCompletionFactory (CSharpCompletionTextEditorExtension ext, SemanticModel semanticModel)
		{
			if (ext == null)
				throw new ArgumentNullException ("ext");
			if (semanticModel == null)
				throw new ArgumentNullException ("semanticModel");
			this.semanticModel = semanticModel;
			this.ext = ext;
		}

		#region ICompletionDataFactory implementation


		class KeywordCompletionData : RoslynCompletionData
		{
			static SignatureMarkupCreator creator = new SignatureMarkupCreator (null, 0);

			SyntaxKind kind;

			public KeywordCompletionData (ICompletionDataKeyHandler keyHandler, SyntaxKind kind) : base (keyHandler)
			{
				this.kind = kind;
			}

			public override Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, System.Threading.CancellationToken cancelToken)
			{
				if (kind == SyntaxKind.IdentifierToken)
					return Task.FromResult (creator.GetKeywordTooltip (SyntaxFactory.Identifier (this.DisplayText)));
				return Task.FromResult (creator.GetKeywordTooltip (SyntaxFactory.Token (kind)));
			}
		}

		CompletionData ICompletionDataFactory.CreateKeywordCompletion (ICompletionDataKeyHandler keyHandler, string data, SyntaxKind syntaxKind)
		{
			return new KeywordCompletionData (keyHandler, syntaxKind) {
				CompletionText = data,
				DisplayText = data,
				Icon = "md-keyword"
			};
		}

		CompletionData ICompletionDataFactory.CreateGenericData (ICompletionDataKeyHandler keyHandler, string data, GenericDataType genericDataType)
		{
			return new RoslynCompletionData (keyHandler) {
				CompletionText = data,
				DisplayText = data,
				Icon = "md-keyword"
			};
		}
		
		ISymbolCompletionData ICompletionDataFactory.CreateEnumMemberCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol alias, IFieldSymbol field)
		{
			return new RoslynSymbolCompletionData (keyHandler, this, field, RoslynCompletionData.SafeMinimalDisplayString (alias ?? field.Type, semanticModel, ext.Editor.CaretOffset, Ambience.NameFormat) + "." + field.Name);
		}
		
		class FormatItemCompletionData : RoslynCompletionData
		{
			string format;
			string description;
			object example;

			public FormatItemCompletionData (ICompletionDataKeyHandler keyHandler, string format, string description, object example) : base (keyHandler)
			{
				this.format = format;
				this.description = description;
				this.example = example;
			}

			public override string DisplayText {
				get {
					return format;
				}
			}

			public override string GetDisplayDescription (bool isSelected)
			{
				return "- <span foreground=\"darkgray\" size='small'>" + description + "</span>";
			}

			string rightSideDescription = null;
			public override string GetRightSideDescription (bool isSelected)
			{
				if (rightSideDescription == null) {
					try {
						rightSideDescription = "<span size='small'>" + string.Format ("{0:" +format +"}", example) +"</span>";
					} catch (Exception e) {
						rightSideDescription = "";
						LoggingService.LogError ("Format error.", e);
					}
				}
				return rightSideDescription;
			}

			public override string CompletionText {
				get {
					return format;
				}
			}

			public override int CompareTo (object obj)
			{
				return 0;
			}
		}

		CompletionData ICompletionDataFactory.CreateFormatItemCompletionData (ICompletionDataKeyHandler keyHandler, string format, string description, object example)
		{
			return new FormatItemCompletionData (keyHandler, format, description, example);
		}

		class XmlDocCompletionData : RoslynCompletionData
		{
			//readonly CSharpCompletionTextEditorExtension ext;
			/*
			#region IListData implementation

			CSharpCompletionDataList list;
			public CSharpCompletionDataList List {
				get {
					return list;
				}
				set {
					list = value;
				}
			}

			#endregion*/

			public XmlDocCompletionData (ICompletionDataKeyHandler keyHandler, RoslynCodeCompletionFactory ext, string title, string description, string insertText) : base (keyHandler, title, "md-keyword", description, insertText ?? title)
			{
				// this.ext = ext;
				//this.title = title;
			}
//			public override TooltipInformation CreateTooltipInformation (bool smartWrap)
//			{
//				var sig = new SignatureMarkupCreator (ext.Editor, ext.DocumentContext, ext.Editor.CaretOffset);
//				sig.BreakLineAfterReturnType = smartWrap;
//				return sig.GetKeywordTooltip (title, null);
//			}

			public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, MonoDevelop.Ide.Editor.Extension.KeyDescriptor descriptor)
			{
				var currentWord = GetCurrentWord (window, descriptor);
				var text = CompletionText;
				if (descriptor.KeyChar == '>' && text.EndsWith (">", StringComparison.Ordinal))
					text = text.Substring (0, text.Length - 1);
				if (text.StartsWith ("<", StringComparison.Ordinal))
					text = text.Substring (1);
				
				window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, text);
			}
		}

		CompletionData ICompletionDataFactory.CreateXmlDocCompletionData (ICompletionDataKeyHandler keyHandler, string title, string description, string insertText)
		{
			return new XmlDocCompletionData (keyHandler, this, title, description, insertText);
		}

		ISymbolCompletionData ICompletionDataFactory.CreateSymbolCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol symbol)
		{
			return new RoslynSymbolCompletionData (keyHandler, this, symbol, symbol.Name);
		}
		
		ISymbolCompletionData ICompletionDataFactory.CreateSymbolCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol symbol, string text)
		{
			return new RoslynSymbolCompletionData (keyHandler, this, symbol, text);
		}

		ISymbolCompletionData ICompletionDataFactory.CreateExistingMethodDelegate (ICompletionDataKeyHandler keyHandler, IMethodSymbol method)
		{
			return new RoslynSymbolCompletionData (keyHandler, this, method, method.Name) { IsDelegateExpected = true };
		}

		CompletionData ICompletionDataFactory.CreateNewOverrideCompletionData(ICompletionDataKeyHandler keyHandler, int declarationBegin, ITypeSymbol currentType, ISymbol m, bool afterKeyword)
		{
			return new CreateOverrideCompletionData (keyHandler, this, declarationBegin, currentType, m, afterKeyword);
		}

		CompletionData ICompletionDataFactory.CreatePartialCompletionData(ICompletionDataKeyHandler keyHandler, int declarationBegin, ITypeSymbol currentType, IMethodSymbol method, bool afterKeyword)
		{
			return new CreatePartialCompletionData (keyHandler, this, declarationBegin, currentType, method, afterKeyword);
		}

		CompletionData ICompletionDataFactory.CreateAnonymousMethod(ICompletionDataKeyHandler keyHandler, string displayText, string description, string textBeforeCaret, string textAfterCaret)
		{
			return new AnonymousMethodCompletionData (keyHandler) {
				CompletionText = textBeforeCaret + "|" + textAfterCaret,
				DisplayText = displayText,
				Description = description
			};
		}

		CompletionData ICompletionDataFactory.CreateNewMethodDelegate(ICompletionDataKeyHandler keyHandler, ITypeSymbol delegateType, string varName, INamedTypeSymbol curType)
		{
			return new EventCreationCompletionData (keyHandler, this, delegateType, varName, curType);
		}

		CompletionData ICompletionDataFactory.CreateObjectCreation (ICompletionDataKeyHandler keyHandler, ITypeSymbol type, ISymbol symbol, int declarationBegin, bool afterKeyword)
		{
			return new ObjectCreationCompletionData (keyHandler, this, semanticModel, type, symbol, declarationBegin, afterKeyword);
		}

		CompletionData ICompletionDataFactory.CreateCastCompletionData (ICompletionDataKeyHandler keyHandler, ISymbol member, SyntaxNode nodeToCast, ITypeSymbol targetType)
		{
			return new CastCompletionData (keyHandler, this, semanticModel, member, nodeToCast, targetType);
		}

		CompletionCategory ICompletionDataFactory.CreateCompletionDataCategory (ISymbol forSymbol)
		{
			return new RoslynCompletionCategory (forSymbol);
		}

		#endregion
	}
}
