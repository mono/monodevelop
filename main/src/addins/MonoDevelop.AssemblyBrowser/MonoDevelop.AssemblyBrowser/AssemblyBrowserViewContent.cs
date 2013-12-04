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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Refactoring;
using System;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Projects;
using System.Linq;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyBrowserViewContent : AbstractViewContent, IOpenNamedElementHandler, INavigable
	{
		readonly static string[] defaultAssemblies = new string[] { "mscorlib", "System", "System.Core", "System.Xml" };
		AssemblyBrowserWidget widget;
		
		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			if (WorkbenchWindow != null) {
				var toolbar = WorkbenchWindow.GetToolbar (this);
				widget.SetToolbar (toolbar);
			}
		}

		public override Gtk.Widget Control {
			get {
				return widget;
			}
		}
		
		internal AssemblyBrowserWidget Widget {
			get {
				return widget;
			}
		}
		
		public AssemblyBrowserViewContent()
		{
			ContentName = GettextCatalog.GetString ("Assembly Browser");
			widget = new AssemblyBrowserWidget ();
			IsDisposed = false;
		}
		
		public override void Load (string fileName)
		{
			ContentName = GettextCatalog.GetString ("Assembly Browser");
			widget.AddReferenceByFileName (fileName);
		}
		
		public override bool IsFile {
			get {
				return false;
			}
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

		#region INavigable implementation 
		
		public NavigationPoint BuildNavigationPoint ()
		{
			return new AssemblyBrowserNavigationPoint ();
		}
		
		#endregion

		#region IUrlHandler implementation 
		
		public void Open (INamedElement element)
		{
			var member = element as IUnresolvedEntity;
			if (member == null) {
				var entity = element as IMember;
				if (entity != null)
					member = entity.UnresolvedMember;

			}
			if (member == null) {
				var entity = element as IType;
				if (entity != null)
					member = entity.GetDefinition ().Parts [0];
			}
			if (member == null)
				return;
			var url = AssemblyBrowserWidget.GetIdString (member); 
			widget.Open (url);
		}
		
		#endregion 

		[MonoDevelop.Components.Commands.CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.FindReferences)]
		public void FindReferences ()
		{
			var member = widget.ActiveMember as IMember;
			if (member == null)
				return;
			FindReferencesHandler.FindRefs (member);
		}
		
		[MonoDevelop.Components.Commands.CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.FindDerivedClasses)]
		public void FindDerivedClasses ()
		{
			var type = widget.ActiveMember as ITypeDefinition;
			if (type == null)
				return;
			FindDerivedClassesHandler.FindDerivedClasses (type);
		}

		public void FillWidget ()
		{
			if (Ide.IdeApp.ProjectOperations.CurrentSelectedSolution == null) {
				foreach (var assembly in defaultAssemblies) {
					Widget.AddReferenceByAssemblyName (assembly, assembly == defaultAssemblies [0]); 
				}
			} else {
				foreach (var project in Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
					Widget.AddProject (project, false);

					var netProject = project as DotNetProject;
					if (netProject == null)
						continue;
					foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
						if (!System.IO.File.Exists (file))
							continue;
						Widget.AddReferenceByFileName (file, false); 
					}
				}
			}
		}
	}

	class AssemblyBrowserNavigationPoint : NavigationPoint
	{
		Document DoShow ()
		{
			foreach (var view in Ide.IdeApp.Workbench.Documents) {
				if (view.GetContent<AssemblyBrowserViewContent> () != null) {
					view.Window.SelectWindow ();
					return view;
				}
			}

			var binding = DisplayBindingService.GetBindings<AssemblyBrowserDisplayBinding> ().FirstOrDefault ();
			var assemblyBrowserView = binding != null ? binding.GetViewContent () : new AssemblyBrowserViewContent ();
			assemblyBrowserView.FillWidget ();

			return Ide.IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
		}

		#region implemented abstract members of NavigationPoint

		public override void Show ()
		{
			DoShow ();
		}

		public override Document ShowDocument ()
		{
			return DoShow ();
		}

		public override string DisplayName {
			get {
				return GettextCatalog.GetString ("Assembly Browser");
			}
		}

		#endregion
	}
}
