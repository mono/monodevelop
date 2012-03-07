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
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Mono.TextEditor;
using System.Threading;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Collections;
using System.Xml;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core.AddIns;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.Documentation;

namespace MonoDevelop.TypeSystem
{
	public static class TypeSystemServiceExt
	{
		public static Project GetProject (this IProjectContent content)
		{
			return TypeSystemService.GetProject (content.Location);
		}
		
		public static Project GetSourceProject (this ITypeDefinition type)
		{
			return TypeSystemService.GetProject (type.Compilation.MainAssembly.UnresolvedAssembly.Location);
		}
		
		public static Project GetSourceProject (this IType type)
		{
			return type.GetDefinition ().GetSourceProject ();
		}
		
		public static IProjectContent GetProjectContent (this IType type)
		{
			return TypeSystemService.GetProjectContext (type.GetSourceProject ());
		}
		
		public static TextLocation GetLocation (this IType type)
		{
			return type.GetDefinition ().Region.Begin;
		}
		
		public static bool IsBaseType (this IType type, IType potentialBase)
		{
			return type.GetAllBaseTypes ().Any (t => t.Equals (potentialBase));
		}
		
		public static bool IsObsolete (this IEntity member)
		{
			if (member == null)
				return false;
			return member.Attributes.Any (a => a.AttributeType.FullName == "System.ObsoleteAttribute");
		}
		
		public static IType Resolve (this IUnresolvedTypeDefinition def, Project project)
		{
			var pf = TypeSystemService.GetProjectContext (project).GetFile (def.Region.FileName);
			var ctx = pf.GetTypeResolveContext (TypeSystemService.GetCompilation (project), def.Region.Begin);
			return def.Resolve (ctx);
		}
		
		public static ITypeDefinition LookupType (this ICompilation compilation, string ns, string name, int typeParameterCount = 0)
		{
			var result = compilation.MainAssembly.GetTypeDefinition (ns, name, typeParameterCount);
			if (result != null)
				return result;
			foreach (var refAsm in compilation.ReferencedAssemblies) {
				result = refAsm.GetTypeDefinition (ns, name, typeParameterCount);
				if (result != null)
					return result;
			}
			return null;
		}
		
		public static ITypeDefinition LookupType (this ICompilation compilation, string fullName, int typeParameterCount = 0)
		{
			int idx = fullName.LastIndexOf ('.');
			string ns, name;
			if (idx > 0) {
				ns = fullName.Substring (0, idx);
				name = fullName.Substring (idx + 1);
			} else {
				ns = "";
				name = fullName;
			}
			return compilation.LookupType (ns, name, typeParameterCount);
		}
	}
	
	/// <summary>
	/// The folding parser is used for generating a preliminary parsed document that does not
	/// contain a full dom - only some basic lexical constructs like comments or pre processor directives.
	/// 
	/// This is useful for opening a document the first time to have some folding regions as start that are folded by default.
	/// Otherwise an irritating screen update will occur.
	/// </summary>
	public interface IFoldingParser
	{
		ParsedDocument Parse (string fileName, string content);
	}
	
	public static class TypeSystemService
	{
		static List<TypeSystemParserNode> parsers;
		
		static IEnumerable<TypeSystemParserNode> Parsers {
			get {
				return parsers;
			}
		}
		
		static TypeSystemService ()
		{
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
		}
		
		static ITypeSystemParser GetParser (string mimeType)
		{
			var provider = Parsers.FirstOrDefault (p => p.CanParse (mimeType));
			return provider != null ? provider.Parser : null;
		}
		
		static TypeSystemParserNode GetTypeSystemParserNode (string mimeType)
		{
			return Parsers.FirstOrDefault (p => p.CanParse (mimeType));
		}
		
		static List<MimeTypeExtensionNode> foldingParsers;

		static IEnumerable<MimeTypeExtensionNode> FoldingParsers {
			get {
				if (foldingParsers == null) {
					foldingParsers = new List<MimeTypeExtensionNode> ();
					AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/FoldingParser", delegate (object sender, ExtensionNodeEventArgs args) {
						switch (args.Change) {
						case ExtensionChange.Add:
							foldingParsers.Add ((MimeTypeExtensionNode)args.ExtensionNode);
							break;
						case ExtensionChange.Remove:
							foldingParsers.Remove ((MimeTypeExtensionNode)args.ExtensionNode);
							break;
						}
					});
				}
				return foldingParsers;
			}
		}
		
		public static IFoldingParser GetFoldingParser (string mimeType)
		{
			var node = FoldingParsers.Where (n => n.MimeType == mimeType).FirstOrDefault ();
			if (node == null)
				return null;
			return node.CreateInstance () as IFoldingParser;
		}

		public static ParsedDocument ParseFile (Project project, string fileName)
		{
			string text;
			
			try {
				if (!File.Exists (fileName))
					return null;
				text = File.ReadAllText (fileName);
			} catch (Exception) {
				return null;
			}
			
			return ParseFile (project, fileName, DesktopService.GetMimeTypeForUri (fileName), text);
		}
		
		public static ParsedDocument ParseFile (Project project, string fileName, string mimeType, TextReader content)
		{
			ProjectContentWrapper wrapper;
			if (project != null) {
				projectContents.TryGetValue (project, out wrapper);
			} else {
				wrapper = null;
			}
			
			var parser = GetParser (mimeType);
			if (parser == null)
				return null;
			try {
				var result = parser.Parse (true, fileName, content, project);
				if (wrapper != null && (result.Flags & ParsedDocumentFlags.NonSerializable) != ParsedDocumentFlags.NonSerializable)
					wrapper.Content = wrapper.Content.UpdateProjectContent (wrapper.Content.GetFile (fileName), result.ParsedFile);
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing :" + e);
				return null;
			}
		}
		
		public static ParsedDocument ParseFile (Project project, string fileName, string mimeType, string content)
		{
			using (var reader = new StringReader (content))
				return ParseFile (project, fileName, mimeType, reader);
		}
		
		public static ParsedDocument ParseFile (Project project, TextEditorData data)
		{
			return ParseFile (project, data.FileName, data.MimeType, data.Text);
		}
		
		public static ParsedDocument ParseFile (string fileName, string mimeType, string text, ProjectContentWrapper wrapper = null)
		{
			using (var reader = new StringReader (text))
				return ParseFile (fileName, mimeType, reader, wrapper);
		}
		
		public static ParsedDocument ParseFile (string fileName, string mimeType, TextReader content, ProjectContentWrapper wrapper = null)
		{
			var parser = GetParser (mimeType);
			if (parser == null)
				return null;
			try {
				var result = parser.Parse (true, fileName, content);
				if (wrapper != null && (result.Flags & ParsedDocumentFlags.NonSerializable) != ParsedDocumentFlags.NonSerializable)
					wrapper.Content = wrapper.Content.UpdateProjectContent (wrapper.Content.GetFile (fileName), result.ParsedFile);
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing :" + e);
				return null;
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
		#region Parser Database Handling

		static string GetCacheDirectory (string filename)
		{
			string result;
			var nameNoExtension = Path.GetFileNameWithoutExtension (filename);
			var derivedDataPath = UserProfile.Current.CacheDir.Combine ("DerivedData");
		
			try {
				// First try to access what we think could be the correct file directly
				if (CheckCacheDirectoryIsCorrect (filename, derivedDataPath.Combine (nameNoExtension), out result))
					return result;

				if (Directory.Exists (derivedDataPath)) {
					var subDirs = Directory.GetDirectories (derivedDataPath);
					// next check any directory which contains the filename
					foreach (var subDir in subDirs.Where (s=> s.Contains (nameNoExtension)))
						if (CheckCacheDirectoryIsCorrect (filename, derivedDataPath.Combine (subDir), out result))
							return result;

					// Finally check every remaining directory
					foreach (var subDir in subDirs.Where (s=> !s.Contains (nameNoExtension)))
						if (CheckCacheDirectoryIsCorrect (filename, derivedDataPath.Combine (subDir), out result))
							return result;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting derived data directories.", e);
			}
			return null;
		}
		
		static bool CheckCacheDirectoryIsCorrect (FilePath filename, FilePath candidate, out string result)
		{
			result = null;
			var dataPath = candidate.Combine ("data.xml");
			
			try {
				if (!File.Exists (dataPath))
				    return false;
				    
				using (var reader = XmlReader.Create (dataPath)) {
					while (reader.Read ()) {
						if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "File") {
							if (reader.GetAttribute ("name") == filename) {
								result = candidate;
								return true;
							}
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading derived data file " + dataPath, e);
			}
			
			result = null;
			return false;
		}
		
		static string GetName (string baseName, int i)
		{
			if (i == 0)
				return baseName;
			return baseName + "-" + i;
		}
		
		static string CreateCacheDirectory (string fileName)
		{
			try {
				string derivedDataPath = UserProfile.Current.CacheDir.Combine ("DerivedData");
				string name = Path.GetFileNameWithoutExtension (fileName);
				string baseName = Path.Combine (derivedDataPath, name);
				int i = 0;
				while (Directory.Exists (GetName (baseName, i)))
					i++;
				
				string cacheDir = GetName (baseName, i);
				
				Directory.CreateDirectory (cacheDir);
				
				System.IO.File.WriteAllText (Path.Combine (cacheDir, "data.xml"), string.Format ("<DerivedData><File name=\"{0}\"/></DerivedData>", fileName));
				return cacheDir;
			} catch (Exception e) {
				LoggingService.LogError ("Error creating cache for " + fileName, e);
				return null;
			}
		}
		
		static T DeserializeObject<T> (string path) where T : class
		{
			try {
				using (var fs = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)) {
					using (var reader = new BinaryReaderWith7BitEncodedInts (fs)) {
						var s = new FastSerializer ();
						return (T)s.Deserialize (reader);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading type system cache.", e);
				return default(T);
			}
		}
		
		static void SerializeObject (string path, object obj)
		{
			try {
				using (var fs = new FileStream (path, FileMode.Create, FileAccess.Write)) {
					using (var writer = new BinaryWriterWith7BitEncodedInts (fs)) {
						FastSerializer s = new FastSerializer ();
						s.Serialize (writer, obj);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while writing type system cache.", e);
			}
		}
		
		static ConcurrentDictionary<string, IProjectContent> projectCache = new ConcurrentDictionary<string, IProjectContent> ();
		
		/// <summary>
		/// Removes all cache directories which are older than 30 days.
		/// </summary>
		static void CleanupCache ()
		{
			string derivedDataPath = UserProfile.Current.CacheDir.Combine ("DerivedData");
			string[] subDirs;
			
			try {
				if (!Directory.Exists (derivedDataPath))
					return;
				subDirs = Directory.GetDirectories (derivedDataPath);
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting derived data directories.", e);
				return;
			}
			
			foreach (var subDir in subDirs) {
				try {
					var days = Math.Abs ((DateTime.Now - Directory.GetLastWriteTime (subDir)).TotalDays);
					if (days > 30)
						Directory.Delete (subDir, true);
				} catch (Exception e) {
					LoggingService.LogError ("Error while removing outdated cache " + subDir, e);
				}
			}
		}
		
		static void RemoveCache (string cacheDir)
		{
			try {
				Directory.Delete (cacheDir, true);
			} catch (Exception e) {
				LoggingService.LogError ("Error while removing cache " + cacheDir, e);
			}
		}
		
		static void TouchCache (string cacheDir)
		{
			try {
				Directory.SetLastWriteTime (cacheDir, DateTime.Now);
			} catch (Exception e) {
				LoggingService.LogError ("Error while touching cache directory " + cacheDir, e);
			}
		}
		
		static void LoadProjectCache (Project project)
		{
			string cacheDir = GetCacheDirectory (project.FileName);
			if (cacheDir == null)
				return;
			
			TouchCache (cacheDir);
			var cache = DeserializeObject<IProjectContent> (Path.Combine (cacheDir, "completion.cache"));
			if (projectCache == null) {
				RemoveCache (cacheDir);
			} else {
				projectCache[project.FileName] = cache;
			}
		}
		
		static void StoreProjectCache (Project project, ProjectContentWrapper wrapper)
		{
			if (!wrapper.WasChanged)
				return;
			string cacheDir = GetCacheDirectory (project.FileName) ?? CreateCacheDirectory (project.FileName);
			TouchCache (cacheDir);
			string fileName = Path.GetTempFileName ();
			
			SerializeObject (fileName, wrapper.Content.RemoveAssemblyReferences (wrapper.Content.AssemblyReferences));
			
			string cacheFile = Path.Combine (cacheDir, "completion.cache");
			
			try {
				if (File.Exists (cacheFile))
					System.IO.File.Delete (cacheFile);
				
				System.IO.File.Move (fileName, cacheFile);
			} catch (Exception e) {
				LoggingService.LogError ("Error whil saving cache " + cacheFile, e);
			}
		}
		#endregion
		
		#region Project loading
		public static void Load (WorkspaceItem item)
		{
			InternalLoad (item);
			ReloadAllReferences ();
			CleanupCache ();
		}
		
		
		static void InternalLoad (WorkspaceItem item)
		{
			if (item is Workspace) {
				var ws = (Workspace)item;
				foreach (WorkspaceItem it in ws.Items)
					InternalLoad (it);
				ws.ItemAdded += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
			} else if (item is Solution) {
				var solution = (Solution)item;
				Parallel.ForEach (solution.GetAllProjects (), project => Load (project));
				solution.SolutionItemAdded += OnSolutionItemAdded;
				solution.SolutionItemRemoved += OnSolutionItemRemoved;
			}
		}
		
		static void ReloadAllReferences ()
		{
			lock (rwLock) {
				foreach (var wrapper in projectContents)
					wrapper.Value.ReloadAssemblyReferences (wrapper.Key);
			}
		}
		
		[Serializable]
		public class UnresolvedAssemblyDecorator : IUnresolvedAssembly
		{
			ProjectContentWrapper wrapper;
			
			IUnresolvedAssembly assembly {
				get {
					return wrapper.Compilation.MainAssembly.UnresolvedAssembly;
				}
			}
			
			public UnresolvedAssemblyDecorator (ProjectContentWrapper wrapper)
			{
				this.wrapper = wrapper;
			}

			#region IUnresolvedAssembly implementation
			public string AssemblyName {
				get {
					return assembly.AssemblyName;
				}
			}
			
			public string Location {
				get {
					return assembly.Location;
				}
				set {
					assembly.Location = value;
				}
			}

			public IEnumerable<IUnresolvedAttribute> AssemblyAttributes {
				get {
					return assembly.AssemblyAttributes;
				}
			}

			public IEnumerable<IUnresolvedAttribute> ModuleAttributes {
				get {
					return assembly.ModuleAttributes;
				}
			}

			public IEnumerable<IUnresolvedTypeDefinition> TopLevelTypeDefinitions {
				get {
					return assembly.TopLevelTypeDefinitions;
				}
			}
			#endregion

			#region IAssemblyReference implementation
			public IAssembly Resolve (ITypeResolveContext context)
			{
				return assembly.Resolve (context);
			}
			#endregion
		}
		
		[Serializable]
		public class ProjectContentWrapper
		{
			IProjectContent content;

			public IProjectContent Content {
				get {
					return content;
				}
				set {
					content = value;
					compilation = null;
					WasChanged = true;
				}
			}
			
			public bool WasChanged = false;
			
			[NonSerialized]
			ICompilation compilation = null;
			
			public ICompilation Compilation {
				get {
					if (compilation == null)
						compilation = Content.CreateCompilation ();
					return compilation;
				}
			}
			
			public Project Project {
				get;
				private set;
			}
			
			public ProjectContentWrapper (Project project, IProjectContent content)
			{
				if (project == null)
					throw new ArgumentNullException ("project");
				if (content == null)
					throw new ArgumentNullException ("content");
				this.Project = project;
				this.content = content;
			}
			
			public IEnumerable<Project> ReferencedProjects {
				get {
					foreach (var pr in Project.GetReferencedItems (ConfigurationSelector.Default)) {
						var referencedProject = pr as Project;
						if (referencedProject != null)
							yield return referencedProject;
					}
				}
			}

			public void ReloadAssemblyReferences (Project project)
			{
				var netProject = project as DotNetProject;
				if (netProject == null)
					return;
				try {
					var contexts = new List<IAssemblyReference> ();
			
					foreach (var referencedProject in ReferencedProjects) {
						ProjectContentWrapper wrapper;
						if (projectContents.TryGetValue (referencedProject, out wrapper))
							contexts.Add (new UnresolvedAssemblyDecorator (wrapper));
					}
					
					AssemblyContext ctx;
					// Add mscorlib reference
					if (netProject.TargetRuntime != null && netProject.TargetRuntime.AssemblyContext != null) {
						var corLibRef = netProject.TargetRuntime.AssemblyContext.GetAssemblyForVersion (typeof(object).Assembly.FullName, null, netProject.TargetFramework);
						if (corLibRef != null) {
							ctx = LoadAssemblyContext (corLibRef.Location);
							if (ctx != null)
								contexts.Add (ctx);
						}
					}
					
					// Get the assembly references throught the project, since it may have custom references
					foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
						string fileName;
						if (!Path.IsPathRooted (file)) {
							fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
						} else {
							fileName = Path.GetFullPath (file);
						}
						ctx = LoadAssemblyContext (fileName);
						
						if (ctx != null)
							contexts.Add (ctx);
					}
					bool changed = WasChanged;
					Content = Content.RemoveAssemblyReferences (Content.AssemblyReferences);
					Content = Content.AddAssemblyReferences (contexts);
					WasChanged = changed;
				} catch (Exception e) {
					if (netProject.TargetRuntime == null) {
						LoggingService.LogError ("Target runtime was null:" + Project);
					} else if (netProject.TargetRuntime.AssemblyContext == null) {
						LoggingService.LogError ("Target runtime assambly context was null:" + Project);
					}
					LoggingService.LogError ("Error while reloading all references of project:" + Project, e);
				}
			}
		}
		
		static Dictionary<Project, ProjectContentWrapper> projectContents = new Dictionary<Project, ProjectContentWrapper> ();
		static Dictionary<Project, int> referenceCounter = new Dictionary<Project, int> ();
		
		public static void Load (Project project)
		{
			if (IncLoadCount (project) != 1)
				return;
			LoadProjectCache (project);
			lock (rwLock) {
				if (projectContents.ContainsKey (project))
					return;
				try {
					IProjectContent context = null;
					if (projectCache.ContainsKey (project.FileName))
						context = projectCache [project.FileName];
					
					ProjectContentWrapper wrapper;
					if (context == null) {
						context = new ICSharpCode.NRefactory.CSharp.CSharpProjectContent ();
						projectContents [project] = wrapper = new ProjectContentWrapper (project, context);
						QueueParseJob (projectContents [project]);
					} else {
						projectContents [project] = wrapper = new ProjectContentWrapper (project, context);
					}
					
					referenceCounter [project] = 1;
					OnProjectContentLoaded (new ProjectContentEventArgs (project, context));
					project.FileChangedInProject += OnFileChanged;
					project.FileAddedToProject += OnFileAdded;
					project.FileRemovedFromProject += OnFileRemoved;
					project.FileRenamedInProject += OnFileRenamed;
					project.Modified += OnProjectModified;
				} catch (Exception ex) {
					LoggingService.LogError ("Parser database for project '" + project.Name + " could not be loaded", ex);
				}
			}
		}
		
		public static Project GetProject (IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");

			ITypeDefinition def;
			if (entity is IType) {
				def = ((IType)entity).GetDefinition ();
			} else {
				def = entity.DeclaringTypeDefinition;
			}
			if (def == null)
				return null;
			
			return GetProject (def.Compilation.MainAssembly.UnresolvedAssembly.Location);
				
		}
		
		public static Project GetProject (string location)
		{
			foreach (var wrapper in projectContents) 
				if (wrapper.Value.Compilation.MainAssembly.UnresolvedAssembly.Location == location)
					return wrapper.Key;
			return null;
		}
		
		#region Project modification handlers
		static void OnFileChanged (object sender, ProjectFileEventArgs args)
		{
			var project = (Project)sender;
			foreach (ProjectFileEventInfo fargs in args) {
				QueueParseJob (projectContents [project], new [] { fargs.ProjectFile });
			}
		}
		
		static void OnFileAdded (object sender, ProjectFileEventArgs args)
		{
			var project = (Project)sender;
			foreach (ProjectFileEventInfo fargs in args) {
				QueueParseJob (projectContents [project], new [] { fargs.ProjectFile });
			}
		}

		static void OnFileRemoved (object sender, ProjectFileEventArgs args)
		{
			var project = (Project)sender;
			foreach (ProjectFileEventInfo fargs in args) {
				projectContents [project].Content = projectContents [project].Content.UpdateProjectContent (projectContents [project].Content.GetFile (fargs.ProjectFile.Name), null);
			}
		}

		static void OnFileRenamed (object sender, ProjectFileRenamedEventArgs args)
		{
			var project = (Project)sender;
			foreach (ProjectFileRenamedEventInfo fargs in args) {
				projectContents [project].Content = projectContents [project].Content.UpdateProjectContent (projectContents [project].Content.GetFile (fargs.OldName), null);
				QueueParseJob (projectContents [project], new [] { fargs.ProjectFile });
			}
		}
		
		static void OnProjectModified (object sender, SolutionItemModifiedEventArgs args)
		{
			if (!args.Any (x => x is SolutionItemModifiedEventInfo && (((SolutionItemModifiedEventInfo)x).Hint == "TargetFramework" || ((SolutionItemModifiedEventInfo)x).Hint == "References")))
				return;
			cachedProjectContents = new Dictionary<Project, ITypeResolveContext> ();
			var project = (Project)sender;
			
			ProjectContentWrapper wrapper;
			projectContents.TryGetValue (project, out wrapper);
			if (wrapper == null)
				return;
			ReloadAllReferences ();
		}
		#endregion
		
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
				Parallel.ForEach (solution.GetAllProjects (), project => Unload (project));
				solution.SolutionItemAdded -= OnSolutionItemAdded;
				solution.SolutionItemRemoved -= OnSolutionItemRemoved;
			}
		}
		
		public static void Unload (Project project)
		{
			if (DecLoadCount (project) != 0)
				return;
			
			if (referenceCounter.ContainsKey (project) && --referenceCounter [project] <= 0) {
				project.FileChangedInProject -= OnFileChanged;
				project.FileAddedToProject -= OnFileAdded;
				project.FileRemovedFromProject -= OnFileRemoved;
				project.FileRenamedInProject -= OnFileRenamed;
				project.Modified -= OnProjectModified;
				
				var wrapper = projectContents [project];
				projectContents.Remove (project);
				referenceCounter.Remove (project);
				StoreProjectCache (project, wrapper);
				
				OnProjectUnloaded (new ProjectUnloadEventArgs (project, wrapper));
			}
		}
		
		public static event EventHandler<ProjectUnloadEventArgs> ProjectUnloaded;

		static void OnProjectUnloaded (ProjectUnloadEventArgs e)
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
		
		
		class SimpleAssemblyResolver : IAssemblyResolver
		{
			string lookupPath;
			Dictionary<string, AssemblyDefinition> cache = new Dictionary<string, AssemblyDefinition> ();
			DefaultAssemblyResolver defaultResolver = new DefaultAssemblyResolver ();
			
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
				return InternalResolve (name.FullName) ?? defaultResolver.Resolve (name);
			}

			public AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
			{
				return InternalResolve (name.FullName) ?? defaultResolver.Resolve (name, parameters);
			}

			public AssemblyDefinition Resolve (string fullName)
			{
				return InternalResolve (fullName) ?? defaultResolver.Resolve (fullName);
			}

			public AssemblyDefinition Resolve (string fullName, ReaderParameters parameters)
			{
				return InternalResolve (fullName) ?? defaultResolver.Resolve (fullName, parameters);
			}
			#endregion
		}
		
		static AssemblyDefinition ReadAssembly (string fileName)
		{
			ReaderParameters parameters = new ReaderParameters ();
			parameters.AssemblyResolver = new DefaultAssemblyResolver (); // new SimpleAssemblyResolver (Path.GetDirectoryName (fileName));
			using (var stream = new MemoryStream (File.ReadAllBytes (fileName))) {
				return AssemblyDefinition.ReadAssembly (stream, parameters);
			}
		}
		
		static bool GetXml (string baseName, MonoDevelop.Core.Assemblies.TargetRuntime runtime, out FilePath xmlFileName)
		{
			string filePattern = Path.GetFileNameWithoutExtension (baseName) + ".*";
			try {
				foreach (string fileName in Directory.EnumerateFileSystemEntries (Path.GetDirectoryName (baseName), filePattern)) {
					if (fileName.ToLower ().EndsWith (".xml")) {
						xmlFileName = LookupLocalizedXmlDoc (fileName);
						return true;
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while retrieving file system entries.", e);
			}
			
			if (MonoDevelop.Core.Platform.IsWindows) {
				string windowsFileName = FindXmlDocumentation (baseName, runtime);
				if (File.Exists (windowsFileName)) {
					xmlFileName = windowsFileName;
					return true;
				}
			}
			
			xmlFileName = "";
			return false;
		}
		
		#region Lookup XML documentation
		static readonly string referenceAssembliesPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\\Framework");
		static readonly string frameworkPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework");
		
		static string FindXmlDocumentation (string assemblyFileName, MonoDevelop.Core.Assemblies.TargetRuntime runtime)
		{
			string fileName;
			ClrVersion version = runtime != null && runtime.CustomFrameworks.Any () ? runtime.CustomFrameworks.First ().ClrVersion : ClrVersion.Default;
			switch (version) {
//			case "1.0":
//				fileName = LookupLocalizedXmlDoc (Path.Combine (frameworkPath, "v1.0.3705", assemblyFileName));
//				break;
			case ClrVersion.Net_1_1:
				fileName = LookupLocalizedXmlDoc (Path.Combine (frameworkPath, "v1.1.4322", assemblyFileName));
				break;
			case ClrVersion.Net_2_0:
			case ClrVersion.Clr_2_1:
				fileName = LookupLocalizedXmlDoc (Path.Combine (frameworkPath, "v2.0.50727", assemblyFileName))
						?? LookupLocalizedXmlDoc (Path.Combine (referenceAssembliesPath, "v3.5"))
						?? LookupLocalizedXmlDoc (Path.Combine (referenceAssembliesPath, "v3.0"))
						?? LookupLocalizedXmlDoc (Path.Combine (referenceAssembliesPath, @".NETFramework\v3.5\Profile\Client"));
				break;
			case ClrVersion.Net_4_0:
			case ClrVersion.Default:
			default:
				fileName = LookupLocalizedXmlDoc (Path.Combine (referenceAssembliesPath, @".NETFramework\v4.0", assemblyFileName))
						?? LookupLocalizedXmlDoc (Path.Combine (frameworkPath, "v4.0.30319", assemblyFileName));
				break;
			}
			return fileName;
		}
		
		static string LookupLocalizedXmlDoc (string fileName)
		{
			return XmlDocumentationProvider.LookupLocalizedXmlDoc (fileName);
		}
		#endregion

		
		[Serializable]
		class AssemblyContext : IUnresolvedAssembly
		{
			public string FileName;
			public DateTime LastWriteTime;
			
			[NonSerialized]
			internal LazyAssemblyLoader CtxLoader;
			
			public IUnresolvedAssembly Ctx {
				get {
					return CtxLoader.Assembly;
				}
			}

			#region IUnresolvedAssembly implementation
			string IUnresolvedAssembly.AssemblyName {
				get {
					return Ctx.AssemblyName;
				}
			}

			string IUnresolvedAssembly.Location {
				get {
					return Ctx.Location;
				}
				set {
					Ctx.Location = value;
				}
			}

			IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.AssemblyAttributes {
				get {
					return Ctx.AssemblyAttributes;
				}
			}

			IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.ModuleAttributes {
				get {
					return Ctx.ModuleAttributes;
				}
			}

			IEnumerable<IUnresolvedTypeDefinition> IUnresolvedAssembly.TopLevelTypeDefinitions {
				get {
					return Ctx.TopLevelTypeDefinitions;
				}
			}
			#endregion

			#region IAssemblyReference implementation
			IAssembly IAssemblyReference.Resolve (ITypeResolveContext context)
			{
				return Ctx.Resolve (context);
			}
			#endregion
		}
		
		class LazyAssemblyLoader
		{
			string fileName;
			string cache;
			
			IUnresolvedAssembly assembly;
			
			public IUnresolvedAssembly Assembly {
				get {
					if (assembly != null)
						return assembly;
					return assembly = LoadAssembly () ?? new DefaultUnresolvedAssembly (fileName);
				}
			}
			
			public LazyAssemblyLoader (string fileName, string cache)
			{
				this.fileName = fileName;
				this.cache = cache;
			}
			
			IUnresolvedAssembly LoadAssembly ()
			{
				var assemblyPath = Path.Combine (cache, "assembly.data");
				try {
					if (File.Exists (assemblyPath)) {
						var deserializedAssembly = DeserializeObject <IUnresolvedAssembly> (assemblyPath);
						if (deserializedAssembly != null)
							return deserializedAssembly;
					}
				} catch (Exception) {
				}
				
				var asm = ReadAssembly (fileName);
				if (asm == null)
					return null;
				
				var loader = new CecilLoader ();
				FilePath xmlDocFile;
				
				var assembly = loader.LoadAssembly (asm);
				assembly.Location = fileName;
				
				if (cache != null)
					SerializeObject (assemblyPath, assembly);
				return assembly;
			}
		}
		
		[Serializable]
		class CombinedDocumentationProvider : IDocumentationProvider
		{
			string fileName;
			
			[NonSerialized]
			IDocumentationProvider baseProvider = null;

			public IDocumentationProvider BaseProvider {
				get {
					if (baseProvider == null) {
						FilePath xmlDocFile;
						if (GetXml (fileName, null, out xmlDocFile)) {
							try {
								baseProvider = new ICSharpCode.NRefactory.Documentation.XmlDocumentationProvider (xmlDocFile);
							} catch (Exception ex) {
								LoggingService.LogWarning ("Ignoring error while reading xml doc from " + xmlDocFile, ex);
							} 
						}
						if (baseProvider == null)
							baseProvider = new MonoDocDocumentationProvider ();
					}
					return baseProvider;
				}
			}
			
			public CombinedDocumentationProvider (string fileName)
			{
				this.fileName = fileName;
			}

			#region IDocumentationProvider implementation
			public DocumentationComment GetDocumentation (IEntity entity)
			{
				var provider = BaseProvider;
				return provider != null ? provider.GetDocumentation (entity) : null;
			}
			#endregion
		}
		
		static AssemblyContext LoadAssemblyContext (string fileName)
		{
			AssemblyContext loadedContext;
			if (cachedAssemblyContents.TryGetValue (fileName, out loadedContext))
				return loadedContext;
			if (!File.Exists (fileName))
				return null;
			string cache = GetCacheDirectory (fileName);
			if (cache != null) {
				TouchCache (cache);
				var deserialized = DeserializeObject <AssemblyContext> (Path.Combine (cache, "assembly.descriptor"));
				if (deserialized != null) {
					deserialized.CtxLoader = new LazyAssemblyLoader (fileName, cache);
					cachedAssemblyContents [fileName] = deserialized;
					return deserialized;
				} else {
					RemoveCache (cache);
				}
			} else {
				cache = CreateCacheDirectory (fileName);
			}
			
			try {
				var result = new AssemblyContext () {
					FileName = fileName,
					LastWriteTime = File.GetLastWriteTime (fileName)
				};
				SerializeObject (Path.Combine (cache, "assembly.descriptor"), result);
				
				result.CtxLoader = new LazyAssemblyLoader (fileName, cache);
				cachedAssemblyContents [fileName] = result;
				return result;
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading assembly " + fileName, ex);
				return null;
			}
		}
		
		public static ICompilation LoadAssemblyContext (MonoDevelop.Core.Assemblies.TargetRuntime runtime, string fileName)
		{
			var asm = ReadAssembly (fileName);
			if (asm == null)
				return null;
			var loc = runtime.AssemblyContext.GetAssemblyLocation ("mscorlib", null);
			
			var mscorlibAsm = loc != fileName ? ReadAssembly (loc) : asm;
			
			var loader = new CecilLoader ();
			loader.DocumentationProvider = new CombinedDocumentationProvider (fileName);
			
			var unresolvedAssembly = loader.LoadAssembly (asm);
			var corLibLoader = new CecilLoader ();
			
			var mscorlib = corLibLoader.LoadAssembly (mscorlibAsm);
			
			return new SimpleCompilation (unresolvedAssembly, mscorlib);
		}
		
		public static IProjectContent GetProjectContext (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			var content = GetProjectContentWrapper (project);
			return content.Content;
		}
		
		public static ICompilation GetCompilation (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			var content = GetProjectContentWrapper (project);
			return content.Compilation;
		}
		
		public static ProjectContentWrapper GetProjectContentWrapper (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			ProjectContentWrapper content;
			if (projectContents.TryGetValue (project, out content)) {
				if (content.Content != null)
					content.Content.Location = project.FileName;
				return content;
			}
			return new ProjectContentWrapper (project, new CSharpProjectContent () { Location = project.FileName });
		}
		
		public static IProjectContent GetContext (FilePath file, string mimeType, string text)
		{
			using (var reader = new StringReader (text)) {
				var parsedDocument = ParseFile (file, mimeType, reader);
				
				var content = new ICSharpCode.NRefactory.CSharp.CSharpProjectContent ();
				return content.UpdateProjectContent (null, parsedDocument.ParsedFile);
			}
		}
		
		static Dictionary<Project, ITypeResolveContext> cachedProjectContents = new Dictionary<Project, ITypeResolveContext> ();
		static Dictionary<string, AssemblyContext> cachedAssemblyContents = new Dictionary<string, AssemblyContext> ();
		
		public static void ForceUpdate (ProjectContentWrapper context)
		{
			CheckModifiedFiles ();
		}
		
		#region Parser queue
		static bool threadRunning;
		
		public static IProgressMonitorFactory ParseProgressMonitorFactory {
			get;
			set;
		}
		
			
		class InternalProgressMonitor
		: NullProgressMonitor
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
		
		static Queue<ParsingJob> parseQueue = new Queue<ParsingJob> ();
		class ParsingJob
		{
			public ProjectContentWrapper Context;
			public IEnumerable<ProjectFile> FileList;
//			public Action<string, IProgressMonitor> ParseCallback;
			
			public void Run (IProgressMonitor monitor)
			{
				TypeSystemParserNode node = null;
				ITypeSystemParser parser = null;
				foreach (var file in (FileList ?? Context.Project.Files)) {
					if (!string.Equals (file.BuildAction, "compile", StringComparison.OrdinalIgnoreCase)) 
						continue;
					
					var fileName = file.FilePath;
					if (node == null || !node.CanParse (fileName)) {
						node = TypeSystemService.GetTypeSystemParserNode (DesktopService.GetMimeTypeForUri (fileName));
						parser = node != null ? node.Parser : null;
					}
					if (parser == null)
						continue;
					
					using (var stream = new System.IO.StreamReader (fileName)) {
						var parsedDocument = parser.Parse (false, fileName, stream, Context.Project);
						Context.Content = Context.Content.UpdateProjectContent (Context.Content.GetFile (fileName), parsedDocument.ParsedFile);
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
		
		static Dictionary<ProjectContentWrapper, ParsingJob> parseQueueIndex = new Dictionary<ProjectContentWrapper, ParsingJob> ();

		internal static int PendingJobCount {
			get {
				lock (parseQueueLock) {
					return parseQueueIndex.Count;
				}
			}
		}
		
		static void QueueParseJob (ProjectContentWrapper context, IEnumerable<ProjectFile> fileList = null)
		{
			var job = new ParsingJob () {
				Context = context,
				FileList = fileList
			};
			
			lock (parseQueueLock) {
				RemoveParseJob (context);
				parseQueueIndex [context] = job;
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
			lock (parseQueueLock) {
				if (parseQueue.Count > 0) {
					var job = parseQueue.Dequeue ();
					parseQueueIndex.Remove (job.Context);
					return job;
				}
				return null;
			}
		}
		
		internal static void WaitForParseQueue ()
		{
			queueEmptied.WaitOne ();
		}
		
		static void RemoveParseJob (ProjectContentWrapper project)
		{
			lock (parseQueueLock) {
				ParsingJob job;
				if (parseQueueIndex.TryGetValue (project, out job)) {
					parseQueueIndex.Remove (project);
				}
			}
		}
		
		static void RemoveParseJobs (IProjectContent context)
		{
			lock (parseQueueLock) {
				foreach (var pj in parseQueue) {
					if (pj.Context == context) {
						parseQueueIndex.Remove (pj.Context);
					}
				}
			}
		}
		
		static void StartParserThread ()
		{
			lock (parseQueueLock) {
				if (!threadRunning) {
					threadRunning = true;
					var t = new Thread (new ThreadStart (ParserUpdateThread));
					t.Name = "Background parser";
					t.IsBackground = true;
					t.Priority = ThreadPriority.AboveNormal;
					t.Start ();
				}
			}
		}
		
		static void ParserUpdateThread ()
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
			return System.IO.File.GetLastWriteTime (file.FilePath) > parsedFile.LastWriteTime;
		}

		static void CheckModifiedFiles (Project project, ProjectContentWrapper content)
		{
			List<ProjectFile> modifiedFiles = null;
			foreach (var file in project.Files) {
				if (!string.Equals (file.BuildAction, "compile", StringComparison.OrdinalIgnoreCase)) 
					continue;
				var fileName = file.Name;
				// if the file is already inside the content a parser exists for it, if not check if it can be parsed.
				var oldFile = content.Content.GetFile (fileName);
				if (oldFile == null) {
					var parser = TypeSystemService.GetParser (DesktopService.GetMimeTypeForUri (fileName));
					if (parser == null)
						continue;
				}
				if (!IsFileModified (file, oldFile))
					continue;
				if (modifiedFiles == null)
					modifiedFiles = new List<ProjectFile> ();
				modifiedFiles.Add (file);
			}
			
			// check if file needs to be removed from project content 
			foreach (var file in content.Content.Files) {
				if (project.GetProjectFile (file.FileName) == null)
					content.Content = content.Content.UpdateProjectContent (file, null);
			}
			
			if (modifiedFiles == null)
				return;
			QueueParseJob (content, modifiedFiles);
		}

		static void CheckModifiedFile (AssemblyContext context)
		{
			try {
				var writeTime = File.GetLastWriteTime (context.FileName);
				if (writeTime != context.LastWriteTime) {
					string cache = GetCacheDirectory (context.FileName);
					context.LastWriteTime = writeTime;
					if (cache != null) {
						SerializeObject (Path.Combine (cache, "assembly.descriptor"), context);
						context.CtxLoader = new LazyAssemblyLoader (context.FileName, cache);
						try {
							// File is reloaded by the lazy loader
							File.Delete (Path.Combine (cache, "assembly.data"));
						} catch (Exception) {
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while updating assembly " + context.FileName, e);
			}
		}
		
		static void CheckModifiedFiles ()
		{
			Queue<KeyValuePair<Project, ProjectContentWrapper>> list;
			
			lock (rwLock) {
				list = new Queue<KeyValuePair<Project, ProjectContentWrapper>> (projectContents);
			}
			
			while (list.Count > 0) {
				var readydb = list.Dequeue ();
				CheckModifiedFiles (readydb.Key, readydb.Value);
			}
			
			Queue<KeyValuePair<string, AssemblyContext>> assemblyList;
			
			lock (rwLock) {
				assemblyList = new Queue<KeyValuePair<string, AssemblyContext>> (cachedAssemblyContents);
			}
			
			while (assemblyList.Count > 0) {
				var readydb = assemblyList.Dequeue ();
				CheckModifiedFile (readydb.Value);
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
	
	public sealed class ProjectUnloadEventArgs : EventArgs
	{
		public readonly Project Project;
		public readonly TypeSystemService.ProjectContentWrapper Wrapper;

		public ProjectUnloadEventArgs (Project project, TypeSystemService.ProjectContentWrapper wrapper)
		{
			this.Project = project;
			this.Wrapper = wrapper;
		}
	}
}

