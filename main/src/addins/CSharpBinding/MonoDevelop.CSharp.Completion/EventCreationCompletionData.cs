// EventCreationCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Text;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Util;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory6.CSharp;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	class EventCreationCompletionData : AnonymousMethodCompletionData
	{
		readonly RoslynCodeCompletionFactory factory;
		readonly ITypeSymbol delegateType;
		readonly INamedTypeSymbol curType;
		readonly string varName;
		
		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tooltipInfo = new TooltipInformation ();
			return tooltipInfo;
		}

		public EventCreationCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, RoslynCodeCompletionFactory factory, ITypeSymbol delegateType, string varName, INamedTypeSymbol curType) : base (keyHandler)
		{
			this.curType = curType;
			this.varName = varName;
			this.DisplayText = varName;
			this.delegateType = delegateType;
			this.factory = factory;
			this.Icon = "md-newmethod";
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			// insert add/remove event handler code after +=/-=
			var editor = factory.Ext.Editor;


			bool AddSemicolon = true;
			var position = window.CodeCompletionContext.TriggerOffset;
			editor.ReplaceText (position, editor.CaretOffset - position, this.DisplayText + (AddSemicolon ? ";" : ""));


			var document = IdeApp.Workbench.ActiveDocument;
			var parsedDocument = document.UpdateParseDocument ();
			var semanticModel = parsedDocument.GetAst<SemanticModel> ();

			var declaringType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol> (position, default(CancellationToken));
			var enclosingSymbol = semanticModel.GetEnclosingSymbol<ISymbol> (position, default(CancellationToken));

			var insertionPoints = InsertionPointService.GetInsertionPoints (
				document.Editor,
				parsedDocument,
				declaringType,
				declaringType.Locations.First()
			);

			var options = new InsertionModeOptions (
				"Create new method",
				insertionPoints,
				async point => {
					if (!point.Success) 
						return;
					var indent = "\t";
					var sb = new StringBuilder ();
					if (enclosingSymbol != null && enclosingSymbol.IsStatic)
						sb.Append ("static ");
					sb.Append ("void ");
					int pos2 = sb.Length;
					sb.Append (this.DisplayText);
					sb.Append (' ');
					sb.Append("(");

					var delegateMethod = delegateType.GetDelegateInvokeMethod();
					for (int k = 0; k < delegateMethod.Parameters.Length; k++) {
						if (k > 0) {
							sb.Append(", ");
						}
						sb.Append (delegateMethod.Parameters [k].ToMinimalDisplayString (semanticModel, position, Ambience.LabelFormat)); 
					}
					sb.Append(")");

					sb.Append (editor.EolMarker);
					sb.Append (indent);
					sb.Append ("{");
					sb.Append (editor.EolMarker);
					sb.Append (indent);
					sb.Append (editor.Options.GetIndentationString ());
					//int cursorPos = pos + sb.Length;
					sb.Append (editor.EolMarker);
					sb.Append (indent);
					sb.Append ("}");
					point.InsertionPoint.Insert (document.Editor, document, sb.ToString ());
					//			// start text link mode after insert
					//			var links = new List<TextLink> ();
					//			var link = new TextLink ("name");
					//			
					//			link.AddLink (new TextSegment (initialOffset, this.DisplayText.Length));
					//			link.AddLink (new TextSegment (initialOffset + pos + pos2, this.DisplayText.Length));
					//			links.Add (link);
					//			editor.StartTextLinkMode (new TextLinkModeOptions (links));
				}
			);

			editor.StartInsertionMode (options);

		}
	}
	

}
