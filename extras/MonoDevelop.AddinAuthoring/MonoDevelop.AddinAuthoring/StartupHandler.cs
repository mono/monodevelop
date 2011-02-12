// 
// StartupHandler.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	[Extension ("/MonoDevelop/Ide/StartupHandlers", NodeName="Class")]
	public class StartupHandler: CommandHandler
	{
		protected override void Run ()
		{
			Ide.IdeApp.Workbench.DocumentOpened += HandleDocumentOpened;
		}
		
		void HandleDocumentOpened (object sender, DocumentEventArgs e)
		{
			if (!(e.Document.Project is DotNetProject) || !e.Document.IsFile)
				return;
			string ext = e.Document.FileName;
			if (!ext.EndsWith (".addin.xml") && !ext.EndsWith (".addin"))
				return;
			
			var data = AddinData.GetAddinData ((DotNetProject)e.Document.Project);
			if (data != null) {
				IWorkbenchWindow window = e.Document.Window;
				var adesc = data.AddinRegistry.ReadAddinManifestFile (e.Document.FileName);
				
				window.AttachViewContent (new ExtensionEditorView (adesc, data));
				window.AttachViewContent (new ExtensionPointsEditorView (adesc, data));
			}
			
		}
	}
}

