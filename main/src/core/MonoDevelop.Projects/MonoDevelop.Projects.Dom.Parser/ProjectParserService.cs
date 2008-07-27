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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Monodoc;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Projects.Dom.Database;

namespace MonoDevelop.Projects.Dom.Parser
{
	public static class ProjectDomService
	{
		static ProjectDom globalDom = new ProjectDom ();
		static Dictionary<string, ProjectDom> doms = new Dictionary<string, ProjectDom> ();
		static List<IParser> parsers = new List<IParser>();
		static RootTree helpTree = RootTree.LoadTree ();
		
		public static List<IParser> Parsers {
			get {
				return parsers;
			}
		}
		public static RootTree HelpTree {
			get {
				return helpTree;
			}
		}
		
		static string codeCompletionDataPath;
		static string GetCodeCompletionDataPath ()
		{
			string result = PropertyService.Get ("MonoDevelop.CodeCompletion.DataDirectory", "");
			if (String.IsNullOrEmpty (result)) {
				result = Path.Combine (PropertyService.ConfigPath, "codecompletiondata");
				PropertyService.Set ("MonoDevelop.CodeCompletion.DataDirectory", result);
			}
			if (!Directory.Exists (result))
				Directory.CreateDirectory (result);
			return result;
		}
		
		static ProjectDomService ()
		{
			codeCompletionDataPath = GetCodeCompletionDataPath ();
			
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
		
		public static IParser GetParserByMime (string mimeType)
		{
			foreach (IParser parser in parsers) {
				if (parser.CanParseMimeType (mimeType))
					return parser;
			}
			return null;
		}
		
		public static IParser GetParserByFileName (string fileName)
		{
			foreach (IParser parser in parsers) {
				if (parser.CanParse (fileName)) {
					return parser;
				}
			}
			return null;
		}
		
		static bool HasDom (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			return doms.ContainsKey (fileName);
		}
				
		static bool HasDom (Project project)
		{
			Debug.Assert (project != null);
			if (project == null) 
				return true;
			return HasDom (project.FileName); 
		}
		
		public static IType GetType (string fullName, int genericParameterCount, bool caseSensitive)
		{
			foreach (ProjectDom dom in doms.Values) {
				IType type = dom.GetType (fullName, genericParameterCount, caseSensitive, true);
				if (type != null)
					return type;
			}
			return null;
		}
		public static IType GetType (IReturnType returnType)
		{
			foreach (ProjectDom dom in doms.Values) {
				IType type = dom.GetType (returnType);
				if (type != null)
					return type;
			}
			return null;
		}
		
		public delegate string ContentDelegate ();
		
		static Dictionary<string, Thread> refreshThreads = new Dictionary<string,Thread> ();
		/*
		static Dictionary<string, ICompilationUnit> compilationUnits = new Dictionary<string, ICompilationUnit> ();
		public static ICompilationUnit GetCompilationUnit (string fileName) 
		{
			if (String.IsNullOrEmpty (fileName))
				return null;
			ICompilationUnit result;
			compilationUnits.TryGetValue (fileName, out result);
			return result;
		}
		
		public static bool ContainsUnit (string fileName)
		{
			return compilationUnits.ContainsKey (fileName) != null;
		}*/
		
		
		public static void Refresh (Project project, string fileName, string mimeType, ContentDelegate getContent)
		{
			ProjectDom dom = GetDatabaseProjectDom (project);
			IParser parser = GetParser (project, mimeType, fileName);
			if (parser == null)
				return;
			if (refreshThreads.ContainsKey (fileName)) {
				refreshThreads [fileName].Abort ();
				refreshThreads.Remove (fileName);
			}
			Thread thread = new Thread (delegate () {
				Thread.Sleep (500);
				try {
					ICompilationUnit unit = parser.Parse (fileName, getContent ());
					if (dom != null)
						dom.UpdateFromParseInfo (unit, fileName);
					OnCompilationUnitUpdated (new CompilationUnitEventArgs (unit));
					OnDomUpdated (new ProjectDomEventArgs (dom));
				} catch (ThreadAbortException) {
				}
			});
			refreshThreads [fileName] = thread;
			
			thread.Priority = ThreadPriority.Lowest;
			thread.IsBackground = true;
			thread.Start ();
		}
		
		public static IParser GetParser (Project project, string mimeType, string fileName)
		{
			if (!String.IsNullOrEmpty (mimeType)) {
				IParser result = GetParserByMime (mimeType);
				if (result != null) {
					return result;
				}
			}
			
			if (!String.IsNullOrEmpty (fileName)) 
				return GetParserByFileName (fileName);
				
			if (project != null) {
				IParser result = GetParser (project is DotNetProject ? ((DotNetProject)project).LanguageName : project.ProjectType);
				if (result != null)
					return result;
			}
			
			// give up
			return null;
		}
		
		public static ICompilationUnit Parse (Project project, string fileName, string mimeType)
		{
			return Parse (project, fileName, mimeType, delegate () { return System.IO.File.ReadAllText (fileName); });
		}
		
		public static ICompilationUnit Parse (Project project, string fileName, string mimeType, ContentDelegate getContent)
		{
			ProjectDom dom = GetDatabaseProjectDom (project);
			IParser parser = GetParser (project, mimeType, fileName);
			if (parser == null)
				return null;
			if (refreshThreads.ContainsKey (fileName)) {
				refreshThreads [fileName].Abort ();
				refreshThreads.Remove (fileName);
			}
			ICompilationUnit unit = parser.Parse (fileName, getContent ());
		//	compilationUnits[fileName] = unit;
			if (dom != null)
				dom.UpdateFromParseInfo (unit, fileName);
			OnCompilationUnitUpdated (new CompilationUnitEventArgs (unit));
			if (dom != null)
				OnDomUpdated (new ProjectDomEventArgs (dom));
			return unit;
		}
		
		static void InsertDom (string name, ProjectDom dom)
		{
			Dictionary<string, ProjectDom> newDoms = new Dictionary<string, ProjectDom> (doms);
			newDoms [name] = dom;
			doms = newDoms;
		}
		
		public static ProjectDom GetDom (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			if (!doms.ContainsKey (fileName)) {
				InsertDom (fileName, new ProjectDom ());
			}
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
					foreach (Project project in solution.GetAllProjects ()) {
						Load (solution, project);
					}
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
			if (doms.ContainsKey (project.FileName)) {
				Dictionary<string, ProjectDom> newDoms = new Dictionary<string, ProjectDom> (doms);
				newDoms.Remove (project.FileName);
				doms = newDoms;
			}
		}
		
		#region Assembly names
		static string GetFullAssemblyName (string s)
		{
			return Runtime.SystemAssemblyService.GetAssemblyFullName (s);
		}
		static string EncodeGacAssemblyName (string assemblyName)
		{
			string[] assemblyPieces = assemblyName.Split(',');
			string res = "";
			foreach (string item in assemblyPieces) {
				string[] pieces = item.Trim ().Split (new char[] { '=' }, 2);
				if(pieces.Length == 1)
					res += pieces[0];
				else if (!(pieces[0] == "Culture" && pieces[1] != "Neutral"))
					res += "_" + pieces[1];
			}
			return res;
		}
		public static bool GetAssemblyInfo (string assemblyName, out string realAssemblyName, out string assemblyFile, out string name)
		{
			name = null;
			assemblyFile = null;
			realAssemblyName = null;
			if (String.IsNullOrEmpty (assemblyName))
				return false;
			string ext = Path.GetExtension (assemblyName).ToLower ();
			
			if (ext == ".dll" || ext == ".exe") 
			{
				name = assemblyName.Substring (0, assemblyName.Length - 4);
				name = name.Replace(',','_').Replace(" ","").Replace('/','_');
				assemblyFile = assemblyName;
			}
			else
			{
				assemblyFile = Runtime.SystemAssemblyService.GetAssemblyLocation (assemblyName);

				bool gotname = false;
				if (assemblyFile != null && File.Exists (assemblyFile)) {
					try {
						assemblyName = AssemblyName.GetAssemblyName (assemblyFile).FullName;
						gotname = true;
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
				if (!gotname) {
					LoggingService.LogError ("Could not load assembly: " + assemblyName);
					return false;
				}
				name = EncodeGacAssemblyName (assemblyName);
			}
			
			realAssemblyName = assemblyName;
			return true;
		}
		#endregion
		
		static ProjectDom Load (Solution solution, string baseDirectory, string uri)
		{
			if (uri.StartsWith ("Assembly:")) {
				string file = uri.Substring (9);
				string fullName = GetFullAssemblyName (file);
				string realAssemblyName, assemblyFile, name;
				GetAssemblyInfo (fullName, out realAssemblyName, out assemblyFile, out name);
				if (String.IsNullOrEmpty (name))
					return null;
				string codeCompletionFile = Path.Combine (codeCompletionDataPath, name) + ".pidb";
				if (HasDom (codeCompletionFile)) 
					return GetDom (codeCompletionFile);
				bool shouldCreate = !File.Exists (codeCompletionFile);
				CodeCompletionDatabase database = new CodeCompletionDatabase (codeCompletionFile);
				if (shouldCreate)
					database.InsertCompilationUnit (DomCecilCompilationUnit.Load (assemblyFile, false, false), fullName);
				ProjectDom dom = new DatabaseProjectDom (database, fullName);
				InsertDom (codeCompletionFile, dom);
				return dom;
			}
			if (uri.StartsWith ("Project:")) {
				string projectName = uri.Substring ("Project:".Length);
				Project referencedProject = solution.FindProjectByName (projectName);
				if (referencedProject != null) 
					return GetDatabaseProjectDom (referencedProject);
			}
			return null;
		}
		
		public static DatabaseProjectDom GetDatabaseProjectDom (Project project)
		{
			if (project == null)
				return null;
			if (!HasDom (project)) {
				string codeCompletionFile = System.IO.Path.ChangeExtension (project.FileName, ".pidb");
				CodeCompletionDatabase database = new CodeCompletionDatabase (codeCompletionFile);
				DatabaseProjectDom dom = new DatabaseProjectDom (database, project.Name);
				InsertDom (project.FileName, dom);
				dom.Project = project;
				return dom;
			}
			return (DatabaseProjectDom)GetDom (project.FileName);
		}
		
		static ProjectDom Load (Solution solution, Project project)
		{
			if (solution == null || project == null)
				return null;
			string type = project.ProjectType;
			if (project is DotNetProject)
				type = ((DotNetProject)project).LanguageName;
			IParser parser = GetParser (type);
			if (parser == null)
				return null;
			DatabaseProjectDom dom = GetDatabaseProjectDom (project);
			
			// load References
			if (project is DotNetProject) {
				DotNetProject netProject = (DotNetProject) project;
				
				string requiredRefUri = "Assembly:";
				requiredRefUri += Runtime.SystemAssemblyService.GetAssemblyNameForVersion (typeof(object).Assembly.GetName().ToString(), netProject.ClrVersion);
				dom.AddReference (Load (solution, project.BaseDirectory, requiredRefUri));
				
				foreach (ProjectReference pr in netProject.References) {
					string[] refIds = GetReferenceKeys (pr);
					foreach (string refId in refIds) {
						dom.AddReference (Load (solution, project.BaseDirectory, refId));
					}
				}
			}
			
			foreach (ProjectFile file in project.Files) {
				if (file.BuildAction != BuildAction.Compile)
					continue;
				if (!dom.NeedCompilation (file.FilePath)) {
					continue;
				}
				string content = null;
				try {
					content = System.IO.File.ReadAllText (file.FilePath);
				} catch (Exception e) {
					
				}
				if (content != null) {
					ICompilationUnit unit = parser.Parse (file.FilePath, content);
					dom.UpdateFromParseInfo (unit, file.FilePath);
					OnCompilationUnitUpdated (new CompilationUnitEventArgs (unit));
				}
			}
			dom.FireLoaded ();
			OnDomUpdated (new ProjectDomEventArgs (dom));
			return dom;
		}
		
		
		
		static string[] GetReferenceKeys (ProjectReference pr)
		{
			switch (pr.ReferenceType) {
				case ReferenceType.Project:
					return new string[] { "Project:" + pr.Reference };
				case ReferenceType.Gac:
					string refId = pr.Reference;
					string ext = System.IO.Path.GetExtension (refId).ToLower ();
					if (ext == ".dll" || ext == ".exe")
						refId = refId.Substring (0, refId.Length - 4);
					return new string[] { "Assembly:" + refId };
				default:
					ArrayList list = new ArrayList ();
					foreach (string s in pr.GetReferencedFileNames (ProjectService.DefaultConfiguration))
						list.Add ("Assembly:" + s);
					return (string[]) list.ToArray (typeof(string));
			}
		}
		
		
		public static void UpdateCommentTasks (string fileName)
		{
		}
		
		static void OnSolutionItemAdded (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem is Project)
				Load (args.SolutionItem.ParentSolution, (Project) args.SolutionItem);
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
		
		public static void NotifyTypeUpdate (Project project, string fileName, TypeUpdateInformation info)
		{
			if (TypesUpdated != null)
				TypesUpdated (null, new TypeUpdateInformationEventArgs (info));
		}
		
		public static event EventHandler<TypeUpdateInformationEventArgs> TypesUpdated;
		
		static void OnDomUpdated (ProjectDomEventArgs args) 
		{
			if (DomUpdated != null) 
				DomUpdated (null, args);
		}
		public static event EventHandler<ProjectDomEventArgs> DomUpdated;
	}
}
