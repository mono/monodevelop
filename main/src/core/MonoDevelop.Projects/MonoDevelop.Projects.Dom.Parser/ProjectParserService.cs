//
// ProjectDomService.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MonoDevelop.Projects;
using Mono.Addins;


namespace MonoDevelop.Projects.Dom.Parser
{
	
	public static class ProjectDomService
	{
		static ProjectDom globalDom = new ProjectDom ();
		static Dictionary<string, ProjectDom> doms = new Dictionary<string, ProjectDom> ();
		static List<IParser> parsers = new List<IParser>();
		
		public static List<IParser> Parsers {
			get {
				return parsers;
			}
		}

		static ProjectDomService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/DomParser", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					parsers.Add ((IParser) args.ExtensionObject);
					break;
				case ExtensionChange.Remove:
					parsers.Remove ((IParser) args.ExtensionObject);
					break;
				}
			});
		}
		
		static IParser GetParser (string projectType)
		{
			foreach (IParser parser in parsers) {
				if (parser.CanParseProjectType (projectType))
					return parser;
			}
			return null;
		}
		
		static IParser GetParserByMime (string mimeType)
		{
			foreach (IParser parser in parsers) {
				if (parser.CanParseMimeType (mimeType))
					return parser;
			}
			return null;
		}
		
		public static ProjectDom GetDom (Project project)
		{
			Debug.Assert (project != null);
			if (project == null) 
				return globalDom;
			return GetDom (project.FileName); 
		}
		
		public delegate string ContentDelegate ();
		
		static Dictionary<string, Thread> refreshThreads = new Dictionary<string,Thread> ();
		public static void Refresh (Project project, string fileName, string mimeType, ContentDelegate getContent)
		{
			ProjectDom dom = GetDom (project);
			IParser parser = project != null ? GetParser (project.ProjectType) : GetParserByMime (mimeType);
			if (parser == null)
				return;
			if (refreshThreads.ContainsKey (fileName)) {
				refreshThreads [fileName].Abort ();
				refreshThreads.Remove (fileName);
			}
			Thread thread = new Thread (delegate () {
				Thread.Sleep (500);
				dom.RemoveCompilationUnit (fileName);
				ICompilationUnit unit = parser.Parse (fileName, getContent ());
				dom.UpdateCompilationUnit (unit);
				OnCompilationUnitUpdated (new CompilationUnitEventArgs (unit));
				OnDomUpdated (new ProjectDomEventArgs (dom));
			});
			refreshThreads [fileName] = thread;
			
			thread.Priority = ThreadPriority.Lowest;
			thread.IsBackground = true;
			thread.Start ();
		}
		
		public static ProjectDom GetDom (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			if (!doms.ContainsKey (fileName))
				doms [fileName] = new ProjectDom ();
			return doms [fileName];
		}
		
		public static void Load (WorkspaceItem item)
		{
			if (item is Workspace) {
				Workspace ws = (Workspace) item;
				foreach (WorkspaceItem childItem in ws.Items)
					Load (childItem);
				ws.ItemAdded   += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
				return;
			}
			if (item is Solution) {
				Thread thread = new Thread (delegate () {
					Solution solution = (Solution) item;
					foreach (Project project in solution.GetAllProjects ())
						Load (project);
					solution.SolutionItemAdded   += OnSolutionItemAdded;
					solution.SolutionItemRemoved += OnSolutionItemRemoved;
				});
				thread.Priority = ThreadPriority.Lowest;
				thread.IsBackground = true;
				thread.Start ();
			}
		}
		
		public static void Unload (WorkspaceItem item)
		{
			if (item is Workspace) {
				Workspace ws = (Workspace) item;
				foreach (WorkspaceItem childItem in ws.Items)
					Unload (childItem);
				ws.ItemAdded   -= OnWorkspaceItemAdded;
				ws.ItemRemoved -= OnWorkspaceItemRemoved;
				return;
			}
			if (item is Solution) {
				Solution solution = (Solution) item;
				foreach (Project project in solution.GetAllProjects ())
					Unload (project);
				solution.SolutionItemAdded   -= OnSolutionItemAdded;
				solution.SolutionItemRemoved -= OnSolutionItemRemoved;
			}
		}
		
		public static void Unload (Project project)
		{
			if (project == null)
				return;
			if (doms.ContainsKey (project.FileName))
				doms.Remove (project.FileName);
		}
		
		public static void Load (Project project)
		{
			IParser parser = GetParser (project.ProjectType);
			if (parser == null)
				return;
			
			ProjectDom dom = GetDom (project);
			foreach (ProjectFile file in project.Files) {
				if (file.BuildAction != BuildAction.Compile)
					continue;
				string content = null;
				try {
					content = System.IO.File.ReadAllText (file.FilePath);
				} catch (Exception e) {
				}
				if (content != null) {
					ICompilationUnit unit = parser.Parse (file.FilePath, content);
					dom.UpdateCompilationUnit (unit);
					OnCompilationUnitUpdated (new CompilationUnitEventArgs (unit));
				}
			}
			dom.FireLoaded ();
			OnDomUpdated (new ProjectDomEventArgs (dom));
		}
		
		static void OnSolutionItemAdded (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem is Project)
				Load ((Project) args.SolutionItem);
		}
		
		static void OnSolutionItemRemoved (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem is Project)
				Unload ((Project) args.SolutionItem);
		}
		
		static void OnWorkspaceItemAdded (object s, WorkspaceItemEventArgs args)
		{
			Load (args.Item);
		}
		
		static void OnWorkspaceItemRemoved (object s, WorkspaceItemEventArgs args)
		{
			Unload (args.Item);
		}
		
		static void OnCompilationUnitUpdated (CompilationUnitEventArgs args) 
		{
			if (CompilationUnitUpdated != null) 
				CompilationUnitUpdated (null, args);
		}
		public static event EventHandler<CompilationUnitEventArgs> CompilationUnitUpdated;
		
		static void OnDomUpdated (ProjectDomEventArgs args) 
		{
			if (DomUpdated != null) 
				DomUpdated (null, args);
		}
		public static event EventHandler<ProjectDomEventArgs> DomUpdated;
	}
}
