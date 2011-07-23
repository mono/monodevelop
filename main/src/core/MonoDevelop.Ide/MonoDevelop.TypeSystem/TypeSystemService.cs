// 
// TypeSystemService.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MonoDevelop.Projects;
using Mono.Cecil;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Mono.TextEditor;
using System.Threading;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Collections;

namespace MonoDevelop.TypeSystem
{
	public static class TypeSystemServiceExt
	{
		public static Project GetProject (this IProjectContent content)
		{
			return content.Annotation<Project> ();
		}
		
		public static Project GetSourceProject (this ITypeDefinition type)
		{
			return type.ProjectContent.GetProject ();
		}
		
		public static Project GetSourceProject (this IType type)
		{
			return type.GetDefinition ().GetSourceProject ();
		}
		
		public static IProjectContent GetProjectContent (this IType type)
		{
			return type.GetDefinition ().ProjectContent;
		}
		
		public static AstLocation GetLocation (this IType type)
		{
			return new AstLocation (type.GetDefinition ().Region.BeginLine, type.GetDefinition ().Region.BeginColumn);
		}
		
		public static string GetDocumentation (this IType type)
		{
			return "TODO";
		}
		
		public static bool IsBaseType (this IType type, ITypeResolveContext ctx, IType potentialBase)
		{
			return type.GetAllBaseTypes (ctx).Any (t => t.Equals (potentialBase));
		}
		
		public static bool IsObsolete (this IEntity member)
		{
			// TODO: Implement me!
			return false;
		}
	}
	
	public static class TypeSystemService
	{
		static List<TypeSystemParserNode> parsers;
		
		static IEnumerable<TypeSystemParserNode> Parsers {
			get {
				if (parsers == null) {
//					Counters.ParserServiceInitialization.BeginTiming ();
					parsers = new List<TypeSystemParserNode> ();
					AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/Parser", delegate (object sender, ExtensionNodeEventArgs args) {
						switch (args.Change) {
						case ExtensionChange.Add:
							parsers.Add ((TypeSystemParserNode)args.ExtensionNode);
							break;
						case ExtensionChange.Remove:
							parsers.Remove ((TypeSystemParserNode)args.ExtensionNode);
							break;
						}
					});
//					Counters.ParserServiceInitialization.EndTiming ();
				}
				return parsers;
			}
		}
		
		static ITypeSystemParser GetParser (string mimeType)
		{
			var provider = Parsers.FirstOrDefault (p => p.CanParse (mimeType));
			return provider != null ? provider.Parser : null;
		}
		
		public static ParsedDocument ParseFile (Project project, string fileName)
		{
			return ParseFile (GetProjectContext (project), fileName, DesktopService.GetMimeTypeForUri (fileName), File.ReadAllText (fileName));
		}
		
		public static ParsedDocument ParseFile (IProjectContent projectContent, string fileName, string mimeType, TextReader content)
		{
			if (projectContent == null)
				throw new ArgumentNullException ("projectContent");
			var parser = GetParser (mimeType);
			if (parser == null)
				return null;
			try {
				var result = parser.Parse (projectContent, true, fileName, content);
				if ((result.Flags & ParsedDocumentFlags.NonSerializable) != ParsedDocumentFlags.NonSerializable)
					((SimpleProjectContent)projectContent).UpdateProjectContent (projectContent.GetFile (fileName), result);
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing :" + e);
				return new ParsedDocument (fileName) { Flags = ParsedDocumentFlags.NonSerializable };
			}
		}
		
		public static event EventHandler ParseOperationStarted;
		
		internal static void StartParseOperation ()
		{
			if ((parseStatus++) == 0) {
				if (ParseOperationStarted != null)
					ParseOperationStarted (null, EventArgs.Empty);
			}
		}
		
		public static event EventHandler ParseOperationFinished;
		
		internal static void EndParseOperation ()
		{
			if (parseStatus == 0)
				return;
			if (--parseStatus == 0) {
				if (ParseOperationFinished != null)
					ParseOperationFinished (null, EventArgs.Empty);
			}
		}
		public static ParsedDocument ParseFile (IProjectContent projectContent, string fileName, string mimeType, string content)
		{
			using (var reader = new StringReader (content))
				return ParseFile (projectContent, fileName, mimeType, reader);
		}
		
		public static ParsedDocument ParseFile (IProjectContent projectContent, TextEditorData data)
		{
			return ParseFile (projectContent, data.FileName, data.MimeType, data.Text);
		}
		
		#region Project loading
		public static void Load (WorkspaceItem item)
		{
			if (item is Workspace) {
				var ws = (Workspace)item;
				foreach (WorkspaceItem it in ws.Items)
				Load (it);
				ws.ItemAdded += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
			} else if (item is Solution) {
				var solution = (Solution)item;
				foreach (Project project in solution.GetAllProjects ())
					Load (project);
				solution.SolutionItemAdded += OnSolutionItemAdded;
				solution.SolutionItemRemoved += OnSolutionItemRemoved;
			}
		}
		
		static Dictionary<Project, SimpleProjectContent> projectContents = new Dictionary<Project, SimpleProjectContent> ();
		static Dictionary<Project, int> referenceCounter = new Dictionary<Project, int> ();
		
		public static void Load (Project project)
		{
			if (IncLoadCount (project) != 1)
				return;
			lock (rwLock) {
				if (projectContents.ContainsKey (project))
					return;
				try {
					var context = new SimpleProjectContent ();
					QueueParseJob (context, project);
					context.AddAnnotation (project);
					projectContents [project] = context;
					referenceCounter [project] = 1;
					OnProjectContentLoaded (new ProjectContentEventArgs (project, context));
					if (project is DotNetProject) {
						((DotNetProject)project).ReferenceAddedToProject += OnProjectReferenceAdded;
						((DotNetProject)project).ReferenceRemovedFromProject += OnProjectReferenceRemoved;
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Parser database for project '" + project.Name + " could not be loaded", ex);
				}
			}
		}
		
		public static event EventHandler<ProjectContentEventArgs> ProjectContentLoaded;
		static void OnProjectContentLoaded (ProjectContentEventArgs e)
		{
			var handler = ProjectContentLoaded;
			if (handler != null)
				handler (null, e);
		}
		
		public static void Unload (WorkspaceItem item)
		{
			if (item is Workspace) {
				var ws = (Workspace)item;
				foreach (WorkspaceItem it in ws.Items)
					Unload (it);
				ws.ItemAdded -= OnWorkspaceItemAdded;
				ws.ItemRemoved -= OnWorkspaceItemRemoved;
			} else if (item is Solution) {
				Solution solution = (Solution)item;
				StoreSolutionCache (solution);
				foreach (var project in solution.GetAllProjects ()) {
					Unload (project);
				}
				solution.SolutionItemAdded -= OnSolutionItemAdded;
				solution.SolutionItemRemoved -= OnSolutionItemRemoved;
			}
		}
		
		static void StoreSolutionCache (Solution solution)
		{
// TODO: Caching!
//			string fileName = Path.GetTempFileName ();
//			using (var stream = File.Create (fileName)) {
//				foreach (var projectContent in projectContents.Values) {
//					var prj = projectContent.GetProject ();
//					if (prj.ParentSolution != solution)
//						continue;
//				}
//			}
//			System.IO.File.Move (fileName, solution.FileName.ChangeExtension (".pidb"));
		}
		
		public static void Unload (Project project)
		{
			if (DecLoadCount (project) != 0)
				return;
			
			if (--referenceCounter [project] <= 0) {
				if (project is DotNetProject) {
					((DotNetProject)project).ReferenceAddedToProject -= OnProjectReferenceAdded;
					((DotNetProject)project).ReferenceRemovedFromProject -= OnProjectReferenceRemoved;
				}
				projectContents.Remove (project);
				referenceCounter.Remove (project);
				
				OnProjectUnloaded (new ProjectEventArgs (project));
			}
		}
		
		public static event EventHandler<ProjectEventArgs> ProjectUnloaded;

		static void OnProjectUnloaded (ProjectEventArgs e)
		{
			var handler = ProjectUnloaded;
			if (handler != null)
				handler (null, e);
		}
		
		static void OnWorkspaceItemAdded (object s, WorkspaceItemEventArgs args)
		{
			Load (args.Item);
		}
		
		static void OnWorkspaceItemRemoved (object s, WorkspaceItemEventArgs args)
		{
			Unload (args.Item);
		}
		
		static void OnSolutionItemAdded (object sender, SolutionItemChangeEventArgs args)
		{
			if (args.SolutionItem is Project)
				Load ((Project)args.SolutionItem);
		}
		
		static void OnSolutionItemRemoved (object sender, SolutionItemChangeEventArgs args)
		{
			if (args.SolutionItem is Project)
				Unload ((Project)args.SolutionItem);
		}
		
		static void OnProjectReferenceAdded (object sender, ProjectReferenceEventArgs args)
		{
//			ITypeResolveContext db = GetProjectDom (args.Project);
//			if (db != null) 
//				db.OnProjectReferenceAdded (args.ProjectReference);
		}
		
		static void OnProjectReferenceRemoved (object sender, ProjectReferenceEventArgs args)
		{
//			ITypeResolveContext db = GetProjectDom (args.Project);
//			if (db != null) 
//				db.OnProjectReferenceRemoved (args.ProjectReference);
		}
		#endregion

		#region Reference Counting
		static Dictionary<object,int> loadCount = new Dictionary<object,int> ();
		static object rwLock = new object ();
		
		static int DecLoadCount (object ob)
		{
			lock (rwLock) {
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
			lock (rwLock) {
				int c;
				if (loadCount.TryGetValue (ob, out c)) {
					c++;
					loadCount [ob] = c;
					return c;
				} else {
					loadCount [ob] = 1;
					return 1;
				}
			}
		}
		#endregion
		
		static Dictionary<string, ITypeResolveContext> assemblyContents = new Dictionary<string, ITypeResolveContext> ();
		
		class SimpleAssemblyResolver : IAssemblyResolver
		{
			string lookupPath;
			Dictionary<string, AssemblyDefinition> cache = new Dictionary<string, AssemblyDefinition> ();
			
			public SimpleAssemblyResolver (string lookupPath)
			{
				this.lookupPath = lookupPath;
			}

			public AssemblyDefinition InternalResolve (string fullName)
			{
				AssemblyDefinition result;
				if (cache.TryGetValue (fullName, out result))
					return result;
				
				var name = AssemblyNameReference.Parse (fullName);
				// need to handle different file extension casings. Some dlls from windows tend to end with .Dll or .DLL rather than '.dll'
				foreach (string file in Directory.GetFiles (lookupPath, name.Name + ".*")) {
					string ext = Path.GetExtension (file);
					if (string.IsNullOrEmpty (ext))
						continue;
					ext = ext.ToUpper ();
					if (ext == ".DLL" || ext == ".EXE") {
						result = ReadAssembly (file);
						break;
					}
				}
				
				if (result == null) {
					var framework = MonoDevelop.Projects.Services.ProjectService.DefaultTargetFramework;
					var assemblyName = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyFullName (fullName, framework);
					var location = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyLocation (assemblyName, framework);
					
					if (!string.IsNullOrEmpty (location) && File.Exists (location)) {
						result = ReadAssembly (location);
					}
				}
				if (result != null)
					cache [fullName] = result;
				return result;
			}

			#region IAssemblyResolver implementation
			public AssemblyDefinition Resolve (AssemblyNameReference name)
			{
				return InternalResolve (name.FullName) ?? GlobalAssemblyResolver.Instance.Resolve (name);
			}

			public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
			{
				return InternalResolve (name.FullName) ?? GlobalAssemblyResolver.Instance.Resolve (name, parameters);
			}

			public AssemblyDefinition Resolve (string fullName)
			{
				return InternalResolve (fullName) ?? GlobalAssemblyResolver.Instance.Resolve (fullName);
			}

			public AssemblyDefinition Resolve (string fullName, ReaderParameters parameters)
			{
				return InternalResolve (fullName) ?? GlobalAssemblyResolver.Instance.Resolve (fullName, parameters);
			}
			#endregion
		}
		
		static AssemblyDefinition ReadAssembly (string fileName)
		{
			ReaderParameters parameters = new ReaderParameters ();
			parameters.AssemblyResolver = new SimpleAssemblyResolver (Path.GetDirectoryName (fileName));
			using (var stream = new MemoryStream (File.ReadAllBytes (fileName))) {
				return AssemblyDefinition.ReadAssembly (stream, parameters);
			}
		}
		
		static bool GetXml (string baseName, out FilePath xmlFileName)
		{
			string filePattern = Path.GetFileNameWithoutExtension (baseName) + ".*";
			try {
				foreach (string fileName in Directory.EnumerateFileSystemEntries (Path.GetDirectoryName (baseName), filePattern)) {
					if (fileName.ToLower ().EndsWith (".xml")) {
						xmlFileName = fileName;
						return true;
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while retrieving file system entries.", e);
			}
			xmlFileName = "";
			return false;
		}
		
		public static ITypeResolveContext LoadAssemblyContext (string fileName)
		{
			var asm = ReadAssembly (fileName);
			if (asm == null)
				return null;
			
			var loader = new CecilLoader ();
			FilePath xmlDocFile;
			if (GetXml (fileName, out xmlDocFile)) {
				try {
					loader.DocumentationProvider = new ICSharpCode.NRefactory.Documentation.XmlDocumentationProvider (xmlDocFile);
				} catch (Exception ex) {
					LoggingService.LogWarning ("Ignoring error while reading xml doc from " + xmlDocFile, ex);
				}
			}
//			loader.InterningProvider = new SimpleInterningProvider ();
			
			var result = loader.LoadAssembly (asm);
			result.AddAnnotation (fileName);
			return result;
		}
		
		public static ITypeResolveContext LoadAssemblyContext (MonoDevelop.Core.Assemblies.TargetRuntime runtime, string fileName)
		{ // TODO: Runtimes
			var asm = ReadAssembly (fileName);
			if (asm == null)
				return null;
			var result = new CecilLoader ().LoadAssembly (asm);
			result.AddAnnotation (fileName);
			return result;
		}
		
		public static IProjectContent GetProjectContext (Project project)
		{
			if (project == null)
				return null;
			SimpleProjectContent content;
			projectContents.TryGetValue (project, out content);
			return content;
		}
		
		public static ITypeResolveContext GetContext (FilePath file, string mimeType, string text)
		{
			SimpleProjectContent content = new SimpleProjectContent ();
			var parsedFile = ParseFile (content, file, mimeType, text);
			content.UpdateProjectContent (null, parsedFile);
			return content;
		}

		public static ITypeResolveContext GetContext (Project project)
		{
			List<ITypeResolveContext> contexts = new List<ITypeResolveContext> ();
			
			SimpleProjectContent content;
			if (projectContents.TryGetValue (project, out content))
				contexts.Add (content);
			foreach (var pr in project.GetReferencedItems (ConfigurationSelector.Default)) {
				var referencedProject = pr as Project;
				if (referencedProject == null)
					continue;
				if (projectContents.TryGetValue (referencedProject, out content))
					contexts.Add (content);
			}
				
			ITypeResolveContext ctx;
			if (project is DotNetProject) {
				var netProject = (DotNetProject)project;
				
				// Add mscorlib reference
				var corLibRef = netProject.TargetRuntime.AssemblyContext.GetAssemblyForVersion (typeof(object).Assembly.FullName, null, netProject.TargetFramework);
				if (!assemblyContents.TryGetValue (corLibRef.Location, out ctx))
					assemblyContents [corLibRef.Location] = ctx = LoadAssemblyContext (corLibRef.Location);
				if (ctx != null)
					contexts.Add (ctx);
				
				// Get the assembly references throught the project, since it may have custom references
				foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
					string fileName;
					if (!Path.IsPathRooted (file)) {
						fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
					} else {
						fileName = Path.GetFullPath (file);
					}
					string refId = "Assembly:" + netProject.TargetRuntime.Id + ":" + fileName;

					if (!assemblyContents.TryGetValue (refId, out ctx)) {
						try {
							assemblyContents [refId] = ctx = LoadAssemblyContext (fileName);
						} catch (Exception) {
						}
					}
					
					if (ctx != null)
						contexts.Add (ctx);
				}
			}
			return new CompositeTypeResolveContext (contexts);
		}
		
		public static void ForceUpdate (ITypeResolveContext context)
		{
			CheckModifiedFiles ();
		}
		
		#region Parser queue
		static bool threadRunning;
		
		public static IProgressMonitorFactory ParseProgressMonitorFactory {
			get;
			set;
		}
		
			
		class InternalProgressMonitor: NullProgressMonitor
		{
			public InternalProgressMonitor ()
			{
				StartParseOperation ();
			}
			
			public override void Dispose ()
			{
				EndParseOperation ();
			}
		}

		internal static IProgressMonitor GetParseProgressMonitor ()
		{
			IProgressMonitor mon;
			if (ParseProgressMonitorFactory != null)
				mon = ParseProgressMonitorFactory.CreateProgressMonitor ();
			else
				mon = new NullProgressMonitor ();
			
			return new AggregatedProgressMonitor (mon, new InternalProgressMonitor ());
		}
		
		static Queue<ParsingJob> parseQueue = new Queue<ParsingJob>();
		class ParsingJob
		{
			public SimpleProjectContent Context;
			public Project Project;
			public IEnumerable<ProjectFile> FileList;
//			public Action<string, IProgressMonitor> ParseCallback;
			
			public void Run (IProgressMonitor monitor)
			{
				foreach (var file in (FileList ?? Project.Files)) {
					if (!string.Equals (file.BuildAction, "compile", StringComparison.OrdinalIgnoreCase)) 
						continue;
					
					var parser = TypeSystemService.GetParser (DesktopService.GetMimeTypeForUri (file.FilePath));
					if (parser == null)
						continue;
					using (var stream = new System.IO.StreamReader (file.FilePath)) {
						var parsedFile = parser.Parse (Context, false, file.FilePath, stream);
						Context.UpdateProjectContent (Context.GetFile (file.FilePath), parsedFile);
					}
//					if (ParseCallback != null)
//						ParseCallback (file.FilePath, monitor);
				}
			}
		}
		static object parseQueueLock = new object ();
		static AutoResetEvent parseEvent = new AutoResetEvent (false);
		static ManualResetEvent queueEmptied = new ManualResetEvent (true);
		static bool trackingFileChanges;
		
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
		
		static int parseStatus;
		
		public static bool IsParsing {
			get { return parseStatus > 0; }
		}
		
		static Dictionary<Project, ParsingJob> parseQueueIndex = new Dictionary<Project,ParsingJob>();
		internal static int PendingJobCount {
			get {
				lock (parseQueueLock) {
					return parseQueueIndex.Count;
				}
			}
		}
		
		public static void QueueParseJob (SimpleProjectContent context, /* Action<string, IProgressMonitor> callback,*/ Project project, IEnumerable<ProjectFile> fileList = null)
		{
			var job = new ParsingJob () {
				Context = context,
//				ParseCallback = callback,
				Project = project,
				FileList = fileList
			};
			
			lock (parseQueueLock)
			{
				RemoveParseJob (project);
				parseQueueIndex [project] = job;
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
				if (parseQueue.Count > 0) {
					var job = parseQueue.Dequeue ();
					parseQueueIndex.Remove (job.Project);
					return job;
				}
				return null;
			}
		}
		
		internal static void WaitForParseQueue ()
		{
			queueEmptied.WaitOne ();
		}
		
		static void RemoveParseJob (Project project)
		{
			lock (parseQueueLock)
			{
				ParsingJob job;
				if (parseQueueIndex.TryGetValue (project, out job)) {
					parseQueueIndex.Remove (project);
				}
			}
		}
		
		static void RemoveParseJobs (IProjectContent context)
		{
			lock (parseQueueLock)
			{
				foreach (var pj in parseQueue) {
					if (pj.Context == context) {
						parseQueueIndex.Remove (pj.Project);
					}
				}
			}
		}
		
		static void StartParserThread()
		{
			lock (parseQueueLock) {
				if (!threadRunning) {
					threadRunning = true;
					var t = new Thread (new ThreadStart (ParserUpdateThread));
					t.Name = "Background parser";
					t.IsBackground  = true;
					t.Priority = ThreadPriority.AboveNormal;
					t.Start ();
				}
			}
		}
		
		static void ParserUpdateThread()
		{
			try {
				while (trackingFileChanges) {
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

		static bool IsFileModified (ProjectFile file, IParsedFile parsedFile)
		{
			if (parsedFile == null)
				return true;
			return System.IO.File.GetLastWriteTime (file.FilePath) > parsedFile.ParseTime;
		}

		static void CheckModifiedFiles (Project project, SimpleProjectContent content)
		{
			List<ProjectFile> modifiedFiles = null;
			foreach (var file in project.Files) {
				if (!string.Equals (file.BuildAction, "compile", StringComparison.OrdinalIgnoreCase)) 
					continue;
					
				var parser = TypeSystemService.GetParser (DesktopService.GetMimeTypeForUri (file.FilePath));
				if (parser == null)
					continue;
				
				if (!IsFileModified (file, content.GetFile (file.FilePath)))
					continue;
				if (modifiedFiles == null)
					modifiedFiles = new List<ProjectFile> ();
				modifiedFiles.Add (file);
			}
			if (modifiedFiles == null)
				return;
			QueueParseJob (content, project, modifiedFiles);
		}
		
		static void CheckModifiedFiles ()
		{
			Queue<KeyValuePair<Project, SimpleProjectContent>> list;
			
			lock (rwLock) {
				list = new Queue<KeyValuePair<Project, SimpleProjectContent>> (projectContents);
			}
			
			while (list.Count > 0) {
				var readydb = list.Dequeue ();
				CheckModifiedFiles (readydb.Key, readydb.Value);
			}
		}
		
		static void ConsumeParsingQueue ()
		{
			int pending = 0;
			IProgressMonitor monitor = null;
			
			try {
				do {
					if (pending > 5 && monitor == null) {
						monitor = GetParseProgressMonitor ();
						monitor.BeginTask (GettextCatalog.GetString ("Generating database"), 0);
					}
					var job = DequeueParseJob ();
					if (job != null) {
						try {
							job.Run (monitor);
						} catch (Exception ex) {
							if (monitor == null)
								monitor = GetParseProgressMonitor ();
							monitor.ReportError (null, ex);
						}
					}
					
					pending = PendingJobCount;
					
				} while (pending > 0);
				
				queueEmptied.Set ();
			} finally {
				if (monitor != null)
					monitor.Dispose ();
			}
		}
		#endregion
	}
}

