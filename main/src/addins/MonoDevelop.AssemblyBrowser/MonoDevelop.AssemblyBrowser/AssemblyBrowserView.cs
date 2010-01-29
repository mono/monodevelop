//
// AssemblyBrowserView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using Mono.Cecil;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Refactoring;

namespace MonoDevelop.AssemblyBrowser
{
	public class AssemblyBrowserViewContent : AbstractViewContent, MonoDevelop.Ide.Gui.Content.IUrlHandler
	{
		AssemblyBrowserWidget widget;
		
		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		public AssemblyBrowserViewContent()
		{
			widget = new AssemblyBrowserWidget ();
			IsDisposed = false;
		}
		
		public override void Load (string fileName)
		{
			this.ContentName = MonoDevelop.Core.GettextCatalog.GetString ("Assembly Browser");
			widget.AddReference (fileName);
		}
		
		public bool IsDisposed {
			get;
			private set;
		}
		
		public override void Dispose ()
		{ 
			IsDisposed = true;
			base.Dispose ();
			widget = null;
			GC.Collect ();
		}
		
		#region IUrlHandler implementation 
		
		public void Open (string url)
		{
			widget.Open (url);
		}
		
		#endregion 
		
		[MonoDevelop.Components.Commands.CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.FindReferences)]
		public void FindReferences ()
		{
			IMember member = widget.ActiveMember;
			if (member == null)
				return;
			ProjectDom dom = ProjectDomService.GetProjectDom (IdeApp.ProjectOperations.CurrentSelectedProject);
			if (dom == null)
				return;
			Refactorer refactorer = new Refactorer (dom, null, null, member, null);
			refactorer.FindReferences ();
		}
		
		[MonoDevelop.Components.Commands.CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.FindDerivedClasses)]
		public void FindDerivedClasses ()
		{
			IMember member = widget.ActiveMember;
			if (member == null)
				return;
			ProjectDom dom = ProjectDomService.GetProjectDom (IdeApp.ProjectOperations.CurrentSelectedProject);
			if (dom == null)
				return;
			Refactorer refactorer = new Refactorer (dom, null, null, member, null);
			refactorer.FindDerivedClasses ();
		}
	}
}
