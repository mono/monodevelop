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

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCodeCompletionFactory : ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory
	{
		readonly CSharpCompletionTextEditorExtension ext;

		public RoslynCodeCompletionFactory (CSharpCompletionTextEditorExtension ext)
		{
			if (ext == null)
				throw new ArgumentNullException ("ext");
			this.ext = ext;
		}

		#region ICompletionDataFactory implementation

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateGenericData (string data, ICSharpCode.NRefactory6.CSharp.Completion.GenericDataType genericDataType)
		{
			return new RoslynCompletionData {
				CompletionText = data,
				DisplayText = data,
				Icon = "md-keyword"
			};
		}
		
		ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateEnumMemberCompletionData (IFieldSymbol field)
		{
			return new RoslynSymbolCompletionData (ext, field, field.Type.Name + "." + field.Name);
		}
		
		class FormatItemCompletionData : MonoDevelop.Ide.CodeCompletion.CompletionData
		{
			string format;
			string description;
			object example;

			public FormatItemCompletionData (string format, string description, object example)
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

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateFormatItemCompletionData (string format, string description, object example)
		{
			return new FormatItemCompletionData (format, description, example);
		}

		class XmlDocCompletionData : CompletionData//, IListData
		{
			readonly CSharpCompletionTextEditorExtension ext;
			readonly string title;
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

			public XmlDocCompletionData (CSharpCompletionTextEditorExtension ext, string title, string description, string insertText) : base (title, "md-keyword", description, insertText ?? title)
			{
				this.ext = ext;
				this.title = title;
			}

//			public override TooltipInformation CreateTooltipInformation (bool smartWrap)
//			{
//				var sig = new SignatureMarkupCreator (ext.Editor, ext.DocumentContext, ext.Editor.CaretOffset);
//				sig.BreakLineAfterReturnType = smartWrap;
//				return sig.GetKeywordTooltip (title, null);
//			}


			public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
			{
				var currentWord = GetCurrentWord (window);
				var text = CompletionText;
				if (keyChar != '>')
					text += ">";
				window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, text);
			}
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateXmlDocCompletionData (string title, string description, string insertText)
		{
			return new XmlDocCompletionData (ext, title, description, insertText);
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateSymbolCompletionData (ISymbol symbol)
		{
			return new RoslynSymbolCompletionData (ext, symbol);
		}
		
		ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateSymbolCompletionData (ISymbol symbol, string text)
		{
			return new RoslynSymbolCompletionData (ext, symbol, text);
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreateNewOverrideCompletionData(int declarationBegin, ITypeSymbol currentType, ISymbol m)
		{
			return new CreateOverrideCompletionData (ext, declarationBegin, currentType, m);
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData ICSharpCode.NRefactory6.CSharp.Completion.ICompletionDataFactory.CreatePartialCompletionData(int declarationBegin, ITypeSymbol currentType, IMethodSymbol method)
		{
			return new CreatePartialCompletionData (ext, declarationBegin, currentType, method);
		}

		#endregion
	}
}
