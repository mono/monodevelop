//
// ProjectDomService.cs
//
// Author:
//   Lluis Sanchez <lluis@novell.com>
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
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Monodoc;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Collections;
using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Projects.Extensions;
//using MonoDevelop.Projects.Dom.Database;

namespace MonoDevelop.Projects.Dom.Parser
{
	public static class ProjectDomService
	{
		//static IParserDatabase parserDatabase = new MonoDevelop.Projects.Dom.MemoryDatabase.MemoryDatabase ();
		
		static bool threadRunning;
		static bool trackingFileChanges;
		static IProgressMonitorFactory parseProgressMonitorFactory;
		static int parseStatus;
		static Dictionary<object,int> loadCount = new Dictionary<object,int> ();
		
		static ManualResetEvent queueEmptied = new ManualResetEvent (true);
		
		const int MAX_PARSING_CACHE_SIZE = 10;
		const int MAX_SINGLEDB_CACHE_SIZE = 10;

		class ParsingCacheEntry
		{
			   public ParsedDocument ParseInformation;
			   public DateTime AccessTime;
		}
		
		class SingleFileCacheEntry
		{
			   public ProjectDom Database;
			   public DateTime AccessTime;
		}
		
		class ParsingJob
		{
			public string File;
			public Action<string, IProgressMonitor> ParseCallback;
			public ProjectDom Database;
		}

		static Dictionary<string,ParsingCacheEntry> parsings = new Dictionary<string,ParsingCacheEntry> ();
		
		static Queue<ParsingJob> parseQueue = new Queue<ParsingJob>();
		static Dictionary<string,ParsingJob> parseQueueIndex = new Dictionary<string,ParsingJob>();
		static object parseQueueLock = new object ();
		static AutoResetEvent parseEvent = new AutoResetEvent (false);
		
		static string codeCompletionPath;

		static Dictionary<string,ProjectDom> databases = new Dictionary<string,ProjectDom>();
		static Dictionary<FilePath,SingleFileCacheEntry> singleDatabases = new Dictionary<FilePath,SingleFileCacheEntry> ();		
		
		static ProjectDomService ()
		{
			codeCompletionPath = GetDefaultCompletionFileLocation ();
			// for unit tests it may not have been initialized.
		}

		public static RootTree HelpTree {
			get {
				return HelpService.HelpTree;
			}
		}
		
		#region Parser Management
		// the special comment tags should be moved to taskservice, but I don't want to put this
		// in GPLed #develop code. TODO: Move this code to the taskservice, when it's MIT X11.
		const string defaultTags = "FIXME:2;TODO:1;HACK:1;UNDONE:0";
		static CommentTagSet specialCommentTags;
		public static CommentTagSet SpecialCommentTags {
			get {
				if (specialCommentTags == null) {
					string tags = PropertyService.Get ("Monodevelop.TaskListTokens", defaultTags);
					specialCommentTags = new CommentTagSet (tags);
				}
				return specialCommentTags;
			}
			set {
				if (!SpecialCommentTags.Equals (value)) {
					specialCommentTags = value;
					PropertyService.Set ("Monodevelop.TaskListTokens", specialCommentTags.ToString ());
					if (SpecialCommentTagsChanged != null)
						SpecialCommentTagsChanged (null, EventArgs.Empty);
				}
			}
		}

		static List<ParserNode> parsers;
		static IEnumerable<ParserNode> Parsers {
			get {
				if (parsers == null) {
					Counters.ParserServiceInitialization.BeginTiming ();
					parsers = new List<ParserNode> ();
					AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/DomParser", delegate (object sender, ExtensionNodeEventArgs args) {
						switch (args.Change) {
						case ExtensionChange.Add:
							parsers.Add ((ParserNode) args.ExtensionNode);
							break;
						case ExtensionChange.Remove:
							parsers.Remove ((ParserNode) args.ExtensionNode);
							break;
						}
					});
					Counters.ParserServiceInitialization.EndTiming ();
				}
				return parsers;
			}
		}
		
		public static IParser GetParser (string fileName)
		{
			return Parsers.Where (n => n.Supports (fileName)).Select (n => n.Parser)
				.Where (p => p.CanParse (fileName))
				.FirstOrDefault ();
		}

		#endregion
		
		public static IExpressionFinder GetExpressionFinder (string fileName)
		{
			IParser parser = GetParser (fileName);
			if (parser != null)
				return parser.CreateExpressionFinder (null);
			return null;
		}
		
		public static IProgressMonitorFactory ParseProgressMonitorFactory {
			get { return parseProgressMonitorFactory; }
			set { parseProgressMonitorFactory = value; }
		}
		
		public static bool IsParsing {
			get { return parseStatus > 0; }
		}
		
		public static bool TrackFileChanges {
			get {
				return trackingFileChanges;
			}
			set {
				lock (parseQueueLock) {
					if (value != trackingFileChanges) {
						trackingFileChanges = value;
						if (value)
							StartParserThread ();
					}
				}
			}
		}

		internal static string CodeCompletionPath {
			get { return codeCompletionPath; }
		}
		
		static string GetDefaultCompletionFileLocation()
		{
			string path = PropertyService.Get<string> ("MonoDevelop.CodeCompletion.DataDirectory", String.Empty);
			if (string.IsNullOrEmpty (path)) {
				path = Path.Combine (PropertyService.ConfigPath, "CodeCompletionData");
				PropertyService.Set ("MonoDevelop.CodeCompletion.DataDirectory", path);
				PropertyService.SaveProperties ();
			}
			
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);

			return path;
		}

		public static ParsedDocument Parse (string fileName, Func<string> getContent)
		{
			List<Project> projects = new List<Project> ();
			
			lock (databases) {
				foreach (ProjectDom db in databases.Values) {
					if (db.Project != null) {
						if (db.Project.IsFileInProject (fileName))
							projects.Add (db.Project);
					}
				}
			}

			if (projects.Count > 0)
				return UpdateFile (projects.ToArray (), fileName, getContent);
			else
				return ParseFile (null, fileName, getContent);
		}

		// Parses a file an updates the parser database
		public static ParsedDocument Parse (Project project, string fileName)
		{
			return Parse (project, fileName, delegate () {
				if (!System.IO.File.Exists (fileName))
					return "";
				try {
					return System.IO.File.ReadAllText (fileName);
				} catch (Exception e) {
					LoggingService.LogError ("Error reading file {0} for ProjectDomService: {1}", fileName, e);
					return "";
				}
			});
		}
		
		public static ParsedDocument Parse (Project project, string fileName, string content)
		{
			return Parse (project, fileName, delegate () { return content; });
		}
		
		public static ParsedDocument Parse (Project project, string fileName, Func<string> getContent)
		{
			Project[] projects = project != null ? new Project[] { project } : null;
			return UpdateFile (projects, fileName, getContent);
		}

		// Parses a file. It does not update the parser database
		public static ParsedDocument ParseFile (ProjectDom dom, string fileName)
		{
			return ParseFile (dom, fileName, null);
		}
		
		// Parses a file. It does not update the parser database
		public static ParsedDocument ParseFile (ProjectDom dom, string fileName, Func<string> getContent)
		{
			string fileContent = getContent != null ? getContent () : null;
			return DoParseFile (dom, fileName, fileContent);
		}

		// Returns the ParsedDocument object for a file. It will parse
		// the file if it has not been recently parsed
		public static ParsedDocument GetParsedDocument (ProjectDom dom, string fileName)
		{
			if (fileName == null || fileName.Length == 0) {
				return null;
			}
			
			ParsedDocument info = GetCachedParseInformation (fileName);
			if (info != null) 
				return info;
			return ParseFile (dom, fileName);
		}
		
		public static void RemoveFileDom (FilePath file)
		{
			if (file.IsNullOrEmpty)
				return;
			file = file.CanonicalPath;
			if (singleDatabases.ContainsKey (file)) {
				singleDatabases[file].Database.Unload ();
				singleDatabases.Remove (file);
			}
		}
		
		public static ProjectDom GetFileDom (FilePath file)
		{
			if (file.IsNull)
				file = Path.GetTempFileName ();
			
			file = file.CanonicalPath;
			
			lock (singleDatabases)
			{
				SingleFileCacheEntry entry;
				if (singleDatabases.TryGetValue (file, out entry)) {
					entry.AccessTime = DateTime.Now;
					return entry.Database;
				}
				else 
				{
					if (singleDatabases.Count >= MAX_SINGLEDB_CACHE_SIZE)
					{
						DateTime tim = DateTime.MaxValue;
						string toDelete = null;
						foreach (KeyValuePair<FilePath,SingleFileCacheEntry> pce in singleDatabases)
						{
							DateTime ptim = pce.Value.AccessTime;
							if (ptim < tim) {
								tim = ptim;
								toDelete = pce.Key;
							}
						}
						singleDatabases.Remove (toDelete);
					}

					
					ProjectDom db = ParserDatabase.LoadSingleFileDom (file);
					db.Uri = "File:" + file;
					db.UpdateReferences ();
					entry = new SingleFileCacheEntry ();
					entry.Database = db;
					entry.AccessTime = DateTime.Now;
					singleDatabases [file] = entry;
					db.ReferenceCount = 1;
					return db;
				}
			}
		}
		
		public static ProjectDom GetProjectDom (Project project)
		{
			if (project == null)
				return null;
			return GetDom ("Project:" + project.FileName);
		}
		
		public static ProjectDom GetAssemblyDom (TargetRuntime runtime, string assemblyName)
		{
			return GetDom ("Assembly:" + runtime.Id + ":" + assemblyName);
		}
		
		public static string LoadAssembly (TargetRuntime runtime, string file)
		{
			string name = "Assembly:" + runtime.Id + ":" + Path.GetFullPath (file);
			if (GetDom (name, true) != null)
				return name;
			else
				return null;
		}
		
		public static void UnloadAssembly (TargetRuntime runtime, string file)
		{
			string name = "Assembly:" + runtime.Id + ":" + Path.GetFullPath (file);
			UnrefDom (name);
		}
		
		public static void Load (WorkspaceItem item)
		{
			if (IncLoadCount (item) != 1)
				return;
			
			lock (databases) {
				if (item is Workspace) {
					Workspace ws = (Workspace) item;
					foreach (WorkspaceItem it in ws.Items)
						Load (it);
					ws.ItemAdded += OnWorkspaceItemAdded;
					ws.ItemRemoved += OnWorkspaceItemRemoved;
				}
				else if (item is Solution) {
					Solution solution = (Solution) item;
					foreach (Project project in solution.GetAllProjects ())
						Load (project);
					// Refresh the references of all projects. This is necessary because
					// some project may have been loaded before the projects their reference,
					// in which case those references are not properly registered.
					foreach (Project project in solution.GetAllProjects ()) {
						ProjectDom dom = GetProjectDom (project);
						if (dom != null)
							dom.UpdateReferences ();
					}
					solution.SolutionItemAdded += OnSolutionItemAdded;
					solution.SolutionItemRemoved += OnSolutionItemRemoved;
				}
			}
		}
		
		public static void Unload (WorkspaceItem item)
		{
			if (DecLoadCount (item) != 0)
				return;
			
			if (item is Workspace) {
				Workspace ws = (Workspace) item;
				foreach (WorkspaceItem it in ws.Items)
					Unload (it);
				ws.ItemAdded -= OnWorkspaceItemAdded;
				ws.ItemRemoved -= OnWorkspaceItemRemoved;
			}
			else if (item is Solution) {
				Solution solution = (Solution) item;
				foreach (Project project in solution.GetAllProjects ())
					Unload (project);
				solution.SolutionItemAdded -= OnSolutionItemAdded;
				solution.SolutionItemRemoved -= OnSolutionItemRemoved;
			}
		}
		
		
		public static void Unload (Project project)
		{
			if (DecLoadCount (project) != 0)
				return;
			
			string uri = "Project:" + project.FileName;
			if (UnrefDom (uri)) {
				if (project is DotNetProject) {
					((DotNetProject)project).ReferenceAddedToProject -= OnProjectReferenceAdded;
					((DotNetProject)project).ReferenceRemovedFromProject -= OnProjectReferenceRemoved;
				}
			}
		}

		public static bool HasDom (Project project)
		{
			Debug.Assert (project != null);
			return (GetProjectDom (project) != null);
		}

		public static void Load (Project project)
		{
			if (IncLoadCount (project) != 1)
				return;
			
			lock (databases)
			{
				string uri = "Project:" + project.FileName;
				if (databases.ContainsKey (uri)) return;
				
				try {
					ProjectDom db = ParserDatabase.LoadProjectDom (project);
					RegisterDom (db, uri);
				
					if (project is DotNetProject) {
						((DotNetProject)project).ReferenceAddedToProject += OnProjectReferenceAdded;
						((DotNetProject)project).ReferenceRemovedFromProject += OnProjectReferenceRemoved;
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Parser database for project '" + project.Name + " could not be loaded", ex);
				}
			}
		}

		public static void RegisterDom (ProjectDom dom, string uri)
		{
			dom.Uri = uri;
			databases [uri] = dom;
			dom.UpdateReferences ();
		}
		
		internal static ProjectDom GetDomForUri (string uri)
		{
			return GetDom (uri);
		}
		
		internal static ProjectDom GetDom (string uri)
		{
			return GetDom (uri, false);
		}
		
		internal static ProjectDom GetDom (string uri, bool addReference)
		{
			lock (databases)
			{
				ProjectDom db;
				if (!databases.TryGetValue (uri, out db)) {
					// Create/load the database
					
					TargetRuntime tr = null;
					TargetFramework fx = null;
					string file;
					
					if (ParseAssemblyUri (uri, out tr, out fx, out file)) {
						db = ParserDatabase.LoadAssemblyDom (tr, fx, file);
						RegisterDom (db, uri);
					}
				}
				if (addReference && db != null)
					db.ReferenceCount++;
				return db;
			}
		}
		
		static readonly string assemblyPrefix = "Assembly:";
		static readonly char[] separators     = new char [] {'/', '\\'};
		public static bool ParseAssemblyUri (string uri, out TargetRuntime runtime, out TargetFramework fx, out string file)
		{
			if (uri.StartsWith (assemblyPrefix)) {
				runtime = null;
				fx = null;
				int curOffset = assemblyPrefix.Length;
				int i = uri.IndexOf (':', curOffset);
				if (i != -1) {
					string rid = uri.Substring (assemblyPrefix.Length, i - assemblyPrefix.Length);
					runtime = Runtime.SystemAssemblyService.GetTargetRuntime (rid);
					curOffset = i + 1;
				}
				i = Math.Max (uri.IndexOfAny (separators, curOffset), curOffset);
				if (uri[i - 1] == ':' && i == curOffset + 2)
					i = curOffset;
				file = uri.Substring (i);
				if (runtime == null)
					runtime = Runtime.SystemAssemblyService.DefaultRuntime;
				if (fx == null)
					fx = TargetFramework.Default;
				return true;
			}
			file = null;
			runtime = null;
			fx = null;
			return false;
		}

		static int DecLoadCount (object ob)
		{
			lock (databases) {
				int c;
				if (loadCount.TryGetValue (ob, out c)) {
					c--;
					if (c == 0)
						loadCount.Remove (ob);
					else
						loadCount [ob] = c;
					return c;
				}
				LoggingService.LogError ("DecLoadCount: Object not registered.");
				return 0;
			}
		}
		
		static int IncLoadCount (object ob)
		{
			lock (databases) {
				int c;
				if (loadCount.TryGetValue (ob, out c)) {
					c++;
					loadCount [ob] = c;
					return c;
				}
				else {
					loadCount [ob] = 1;
					return 1;
				}
			}
	
		}

		internal static bool UnrefDom (string uri)
		{
			ProjectDom db;
			lock (databases) {
				if (databases.TryGetValue (uri, out db)) {
					if (db.ReferenceCount > 1) {
						db.ReferenceCount--;
						return false;
					}

					// It has to be deleted by iterating because a ProjectDom
					// may be registered using different uris (e.g. full/partial assembly name
					List<string> uris = new List<string> (from p in databases where p.Value.Uri == db.Uri select p.Key);
					uris.ForEach (u => databases.Remove (u));
					
					// Delete all pending parse jobs for this database
					RemoveParseJobs (db);
				}
			}
			if (db != null)
				db.Unload ();
			return true;
		}
		
		internal static IProgressMonitor GetParseProgressMonitor ()
		{
			IProgressMonitor mon;
			if (parseProgressMonitorFactory != null)
				mon = parseProgressMonitorFactory.CreateProgressMonitor ();
			else
				mon = new NullProgressMonitor ();
			
			return new AggregatedProgressMonitor (mon, new InternalProgressMonitor ());
		}
		
		static void OnWorkspaceItemAdded (object s, WorkspaceItemEventArgs args)
		{
			Load (args.Item);
		}
		
		static void OnWorkspaceItemRemoved (object s, WorkspaceItemEventArgs args)
		{
			Unload (args.Item);
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
		
		static void OnProjectReferenceAdded (object sender, ProjectReferenceEventArgs args)
		{
			ProjectDom db = GetProjectDom (args.Project);
			if (db != null) {
				db.OnProjectReferenceAdded (args.ProjectReference);
			}
		}
		
		static void OnProjectReferenceRemoved (object sender, ProjectReferenceEventArgs args)
		{
			ProjectDom db = GetProjectDom (args.Project);
			if (db != null) {
				db.OnProjectReferenceRemoved (args.ProjectReference);
			}
		}
		
		internal static int PendingJobCount {
			get {
				lock (parseQueueLock) {
					return parseQueueIndex.Count;
				}
			}
		}

		static IParserDatabase parserDatabase = null;
		public static IParserDatabase ParserDatabase {
			get {
				if (parserDatabase == null) {
					parserDatabase = new MonoDevelop.Projects.Dom.Serialization.ParserDatabase ();
					parserDatabase.Initialize ();
				}
				return parserDatabase;
			}
		}
		
		public static void QueueParseJob (ProjectDom db, Action<string, IProgressMonitor> callback, string file)
		{
			ParsingJob job = new ParsingJob ();
			job.ParseCallback = callback;
			job.File = file;
			job.Database = db;
			lock (parseQueueLock)
			{
				RemoveParseJob (file);
				parseQueueIndex [file] = job;
				parseQueue.Enqueue (job);
				parseEvent.Set ();
				
				if (parseQueueIndex.Count == 1)
					queueEmptied.Reset ();
			}
		}
		
		static bool WaitForParseJob (int timeout)
		{
			return parseEvent.WaitOne (5000, true);
		}
		
		static ParsingJob DequeueParseJob ()
		{
			lock (parseQueueLock)
			{
				while (parseQueue.Count > 0) {
					ParsingJob job = parseQueue.Dequeue ();
					if (job.ParseCallback != null) {
						parseQueueIndex.Remove (job.File);
						return job;
					}
				}
				return null;
			}
		}
		
		internal static void WaitForParseQueue ()
		{
			queueEmptied.WaitOne ();
		}
		
		static void RemoveParseJob (string file)
		{
			lock (parseQueueLock)
			{
				ParsingJob job;
				if (parseQueueIndex.TryGetValue (file, out job)) {
					job.ParseCallback = null;
					parseQueueIndex.Remove (file);
				}
			}
		}
		
		static void RemoveParseJobs (ProjectDom dom)
		{
			lock (parseQueueLock)
			{
				foreach (ParsingJob pj in parseQueue) {
					if (pj.Database == dom) {
						pj.ParseCallback = null;
						parseQueueIndex.Remove (pj.File);
					}
				}
			}
		}
		
		static void StartParserThread()
		{
			lock (parseQueueLock) {
				if (!threadRunning) {
					threadRunning = true;
					Thread t = new Thread(new ThreadStart(ParserUpdateThread));
					t.Name = "Background parser";
					t.IsBackground  = true;
					t.Start();
				}
			}
		}
		
		static void ParserUpdateThread()
		{
			try {
				while (trackingFileChanges)
				{
					if (!WaitForParseJob (5000))
						CheckModifiedFiles ();
					else if (trackingFileChanges)
						ConsumeParsingQueue ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error in parsing thread", ex);
			}
			lock (parseQueueLock) {
				threadRunning = false;
				if (trackingFileChanges)
					StartParserThread ();
			}
		}
		
		static void CheckModifiedFiles ()
		{
			// Check databases following a bottom-up strategy in the dependency
			// tree. This will help resolving parsed classes.
			
			Set<ProjectDom> list = new Set<ProjectDom> ();
			lock (databases) {
				// There may be several uris for the same db
				foreach (ProjectDom ob in databases.Values)
					list.Add (ob);
			}
			
			Set<ProjectDom> done = new Set<ProjectDom> ();
			while (list.Count > 0) 
			{
				ProjectDom readydb = null;
				ProjectDom bestdb = null;
				int bestRefCount = int.MaxValue;
				
				// Look for a db with all references resolved
				foreach (ProjectDom db in list)
				{
					bool allDone = true;
					foreach (ProjectDom refdb in db.References) {
						if (!done.Contains (refdb)) {
							allDone = false;
							break;
						}
					}
					
					if (allDone) {
						readydb = db;
						break;
					}
					else if (db.References.Count < bestRefCount) {
						bestdb = db;
						bestRefCount = db.References.Count;
						break;
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
		
		static void ConsumeParsingQueue ()
		{
			int pending = 0;
			IProgressMonitor monitor = null;
			
			try {
				Set<ProjectDom> dbsToFlush = new Set<ProjectDom> ();
				do {
					if (pending > 5 && monitor == null) {
						monitor = GetParseProgressMonitor ();
						monitor.BeginTask (GettextCatalog.GetString ("Generating database"), 0);
					}
					
					ParsingJob job = DequeueParseJob ();
					
					if (job != null) {
						try {
							job.ParseCallback (job.File, monitor);
							if (job.Database != null)
								dbsToFlush.Add (job.Database);
						} catch (Exception ex) {
							if (monitor == null)
								monitor = GetParseProgressMonitor ();
							monitor.ReportError (null, ex);
						}
					}
					
					pending = PendingJobCount;
					
				}
				while (pending > 0);
				
				queueEmptied.Set ();
				
				// Flush the parsed databases
				foreach (ProjectDom db in dbsToFlush)
					db.Flush ();
				
			} finally {
				if (monitor != null) monitor.Dispose ();
			}
		}

		static ParsedDocument UpdateFile (Project[] projects, string fileName, Func<string> getContent)
		{
			try {
				if (GetParser (fileName) == null)
					return null;
				
				ParsedDocument parseInformation = null;
				string fileContent;
				if (getContent == null) {
					StreamReader sr = new StreamReader (fileName);
					fileContent = sr.ReadToEnd ();
					sr.Close ();
				} else
					fileContent = getContent ();
				
				// Remove any pending jobs for this file
				RemoveParseJob (fileName);
				ProjectDom dom = projects != null && projects.Length > 0 ? GetProjectDom (projects [0]) : null;
				parseInformation = DoParseFile (dom, fileName, fileContent);
				if (parseInformation == null)
					return null;
				// don't update project dom with incorrect parse informations, they may not contain all
				// information.
				if (projects != null && projects.Length > 0 && parseInformation.CompilationUnit != null)
					SetSourceProject (parseInformation.CompilationUnit, dom);
				
//				if (parseInformation.Errors.Any ()) {
//					Console.WriteLine (fileName + "-- Errors:");
//					foreach (var e in parseInformation.Errors) {
//						Console.WriteLine (e);
//					}
//				}
				
				if (parseInformation.CompilationUnit != null &&
				    (parseInformation.Flags & ParsedDocumentFlags.NonSerializable) == 0) {
					if (projects != null && projects.Length > 0) {
						foreach (Project project in projects) {
							ProjectDom db = GetProjectDom (project);
							if (db != null) {
								try {
									if (parseInformation.HasErrors) {
										db.UpdateTemporaryCompilationUnit (parseInformation.CompilationUnit);
									} else {
										db.UpdateTagComments (fileName, parseInformation.TagComments);
										db.RemoveTemporaryCompilationUnit (parseInformation.CompilationUnit);
										TypeUpdateInformation res = db.UpdateFromParseInfo (parseInformation.CompilationUnit);
										if (res != null)
											NotifyTypeUpdate (project, fileName, res);
										UpdatedCommentTasks (fileName, parseInformation.TagComments, project);
									}
								} catch (Exception e) {
									LoggingService.LogError (e.ToString ());
								}
							}
						}
					} else {
						ProjectDom db = GetFileDom (fileName);
						db.UpdateFromParseInfo (parseInformation.CompilationUnit);
					}
				}
				
				return parseInformation;
			} catch (Exception e) {
				LoggingService.LogError (e.ToString ());
				return null;
			}
		}
		
		static ParsedDocument DoParseFile (ProjectDom dom, string fileName, string fileContent)
		{
			using (Counters.FileParse.BeginTiming ()) {
				IParser parser = GetParser (fileName);
				if (parser == null)
					return null;
				
				ParsedDocument parserOutput = null;
				
				if (fileContent == null) {
					try {
						if (!File.Exists (fileName))
							return null;
						using (StreamReader sr = File.OpenText (fileName)) {
							fileContent = sr.ReadToEnd();
						}
					} catch (Exception e) {
						LoggingService.LogError ("Got exception while reading file", e);
						return null;
					}
				}
	
				parserOutput = parser.Parse (dom, fileName, fileContent);
				
	/*			ParseInformation parseInformation = GetCachedParseInformation (fileName);
				bool newInfo = false;
				
				if (parseInformation == null) {
					parseInformation = new ParseInformation();
					newInfo = true;
				}
				
				if (parserOutput.Errors != null && parserOutput.Errors.Count > 0) {
					parseInformation.DirtyCompilationUnit = parserOutput;
				} else {
					parseInformation.ValidCompilationUnit = parserOutput;
					parseInformation.DirtyCompilationUnit = null;
				}
				
				if (newInfo) {
					AddToCache (parseInformation, fileName);
				}
	*/
				if (parserOutput != null) {
					AddToCache (parserOutput);
					OnParsedDocumentUpdated (new ParsedDocumentEventArgs (parserOutput));
				}
				
				return parserOutput;
			}
		}

		static void SetSourceProject (ICompilationUnit unit, ProjectDom dom)
		{
			foreach (IType t in unit.Types)
				t.SourceProjectDom = dom;
		}

		static ParsedDocument GetCachedParseInformation (string fileName)
		{
			lock (parsings) 
			{
				ParsingCacheEntry en;
				if (parsings.TryGetValue (fileName, out en)) {
					en.AccessTime = DateTime.Now;
					return en.ParseInformation;
				}
				else
					return null;
			}
		}
		
		static void AddToCache (ParsedDocument info)
		{
			if (info == null)
				throw new ArgumentNullException ("info");
			lock (parsings) 
			{
				if (parsings.Count >= MAX_PARSING_CACHE_SIZE)
				{
					DateTime tim = DateTime.MaxValue;
					string toDelete = null;
					foreach (KeyValuePair<string, ParsingCacheEntry> pce in parsings)
					{
						DateTime ptim = pce.Value.AccessTime;
						if (ptim < tim) {
							tim = ptim;
							toDelete = pce.Key;
						}
					}
					parsings.Remove (toDelete);
				}
				
				ParsingCacheEntry en = new ParsingCacheEntry();
				en.ParseInformation = info;
				en.AccessTime = DateTime.Now;
				parsings [info.FileName] = en;
			}
		}
		
		internal static int ResolveTypes (ProjectDom db, ICompilationUnit unit, IList<IType> types, out List<IType> result)
		{
			TypeResolverVisitor tr = new TypeResolverVisitor (db, unit);
			
			int unresolvedCount = 0;
			result = new List<IType> ();
			foreach (IType c in types) {
				tr.UnresolvedCount = 0;
				DomType rc = (DomType)c.AcceptVisitor (tr, null);
				rc.Resolved = true;
				// no need to set the base type here - the completion db handles that
				// (setting to system.object is wrong for enums & structs - and interfaces may never have a base)
/*				
				if (tr.UnresolvedCount == 0 && c.FullName != "System.Object") {
					// If the class has no base classes, make sure it subclasses System.Object
					if (rc.BaseType == null)
						rc.BaseType = new DomReturnType ("System.Object");
				}*/
				
				result.Add (rc);
				unresolvedCount += tr.UnresolvedCount;
			}
				
			return unresolvedCount;
		}

		internal static void StartParseOperation ()
		{
			if ((parseStatus++) == 0) {
				if (ParseOperationStarted != null)
					ParseOperationStarted (null, EventArgs.Empty);
			}
		}
		
		internal static void EndParseOperation ()
		{
			if (parseStatus == 0)
				return;

			if (--parseStatus == 0) {
				if (ParseOperationFinished != null)
					ParseOperationFinished (null, EventArgs.Empty);
			}
		}
		
		internal static void UpdatedCommentTasks (string file, IList<Tag> commentTasks, Project project)
		{
			if (CommentTasksChanged != null)
				CommentTasksChanged (null, new CommentTasksChangedEventArgs (file, commentTasks, project));
		}
		
		public static void NotifyAssemblyInfoChange (string file, string asmName)
		{
			AssemblyInformationEventArgs args = new AssemblyInformationEventArgs (file, asmName);
			if (AssemblyInformationChanged != null)
				AssemblyInformationChanged (null, args);
		}

		static void OnParsedDocumentUpdated (ParsedDocumentEventArgs args) 
		{
			if (ParsedDocumentUpdated != null) 
				ParsedDocumentUpdated (null, args);
		}
		public static event EventHandler<ParsedDocumentEventArgs> ParsedDocumentUpdated;
		
		public static void NotifyTypeUpdate (Project project, string fileName, TypeUpdateInformation info)
		{
			if (TypesUpdated != null)
				TypesUpdated (null, new TypeUpdateInformationEventArgs (project, info));
		}
		
		public static event EventHandler<TypeUpdateInformationEventArgs> TypesUpdated;
		public static event AssemblyInformationEventHandler AssemblyInformationChanged;
		public static event EventHandler<CommentTasksChangedEventArgs> CommentTasksChanged;
		public static event EventHandler SpecialCommentTagsChanged;
		public static event EventHandler ParseOperationStarted;
		public static event EventHandler ParseOperationFinished;
	}
	
	class InternalProgressMonitor: NullProgressMonitor
	{
		public InternalProgressMonitor ()
		{
			ProjectDomService.StartParseOperation ();
		}
		
		public override void Dispose ()
		{
			ProjectDomService.EndParseOperation ();
		}
	}
	
	public interface IProgressMonitorFactory
	{
		IProgressMonitor CreateProgressMonitor ();
	}
	
}
