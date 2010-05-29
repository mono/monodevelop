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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.CodeCompletion;
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
	}
	
	/// <summary>
	/// Embedded completion information calculated from the AspNetParsedDocument
	/// </summary>
	public class DocumentInfo
	{
		public DocumentInfo (AspNetParsedDocument aspNetParsedDocument, IEnumerable<string> imports)
		{
			this.AspNetDocument = aspNetParsedDocument;
			this.Imports = imports;
			ScriptBlocks = new List<TagNode> ();
			Expressions = new List<ExpressionNode> ();
			aspNetParsedDocument.RootNode.AcceptVisit (new ExpressionCollector (this));
		}		
		
		public AspNetParsedDocument AspNetDocument { get; private set; }
		public ParsedDocument ParsedDocument { get; set; }
		public List<ExpressionNode> Expressions { get; private set; }
		public List<TagNode> ScriptBlocks { get; private set; }
		public ProjectDom Dom { get; set; }
		public IEnumerable<string> Imports { get; private set; }
		
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
		
		LocalDocumentInfo BuildLocalDocument (DocumentInfo info, TextEditorData textEditorData, string expressionText, 
			bool isExpression);
		
		ICompletionDataList HandleCompletion (MonoDevelop.Ide.Gui.Document document, DocumentInfo info, 
			LocalDocumentInfo localInfo, char currentChar, ref int triggerWordLength);
		IParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document document, DocumentInfo info,
			LocalDocumentInfo localInfo, char completionChar);
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
}
