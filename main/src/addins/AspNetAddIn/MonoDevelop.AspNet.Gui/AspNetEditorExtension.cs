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
			return Array.IndexOf (supportedExtensions, System.IO.Path.GetExtension (doc.Title)) > -1;
		}
		
		protected Document GetAspNetDocument ()
		{
			if (Document == null)
				throw new InvalidOperationException ("Editor extension not yet initialized");
			AspNetAppProject proj = Document.Project as AspNetAppProject;
			if (proj != null) {
				ProjectFile pf = proj.ProjectFiles.GetFile (Document.FileName);
				return proj.GetDocument (pf);
			} else {
				throw new NotImplementedException ("ASP.NET code completion currently only works within projects");
			}
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			Document doc = GetAspNetDocument ();
			
			//FIXME: rows in completionContext is zero-indexed, ILocation is 1-indexed. This could easily cause bugs.
			Node n = doc.RootNode.GetNodeAtPosition (completionContext.TriggerLine + 1, completionContext.TriggerLineOffset);
			if (n != null)
				MonoDevelop.Core.LoggingService.LogDebug ("AspNetCompletion({0},{1}): {2}",
				    completionContext.TriggerLine + 1,
				    completionContext.TriggerLineOffset,
				    n.ToString ());
			
			if (completionChar == '<')
				return GetHtmlCompletionData ();
			return base.HandleCodeCompletion (completionContext, completionChar); 
		}

		//TODO: implement different HTML versions
		//maybe get them from the web type manager
		CodeCompletionDataProvider GetHtmlCompletionData ()
		{
			string[] tags = {"html", "a", "em", "meta", "strong", "div", "span", "head", "title", "meta"};
			CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
			foreach (string tag in tags)
				cp.AddCompletionData (new CodeCompletionData (tag, "md-literal"));
			return cp;
		}
	}
}
