﻿//
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
using System.Collections.Immutable;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis.CodeRefactorings;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyBrowserViewContent : DocumentController, IOpenNamedElementHandler, INavigable
	{
		readonly static string[] defaultAssemblies = new string[] { "mscorlib", "System", "System.Core", "System.Xml" };
		AssemblyBrowserWidget widget;
		CancellationTokenSource cts = new CancellationTokenSource ();

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			widget.SetToolbar (view.GetToolbar ());
			return widget;
		}

		internal AssemblyBrowserWidget Widget {
			get {
				return widget;
			}
		}
		
		public AssemblyBrowserViewContent()
		{
			DocumentTitle = GettextCatalog.GetString ("Assembly Browser");
			widget = new AssemblyBrowserWidget ();
			FillWidget ();
		}

		protected override Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			if (modelDescriptor is FileDescriptor fileDescriptor) {
				Load (fileDescriptor.FilePath);
			}

			return Task.CompletedTask;
		}

		protected override bool OnTryReuseDocument (ModelDescriptor modelDescriptor)
		{
			// This descriptor is provided when using the Tools menu command to open the assembly browser
			if (modelDescriptor is AssemblyBrowserDescriptor)
				return true;

			// Opening an assembly, the assembly browser can handle it
			if (modelDescriptor is FileDescriptor file && (file.FilePath.HasExtension (".dll") || file.FilePath.HasExtension (".exe"))) {
				Load (file.FilePath);
				return true;
			}
			return base.OnTryReuseDocument (modelDescriptor);
		}

		public void Load (FilePath filePath)
		{
			var loader = widget.AddReferenceByFileName (filePath);
			if (loader != null) {
				loader.LoadingTask
					.ContinueWith (t => widget.SelectAssembly (t.Result), Runtime.MainTaskScheduler)
					.Ignore ();
			}
		}

		internal void EnsureDefinitionsLoaded (ImmutableList<AssemblyLoader> definitions)
		{
			widget.EnsureDefinitionsLoaded (definitions);
		}

		protected override void OnDispose ()
		{
			if (cts != null) {
				cts.Cancel ();
				cts.Dispose ();
				cts = null;
			}

			widget = null;
			Disposed?.Invoke (this, EventArgs.Empty);
			base.OnDispose ();
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

		public void FillWidget ()
		{
			if (Ide.IdeApp.ProjectOperations.CurrentSelectedSolution == null) {
				foreach (var assembly in defaultAssemblies) {
					Widget.AddReferenceByAssemblyName (assembly);
				}
			} else {
				var token = cts.Token;

				var workspace = IdeApp.TypeSystemService.GetWorkspaceAsync (IdeApp.ProjectOperations.CurrentSelectedSolution)
					.ContinueWith (t => {
						if (token.IsCancellationRequested)
							return;

						foreach (var project in IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
							try {
								Widget.AddProject (project, false);
							} catch (Exception e) {
								LoggingService.LogError ("Error while adding project " + project.Name + " to the tree.", e);
							}
						}
						widget.StartSearch ();
					}, token, TaskContinuationOptions.DenyChildAttach, Runtime.MainTaskScheduler);
			}
		}
	}
}
