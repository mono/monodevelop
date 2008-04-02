// 
// AspNetEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;

namespace MonoDevelop.AspNet.Gui
{
	
	
	public class AspNetEditorExtension : CompletionTextEditorExtension
	{
		
		public AspNetEditorExtension () : base ()
		{
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			string[] supportedExtensions = {".aspx", ".ascx", ".master"};
			return (doc.Project is AspNetAppProject) && Array.IndexOf (supportedExtensions, System.IO.Path.GetExtension (doc.Title)) > -1;
		}
		
		protected Document GetAspNetDocument ()
		{
			return new Document (Buffer, Document.Project, FileName);
		}
		
		protected ITextBuffer Buffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return (ITextBuffer) Document.GetContent (typeof(ITextBuffer));
			}
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			//FIXME: lines in completionContext are zero-indexed, but ILocation and buffer are 1-indexed. This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			ITextBuffer buf = this.Buffer;
			
			//FIXME: the char from completionChar isn't converted 100% accurately from GDK, so better to fetch from buffer
			// and the TriggerOffset seems unreliable too
			int currentPosition = buf.CursorPosition - 1;
			char currentChar = buf.GetCharAt (currentPosition);
			char previousChar = buf.GetCharAt (currentPosition - 1);
			
			//don't trigger on arrow keys
			if (currentChar != completionChar)
				return base.HandleCodeCompletion (completionContext, currentChar);
			
			//doctype completion
			if (line <= 5 && currentChar ==' ' && previousChar == 'E') {
				int start = currentPosition - 9;
				if (start >= 0) {
					string readback = Buffer.GetText (start, currentPosition);
					if (string.Compare (readback, "<!DOCTYPE", System.StringComparison.InvariantCulture) == 0)
						return new DocTypeCompletionDataProvider ();
				}
			}
			
			//decide whether completion will be activated, to avoid unnecessary
			//parsing, which hurts editor responsiveness
			switch (currentChar) {
			case '>':
			case '<':
				break;
			case ' ':
				if (!char.IsWhiteSpace (previousChar))
					break;
				else
					goto default;
			case '"':
			case '\'':
				if (previousChar == '=' || char.IsWhiteSpace (previousChar))
					break;
				else
					goto default;
			default:
				return base.HandleCodeCompletion (completionContext, currentChar);
			}
			
			Document doc;
			try {
				doc = GetAspNetDocument ();
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error in ASP.NET parser", ex);
				return base.HandleCodeCompletion (completionContext, currentChar);
			}
			
			Node n = doc.RootNode.GetNodeAtPosition (line, col);
			LoggingService.LogDebug ("AspNetCompletion({0},{1}): {2}", line, col, n==null? "(not found)" : n.ToString ());
			
			HtmlSchema schema = HtmlSchemaService.GetSchema (doc.Info.DocType);
			if (schema == null)
				schema = HtmlSchemaService.DefaultDocType;
			
			if (currentChar == '<') {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				TagNode parent = null;
				if (n != null)
					parent = n.Parent as TagNode;
				
				AddHtmlTagCompletionData (cp, schema, parent);
				AddAspBeginExpressions (cp);
				
				if (line < 4 && string.IsNullOrEmpty (doc.Info.DocType))
					cp.AddCompletionData (new CodeCompletionData ("!DOCTYPE", "md-literal"));
				return cp;
			}
			
			//closing tag completion
			if (currentPosition - 1 > 0 && currentChar == '>') {
				//get previous node in document
				int linePrev, colPrev;
				buf.GetLineColumnFromPosition (currentPosition - 1, out linePrev, out colPrev);
				TagNode tnPrev = doc.RootNode.GetNodeAtPosition (linePrev, colPrev) as TagNode;
				LoggingService.LogDebug ("AspNetCompletionPrev({0},{1}): {2}", linePrev, colPrev, tnPrev==null? "(not found)" : tnPrev.ToString ());
				
				if (tnPrev != null && !string.IsNullOrEmpty (tnPrev.TagName) && !tnPrev.IsClosed) {
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					cp.AddCompletionData (
					    new MonoDevelop.XmlEditor.Completion.ClosingBracketCompletionData (
					        String.Concat ("</", tnPrev.TagName, ">"), currentPosition)
					    );
					return cp;
				}
			}
			
			//attributes within tags
			MonoDevelop.AspNet.Parser.Dom.TagNode tn = n as TagNode;
			if (tn != null && tn.LocationContainsPosition (line, col) == 0) {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				
				//attributes
				if (currentChar == ' ') {
					AddHtmlAttributeCompletionData (cp, schema, tn);
					if (tn.Attributes ["runat"] == null)
						cp.AddCompletionData (new CodeCompletionData ("runat=\"server\"", "md-literal"));
					return cp;
				
				//attribute values
				} else if ((currentChar == '"' || currentChar == '\'')
				    && currentPosition + 1 < buf.Length && buf.GetCharAt (currentPosition + 1) == currentChar)
				{
					string att = GetAttributeName (buf, currentPosition);
					if (!string.IsNullOrEmpty (att))
						AddHtmlAttributeValueCompletionData (cp, schema, tn, att);
				}
				return cp;
			}
			
			return base.HandleCodeCompletion (completionContext, currentChar); 
		}
		
		static string GetAttributeName (ITextBuffer buf, int offset)
		{
			int start = -1, end = -1;
			int i = offset;
			for (; i > 0; i++) {
				char c = buf.GetCharAt (i);
				if (!char.IsWhiteSpace (c) && c != '=') {
					end = i;
					break;
				}
			}
			i++;
			for (; i > 0; i++) {
				char c = buf.GetCharAt (i);
				if (!char.IsLetterOrDigit (c)) {
					start = i;
					break;
				}
			}
			
			if (end - start > 0)
				return (buf.GetText (start, end));
			return null;
		}
		
		void AddHtmlTagCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, TagNode parentTag)
		{
			if (schema == null || schema.CompletionProvider == null)
				return;
			
			ICompletionData[] data = null;
			if (parentTag == null || string.IsNullOrEmpty (parentTag.TagName)) {
				data = schema.CompletionProvider.GetElementCompletionData ();
			} else {
				data = schema.CompletionProvider.GetChildElementCompletionData (parentTag.TagName);
			}
			
			foreach (ICompletionData datum in data)
				provider.AddCompletionData (datum);			
		}
		
		void AddHtmlAttributeCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, TagNode parentTag)
		{
			if (schema.CompletionProvider == null || parentTag == null || string.IsNullOrEmpty (parentTag.TagName))
				return;
			
			//add atts only if they're not aready in the tag
			foreach (ICompletionData datum in schema.CompletionProvider.GetAttributeCompletionData (parentTag.TagName))
				if (parentTag != null && parentTag.Attributes != null && parentTag.Attributes[datum.Text[0]] == null)
					provider.AddCompletionData (datum);
		}
		
		void AddHtmlAttributeValueCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, TagNode parentTag, string attributeName)
		{
			if (schema.CompletionProvider == null || parentTag == null || string.IsNullOrEmpty (parentTag.TagName))
				return;
			
			//add atts only if they're not aready in the tag
			foreach (ICompletionData datum in schema.CompletionProvider.GetAttributeValueCompletionData (parentTag.TagName, attributeName))
				provider.AddCompletionData (datum);
		}
		
		void AddAspBeginExpressions (CodeCompletionDataProvider provider)
		{
			provider.AddCompletionData (new CodeCompletionData ("%", "md-literal"));  //render block
			provider.AddCompletionData (new CodeCompletionData ("%=", "md-literal")); //render expression
			provider.AddCompletionData (new CodeCompletionData ("%@", "md-literal")); //directive
			provider.AddCompletionData (new CodeCompletionData ("%#", "md-literal")); //databinding
			provider.AddCompletionData (new CodeCompletionData ("%$", "md-literal")); //resource FIXME 2.0 only
		}
	}
}
