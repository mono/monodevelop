// 
// ILanguageDocumentBuilder.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;
using S = MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;
using System.Text;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.Completion;

namespace MonoDevelop.AspNet.Gui
{
	
	/// <summary>
	/// Embedded local region completion information for each keystroke
	/// </summary>
	public class LocalDocumentInfo
	{
		public string LocalDocument { get; set; }
		public ParsedDocument ParsedLocalDocument { get; set; }
		public int CaretPosition { get; set; }
		public int OriginalCaretPosition { get; set; }
		public HiddenDocument HiddenDocument { get; set; }
		
		public List<OffsetInfo> OffsetInfos = new List<OffsetInfo> ();
		
		public class OffsetInfo 
		{
			public int FromOffset {
				get;
				private set;
			}

			public int ToOffset {
				get;
				private set;
			}

			public int Length {
				get;
				private set;
			}
			
			public OffsetInfo (int fromOffset, int toOffset, int length)
			{
				this.FromOffset = fromOffset;
				this.ToOffset = toOffset;
				this.Length = length;
			}
		}
		
		public void AddTextPosition (int fromOffset, int toOffset, int length)
		{
			OffsetInfos.Add (new OffsetInfo (fromOffset, toOffset, length));
		}
	}
	
	/// <summary>
	/// Embedded completion information calculated from the AspNetParsedDocument
	/// </summary>
	public class DocumentInfo
	{
		public DocumentInfo (ICompilation dom, AspNetParsedDocument aspNetParsedDocument, IEnumerable<string> imports,
		                     IList<ICompilation> references)
		{
			this.Dom = dom;
			this.AspNetDocument = aspNetParsedDocument;
			this.Imports = imports;
			this.References = references;
			ScriptBlocks = new List<TagNode> ();
			Expressions = new List<ExpressionNode> ();
			aspNetParsedDocument.RootNode.AcceptVisit (new ExpressionCollector (this));
		}
		
		public ICompilation Dom { get; private set; }
		public AspNetParsedDocument AspNetDocument { get; private set; }
		public ParsedDocument ParsedDocument { get; set; }
		public List<ExpressionNode> Expressions { get; private set; }
		public List<TagNode> ScriptBlocks { get; private set; }
		public IList<ICompilation> References { get; set; }
		public IEnumerable<string> Imports { get; private set; }
		
		public IUnresolvedTypeDefinition CodeBesideClass { get; set; }
		
		public string BaseType {
			get {
				return string.IsNullOrEmpty (AspNetDocument.Info.InheritedClass)?
					GetDefaultBaseClass (AspNetDocument.Type) : AspNetDocument.Info.InheritedClass;
			}
		}
		
		public string ClassName  {
			get {
				return string.IsNullOrEmpty (AspNetDocument.Info.ClassName)?
					"Generated" : AspNetDocument.Info.ClassName;
			}
		}
		
		static string GetDefaultBaseClass (MonoDevelop.AspNet.WebSubtype type)
		{
			switch (type) {
			case MonoDevelop.AspNet.WebSubtype.WebForm:
				return "System.Web.UI.Page";
			case MonoDevelop.AspNet.WebSubtype.MasterPage:
				return "System.Web.UI.MasterPage";
			case MonoDevelop.AspNet.WebSubtype.WebControl:
				return "System.Web.UI.UserControl";
			}
			throw new InvalidOperationException (string.Format ("Unexpected filetype '{0}'", type));
		}
		
		class ExpressionCollector : Visitor
		{
			DocumentInfo parent;
			
			public ExpressionCollector (DocumentInfo parent)
			{
				this.parent = parent;
			}
			
			public override void Visit (TagNode node)
			{
				if (node.TagName == "script" && (string)node.Attributes["runat"] == "server")
					parent.ScriptBlocks.Add (node);
			}
			
			public override void Visit (ExpressionNode node)
			{
				parent.Expressions.Add (node);
			}
		}
	}
	
	/// <summary>
	/// Code completion for languages embedded in ASP.NET documents
	/// </summary>
	public interface ILanguageCompletionBuilder 
	{
		bool SupportsLanguage (string language);
		
		ParsedDocument BuildDocument (DocumentInfo info, TextEditorData textEditorData);
		
		ICompletionWidget CreateCompletionWidget (MonoDevelop.Ide.Gui.Document realDocument, LocalDocumentInfo localInfo);
		
		LocalDocumentInfo BuildLocalDocument (DocumentInfo info, TextEditorData textEditorData, string expressionText, string textAfterCaret, bool isExpression);
		
		ICompletionDataList HandlePopupCompletion (MonoDevelop.Ide.Gui.Document realDocument, DocumentInfo info, LocalDocumentInfo localInfo);
		ICompletionDataList HandleCompletion (MonoDevelop.Ide.Gui.Document realDocument, CodeCompletionContext completionContext, DocumentInfo info, LocalDocumentInfo localInfo, char currentChar, ref int triggerWordLength);
		ParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document realDocument, CodeCompletionContext completionContext, DocumentInfo info, LocalDocumentInfo localInfo, char completionChar);
		bool GetParameterCompletionCommandOffset (MonoDevelop.Ide.Gui.Document realDocument, DocumentInfo info, LocalDocumentInfo localInfo, out int cpos);
	}
	
	public static class LanguageCompletionBuilderService
	{
		static List<ILanguageCompletionBuilder> builder = new List<ILanguageCompletionBuilder> ();
		
		public static IEnumerable<ILanguageCompletionBuilder> Builder {
			get {
				return builder;
			}
		}
		
		static LanguageCompletionBuilderService ()
		{
			Mono.Addins.AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Asp/CompletionBuilders", delegate(object sender, Mono.Addins.ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case Mono.Addins.ExtensionChange.Add:
					builder.Add ((ILanguageCompletionBuilder)args.ExtensionObject);
					break;
				case Mono.Addins.ExtensionChange.Remove:
					builder.Remove ((ILanguageCompletionBuilder)args.ExtensionObject);
					break;
				}
			});
		}
		
		public static ILanguageCompletionBuilder GetBuilder (string language)
		{
			foreach (ILanguageCompletionBuilder b in Builder) {
				if (b.SupportsLanguage (language))
					return b;
			}
			return null;
		}
	}
	
	public class HiddenDocument : MonoDevelop.Ide.Gui.Document
	{
		internal ParsedDocument HiddenParsedDocument;
		public override ParsedDocument ParsedDocument {
			get {
				return HiddenParsedDocument;
			}
		}
		
		public HiddenDocument (MonoDevelop.Ide.Gui.IWorkbenchWindow window) : base(window)
		{
		}
	}
}
