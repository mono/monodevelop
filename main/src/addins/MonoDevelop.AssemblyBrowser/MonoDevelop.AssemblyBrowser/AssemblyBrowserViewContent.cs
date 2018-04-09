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
using System;
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyBrowserViewContent : ViewContent, IOpenNamedElementHandler, INavigable
	{
		readonly static string[] defaultAssemblies = new string[] { "mscorlib", "System", "System.Core", "System.Xml" };
		AssemblyBrowserWidget widget;
		
		protected override void OnWorkbenchWindowChanged ()
		{
			base.OnWorkbenchWindowChanged ();
			if (WorkbenchWindow != null) {
				var toolbar = WorkbenchWindow.GetToolbar (this);
				widget.SetToolbar (toolbar);
			}
		}

		public override Control Control {
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
		
		public override Task Load (FileOpenInformation fileOpenInformation)
		{
			ContentName = GettextCatalog.GetString ("Assembly Browser");
			var loader = widget.AddReferenceByFileName (fileOpenInformation.FileName);
			if (loader == null)
				return Task.FromResult (true);
			loader.LoadingTask.ContinueWith (delegate {
				widget.SelectAssembly (loader);
			});
			return Task.FromResult (true);
		}

		internal void EnsureDefinitionsLoaded (List<AssemblyLoader> definitions)
		{
			widget.EnsureDefinitionsLoaded (definitions);
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
			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
		}

		internal event EventHandler Disposed;

		#region INavigable implementation 
		
		public NavigationPoint BuildNavigationPoint ()
		{
			return widget.BuildNavigationPoint ();
		}
		
		#endregion

		#region IUrlHandler implementation 
		
		public void Open (Microsoft.CodeAnalysis.ISymbol element, bool expandNode = true)
		{
			var url = element.OriginalDefinition.GetDocumentationCommentId ();//AssemblyBrowserWidget.GetIdString (member); 
			if (element.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public)
				widget.PublicApiOnly = false;
			widget.Open (url, expandNode: expandNode);
		}

		public void Open (string documentationCommentId, bool openInPublicOnlyMode = true, bool expandNode = true)
		{
			if (!openInPublicOnlyMode)
				widget.PublicApiOnly = false;
			widget.Open (documentationCommentId, expandNode: expandNode);
		}

		#endregion 

		[MonoDevelop.Components.Commands.CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.FindReferences)]
		public void FindReferences ()
		{
			var member = widget.ActiveMember as IMember;
			if (member == null)
				return;
			// FindReferencesHandler.FindRefs (member);
		}
		
		[MonoDevelop.Components.Commands.CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.FindDerivedClasses)]
		public void FindDerivedClasses ()
		{
			var type = widget.ActiveMember as ITypeDefinition;
			if (type == null)
				return;
			//FindDerivedClassesHandler.FindDerivedClasses (type);
		}

		public async void FillWidget ()
		{
			if (Ide.IdeApp.ProjectOperations.CurrentSelectedSolution == null) {
				foreach (var assembly in defaultAssemblies) {
					Widget.AddReferenceByAssemblyName (assembly); 
				}
			} else {
				foreach (var project in Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
					Widget.AddProject (project, false);

					var netProject = project as DotNetProject;
					if (netProject == null)
						continue;
					foreach (var file in await netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
						if (!System.IO.File.Exists (file.FilePath))
							continue;
						Widget.AddReferenceByFileName (file.FilePath); 
					}
				}
			}
		}
	}


}
