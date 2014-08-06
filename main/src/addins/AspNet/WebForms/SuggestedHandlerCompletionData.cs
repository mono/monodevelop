// 
// SuggestedHandlerCompletionData.cs
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
using System.CodeDom;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.DesignerSupport;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace MonoDevelop.AspNet.WebForms
{
	class SuggestedHandlerCompletionData : CompletionData
	{
		readonly Project project;
		readonly CodeMemberMethod methodInfo;
		readonly IType codeBehindClass;
		readonly IUnresolvedTypeDefinition codeBehindClassPart;
		
		public SuggestedHandlerCompletionData (Project project, CodeMemberMethod methodInfo, IType codeBehindClass, IUnresolvedTypeDefinition codeBehindClassPart)
		{
			this.project = project;
			this.methodInfo = methodInfo;
			this.codeBehindClass = codeBehindClass;
			this.codeBehindClassPart = codeBehindClassPart;
		}
		
		public override IconId Icon {
			get { return "md-method"; }
		}

		public override string DisplayText {
			get { return methodInfo.Name; }
		}
		
		public override string CompletionText {
			get { return methodInfo.Name; }
		}

		public override string Description {
			get {
				//NOTE: code completion window emphasises first line, so is translated separately
				return GettextCatalog.GetString ("A suggested event handler method name.\n") +
					GettextCatalog.GetString (
					    "If you accept this suggestion, the method will\n" + 
					    "be generated in the CodeBehind class.");
			}
		}
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			//insert the method name
			MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buf = window.CompletionWidget as MonoDevelop.Ide.Gui.Content.IEditableTextBuffer;
			if (buf != null) {
				using (var undo = buf.OpenUndoGroup ()) {
					buf.DeleteText (window.CodeCompletionContext.TriggerOffset, buf.CursorPosition - window.CodeCompletionContext.TriggerOffset);
					buf.InsertText (buf.CursorPosition, methodInfo.Name);
				}
			}
			
			//generate the codebehind method
			if (codeBehindClassPart != null && project != null)
				BindingService.AddMemberToClass (project, codeBehindClass.GetDefinition (), codeBehindClassPart, methodInfo, false);
			else
				BindingService.AddMemberToClass (project, codeBehindClass.GetDefinition (), codeBehindClass.GetDefinition ().Parts.First (), methodInfo, false);
		}
	}
}
