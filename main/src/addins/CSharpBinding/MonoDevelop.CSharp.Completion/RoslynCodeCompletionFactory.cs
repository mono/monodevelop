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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using Gtk;

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

			protected readonly RoslynCodeCompletionFactory factory;

			protected CSharpCompletionTextEditorExtension ext { get { return factory?.Ext; } }


			public KeywordCompletionData (ICompletionDataKeyHandler keyHandler, RoslynCodeCompletionFactory factory) : base (keyHandler)
			{
				this.factory = factory;
			}

			static bool IsBracketAlreadyInserted (CSharpCompletionTextEditorExtension ext)
			{
				var Editor = ext.Editor;
				int offset = Editor.CaretOffset;
				while (offset < Editor.Length) {
					char ch = Editor.GetCharAt (offset);
					if (!char.IsLetterOrDigit (ch))
						break;
					offset++;
				}
				while (offset < Editor.Length) {
					char ch = Editor.GetCharAt (offset);
					if (!char.IsWhiteSpace (ch))
						return ch == '(';
					offset++;
				}
				return false;
			}

			public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, MonoDevelop.Ide.Editor.Extension.KeyDescriptor descriptor)
			{
				if (this.CompletionText == "sizeof" || this.CompletionText == "nameof" || this.CompletionText == "typeof") {
					string partialWord = GetCurrentWord (window, descriptor);
					int skipChars = 0;
					bool runCompletionCompletionCommand = false;
					var method = Symbol as IMethodSymbol;

					bool addParens = IdeApp.Preferences.AddParenthesesAfterCompletion;
					bool addOpeningOnly = IdeApp.Preferences.AddOpeningOnly;
					var Editor = ext.Editor;
					var Policy = ext.FormattingPolicy;
					string insertionText = this.CompletionText;

					if (addParens && !IsBracketAlreadyInserted (ext)) {
						var line = Editor.GetLine (Editor.CaretLine);
						//var start = window.CodeCompletionContext.TriggerOffset + partialWord.Length + 2;
						//var end = line.Offset + line.Length;
						//string textToEnd = start < end ? Editor.GetTextBetween (start, end) : "";
						bool addSpace = Policy.SpaceAfterMethodCallName && MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.OnTheFlyFormatting;

						var keys = new [] { SpecialKey.Return, SpecialKey.Tab, SpecialKey.Space };
						if (keys.Contains (descriptor.SpecialKey) || descriptor.KeyChar == ' ') {
							if (addOpeningOnly) {
								insertionText += addSpace ? " (|" : "(|";
							} else {
								insertionText += addSpace ? " (|)" : "(|)";
							}
						}
						ka |= KeyActions.Ignore;
					}

					window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, partialWord, insertionText);
					int offset = Editor.CaretOffset;
					for (int i = skipChars - 1; i-- > 0;) {
						Editor.StartSession (new SkipCharSession (Editor.GetCharAt (offset)));
						offset++;
					}

					if (runCompletionCompletionCommand && IdeApp.Workbench != null) {
						Application.Invoke ((o, args) => {
							ext.RunCompletionCommand ();
						});
					}
				} else {
					base.InsertCompletionText (window, ref ka, descriptor);
				}
			}
		}

		CompletionData ICompletionDataFactory.CreateKeywordCompletion (ICompletionDataKeyHandler keyHandler, string data)
		{
			return new KeywordCompletionData (keyHandler, this) {
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
						rightSideDescription = "<span size='small'>" + string.Format ("{0:" + format + "}", example) + "</span>";
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
			return new AnonymousMethodCompletionData (this, keyHandler) {
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
