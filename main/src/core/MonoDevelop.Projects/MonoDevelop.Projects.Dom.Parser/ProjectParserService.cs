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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MonoDevelop.Core;
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
			StartParserThread ();
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
		
		static IParser GetParserByFileName (string fileName)
		{
			foreach (IParser parser in parsers) {
				if (parser.CanParse (fileName))
					return parser;
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
		
		public static ProjectDom GetDom (Project project)
		{
			Debug.Assert (project != null);
			if (project == null) 
				return globalDom;
			return GetDom (project.FileName); 
		}
		
		public static IType GetType (string fullName, int genericParameterCount, bool caseSensitive)
		{
			foreach (ProjectDom dom in doms.Values) {
				IType type = dom.GetType (fullName, genericParameterCount, caseSensitive);
				if (type != null)
					return type;
			}
			return null;
		}
		
		public static IType GetType (SearchTypeRequest request)
		{
			IParserContext context = GetContext (request.CurrentCompilationUnit);
			SearchTypeResult result = context.SearchType (request);
			if (result == null)
				return null;
			return context.LookupType (result.Result);
		}
		
		public static SearchTypeResult SearchType (SearchTypeRequest request)
		{
			IParserContext context = GetContext (request.CurrentCompilationUnit);
			return context.SearchType (request);
		}
		
		static IParserContext GetContext (ICompilationUnit unit)
		{
			ProjectDom foundDom = null;
			foreach (ProjectDom dom in doms.Values) {
				ProjectCodeCompletionDatabase pccd = dom.Database as ProjectCodeCompletionDatabase;
				if (pccd == null)
					continue;
				if (pccd.Project.GetProjectFile (unit.FileName) != null) {
					foundDom = dom;
					break;
				}
			}
			return	 new DefaultParserContext (foundDom);
		}
		
		public delegate string ContentDelegate ();
		
		static Dictionary<string, Thread> refreshThreads = new Dictionary<string,Thread> ();
		
		public static void Refresh (Project project, string fileName, string mimeType, ContentDelegate getContent)
		{
			ProjectDom dom = GetDom (project);
			
			IParser parser = project != null ? GetParser (project is DotNetProject ? ((DotNetProject)project).LanguageName : project.ProjectType) : GetParserByMime (mimeType);
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
			if (project != null) {
				IParser result = GetParser (project is DotNetProject ? ((DotNetProject)project).LanguageName : project.ProjectType);
				if (result != null)
					return result;
			}
			if (!String.IsNullOrEmpty (mimeType)) {
				IParser result = GetParserByMime (mimeType);
				if (result != null)
					return result;
			}
			if (!String.IsNullOrEmpty (fileName)) 
				return GetParserByFileName (fileName);
			// give up
			return null;
		}
		
		public static ICompilationUnit Parse (Project project, string fileName, string mimeType, ContentDelegate getContent)
		{
			ProjectDom dom = GetDom (project);
			IParser parser = GetParser (project, mimeType, fileName);
			
			if (parser == null)
				return null;
			if (refreshThreads.ContainsKey (fileName)) {
				refreshThreads [fileName].Abort ();
				refreshThreads.Remove (fileName);
			}
			ICompilationUnit unit = parser.Parse (fileName, getContent ());
			((ProjectCodeCompletionDatabase)dom.Database).UpdateFromParseInfo (unit, fileName);
			OnCompilationUnitUpdated (new CompilationUnitEventArgs (unit));
			OnDomUpdated (new ProjectDomEventArgs (dom));
			return unit;
		}
		
		public static ProjectDom GetDom (string fileName)
		{
			Debug.Assert (!String.IsNullOrEmpty (fileName));
			if (!doms.ContainsKey (fileName)) {
				Dictionary<string, ProjectDom> newDoms = new Dictionary<string, ProjectDom> (doms);
				newDoms [fileName] = new ProjectDom ();
				doms = newDoms;
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
						Load (project);
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
		
		public static ProjectDom Load (string baseDirectory, string uri)
		{
			if (uri.StartsWith ("Assembly:")) {
				string file = uri.Substring (9);
				string fullName = AssemblyCodeCompletionDatabase.GetFullAssemblyName (file);
				
				bool isNew = !HasDom (uri);
				ProjectDom dom = GetDom (uri);
				if (isNew) {
					string realAssemblyName, assemblyFile, name;
					AssemblyCodeCompletionDatabase.GetAssemblyInfo (fullName, out realAssemblyName, out assemblyFile, out name);
					Thread thread = new Thread (delegate () {
						dom.UpdateFromParseInfo (DomCecilCompilationUnit.Load (assemblyFile, false, false), assemblyFile);
					});
					thread.Priority = ThreadPriority.Lowest;
					thread.IsBackground = true;
					thread.Start ();
					
//					AssemblyCodeCompletionDatabase adb = new AssemblyCodeCompletionDatabase (baseDirectory, file);
//					adb.ParseInExternalProcess = true;
//					adb.ParseAll ();
//					dom.Database = adb;
//					foreach (ReferenceEntry re in dom.Database.References)
//						Load (baseDirectory, re.Uri);
				}
				return dom;
			}
			return null;
		}
		
		public static ProjectDom Load (Project project)
		{
			string type = project.ProjectType;
			if (project is DotNetProject)
				type = ((DotNetProject)project).LanguageName;
			IParser parser = GetParser (type);
			if (parser == null) {
				return null;
			}
			
			ProjectDom dom = GetDom (project);
			
			if (project is DotNetProject) {
				DotNetProject netProject = (DotNetProject) project;
				
				string requiredRefUri = "Assembly:";
				requiredRefUri += Runtime.SystemAssemblyService.GetAssemblyNameForVersion (typeof(object).Assembly.GetName().ToString(), netProject.ClrVersion);
				dom.AddReference (Load (project.BaseDirectory, requiredRefUri));
				
				foreach (ProjectReference pr in netProject.References) {
					string[] refIds = GetReferenceKeys (pr);
					foreach (string refId in refIds) {
						dom.AddReference (Load (project.BaseDirectory, refId));
					}
				}
			}
			
//			dom.Database.UpdateFromProject ();
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
		
		
		static bool threadRunning = false;
		static bool trackingFileChanges = false;
		static object parseQueueLock = new object ();
		static Queue parseQueue = new Queue();
		static AutoResetEvent parseEvent = new AutoResetEvent (false);
		
		static void ParserUpdateThread ()
		{
			try {
				while (trackingFileChanges) {
					if (!parseEvent.WaitOne (5000, true))
						CheckModifiedFiles ();
					else if (trackingFileChanges)
						ConsumeParsingQueue ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error in parsing thread", ex);
			}
			lock (parseQueue) {
				threadRunning = false;
				if (trackingFileChanges)
					StartParserThread ();
			}
		}
		static void ConsumeParsingQueue ()
		{
			int pending = 0;
			IProgressMonitor monitor = null;
			
			try {
				Dictionary<CodeCompletionDatabase,CodeCompletionDatabase> dbsToFlush = new Dictionary<CodeCompletionDatabase,CodeCompletionDatabase> ();
				do {
					if (pending > 5 && monitor == null) {
						monitor = null; //GetParseProgressMonitor ();
						if (monitor != null)
							monitor.BeginTask ("Generating database", 0);
					}
					
					ParsingJob job = null;
					lock (parseQueueLock)
					{
						if (parseQueue.Count > 0)
							job = (ParsingJob) parseQueue.Dequeue ();
					}
					
					if (job != null) {
						try {
							job.ParseCallback (job.Data, monitor);
							dbsToFlush [job.Database] = job.Database;
						} catch (Exception ex) {
							if (monitor == null)
								monitor = null; //GetParseProgressMonitor ();
							if (monitor != null)
								monitor.ReportError (null, ex);
						}
					}
					
					lock (parseQueueLock)
						pending = parseQueue.Count;
					
				}
				while (pending > 0);
				
				// Flush the parsed databases
				foreach (CodeCompletionDatabase db in dbsToFlush.Keys)
					db.Flush ();
				
			} finally {
				if (monitor != null) monitor.Dispose ();
			}
		}
		public delegate void JobCallback (object data, IProgressMonitor monitor);
		class ParsingJob
		{
			public object Data;
			public JobCallback ParseCallback;
			public CodeCompletionDatabase Database;
		}
		
		static void StartParserThread ()
		{
			/*lock (parseQueueLock) {
				if (!threadRunning) {
					threadRunning = true;
					Thread t = new Thread(new ThreadStart(ParserUpdateThread));
					t.IsBackground  = true;
					t.Start();
				}
			}*/
		}
		
		static void CheckModifiedFiles ()
		{
			// Check databases following a bottom-up strategy in the dependency
			// tree. This will help resolving parsed classes.
			
			List<CodeCompletionDatabase> list = new List<CodeCompletionDatabase> ();
			lock (doms)  {
				// There may be several uris for the same db
				foreach (ProjectDom dom in doms.Values) {
					if (dom.Database == null)
						continue;
					if (!list.Contains (dom.Database))
						list.Add (dom.Database);
				}
			}
			
			List<CodeCompletionDatabase> done = new List<CodeCompletionDatabase> ();
			while (list.Count > 0) 
			{
				CodeCompletionDatabase readydb = null;
				CodeCompletionDatabase bestdb = null;
				int bestRefCount = int.MaxValue;
				
				// Look for a db with all references resolved
				for (int n=0; n<list.Count && readydb==null; n++)
				{
					CodeCompletionDatabase db = (CodeCompletionDatabase)list[n];

					bool allDone = true;
					foreach (ReferenceEntry re in db.References) {
						CodeCompletionDatabase refdb = GetDom (re.Uri) != null ? GetDom (re.Uri).Database : null;
						if (refdb != null && !done.Contains (refdb)) {
							allDone = false;
							break;
						}
					}
					
					if (allDone)
						readydb = db;
					else if (db.References.Count < bestRefCount) {
						bestdb = db;
						bestRefCount = db.References.Count;
					}
				}

				// It may not find any db without resolved references if there
				// are circular dependencies. In this case, take the one with
				// less references
				
				if (readydb == null)
					readydb = bestdb;
					
				readydb.CheckModifiedFiles ();
				list.Remove (readydb);
				done.Add (readydb);
			}
		}
		
		
		public static void UpdateCommentTasks (string fileName)
		{
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
