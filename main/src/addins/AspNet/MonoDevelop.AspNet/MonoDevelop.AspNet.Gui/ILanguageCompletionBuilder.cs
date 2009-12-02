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
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;
using S = MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;
using System.Text;

namespace MonoDevelop.AspNet.Gui
{
	public class LocalDocumentInfo {
		public string LocalDocument {
			get;
			set;
		}
		
		public ParsedDocument ParsedLocalDocument {
			get;
			set;
		}
	}
	
	public class DocumentInfo {
		public MonoDevelop.AspNet.Parser.AspNetParsedDocument AspNetParsedDocument {
			get;
			set;
		}
		
		public ParsedDocument ParsedDocument {
			get;
			set;
		}
		
		List<KeyValuePair<ILocation, string>> expressions = new List<KeyValuePair<ILocation, string>> ();
		public List<KeyValuePair<ILocation, string>> Expressions {
			get {
				return expressions;
			}
		}
	}
	
	public interface ILanguageCompletionBuilder 
	{
		bool SupportsLanguage (string language);
		
		DocumentInfo BuildDocument (AspNetParsedDocument aspDocument);
		LocalDocumentInfo BuildLocalDocument (DocumentInfo info, ILocation location, string expressionText, bool isExpression);
		
		ICompletionDataList HandleCompletion (MonoDevelop.Ide.Gui.Document document, ProjectDom currentDom, char currentChar, ref int triggerWordLength);
		IParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document document, ProjectDom currentDom, char completionChar);
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
					Console.WriteLine (args.ExtensionObject);
					Console.WriteLine (args.ExtensionNode);
					builder.Add ((ILanguageCompletionBuilder)args.ExtensionObject);
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
