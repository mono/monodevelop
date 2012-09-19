//
// PropertyPadVisitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport
{
	class PropertyPadVisitor: ICommandTargetVisitor
	{
		// Set to true when a property pad provider is found
		bool found;

		// Set to true if the current active document has been visited
		bool visitedCurrentDoc;

		// Set to true if the current active document is being visited
		// on a second try
		bool visitingCurrentDoc;

		public void Start ()
		{
			found = false;
			visitedCurrentDoc = false;
		}

		public void End ()
		{
			if (!found) {
				if (!visitingCurrentDoc && !visitedCurrentDoc) {
					// A provider has not been found, but the current document has not been visited
					// (the focus may be for example in a pad).
					// Try visiting the command route again, but this time starting at the currently
					// active document. The visitingCurrentDoc flag is used to avoid entering in a loop.
					var wb = (DefaultWorkbench)IdeApp.Workbench.RootWindow;
					if (wb.ActiveWorkbenchWindow != null) {
						visitingCurrentDoc = true;
						IdeApp.CommandService.VisitCommandTargets (this, wb.ActiveWorkbenchWindow);
						visitingCurrentDoc = false;
						// All done, VisitCommandTargets will set the final state of the pad
						return;
					}
				}
				DesignerSupport.Service.ReSetPad ();
			}
		}

		public bool Visit (object ob)
		{
			if (ob == ((DefaultWorkbench)IdeApp.Workbench.RootWindow).ActiveWorkbenchWindow)
				visitedCurrentDoc = true;

			if (ob is PropertyPad) {
				// Don't change the property grid selection when the focus is inside the property grid itself
				found = true;
				return true;
			}
			else if (ob is IPropertyPadProvider) {
				DesignerSupport.Service.SetPadContent ((IPropertyPadProvider)ob);
				found = true;
				return true;
			}
			else if (ob is ICustomPropertyPadProvider) {
				DesignerSupport.Service.SetPadContent ((ICustomPropertyPadProvider)ob);
				found = true;
				return true;
			}
			else
				return false;
		}
	}
}
