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
			if (Document == null)
				throw new InvalidOperationException ("Editor extension not yet initialized");
			ITextBuffer buffer = (ITextBuffer) Document.GetContent (typeof(ITextBuffer));
			if (buffer != null)
				return new Document (buffer, Document.Project, FileName);
			else
				throw new Exception ("Cannot retrieve ITextBuffer");
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			Document doc;
			try {
				doc = GetAspNetDocument ();
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error in ASP.NET parser", ex);
				return base.HandleCodeCompletion (completionContext, completionChar);
			}
			
			HtmlSchema schema = HtmlSchemaService.GetSchema (doc.Info.DocType);
			
			//FIXME: lines in completionContext are zero-indexed, but ILocation is 1-indexed. This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			Node n = doc.RootNode.GetNodeAtPosition (line, col);
			if (n != null) {
				MonoDevelop.Core.LoggingService.LogDebug ("AspNetCompletion({0},{1}): {2}",
				    completionContext.TriggerLine + 1,
				    completionContext.TriggerLineOffset,
				    n.ToString ());
			}
			
			if (completionChar == '<') {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				TagNode parent = null;
				if (n != null)
					parent = n.Parent as TagNode;
				
				AddHtmlTagCompletionData (cp, schema, parent);
				AddAspBeginExpressions (cp);
				return cp;
			}
			
			TagNode tn = n as TagNode;
			
			//attributes within tags
			if  (completionChar == ' ' && tn != null && tn.LocationContainsPosition (line, col) == 0) {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				AddHtmlAttributeCompletionData (cp, schema, tn);
				if (tn.Attributes ["runat"] == null)
					cp.AddCompletionData (new CodeCompletionData ("runat=\"server\"", "md-literal"));
				return cp;
			}
				
			
			return base.HandleCodeCompletion (completionContext, completionChar); 
		}
		
		void AddHtmlTagCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, TagNode parentTag)
		{
			string parentTagId = string.Empty;
			if (parentTag != null)
				parentTagId = parentTag.TagName;
			foreach (string tag in schema.GetValidChildren (parentTagId))
				provider.AddCompletionData (new CodeCompletionData (tag, "md-literal"));			
		}
		
		void AddHtmlAttributeCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, TagNode parentTag)
		{
			string parentTagId = string.Empty;
				parentTagId = parentTag.TagName;
			//add atts only if they're not aready in the tag
			foreach (string att in schema.GetValidAttributes (parentTagId)) {
				if (parentTag != null && parentTag.Attributes != null && parentTag.Attributes[att] == null)
					provider.AddCompletionData (new CodeCompletionData (att, "md-literal"));
			}
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
