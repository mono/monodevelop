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
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis;
using GLib;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCompletionData : CompletionData, ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData
	{
		List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> overloads;
		
		public override bool HasOverloads {
			get {
				return overloads != null;
			}
		}
		
		void ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.AddOverload (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData data)
		{
			if (overloads == null)
				overloads = new List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> ();
			overloads.Add (data);
			sorted = null;
//			//if any of the overloads is obsolete, we should not mark the item obsolete
//			if (!(overload.Entity as IMember).IsObsolete ())
//				DisplayFlags &= ~DisplayFlags.Obsolete;

		}

		ICSharpCode.NRefactory6.CSharp.Completion.CompletionCategory ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.CompletionCategory {
			get;
			set;
		}

		ICSharpCode.NRefactory6.CSharp.Completion.DisplayFlags ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.DisplayFlags {
			get;
			set;
		}

		List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> sorted;

		IEnumerable<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.OverloadedData {
			get {
				if (overloads == null)
					return new ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData[] { this };
				
				if (sorted == null) {
					sorted = new List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> (overloads);
					sorted.Add (this);
					// sorted.Sort (new OverloadSorter ());
				}
				return sorted;
			}
		}
		
//		class OverloadSorter : IComparer<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData>
//		{
//			public OverloadSorter ()
//			{
//			}
//
//			public int Compare (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData x, ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData y)
//			{
//				var mx = ((RoslynCompletionData)x).Entity as IMember;
//				var my = ((RoslynCompletionData)y).Entity as IMember;
//				int result;
//				
//				if (mx is ITypeDefinition && my is ITypeDefinition) {
//					result = ((((ITypeDefinition)mx).TypeParameters.Count).CompareTo (((ITypeDefinition)my).TypeParameters.Count));
//					if (result != 0)
//						return result;
//				}
//				
//				if (mx is IMethod && my is IMethod) {
//					return MethodParameterDataProvider.MethodComparer ((IMethod)mx, (IMethod)my);
//				}
//				string sx = mx.ReflectionName;// ambience.GetString (mx, flags);
//				string sy = my.ReflectionName;// ambience.GetString (my, flags);
//				result = sx.Length.CompareTo (sy.Length);
//				return result == 0 ? string.Compare (sx, sy) : result;
//			}
//		}

	}

	class RoslynSymbolCompletionData : RoslynCompletionData, ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData
	{
		readonly ISymbol symbol;

		public ISymbol Symbol {
			get {
				return symbol;
			}
		}
		
		public override string DisplayText {
			get {
				return text ?? symbol.Name;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override string CompletionText {
			get {
				return text ?? symbol.Name;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override MonoDevelop.Core.IconId Icon {
			get {
				return MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (symbol);
			}
			set {
				throw new NotSupportedException ();
			}
		}

		readonly string text;
		CSharpCompletionTextEditorExtension ext;

		public RoslynSymbolCompletionData (CSharpCompletionTextEditorExtension ext, ISymbol symbol, string text = null) : base ()
		{
			this.ext = ext;
			this.text = text;
			this.symbol = symbol;
		}
		
		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			return CreateTooltipInformation (ext.Document, Symbol, smartWrap);
		}
		
		public static TooltipInformation CreateTooltipInformation (MonoDevelop.Ide.Gui.Document doc, ISymbol entity, bool smartWrap, bool createFooter = false)
		{
			var tooltipInfo = new TooltipInformation ();
//			if (resolver == null)
//				resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
			var sig = new SignatureMarkupCreator (doc, doc != null ? doc.Editor.Caret.Offset : 0);
			sig.BreakLineAfterReturnType = smartWrap;
			try {
				tooltipInfo.SignatureMarkup = sig.GetMarkup (entity);
			} catch (Exception e) {
				LoggingService.LogError ("Got exception while creating markup for :" + entity, e);
				return new TooltipInformation ();
			}
			tooltipInfo.SummaryMarkup = AmbienceService.GetSummaryMarkup (entity) ?? "";
			
//			if (entity is IMember) {
//				var evt = (IMember)entity;
//				if (evt.ReturnType.Kind == TypeKind.Delegate) {
//					tooltipInfo.AddCategory (GettextCatalog.GetString ("Delegate Info"), sig.GetDelegateInfo (evt.ReturnType));
//				}
//			}
			if (entity is IMethodSymbol) {
				var method = (IMethodSymbol)entity;
				if (method.IsExtensionMethod) {
					tooltipInfo.AddCategory (GettextCatalog.GetString ("Extension Method from"), method.ContainingType.Name);
				}
			}
			if (createFooter) {
				tooltipInfo.FooterMarkup = sig.CreateFooter (entity);
			}
			return tooltipInfo;
		}
		
//		public static TooltipInformation CreateTooltipInformation (ICompilation compilation, CSharpUnresolvedFile file, TextEditorData textEditorData, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy formattingPolicy, IType type, bool smartWrap, bool createFooter = false)
//		{
//			var tooltipInfo = new TooltipInformation ();
//			var resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
//			var sig = new SignatureMarkupCreator (resolver, formattingPolicy.CreateOptions ());
//			sig.BreakLineAfterReturnType = smartWrap;
//			try {
//				tooltipInfo.SignatureMarkup = sig.GetMarkup (type.IsParameterized ? type.GetDefinition () : type);
//			} catch (Exception e) {
//				LoggingService.LogError ("Got exception while creating markup for :" + type, e);
//				return new TooltipInformation ();
//			}
//			if (type.IsParameterized) {
//				var typeInfo = new StringBuilder ();
//				for (int i = 0; i < type.TypeParameterCount; i++) {
//					typeInfo.AppendLine (type.GetDefinition ().TypeParameters [i].Name + " is " + sig.GetTypeReferenceString (type.TypeArguments [i]));
//				}
//				tooltipInfo.AddCategory ("Type Parameters", typeInfo.ToString ());
//			}
//
//			var def = type.GetDefinition ();
//			if (def != null) {
//				if (createFooter)
//					tooltipInfo.FooterMarkup = sig.CreateFooter (def);
//				tooltipInfo.SummaryMarkup = AmbienceService.GetSummaryMarkup (def) ?? "";
//			}
//			return tooltipInfo;
//		}
	}

}
