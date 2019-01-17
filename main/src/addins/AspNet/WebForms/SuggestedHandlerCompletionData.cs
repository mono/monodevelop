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
using System.Linq;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.AspNet.WebForms
{
	class SuggestedHandlerCompletionData : CompletionData
	{
		readonly MonoDevelop.Projects.Project project;
		readonly CodeMemberMethod methodInfo;
		readonly INamedTypeSymbol codeBehindClass;
		readonly Location codeBehindClassLocation;
		
		public SuggestedHandlerCompletionData (MonoDevelop.Projects.Project project, CodeMemberMethod methodInfo, INamedTypeSymbol codeBehindClass, Location codeBehindClassLocation)
		{
			this.project = project;
			this.methodInfo = methodInfo;
			this.codeBehindClass = codeBehindClass;
			this.codeBehindClassLocation = codeBehindClassLocation;
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
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			//insert the method name
			var buf = window.CompletionWidget;
			if (buf != null) {
				buf.Replace (window.CodeCompletionContext.TriggerOffset, buf.CaretOffset - window.CodeCompletionContext.TriggerOffset, methodInfo.Name);
			}
			
			//generate the codebehind method

			// TODO: Roslyn port.
//			if (codeBehindClassLocation != null && project != null)
//				BindingService.AddMemberToClass (project, codeBehindClass, codeBehindClassLocation, methodInfo, false);
//			else
//				BindingService.AddMemberToClass (project, codeBehindClass, codeBehindClass.Locations.First (), methodInfo, false);
		}
	}
}
