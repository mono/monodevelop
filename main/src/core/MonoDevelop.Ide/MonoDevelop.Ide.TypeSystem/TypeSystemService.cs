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
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Mono.TextEditor;
using System.Threading;
using MonoDevelop.Core.ProgressMonitoring;
using System.Xml;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.NRefactory;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Core.Assemblies;
using System.Text;
using ICSharpCode.NRefactory.Completion;
using System.Diagnostics;

namespace MonoDevelop.Ide.TypeSystem
{
	public static class TypeSystemServiceExt
	{
		[Obsolete ("Don't use this method the caller should always have the project and get the type system from that instead the other way around.")]
		public static Project GetProject (this IProjectContent content)
		{
			return TypeSystemService.GetProject (content.Location);
		}

		[Obsolete ("Use TryGetSourceProject.")]
		public static Project GetSourceProject (this ITypeDefinition type)
		{
			var location = type.Compilation.MainAssembly.UnresolvedAssembly.Location;
			if (string.IsNullOrEmpty (location))
				return null;
			return TypeSystemService.GetProject (location);
		}

		[Obsolete ("Use TryGetSourceProject.")]
		public static Project GetSourceProject (this IType type)
		{
			return type.GetDefinition ().GetSourceProject ();
		}

		/// <summary>
		/// Tries to the get source project for a given type definition. This operation may fall if it was called on an outdated
		/// compilation unit or the correspondening project was unloaded.
		/// </summary>
		/// <returns><c>true</c>, if get source project was found, <c>false</c> otherwise.</returns>
		/// <param name="type">The type definition.</param>
		/// <param name="project">The project or null if it wasn't found.</param>
		public static bool TryGetSourceProject (this ITypeDefinition type, out Project project)
		{
			var location = type.Compilation.MainAssembly.UnresolvedAssembly.Location;
			if (string.IsNullOrEmpty (location)) {
				project = null;
				return false;
			}
			project = TypeSystemService.GetProject (location);
			return project != null;
		}

		/// <summary>
		/// Tries to the get source project for a given type. This operation may fall if it was called on an outdated
		/// compilation unit or the correspondening project was unloaded.
		/// </summary>
		/// <returns><c>true</c>, if get source project was found, <c>false</c> otherwise.</returns>
		/// <param name="type">The type.</param>
		/// <param name="project">The project or null if it wasn't found.</param>
		public static bool TryGetSourceProject (this IType type, out Project project)
		{
			var def = type.GetDefinition ();
			if (def == null) {
				project = null;
				return false;
			}
			return def.TryGetSourceProject (out project);
		}


		internal static Project GetProjectWhereTypeIsDefined (this ITypeDefinition type)
		{
			var location = type.ParentAssembly.UnresolvedAssembly.Location;
			if (string.IsNullOrEmpty (location))
				return null;
			return TypeSystemService.GetProject (location);
		}

		internal static Project GetProjectWhereTypeIsDefined (this IType type)
		{
			return type.GetDefinition ().GetSourceProject ();
		}

		[Obsolete ("Don't use this method the caller should always have the project and get the type system from that instead the other way around.")]
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
			return member != null && member.Attributes.Any (a => a.AttributeType.FullName == "System.ObsoleteAttribute");
		}

		public static bool IsObsolete (this IEntity member, out string reason)
		{
			if (member == null) {
				reason = null;
				return false;
			}
			var attr = member.Attributes.FirstOrDefault (a => a.AttributeType.FullName == "System.ObsoleteAttribute");
			if (attr == null) {
				reason = null;
				return false;
			}
			reason = attr.PositionalArguments.Count > 0 ? attr.PositionalArguments [0].ConstantValue.ToString () : null;
			return true;
		}

		public static IType Resolve (this IUnresolvedTypeDefinition def, Project project)
		{
			var compilation = TypeSystemService.GetCompilation (project);
			var ctx = new SimpleTypeResolveContext (compilation.MainAssembly);
			var resolvedType = def.Resolve (ctx);
			return resolvedType;
		}

		[Obsolete ("Do not use this method. Use type references to resolve types. Type references from full reflection names can be got from ReflectionHelper.ParseReflectionName.")]
		public static ITypeDefinition LookupType (this ICompilation compilation, string ns, string name, int typeParameterCount = -1)
		{
			var tc = Math.Max (typeParameterCount, 0);
			ITypeDefinition result;
			foreach (var refAsm in compilation.Assemblies) {
				result = refAsm.GetTypeDefinition (ns, name, tc);
				if (result != null)
					return result;
			}
			if (typeParameterCount < 0) {
				for (int i = 1; i < 50; i++) {
					result = LookupType (compilation, ns, name, i);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		[Obsolete ("Do not use this method. Use type references to resolve types. Type references from full reflection names can be got from ReflectionHelper.ParseReflectionName.")]
		public static ITypeDefinition LookupType (this ICompilation compilation, string fullName, int typeParameterCount = -1)
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
		const string CurrentVersion = "1.1.5";
		static readonly List<TypeSystemParserNode> parsers;
		static string[] filesSkippedInParseThread = new string[0];

		static IEnumerable<TypeSystemParserNode> Parsers {
			get {
				return parsers;
			}
		}

		public static void RemoveSkippedfile (FilePath fileName)
		{
			filesSkippedInParseThread = filesSkippedInParseThread.Where (f => f != fileName).ToArray ();
		}

		public static void AddSkippedFile (FilePath fileName)
		{
			if (filesSkippedInParseThread.Any (f => f == fileName))
				return;
			filesSkippedInParseThread = filesSkippedInParseThread.Concat (new string[] { fileName }).ToArray ();
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

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/OutputTracking", delegate (object sender, ExtensionNodeEventArgs args) {
				var projectType = ((TypeSystemOutputTrackingNode)args.ExtensionNode).ProjectType;
				switch (args.Change) {
				case ExtensionChange.Add:
					outputTrackedProjects.Add (projectType);
					break;
				case ExtensionChange.Remove:
					outputTrackedProjects.Remove (projectType);
					break;
				}
			});

			FileService.FileChanged += delegate(object sender, FileEventArgs e) {
				if (!TrackFileChanges)
					return;
				foreach (var file in e) {
					// Open documents are handled by the Document class itself.
					if (IdeApp.Workbench != null && IdeApp.Workbench.GetDocument (file.FileName) != null)
						continue;
					//
					lock (projectWrapperUpdateLock) {
						foreach (var wrapper in projectContents.Values) {
							var projectFile = wrapper.Project.Files.GetFile (file.FileName);
							if (projectFile != null)
								QueueParseJob (wrapper, new [] { projectFile });
						}
						UnresolvedAssemblyProxy ctx;
						if (cachedAssemblyContents.TryGetValue (file.FileName, out ctx))
							CheckModifiedFile (ctx);
					}
				}

				foreach (var content in projectContents.Values.ToArray ()) {
					var files = new List<ProjectFile> ();
					foreach (var file in e) {
						var f = content.Project.GetProjectFile (file.FileName);
						if (f == null || f.BuildAction == BuildAction.None)
							continue;
						files.Add (f);
					}
					if (files.Count > 0)
						QueueParseJob (content, files);
				}

			};
			if (IdeApp.ProjectOperations != null) {
				IdeApp.ProjectOperations.EndBuild += HandleEndBuild;
			}
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
			}
		}

		static void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			foreach (var pr in projectContents.Keys.ToArray ()) {
				var project = pr as DotNetProject;
				if (project != null)
					CheckProjectOutput (project, true);
			}
		}

		static readonly List<string> outputTrackedProjects = new List<string> ();

		static void CheckProjectOutput (DotNetProject project, bool autoUpdate)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (outputTrackedProjects.Contains (project.ProjectType, StringComparer.OrdinalIgnoreCase)) {
				var fileName = project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);

				var wrapper = GetProjectContentWrapper (project);
				bool update = wrapper.UpdateTrackedOutputAssembly (fileName);
				if (autoUpdate && update) {
					wrapper.ReconnectAssemblyReferences ();

					// update documents
					foreach (var openDocument in IdeApp.Workbench.Documents) {
						openDocument.ReparseDocument ();
					}
				}
			}
		}

		static void HandleEndBuild (object sender, BuildEventArgs args)
		{
			var project = args.SolutionItem as DotNetProject;
			if (project == null)
				return;
			CheckProjectOutput (project, true);
		}

		public static TypeSystemParser GetParser (string mimeType, string buildAction = BuildAction.Compile)
		{
			var n = GetTypeSystemParserNode (mimeType, buildAction);
			return n != null ? n.Parser : null;
		}

		static TypeSystemParserNode GetTypeSystemParserNode (string mimeType, string buildAction)
		{
			foreach (var mt in DesktopService.GetMimeTypeInheritanceChain (mimeType)) {
				var provider = Parsers.FirstOrDefault (p => p.CanParse (mt, buildAction));
				if (provider != null)
					return provider;
			}
			return null;
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
			foreach (var mt in DesktopService.GetMimeTypeInheritanceChain (mimeType)) {
				var node = FoldingParsers.FirstOrDefault (n => n.MimeType == mt);
				if (node != null)
					return node.CreateInstance () as IFoldingParser;
			}
			return null;
		}

		public static ParsedDocument ParseFile (Project project, string fileName)
		{
			string text;
			
			try {
				if (!File.Exists (fileName))
					return null;
				text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (fileName);
			} catch (Exception) {
				return null;
			}
			
			return ParseFile (project, fileName, DesktopService.GetMimeTypeForUri (fileName), text);
		}

		static readonly object projectWrapperUpdateLock = new object ();

		public static ParsedDocument ParseFile (Project project, string fileName, string mimeType, TextReader content)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			var parser = GetParser (mimeType);
			if (parser == null)
				return null;

			var t = Counters.ParserService.FileParsed.BeginTiming (fileName);
			try {
				var result = parser.Parse (true, fileName, content, project);
				lock (projectWrapperUpdateLock) {
					ProjectContentWrapper wrapper;
					if (project != null) {
						projectContents.TryGetValue (project, out wrapper);
					} else {
						wrapper = null;
					}
					if (wrapper != null && (result.Flags & ParsedDocumentFlags.NonSerializable) != ParsedDocumentFlags.NonSerializable) {
						var oldFile = wrapper.Content.GetFile (fileName);
						wrapper.UpdateContent (c => c.AddOrUpdateFiles (result.ParsedFile));
						UpdateProjectCommentTasks (wrapper, result);
						if (oldFile != null)
							wrapper.InformFileRemoved (new ParsedFileEventArgs (oldFile));
						wrapper.InformFileAdded (new ParsedFileEventArgs (result.ParsedFile));
					}

					// The parsed file could be included in other projects as well, therefore
					// they need to be updated.
					foreach (var cnt in projectContents.ToArray ()) {
						if (cnt.Key == project)
							continue;
						// Use the project context because file lookup is faster there than in the project class.
						var file = cnt.Value.Content.GetFile (fileName);
						if (file != null) {
							cnt.Value.UpdateContent (c => c.AddOrUpdateFiles (result.ParsedFile));
							cnt.Value.InformFileRemoved (new ParsedFileEventArgs (file));
							cnt.Value.InformFileAdded (new ParsedFileEventArgs (result.ParsedFile));
						}
					}
				}
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing: " + e);
				return null;
			} finally {
				t.Dispose ();
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
			var t = Counters.ParserService.FileParsed.BeginTiming (fileName);
			try {
				var result = parser.Parse (true, fileName, content);
				lock (projectWrapperUpdateLock) {
					if (wrapper != null && (result.Flags & ParsedDocumentFlags.NonSerializable) != ParsedDocumentFlags.NonSerializable) {
						var oldFile = wrapper.Content.GetFile (fileName);
						wrapper.UpdateContent (c => c.AddOrUpdateFiles (result.ParsedFile));
						UpdateProjectCommentTasks (wrapper, result);
						if (oldFile != null)
							wrapper.InformFileRemoved (new ParsedFileEventArgs (oldFile));
						wrapper.InformFileAdded (new ParsedFileEventArgs (result.ParsedFile));
					}
				}
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing :" + e);
				return null;
			} finally {
				t.Dispose ();
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

		static string GetCacheDirectory (TargetFramework framework)
		{
			var derivedDataPath = UserProfile.Current.CacheDir.Combine ("DerivedData");

			var name = new StringBuilder ();
			foreach (var ch in framework.Name) {
				if (char.IsLetterOrDigit (ch)) {
					name.Append (ch);
				} else {
					name.Append ('_');
				}
			}

			string result = derivedDataPath.Combine (name.ToString ());
			try {
				if (!Directory.Exists (result))
					Directory.CreateDirectory (result);
			} catch (Exception e) {
				LoggingService.LogError ("Error while creating derived data directories.", e);
			}
			return result;
		}

		static string InternalGetCacheDirectory (FilePath filename)
		{
			CanonicalizePath (ref filename);
			var assemblyCacheRoot = GetAssemblyCacheRoot (filename);
			try {
				if (!Directory.Exists (assemblyCacheRoot))
					return null;
				foreach (var dir in Directory.EnumerateDirectories (assemblyCacheRoot)) {
					string result;
					if (CheckCacheDirectoryIsCorrect (filename, dir, out result))
						return result;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting derived data directories.", e);
			}
			return null;
		}

		/// <summary>
		/// Gets the cache directory for a projects derived data cache directory.
		/// If forceCreation is set to false the method may return null, if the cache doesn't exist.
		/// </summary>
		/// <returns>The cache directory.</returns>
		/// <param name="project">The project to get the cache for.</param>
		/// <param name="forceCreation">If set to <c>true</c> the creation is forced and the method doesn't return null.</param>
		public static string GetCacheDirectory (Project project, bool forceCreation = false)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			return GetCacheDirectory (project.FileName, forceCreation);
		}

		static readonly Dictionary<string, object> cacheLocker = new Dictionary<string, object> ();

		/// <summary>
		/// Gets the cache directory for arbitrary file names.
		/// If forceCreation is set to false the method may return null, if the cache doesn't exist.
		/// </summary>
		/// <returns>The cache directory.</returns>
		/// <param name="fileName">The file name to get the cache for.</param>
		/// <param name="forceCreation">If set to <c>true</c> the creation is forced and the method doesn't return null.</param>
		public static string GetCacheDirectory (string fileName, bool forceCreation = false)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			object locker;
			bool newLock;
			lock (cacheLocker) {
				if (!cacheLocker.TryGetValue (fileName, out locker)) {
					cacheLocker [fileName] = locker = new object ();
					newLock = true;
				} else {
					newLock = false;
				}
			}
			lock (locker) {
				var result = InternalGetCacheDirectory (fileName);
				if (newLock && result != null)
					TouchCache (result);
				if (forceCreation && result == null)
					result = CreateCacheDirectory (fileName);
				return result;
			}
		}

		struct CacheDirectoryInfo
		{
			public static readonly CacheDirectoryInfo Empty = new CacheDirectoryInfo ();

			public string FileName { get; set; }

			public string Version { get; set; }
		}

		static readonly Dictionary<FilePath, CacheDirectoryInfo> cacheDirectoryCache = new Dictionary<FilePath, CacheDirectoryInfo> ();

		static void CanonicalizePath (ref FilePath fileName)
		{
			try {
				// There are some situations where that may cause an exception.
				fileName = fileName.CanonicalPath;
			} catch (Exception) {
				// Fallback
				string fp = fileName;
				if (fp.Length > 0 && fp [fp.Length - 1] == Path.DirectorySeparatorChar)
					fileName = fp.TrimEnd (Path.DirectorySeparatorChar);
				if (fp.Length > 0 && fp [fp.Length - 1] == Path.AltDirectorySeparatorChar)
					fileName = fp.TrimEnd (Path.AltDirectorySeparatorChar);
			}
		}

		static bool CheckCacheDirectoryIsCorrect (FilePath filename, FilePath candidate, out string result)
		{
			CanonicalizePath (ref filename);
			CanonicalizePath (ref candidate);
			lock (cacheDirectoryCache) {
				CacheDirectoryInfo info;
				if (!cacheDirectoryCache.TryGetValue (candidate, out info)) {
					var dataPath = candidate.Combine ("data.xml");

					try {
						if (!File.Exists (dataPath)) {
							result = null;
							return false;
						}
						using (var reader = XmlReader.Create (dataPath)) {
							while (reader.Read ()) {
								if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "File") {
									info.Version = reader.GetAttribute ("version");
									info.FileName = reader.GetAttribute ("name");
								}
							}
						}
						cacheDirectoryCache [candidate] = info;
					} catch (Exception e) {
						LoggingService.LogError ("Error while reading derived data file " + dataPath, e);
					}
				}
	
				if (info.Version == CurrentVersion && info.FileName == filename) {
					result = candidate;
					return true;
				}
	
				result = null;
				return false;
			}
		}

		static string GetAssemblyCacheRoot (string filename)
		{
			string derivedDataPath = UserProfile.Current.CacheDir.Combine ("DerivedData");
			string name = Path.GetFileName (filename);
			return Path.Combine (derivedDataPath, name + "-" + GetStableHashCode(name).ToString ("x")); 	
		}

		/// <summary>
		/// Retrieves a hash code for the specified string that is stable across
		/// .NET upgrades.
		/// 
		/// Use this method instead of the normal <c>string.GetHashCode</c> if the hash code
		/// is persisted to disk.
		/// </summary>
		static int GetStableHashCode(string text)
		{
			unchecked {
				int h = 0;
				foreach (char c in text) {
					h = (h << 5) - h + c;
				}
				return h;
			}
		}

		static IEnumerable<string> GetPossibleCacheDirNames (string baseName)
		{
			int i = 0;
			while (i < 4096) {
				yield return Path.Combine (baseName, i.ToString ());
				i++;
			}
			throw new Exception ("Too many cache directories");
		}

		static string CreateCacheDirectory (FilePath fileName)
		{
			CanonicalizePath (ref fileName);
			try {
				string cacheRoot = GetAssemblyCacheRoot (fileName);
				string cacheDir = GetPossibleCacheDirNames (cacheRoot).First (d => !Directory.Exists (d));

				Directory.CreateDirectory (cacheDir);

				File.WriteAllText (
					Path.Combine (cacheDir, "data.xml"),
					string.Format ("<DerivedData><File name=\"{0}\" version =\"{1}\"/></DerivedData>", fileName, CurrentVersion)
				);

				return cacheDir;
			} catch (Exception e) {
				LoggingService.LogError ("Error creating cache for " + fileName, e);
				return null;
			}
		}

		static readonly FastSerializer sharedSerializer = new FastSerializer ();

		static T DeserializeObject<T> (string path) where T : class
		{
			var t = Counters.ParserService.ObjectDeserialized.BeginTiming (path);
			try {
				using (var fs = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)) {
					using (var reader = new BinaryReaderWith7BitEncodedInts (fs)) {
						lock (sharedSerializer) {
							return (T)sharedSerializer.Deserialize (reader);
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while trying to deserialize " + typeof(T).FullName + ". stack trace:" + Environment.StackTrace, e);
				return default(T);
			} finally {
				t.Dispose ();
			}
		}

		static void SerializeObject (string path, object obj)
		{
			if (obj == null)
				throw new ArgumentNullException ("obj");

			var t = Counters.ParserService.ObjectSerialized.BeginTiming (path);
			try {
				using (var fs = new FileStream (path, FileMode.Create, FileAccess.Write)) {
					using (var writer = new BinaryWriterWith7BitEncodedInts (fs)) {
						lock (sharedSerializer) {
							sharedSerializer.Serialize (writer, obj);
						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("-----------------Serialize stack trace:");
				Console.WriteLine (Environment.StackTrace);
				LoggingService.LogError ("Error while writing type system cache. (object:" + obj.GetType () + ")", e);
			} finally {
				t.Dispose ();
			}
		}

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

		static void StoreExtensionObject (string cacheDir, object extensionObject)
		{
			if (cacheDir == null)
				throw new ArgumentNullException ("cacheDir");
			if (extensionObject == null)
				throw new ArgumentNullException ("extensionObject");
			var fileName = Path.GetTempFileName ();
			SerializeObject (fileName, extensionObject);
			var cacheFile = Path.Combine (cacheDir, extensionObject.GetType ().FullName + ".cache");

			try {
				if (File.Exists (cacheFile))
					File.Delete (cacheFile);
				File.Move (fileName, cacheFile);
			} catch (Exception e) {
				LoggingService.LogError ("Error whil saving cache " + cacheFile + " for extension object:" + extensionObject, e);
			}
		}

		static void StoreProjectCache (Project project, ProjectContentWrapper wrapper)
		{
			if (!wrapper.WasChanged)
				return;
			string cacheDir = GetCacheDirectory (project, true);
			string fileName = Path.GetTempFileName ();
			
			SerializeObject (fileName, wrapper.Content.RemoveAssemblyReferences (wrapper.Content.AssemblyReferences));
			
			string cacheFile = Path.Combine (cacheDir, "completion.cache");
			
			try {
				if (File.Exists (cacheFile))
					File.Delete (cacheFile);
				File.Move (fileName, cacheFile);
			} catch (Exception e) {
				LoggingService.LogError ("Error whil saving cache " + cacheFile, e);
			}

			foreach (var extensionObject in wrapper.ExtensionObjects) {
				StoreExtensionObject (cacheDir, extensionObject);
			}
		}

		#endregion

		#region Project loading

		public static void Load (WorkspaceItem item)
		{
			using (Counters.ParserService.WorkspaceItemLoaded.BeginTiming ()) {
				InternalLoad (item);
				CleanupCache ();
			}
		}
		static CancellationTokenSource loadCancellationSource = new CancellationTokenSource ();
		static bool loadWs = false;
		static void InternalLoad (WorkspaceItem item)
		{
			var ws = item as Workspace;
			if (ws != null) {
				loadWs = true;
				loadCancellationSource.Cancel ();
				loadCancellationSource = new CancellationTokenSource ();
				foreach (WorkspaceItem it in ws.Items)
					InternalLoad (it);
				ws.ItemAdded += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
			} else {
				if (!loadWs) {
					loadCancellationSource.Cancel ();
					loadCancellationSource = new CancellationTokenSource ();
				}
				var solution = item as Solution;
				if (solution != null) {
					Parallel.ForEach (solution.GetAllProjects (), project => LoadProject (project));
					var list = projectContents.Values.ToList ();
					Task.Factory.StartNew (delegate {
						foreach (var wrapper in list) {
							CheckModifiedFiles (wrapper.Project, wrapper.Project.Files.ToArray (), wrapper, loadCancellationSource.Token);
						}
					});

					solution.SolutionItemAdded += OnSolutionItemAdded;
					solution.SolutionItemRemoved += OnSolutionItemRemoved;
					OnSolutionLoaded (new SolutionEventArgs (solution));
				}
			}
		}

		public static event EventHandler<SolutionEventArgs> SolutionLoaded;

		static void OnSolutionLoaded (SolutionEventArgs e)
		{
			var handler = SolutionLoaded;
			if (handler != null)
				handler (null, e);
		}

		[Serializable]
		public class UnresolvedAssemblyDecorator : IUnresolvedAssembly
		{
			readonly ProjectContentWrapper wrapper;

			IUnresolvedAssembly assembly {
				get {
					if (wrapper.OutputAssembly != null)
						return wrapper.OutputAssembly;
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

			public string FullAssemblyName {
				get {
					return assembly.FullAssemblyName;
				}
			}

			public string Location {
				get {
					return assembly.Location;
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
			readonly Dictionary<Type, object> extensionObjects = new Dictionary<Type, object> ();
			List<ProjectContentWrapper> referencedWrappers = new List<ProjectContentWrapper>();
			List<UnresolvedAssemblyProxy> referencedAssemblies = new List<UnresolvedAssemblyProxy>();
			IProjectContent _content;
			bool referencesConnected;

			public bool ReferencesConnected {
				get {
					return GetReferencesConnected (this, new HashSet<ProjectContentWrapper> ());
				}
			}

			static bool GetReferencesConnected (ProjectContentWrapper pcw, HashSet<ProjectContentWrapper> wrapper)
			{
				if (wrapper.Contains (pcw))
					return true;
				wrapper.Add (pcw); 
				return pcw.referencesConnected && pcw.referencedWrappers.All (w => GetReferencesConnected (w, wrapper));
			}

			public bool IsLoaded {
				get {
					return GetIsLoaded (this, new HashSet<ProjectContentWrapper> ());
				}
			}

			static bool GetIsLoaded (ProjectContentWrapper pcw, HashSet<ProjectContentWrapper> wrapper)
			{
				if (wrapper.Contains (pcw))
					return true;
				wrapper.Add (pcw); 
				return !pcw.InLoad && pcw.referencedWrappers.All (w => GetIsLoaded (w, wrapper));
			}

			public IProjectContent Content {
				get {
					if (!referencesConnected) {
						referencesConnected = true;
						ReconnectAssemblyReferences ();
					}
					return _content;
				}
				private set {
					if (value == null)
						throw new InvalidOperationException ("Project content can't be null");
					_content = value;
				}
			}

			/// <summary>
			/// Gets the extension objects attached to the content wrapper.
			/// </summary>
			public IEnumerable<object> ExtensionObjects {
				get {
					return extensionObjects.Values;
				}
			}

			/// <summary>
			/// Updates an extension object for the wrapper. Note that only one extension object of a certain
			/// type may be stored inside the project content wrapper.
			/// 
			/// The extension objects need to be serializable and are stored in the project cache on project unload.
			/// </summary>
			public void UpdateExtensionObject (object ext)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				extensionObjects [ext.GetType ()] = ext;
			}

			/// <summary>
			/// Gets a specific extension object. This may lazy load an existing extension object from disk,
			/// if called the first time and a serialized extension object exists.
			/// </summary>
			/// <returns>
			/// The extension object. Or null, if no extension object of the specified type was registered.
			/// </returns>
			/// <typeparam name='T'>
			/// The type of the extension object.
			/// </typeparam>
			public T GetExtensionObject<T> () where T : class
			{
				object result;
				if (extensionObjects.TryGetValue (typeof(T), out result))
					return (T)result;

				string cacheDir = GetCacheDirectory (Project);
				if (cacheDir == null)
					return default(T);

				try {
					string fileName = Path.Combine (cacheDir, typeof(T).FullName + ".cache");
					if (File.Exists (fileName)) {
						var deserialized = DeserializeObject<T> (fileName);
						extensionObjects [typeof(T)] = deserialized;
						return deserialized;
					}
				} catch (Exception) {
					Console.WriteLine ("Can't deserialize :" + typeof(T).FullName);
				}

				return default (T);
			}

			List<Action<IProjectContent>> loadActions = new List<Action<IProjectContent>> ();

			public void RunWhenLoaded (Action<IProjectContent> act)
			{
				lock (updateContentLock) {
					if (Content is LazyProjectLoader) {
						if (loadActions != null) {
							loadActions.Clear ();
							loadActions.Add (act);
						}
						return;
					}
				}
				act (Content);
			}

			void ClearCachedCompilations ()
			{
				// Need to clear this compilation & all compilations that reference this directly or indirectly
				var stack = new Stack<ProjectContentWrapper> ();
				stack.Push (this);
				var cleared = new HashSet<ProjectContentWrapper> ();
				while (stack.Count > 0) {
					var cur = stack.Pop ();
					if (cleared.Contains (cur))
						continue;
					cleared.Add (cur);
					cur.compilation = null;
					foreach (var project in cur.ReferencedProjects)
						stack.Push (GetProjectContentWrapper (project));
				}
			}

			readonly object updateContentLock = new object ();

			public void UpdateContent (Func<IProjectContent, IProjectContent> updateFunc)
			{
				lock (updateContentLock) {
					var lazyProjectLoader = Content as LazyProjectLoader;
					if (lazyProjectLoader != null) {
						lazyProjectLoader.ContextTask.Wait ();
						if (loadActions != null) {
							var action = loadActions.FirstOrDefault ();
							loadActions = null;
							if (action != null)
								action (Content);
						}
					}
					Content = updateFunc (Content);
					ClearCachedCompilations ();
					WasChanged = true;
				}
			}

			public void InformFileRemoved (ParsedFileEventArgs e)
			{
				var handler = FileRemoved;
				if (handler != null)
					handler (this, e);
			}

			public void InformFileAdded (ParsedFileEventArgs e)
			{
				var handler = FileAdded;
				if (handler != null)
					handler (this, e);
			}

			public EventHandler<ParsedFileEventArgs> FileAdded;
			public EventHandler<ParsedFileEventArgs> FileRemoved;
			public bool WasChanged;
			[NonSerialized]
			ICompilation compilation;

			public ICompilation Compilation {
				get {
					lock (updateContentLock) {
						if (compilation == null) {
							compilation = Content.CreateCompilation ();
						}
						return compilation;
					}
				}
			}

			public Project Project {
				get;
				private set;
			}

			[NonSerialized]
			int loadOperationDepth = 0;
			[NonSerialized]
			readonly object loadOperationLocker = new object ();

			internal void BeginLoadOperation ()
			{
				lock (loadOperationLocker) {
					loadOperationDepth++;
				}
			}

			internal void EndLoadOperation ()
			{
				lock (loadOperationLocker) {
					if (loadOperationDepth > 0) {
						loadOperationDepth--;
					}
				}
				OnLoad (EventArgs.Empty);
			}
			bool inLoad;
			public bool InLoad {
				get {
					return inLoad;
				}
			}

			[NonSerialized]
			CancellationTokenSource src;

			internal void CancelLoad ()
			{
				if (src != null)
					src.Cancel ();
			}

			void UpdateLoadState ()
			{
				inLoad = loadOperationDepth > 0 || referencedWrappers.Any (w => w.InLoad) || referencedAssemblies.Any (a => a.InLoad);
			}

			internal void RequestLoad ()
			{
				UpdateLoadState ();
				if (!InLoad)
					return;
				CancelLoad ();
				src = new CancellationTokenSource ();
				var token = src.Token;
				Task.Factory.StartNew (delegate {
					var s = new Stack<ProjectContentWrapper> ();
					s.Push (this);
					var w = new HashSet<ProjectContentWrapper> ();
					while (s.Count > 0) {
						var wrapper = s.Pop ();
						if (token.IsCancellationRequested)
							return;
						if (w.Contains (wrapper))
							continue;
						w.Add (wrapper);

						foreach (var asm in wrapper.referencedAssemblies.ToArray ()) {
							if (token.IsCancellationRequested)
								return;
							var ctxLoader = asm.CtxLoader;
							if (ctxLoader != null)
								ctxLoader.EnsureAssemblyLoaded ();
						}
						foreach (var rw in wrapper.referencedWrappers.ToArray ()) {
							if (token.IsCancellationRequested)
								return;
							s.Push (rw); 
						}
					}
				});
			}

			public event EventHandler Loaded;

			protected virtual void OnLoad (EventArgs e)
			{
				UpdateLoadState ();
				if (InLoad)
					return;
				var handler = Loaded;
				if (handler != null)
					handler (this, e);
			}

			[NonSerialized]
			internal LazyAssemblyLoader OutputAssembly;

			internal bool UpdateTrackedOutputAssembly (FilePath fileName)
			{
				if (File.Exists (fileName)) {
					OutputAssembly = new LazyAssemblyLoader (fileName, null);
					// a clean operation could remove the assembly, thefore we need to load it.
					OutputAssembly.EnsureAssemblyLoaded ();
					return true;
				}
				return false;
			}

			public ProjectContentWrapper (Project project)
			{
				if (project == null)
					throw new ArgumentNullException ("project");
				this.Project = project;
				this.Content = new LazyProjectLoader (this);
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

			class LazyProjectLoader : IProjectContent
			{
				readonly ProjectContentWrapper wrapper;
				readonly Task<IProjectContent> contextTask;

				public Task<IProjectContent> ContextTask {
					get {
						return contextTask;
					}
				}

				public IProjectContent Content {
					get {
						if (References != null)
							return contextTask.Result.AddAssemblyReferences (References); 
						return contextTask.Result;
					}
				}

				public List<IAssemblyReference> References {
					get;
					set;
				}

				public LazyProjectLoader (ProjectContentWrapper wrapper)
				{
					this.wrapper = wrapper;
					contextTask = Task.Factory.StartNew (delegate {
						try {
							this.wrapper.BeginLoadOperation ();
							var p = this.wrapper.Project;
							var context = LoadProjectCache (p);

							var assemblyName = p.ParentSolution != null ? p.GetOutputFileName (p.ParentSolution.DefaultConfigurationSelector).FileNameWithoutExtension : p.Name;
							if (string.IsNullOrEmpty (assemblyName))
								assemblyName = p.Name;

							if (context != null) {
								return context.SetAssemblyName (assemblyName) ?? context;
							}

							context = new MonoDevelopProjectContent (p);
							context = context.SetLocation (p.FileName);
							context = context.SetAssemblyName (assemblyName);

							QueueParseJob (this.wrapper);
							return context;
						} finally {
							this.wrapper.EndLoadOperation ();
						}
					});
				}

				static IProjectContent LoadProjectCache (Project project)
				{
					string cacheDir = GetCacheDirectory (project);
					if (cacheDir == null)
						return null;
					
					var cacheFile = Path.Combine (cacheDir, "completion.cache");
					if (!File.Exists (cacheFile))
						return null;
					try {
						var cache = DeserializeObject<IProjectContent> (cacheFile);
						var monoDevelopProjectContent = cache as MonoDevelopProjectContent;
						if (monoDevelopProjectContent != null)
							monoDevelopProjectContent.Project = project;
						return cache;
					} catch (Exception e) {
						LoggingService.LogWarning ("Error while reading completion cache, regenerating", e); 
						Directory.Delete (cacheDir, true);
						return null;
					}
				}

				#region IAssemblyReference implementation

				IAssembly IAssemblyReference.Resolve (ITypeResolveContext context)
				{
					return Content.Resolve (context);
				}

				#endregion

				#region IUnresolvedAssembly implementation

				string IUnresolvedAssembly.AssemblyName {
					get {
						return Content.AssemblyName;
					}
				}

				string IUnresolvedAssembly.FullAssemblyName {
					get {
						return Content.FullAssemblyName;
					}
				}

				string IUnresolvedAssembly.Location {
					get {
						return Content.Location;
					}
				}

				IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.AssemblyAttributes {
					get {
						return Content.AssemblyAttributes;
					}
				}

				IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.ModuleAttributes {
					get {
						return Content.ModuleAttributes;
					}
				}

				IEnumerable<IUnresolvedTypeDefinition> IUnresolvedAssembly.TopLevelTypeDefinitions {
					get {
						return Content.TopLevelTypeDefinitions;
					}
				}

				#endregion

				#region IProjectContent implementation

				string IProjectContent.ProjectFileName { get { return Content.ProjectFileName; } }

				IUnresolvedFile IProjectContent.GetFile (string fileName)
				{
					return Content.GetFile (fileName);
				}

				ICompilation IProjectContent.CreateCompilation ()
				{
					return Content.CreateCompilation ();
				}

				public ICompilation CreateCompilation (ISolutionSnapshot solutionSnapshot)
				{
					return Content.CreateCompilation (solutionSnapshot);
				}

				IProjectContent IProjectContent.SetAssemblyName (string newAssemblyName)
				{
					return Content.SetAssemblyName (newAssemblyName);
				}

				IProjectContent IProjectContent.SetLocation (string newLocation)
				{
					return Content.SetLocation (newLocation);
				}

				IProjectContent IProjectContent.AddAssemblyReferences (IEnumerable<IAssemblyReference> references)
				{
					return Content.AddAssemblyReferences (references);
				}

				IProjectContent IProjectContent.AddAssemblyReferences (params IAssemblyReference[] references)
				{
					return Content.AddAssemblyReferences (references);
				}

				IProjectContent IProjectContent.RemoveAssemblyReferences (IEnumerable<IAssemblyReference> references)
				{
					return Content.RemoveAssemblyReferences (references);
				}

				IProjectContent IProjectContent.RemoveAssemblyReferences (params IAssemblyReference[] references)
				{
					return Content.RemoveAssemblyReferences (references);
				}
				#pragma warning disable 618
				IProjectContent IProjectContent.UpdateProjectContent (IUnresolvedFile oldFile, IUnresolvedFile newFile)
				{
					return Content.UpdateProjectContent (oldFile, newFile);
				}

				public IProjectContent UpdateProjectContent (IEnumerable<IUnresolvedFile> oldFiles, IEnumerable<IUnresolvedFile> newFiles)
				{
					return Content.UpdateProjectContent (oldFiles, newFiles);
				}
				#pragma warning restore 618

				public IProjectContent AddOrUpdateFiles (IEnumerable<IUnresolvedFile> newFiles)
				{
					return Content.AddOrUpdateFiles (newFiles);
				}

				public IProjectContent AddOrUpdateFiles (params IUnresolvedFile[] newFiles)
				{
					return Content.AddOrUpdateFiles (newFiles);
				}

				IEnumerable<IUnresolvedFile> IProjectContent.Files {
					get {
						return Content.Files;
					}
				}

				IEnumerable<IAssemblyReference> IProjectContent.AssemblyReferences {
					get {
						return Content.AssemblyReferences;
					}
				}

				IProjectContent IProjectContent.SetProjectFileName (string newProjectFileName)
				{
					return Content.SetProjectFileName (newProjectFileName);
				}

				IProjectContent IProjectContent.RemoveFiles (IEnumerable<string> fileNames)
				{
					return Content.RemoveFiles (fileNames);
				}

				IProjectContent IProjectContent.RemoveFiles (params string[] fileNames)
				{
					return Content.RemoveFiles (fileNames);
				}

				#endregion

				object compilerSettings;

				public IProjectContent SetCompilerSettings (object compilerSettings)
				{
					this.compilerSettings = compilerSettings;
					return this;
				}

				public object CompilerSettings {
					get {
						return compilerSettings;
					}
				}
			}

			bool HasCyclicRefs (ProjectContentWrapper wrapper, HashSet<Project> nonCyclicCache)
			{
				if (nonCyclicCache.Contains (wrapper.Project))
					return false;
				nonCyclicCache.Add (wrapper.Project);
				foreach (var referencedProject in wrapper.ReferencedProjects) {
					ProjectContentWrapper w;
					if (referencedProject == Project || referencedProject == wrapper.Project || projectContents.TryGetValue (referencedProject, out w) && HasCyclicRefs (w, nonCyclicCache)) {
						return true;
					}
				}
				return false;
			}
			public void ReconnectAssemblyReferences ()
			{
				var netProject = Project as DotNetProject;
				if (netProject == null)
					return;
				CancelLoad ();
				try {
					var contexts = new List<IAssemblyReference> ();
					var nonCyclicCache = new HashSet<Project> ();
					foreach (var referencedWrapper in referencedWrappers) {
						referencedWrapper.Loaded += HandleReferencedProjectInLoadChange;
					}
					var newReferencedWrappers = new List<ProjectContentWrapper>();
					foreach (var referencedProject in ReferencedProjects) {
						ProjectContentWrapper wrapper;
						if (projectContents.TryGetValue (referencedProject, out wrapper)) {
							if (HasCyclicRefs (wrapper, nonCyclicCache))
								continue;
							wrapper.Loaded += HandleReferencedProjectInLoadChange;
							newReferencedWrappers.Add (wrapper);
							contexts.Add (new UnresolvedAssemblyDecorator (wrapper));
						}
					}
					this.referencedWrappers = newReferencedWrappers;

					UnresolvedAssemblyProxy ctx;
					// Add mscorlib reference

					// hack: find the NoStdLib flag
					var config = IdeApp.Workspace != null ? netProject.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration : null;
					bool noStdLib = false;
					if (config != null) {
						var parameters = config.CompilationParameters;
						if (parameters != null) {
							var prop = parameters.GetType ().GetProperty ("NoStdLib");
							if (prop != null) {
								var val = prop.GetValue (parameters, null);
								if (val is bool)
									noStdLib = (bool)val;
							}
						}
					}

					if (!noStdLib && netProject.TargetRuntime != null && netProject.TargetRuntime.AssemblyContext != null) {
						var corLibRef = netProject.TargetRuntime.AssemblyContext.GetAssemblyForVersion (
							typeof(object).Assembly.FullName,
							null,
							netProject.TargetFramework
						);
						if (corLibRef != null) {
							ctx = LoadAssemblyContext (corLibRef.Location);
							if (ctx != null) 
								contexts.Add (ctx);
						}
					}
					// Get the assembly references throught the project, since it may have custom references
					foreach (var asm in referencedAssemblies) {
						asm.Loaded += HandleReferencedProjectInLoadChange;
					}
					var newReferencedAssemblies = new List<UnresolvedAssemblyProxy>();
					try {
						foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {

							// HACK: core reference get added automatically, even if no std lib is set.
							if (noStdLib && file.Contains ("System.Core.dll"))
								continue;

							string fileName;
							if (!Path.IsPathRooted (file)) {
								fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
							} else {
								fileName = Path.GetFullPath (file);
							}
							ctx = LoadAssemblyContext (fileName);
							if (ctx != null) {
								newReferencedAssemblies.Add (ctx);
								ctx.Loaded += HandleReferencedProjectInLoadChange;
								contexts.Add (ctx);
							} else {
								LoggingService.LogWarning ("TypeSystemService: Can't load assembly context for:" + file);
							}
						}
					} catch (Exception e) {
						LoggingService.LogError ("Error while getting assembly references", e);
					}
					referencedAssemblies = newReferencedAssemblies;
					bool changed = WasChanged;
					var lazyProjectLoader = Content as LazyProjectLoader;
					if (lazyProjectLoader != null) {
						lazyProjectLoader.References = contexts;
					} else {
						UpdateContent (c => c.RemoveAssemblyReferences (Content.AssemblyReferences));
						UpdateContent (c => c.AddAssemblyReferences (contexts));
					}
					WasChanged = changed;
				} catch (Exception e) {
					if (netProject.TargetRuntime == null) {
						LoggingService.LogError ("Target runtime was null: " + Project.Name);
					} else if (netProject.TargetRuntime.AssemblyContext == null) {
						LoggingService.LogError ("Target runtime assembly context was null: " + Project.Name);
					}
					LoggingService.LogError ("Error while reloading all references of project: " + Project.Name, e);
				} finally {
					RequestLoad ();
					OnLoad (EventArgs.Empty);
				}
			}

			void HandleReferencedProjectInLoadChange (object sender, EventArgs e)
			{
				OnLoad (EventArgs.Empty);
			}

			internal void Unload ()
			{
				CancelLoad ();
				foreach (var asm in referencedAssemblies) {
					asm.Loaded -= HandleReferencedProjectInLoadChange;
				}
				loadActions = null;
				referencedWrappers.Clear ();
				referencedAssemblies.Clear ();
				Loaded = null;
				Content = new CSharpProjectContent ();
			}
		}

		static readonly object projectContentLock = new object ();
		static readonly Dictionary<Project, ProjectContentWrapper> projectContents = new Dictionary<Project, ProjectContentWrapper> ();

		public static ProjectContentWrapper LoadProject (Project project)
		{
			if (IncLoadCount (project) != 1)
				return null;
			lock (projectContentLock) {
				if (projectContents.ContainsKey (project))
					return null;
				try {
					Counters.ParserService.ProjectsLoaded++;
					ProjectContentWrapper wrapper;
					projectContents [project] = wrapper = new ProjectContentWrapper (project);
					var dotNetProject = project as DotNetProject;
					if (dotNetProject != null)
						CheckProjectOutput (dotNetProject, false);

					project.FileAddedToProject += OnFileAdded;
					project.FileRemovedFromProject += OnFileRemoved;
					project.FileRenamedInProject += OnFileRenamed;
					project.Modified += OnProjectModified;


					if (dotNetProject != null) {
						StartFrameworkLookup (dotNetProject);
					}
					return wrapper;
				} catch (Exception ex) {
					LoggingService.LogError ("Parser database for project '" + project.Name + " could not be loaded", ex);
				}
				return null;
			}
		}

		public static Project GetProject (IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");
			return GetProject (entity.ParentAssembly.UnresolvedAssembly.Location);
		}

		public static Project GetProject (string location)
		{
			foreach (var wrapper in projectContents)
				if (wrapper.Value.Compilation.MainAssembly.UnresolvedAssembly.Location == location)
					return wrapper.Key;
			return null;
		}

		#region Project modification handlers

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
				var wrapper = projectContents [project];
				var fileName = fargs.ProjectFile.Name;
				var file = wrapper.Content.GetFile (fileName);
				if (file == null)
					continue;
				wrapper.UpdateContent (c => c.RemoveFiles (fileName));
				wrapper.InformFileRemoved (new ParsedFileEventArgs (file));

				var tags = wrapper.GetExtensionObject <ProjectCommentTags> ();
				if (tags != null)
					tags.RemoveFile (wrapper.Project, fileName);
			}
		}

		static void OnFileRenamed (object sender, ProjectFileRenamedEventArgs args)
		{
			var project = (Project)sender;
			foreach (ProjectFileRenamedEventInfo fargs in args) {
				var content = projectContents [project];
				var file = content.Content.GetFile (fargs.OldName);
				if (file == null)
					continue;
				content.UpdateContent (c => c.RemoveFiles (fargs.OldName));
				content.InformFileRemoved (new ParsedFileEventArgs (file));

				var tags = content.GetExtensionObject <ProjectCommentTags> ();
				if (tags != null)
					tags.RemoveFile (project, fargs.OldName);

				QueueParseJob (content, new [] { fargs.ProjectFile });
			}
		}

		static void OnProjectModified (object sender, SolutionItemModifiedEventArgs args)
		{
			if (!args.Any (x => x.Hint == "TargetFramework" || x.Hint == "References"))
				return;
			var project = (Project)sender;
			
			ProjectContentWrapper wrapper;
			projectContents.TryGetValue (project, out wrapper);
			if (wrapper == null)
				return;
			wrapper.ReconnectAssemblyReferences ();
		}

		#endregion

		internal static void Unload (WorkspaceItem item)
		{
			var ws = item as Workspace;
			TrackFileChanges = false;
			loadCancellationSource.Cancel ();
			if (ws != null) {
				foreach (WorkspaceItem it in ws.Items)
					Unload (it);
				ws.ItemAdded -= OnWorkspaceItemAdded;
				ws.ItemRemoved -= OnWorkspaceItemRemoved;
				projectContents.Clear ();
				loadWs = false;
			} else {
				var solution = item as Solution;
				if (solution != null) {
					foreach (var project in solution.GetAllProjects ()) {
						UnloadProject (project);
					}
					solution.SolutionItemAdded -= OnSolutionItemAdded;
					solution.SolutionItemRemoved -= OnSolutionItemRemoved;
				}
			}

			var oldCache = cachedAssemblyContents.Values.ToList ();
			cachedAssemblyContents.Clear ();
			lock (parseQueueLock) {
				parseQueueIndex.Clear ();
				parseQueue.Clear ();
			}
			TrackFileChanges = true;
		}

		internal static void UnloadProject (Project project)
		{
			if (DecLoadCount (project) != 0)
				return;
			Counters.ParserService.ProjectsLoaded--;
			project.FileAddedToProject -= OnFileAdded;
			project.FileRemovedFromProject -= OnFileRemoved;
			project.FileRenamedInProject -= OnFileRenamed;
			project.Modified -= OnProjectModified;
				
			ProjectContentWrapper wrapper;
			lock (projectWrapperUpdateLock) {
				if (!projectContents.TryGetValue (project, out wrapper))
					return;
				projectContents.Remove (project);
			}
			StoreProjectCache (project, wrapper);
			OnProjectUnloaded (new ProjectUnloadEventArgs (project, wrapper));
			wrapper.Unload ();
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
			var project = args.SolutionItem as Project;
			if (project != null) {
				var wrapper = LoadProject (project);
				if (wrapper != null) {
					wrapper.ReconnectAssemblyReferences ();
					var files = wrapper.Project.Files.ToArray ();
					Task.Factory.StartNew (delegate {
						CheckModifiedFiles (wrapper.Project, files, wrapper);
					});
				}
			}
		}

		static void OnSolutionItemRemoved (object sender, SolutionItemChangeEventArgs args)
		{
			var project = args.SolutionItem as Project;
			if (project != null)
				UnloadProject (project);
		}

		#endregion

		#region Reference Counting

		static readonly Dictionary<Project,int> loadCount = new Dictionary<Project,int> ();
		static readonly object rwLock = new object ();

		static int DecLoadCount (Project ob)
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

		static int IncLoadCount (Project ob)
		{
			lock (rwLock) {
				int c;
				if (loadCount.TryGetValue (ob, out c)) {
					c++;
					loadCount [ob] = c;
					return c;
				}
				loadCount [ob] = 1;
				return 1;
			}
		}

		#endregion

		static bool GetXml (string baseName, TargetRuntime runtime, out FilePath xmlFileName)
		{
			try {
				xmlFileName = LookupLocalizedXmlDoc (baseName);
				if (!xmlFileName.IsNull)
					return true;
			} catch (Exception e) {
				LoggingService.LogError ("Error while looking up XML docs.", e);
			}
			
			if (MonoDevelop.Core.Platform.IsWindows) {
				string windowsFileName = FindWindowsXmlDocumentation (baseName, runtime);
				if (File.Exists (windowsFileName)) {
					xmlFileName = windowsFileName;
					return true;
				}
			}
			
			xmlFileName = "";
			return false;
		}

		#region Lookup XML documentation

		// ProgramFilesX86 is broken on 32-bit WinXP, this is a workaround
		static string GetProgramFilesX86 ()
		{
			return Environment.GetFolderPath (IntPtr.Size == 8 ?
				Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
		}

		static readonly string referenceAssembliesPath = Path.Combine (GetProgramFilesX86 (), @"Reference Assemblies\Microsoft\\Framework");
		static readonly string frameworkPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework");

		static string FindWindowsXmlDocumentation (string assemblyFileName, TargetRuntime runtime)
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

		class UnresolvedAssemblyProxy : IUnresolvedAssembly
		{
			public readonly string FileName;
			internal LazyAssemblyLoader CtxLoader;

			public IUnresolvedAssembly Ctx {
				get {
					return CtxLoader;
				}
			}

			public bool InLoad {
				get {
					return CtxLoader == null || CtxLoader.InLoad;
				}
			}

			public event EventHandler Loaded {
				add {
					var ctxLoader = CtxLoader;
					if (ctxLoader != null)
						ctxLoader.Loaded += value;
				}
				remove { 
					var ctxLoader = CtxLoader;
					if (ctxLoader != null)
						ctxLoader.Loaded -= value;
				}
			}

			public UnresolvedAssemblyProxy (string fileName)
			{
				if (fileName == null)
					throw new ArgumentNullException ("fileName");
				this.FileName = fileName;
			}

			#region IUnresolvedAssembly implementation

			string IUnresolvedAssembly.AssemblyName {
				get {
					return Ctx.AssemblyName;
				}
			}

			string IUnresolvedAssembly.FullAssemblyName {
				get {
					return Ctx.FullAssemblyName;
				}
			}

			string IUnresolvedAssembly.Location {
				get {
					return Ctx.Location;
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

		internal class LazyAssemblyLoader : IUnresolvedAssembly
		{
			class LazyAssembly : IAssembly
			{
				readonly LazyAssemblyLoader loader;
				readonly ITypeResolveContext context;
				IAssembly assembly;

				IAssembly Assembly {
					get {
						lock (loader) {
							if (assembly == null) {
								loader.EnsureAssemblyLoaded ();
								assembly = loader.assembly.Resolve (context);
							}
							return assembly;
						}
					}
				}


				public LazyAssembly (LazyAssemblyLoader loader, ITypeResolveContext context)
				{
					this.loader = loader;
					this.context = context;
				}

				#region IAssembly implementation

				bool IAssembly.InternalsVisibleTo (IAssembly assembly)
				{
					return Assembly.InternalsVisibleTo (assembly);
				}

				ITypeDefinition IAssembly.GetTypeDefinition (TopLevelTypeName typeName)
				{
					return Assembly.GetTypeDefinition (typeName);
				}

				IUnresolvedAssembly IAssembly.UnresolvedAssembly {
					get {
						return Assembly.UnresolvedAssembly;
					}
				}

				bool IAssembly.IsMainAssembly {
					get {
						return Assembly.IsMainAssembly;
					}
				}

				string IAssembly.AssemblyName {
					get {
						return Assembly.AssemblyName;
					}
				}

				string IAssembly.FullAssemblyName {
					get {
						return Assembly.FullAssemblyName;
					}
				}

				IList<IAttribute> IAssembly.AssemblyAttributes {
					get {
						return Assembly.AssemblyAttributes;
					}
				}

				IList<IAttribute> IAssembly.ModuleAttributes {
					get {
						return Assembly.ModuleAttributes;
					}
				}

				INamespace IAssembly.RootNamespace {
					get {
						return Assembly.RootNamespace;
					}
				}

				IEnumerable<ITypeDefinition> IAssembly.TopLevelTypeDefinitions {
					get {
						return Assembly.TopLevelTypeDefinitions;
					}
				}

				#endregion

				#region ICompilationProvider implementation

				ICompilation ICompilationProvider.Compilation {
					get {
						return Assembly.Compilation;
					}
				}

				#endregion

			}

			#region IAssemblyReference implementation

			IAssembly IAssemblyReference.Resolve (ITypeResolveContext context)
			{
				if (assembly != null)
					return assembly.Resolve (context);
				return new LazyAssembly (this, context);
			}

			#endregion

			#region IUnresolvedAssembly implementation

			readonly object assemblyLock = new object ();

			string IUnresolvedAssembly.AssemblyName {
				get {
					lock (assemblyLock) {
						EnsureAssemblyLoaded ();
						return assembly.AssemblyName;
					}
				}
			}

			string IUnresolvedAssembly.FullAssemblyName {
				get {
					lock (assemblyLock) {
						EnsureAssemblyLoaded ();
						return assembly.FullAssemblyName;
					}
				}
			}

			string IUnresolvedAssembly.Location {
				get {
					lock (assemblyLock) {
						EnsureAssemblyLoaded ();
						return assembly.Location;
					}
				}
			}

			IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.AssemblyAttributes {
				get {
					lock (assemblyLock) {
						EnsureAssemblyLoaded ();
						return assembly.AssemblyAttributes;
					}
				}
			}

			IEnumerable<IUnresolvedAttribute> IUnresolvedAssembly.ModuleAttributes {
				get {
					lock (assemblyLock) {
						EnsureAssemblyLoaded ();
						return assembly.ModuleAttributes;
					}
				}
			}

			IEnumerable<IUnresolvedTypeDefinition> IUnresolvedAssembly.TopLevelTypeDefinitions {
				get {
					lock (assemblyLock) {
						EnsureAssemblyLoaded ();
						return assembly.TopLevelTypeDefinitions;
					}
				}
			}

			#endregion

			readonly string fileName;
			readonly string cache;
			IUnresolvedAssembly assembly;

			readonly object asmLocker = new object ();
			internal void EnsureAssemblyLoaded ()
			{
				lock (asmLocker) {
					if (assembly != null)
						return;
					var loadedAssembly = LoadAssembly ();
					if (loadedAssembly == null) {
						LoggingService.LogWarning ("Assembly " + fileName + " could not be loaded cleanly.");
						assembly = new DefaultUnresolvedAssembly (fileName);
					} else {
						assembly = loadedAssembly;
					}

					OnLoad (EventArgs.Empty);
				}
			}

			public override string ToString ()
			{
				return string.Format ("[LazyAssemblyLoader: fileName={0}, assembly={1}]", fileName, assembly);
			}
			
			public bool InLoad {
				get {
					return assembly == null;
				}
			}

			public event EventHandler Loaded;

			protected virtual void OnLoad (EventArgs e)
			{
				var handler = Loaded;
				if (handler != null)
					handler (this, e);
			}

			public LazyAssemblyLoader (string fileName, string cache)
			{
				this.fileName = fileName;
				this.cache = cache;
			}

			IUnresolvedAssembly LoadAssembly ()
			{
				var assemblyPath = cache != null ? Path.Combine (cache, "assembly.data") : null;
				try {
					if (assemblyPath != null && File.Exists (assemblyPath)) {
						var deserializedAssembly = DeserializeObject <IUnresolvedAssembly> (assemblyPath);
						if (deserializedAssembly != null) {
							return deserializedAssembly;
						}
					}
				} catch (Exception) {
				}

				IUnresolvedAssembly result;
				try {
					var loader = new IkvmLoader ();
					loader.IncludeInternalMembers = true;
					loader.DocumentationProvider = new CombinedDocumentationProvider (fileName);
					result = loader.LoadAssemblyFile (fileName);
				} catch (Exception e) {
					LoggingService.LogError ("Can't convert assembly: " + fileName, e);
					return null;
				}

				if (cache != null) {
					var writeTime = File.GetLastWriteTimeUtc (fileName);
					SerializeObject (assemblyPath, result);
					if (File.Exists (assemblyPath)) {
						try {
							File.SetCreationTimeUtc (assemblyPath, writeTime);
						} catch (Exception e) {
							LoggingService.LogError ("Can't set creation time for: " + assemblyPath, e);
						}
					}
				}
				return result;
			}
		}

		[Serializable]
		class CombinedDocumentationProvider : IDocumentationProvider
		{
			readonly string fileName;
			[NonSerialized]
			IDocumentationProvider baseProvider;

			public IDocumentationProvider BaseProvider {
				get {
					if (baseProvider == null) {
						FilePath xmlDocFile;
						if (GetXml (fileName, null, out xmlDocFile)) {
							try {
								baseProvider = new XmlDocumentationProvider (xmlDocFile);
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

		static readonly object assemblyContextLock = new object ();

		static UnresolvedAssemblyProxy LoadAssemblyContext (FilePath fileName)
		{
			CanonicalizePath (ref fileName);

			UnresolvedAssemblyProxy loadedContext;
			if (cachedAssemblyContents.TryGetValue (fileName, out loadedContext)) {
				return loadedContext;
			}
			if (!File.Exists (fileName))
				return null;
			lock (assemblyContextLock) {
				if (cachedAssemblyContents.TryGetValue (fileName, out loadedContext)) {
					CheckModifiedFile (loadedContext);
					return loadedContext;
				}

				string cache = GetCacheDirectory (fileName, true);

				try {
					var result = new UnresolvedAssemblyProxy (fileName);
					result.CtxLoader = new LazyAssemblyLoader (fileName, cache);
					CheckModifiedFile (result);
					var newcachedAssemblyContents = new Dictionary<string, UnresolvedAssemblyProxy> (cachedAssemblyContents);
					newcachedAssemblyContents [fileName] = result;
					cachedAssemblyContents = newcachedAssemblyContents;
					OnAssemblyLoaded (new AssemblyLoadedEventArgs (result.CtxLoader));
					return result;
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading assembly " + fileName, ex);
					return null;
				}
			}
		}

		internal static event EventHandler<AssemblyLoadedEventArgs> AssemblyLoaded;

		static  void OnAssemblyLoaded (AssemblyLoadedEventArgs e)
		{
			var handler = AssemblyLoaded;
			if (handler != null)
				handler (null, e);
		}

		public static IUnresolvedAssembly LoadAssemblyContext (TargetRuntime runtime, TargetFramework fx, string fileName)
		{
			if (File.Exists (fileName))
				return LoadAssemblyContext (fileName);
			var corLibRef = runtime.AssemblyContext.GetAssemblyForVersion (fileName, null, fx);
			return corLibRef == null ? null : LoadAssemblyContext (corLibRef.Location);
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

		public static ICompilation GetCompilation (SystemAssembly assembly, ICompilation compilation)
		{
			var ctx = LoadAssemblyContext (assembly.Location);
			var list = compilation.ReferencedAssemblies.Select (r => r.UnresolvedAssembly).ToList ();
			list.Add (compilation.MainAssembly.UnresolvedAssembly);
			var	result = new SimpleCompilation (ctx, list);
			return result;
		}

		static IEnumerable<SystemAssembly> GetFrameworkAssemblies (DotNetProject netProject)
		{
			var assemblies = new Dictionary<string, SystemAssembly> ();
			foreach (var systemPackage in netProject.AssemblyContext.GetPackages ()) {
				foreach (var assembly in systemPackage.Assemblies) {
					SystemAssembly existing;
					if (assemblies.TryGetValue (assembly.Name, out existing)) {
						Version v1, v2;
						if (!Version.TryParse (existing.Version, out v1))
							continue;
						if (!Version.TryParse (assembly.Version, out v2))
							continue;
						if (v1 > v2)
							continue;
					}
					assemblies [assembly.Name] = assembly;
				}
			}
			return assemblies.Values;
		}

		class FrameworkTask
		{
			public int RetryCount { get; set; }

			public Task<FrameworkLookup> Task { get; set; }
		}

		readonly static Dictionary<string, FrameworkTask> frameworkLookup = new Dictionary<string, FrameworkTask> ();

		static void StartFrameworkLookup (DotNetProject netProject)
		{
			if (netProject == null)
				throw new ArgumentNullException ("netProject");
			lock (frameworkLookup) {
				FrameworkTask result;
				if (netProject.TargetFramework == null)
					return;
				var frameworkName = netProject.TargetFramework.Name;
				if (!frameworkLookup.TryGetValue (frameworkName, out result))
					frameworkLookup [frameworkName] = result = new FrameworkTask ();
				if (result.Task != null)
					return;
				result.Task = Task.Factory.StartNew (delegate {
					return GetFrameworkLookup (netProject);
				});
			}
		}

		public static bool TryGetFrameworkLookup (DotNetProject project, out FrameworkLookup lookup)
		{
			lock (frameworkLookup) {
				FrameworkTask result;
				if (frameworkLookup.TryGetValue (project.TargetFramework.Name, out result)) {
					if (!result.Task.IsCompleted) {
						lookup = null;
						return false;
					}
					lookup = result.Task.Result;
					return true;
				}
			}
			lookup = null;
			return false;
		}

		public static bool RecreateFrameworkLookup (DotNetProject netProject)
		{
			lock (frameworkLookup) {
				FrameworkTask result;
				var frameworkName = netProject.TargetFramework.Name;
				if (!frameworkLookup.TryGetValue (frameworkName, out result))
					return false;
				if (result.RetryCount > 5) {
					LoggingService.LogError ("Can't create framework lookup for:" + frameworkName);
					return false;
				}
				result.RetryCount++;
				LoggingService.LogInfo ("Trying to recreate framework lookup for {0}, try {1}.", frameworkName, result.RetryCount);
				result.Task = null;
				StartFrameworkLookup (netProject);
				return true;
			}
		}

		static FrameworkLookup GetFrameworkLookup (DotNetProject netProject)
		{
			FrameworkLookup result;
			string fileName;
			var cache = GetCacheDirectory (netProject.TargetFramework);
			fileName = Path.Combine (cache, "FrameworkLookup_" + FrameworkLookup.CurrentVersion + ".dat");
			try {
				if (File.Exists (fileName)) {
					result = FrameworkLookup.Load (fileName);
					if (result != null) {
						return result;
					}
				}
			} catch (Exception e) {
				LoggingService.LogWarning ("Can't read framework cache - recreating...", e);
			}

			try {
				using (var creator = FrameworkLookup.Create (fileName)) {
					foreach (var assembly in GetFrameworkAssemblies (netProject)) {
						var ctx = LoadAssemblyContext (assembly.Location);
						foreach (var type in ctx.Ctx.GetAllTypeDefinitions ()) {
							if (!type.IsPublic)
								continue;
							creator.AddLookup (assembly.Package.Name, assembly.FullName, type);
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while storing framework lookup", e);
				return FrameworkLookup.Empty;
			}

			try {
				result = FrameworkLookup.Load (fileName);
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Error loading framework lookup", e);
				return FrameworkLookup.Empty;
			}
		}

		public static ProjectContentWrapper GetProjectContentWrapper (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			ProjectContentWrapper content;
			if (projectContents.TryGetValue (project, out content))
				return content;
			return new ProjectContentWrapper (project);
		}

		public static IProjectContent GetContext (FilePath file, string mimeType, string text)
		{
			using (var reader = new StringReader (text)) {
				var parsedDocument = ParseFile (file, mimeType, reader);
				
				var content = new CSharpProjectContent ();
				return content.AddOrUpdateFiles (parsedDocument.ParsedFile);
			}
		}

		static Dictionary<string, UnresolvedAssemblyProxy> cachedAssemblyContents = new Dictionary<string, UnresolvedAssemblyProxy> ();

		/// <summary>
		/// Force the update of a project context. Note: This method blocks the thread.
		/// It was just implemented for use inside unit tests.
		/// </summary>
		public static void ForceUpdate (ProjectContentWrapper context)
		{
			CheckModifiedFiles ();
			while (!context.IsLoaded) {
				Thread.Sleep (10);
			}
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
			var mon = ParseProgressMonitorFactory != null ? ParseProgressMonitorFactory.CreateProgressMonitor () : new NullProgressMonitor ();
			
			return new AggregatedProgressMonitor (mon, new InternalProgressMonitor ());
		}

		static readonly Queue<ParsingJob> parseQueue = new Queue<ParsingJob> ();

		class ParsingJob
		{
			public ProjectContentWrapper Context;
			public IEnumerable<ProjectFile> FileList;
			//			public Action<string, IProgressMonitor> ParseCallback;
			public void Run (IProgressMonitor monitor, CancellationToken token)
			{
				TypeSystemParserNode node = null;
				TypeSystemParser parser = null;
				var tags = Context.GetExtensionObject <ProjectCommentTags> ();
				string mimeType = null, oldExtension = null, buildAction = null;
				try {
					Context.BeginLoadOperation ();
					foreach (var file in (FileList ?? Context.Project.Files)) {
						if (token.IsCancellationRequested)
							return;
						var fileName = file.FilePath;
						if (filesSkippedInParseThread.Any (f => f == fileName))
							continue;
						if (node == null || !node.CanParse (fileName, file.BuildAction)) {
							node = GetTypeSystemParserNode (DesktopService.GetMimeTypeForUri (fileName), file.BuildAction);
							parser = node != null ? node.Parser : null;
						}

						if (parser == null || !File.Exists (fileName))
							continue;
						ParsedDocument parsedDocument;
						try {
							parsedDocument = parser.Parse (false, fileName, Context.Project);
						} catch (Exception e) {
							LoggingService.LogError ("Error while parsing " + fileName, e);
							continue;
						} 
						if (token.IsCancellationRequested)
							return;
						if (tags != null)
							tags.UpdateTags (Context.Project, parsedDocument.FileName, parsedDocument.TagComments);
						if (token.IsCancellationRequested)
							return;
						var oldFile = Context.Content.GetFile (fileName);
						Context.UpdateContent (c => c.AddOrUpdateFiles (parsedDocument.ParsedFile));
						if (oldFile != null)
							Context.InformFileRemoved (new ParsedFileEventArgs (oldFile));
						Context.InformFileAdded (new ParsedFileEventArgs (parsedDocument.ParsedFile));
					}
				} finally {
					if (!token.IsCancellationRequested)
						Context.EndLoadOperation ();
				}
			}
		}

		static void UpdateProjectCommentTasks (ProjectContentWrapper context, ParsedDocument parsedDocument)
		{
			var tags = context.GetExtensionObject <ProjectCommentTags> ();
			if (tags != null) // When tags are not there they're updated first time the tasks are requested.
				tags.UpdateTags (context.Project, parsedDocument.FileName, parsedDocument.TagComments);
		}
		//		public static event EventHandler<ProjectFileEventArgs> FileParsed;
		static readonly object parseQueueLock = new object ();
		static readonly AutoResetEvent parseEvent = new AutoResetEvent (false);
		static readonly ManualResetEvent queueEmptied = new ManualResetEvent (true);
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

		static readonly Dictionary<ProjectContentWrapper, ParsingJob> parseQueueIndex = new Dictionary<ProjectContentWrapper, ParsingJob> ();

		internal static int PendingJobCount {
			get {
				lock (parseQueueLock) {
					return parseQueueIndex.Count;
				}
			}
		}

		static void QueueParseJob (ProjectContentWrapper context, IEnumerable<ProjectFile> fileList = null)
		{
			var job = new ParsingJob {
				Context = context,
				FileList = fileList
			};
			lock (parseQueueLock) {
				RemoveParseJob (context);
				context.BeginLoadOperation ();
				parseQueueIndex [context] = job;
				parseQueue.Enqueue (job);
				parseEvent.Set ();
				
				if (parseQueueIndex.Count == 1)
					queueEmptied.Reset ();
			}
		}

		static bool WaitForParseJob (int timeout = 5000)
		{
			return parseEvent.WaitOne (timeout, true);
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
					project.EndLoadOperation ();
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
					WaitForParseJob ();
//						CheckModifiedFiles ();
					if (trackingFileChanges)
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

		static bool IsFileModified (ProjectFile file, IUnresolvedFile parsedFile)
		{
			if (parsedFile == null)
				return true;
			try {
				return File.GetLastWriteTimeUtc (file.FilePath) > parsedFile.LastWriteTime;
			} catch (Exception) {
				return false;
			}
		}

		static void CheckModifiedFiles (Project project, ProjectFile[] projectFiles, ProjectContentWrapper content, CancellationToken token = default (CancellationToken))
		{
			if (token.IsCancellationRequested)
				return;
			content.RunWhenLoaded (delegate(IProjectContent cnt) {
				try {
					content.BeginLoadOperation ();
					var modifiedFiles = new List<ProjectFile> ();
					var oldFileNewFile = new List<Tuple<ProjectFile, IUnresolvedFile>> ();

					foreach (var file in projectFiles) {
						if (token.IsCancellationRequested)
							return;
						if (file.BuildAction == null)
							continue;
						// if the file is already inside the content a parser exists for it, if not check if it can be parsed.
						var oldFile = cnt.GetFile (file.Name);
						oldFileNewFile.Add (Tuple.Create (file, oldFile));
					}

					// This is disk intensive and slow
					oldFileNewFile.RemoveAll (t => !IsFileModified (t.Item1, t.Item2));

					foreach (var v in oldFileNewFile) {
						var file = v.Item1;
						var oldFile = v.Item2;
						if (oldFile == null) {
							var parser = TypeSystemService.GetParser (DesktopService.GetMimeTypeForUri (file.Name), file.BuildAction);
							if (parser == null)
								continue;
						}
						modifiedFiles.Add (file);
					}
					var tags = content.GetExtensionObject <ProjectCommentTags> ();

					// check if file needs to be removed from project content 
					foreach (var file in cnt.Files) {
						if (token.IsCancellationRequested)
							return;
						if (project.GetProjectFile (file.FileName) == null) {
							content.UpdateContent (c => c.RemoveFiles (file.FileName));
							content.InformFileRemoved (new ParsedFileEventArgs (file));
							if (tags != null)
								tags.RemoveFile (project, file.FileName);
						}
					}
					if (token.IsCancellationRequested)
						return;
					if (modifiedFiles.Count > 0)
						QueueParseJob (content, modifiedFiles);
				} catch (Exception e) {
					LoggingService.LogError ("Exception in check modified files.", e);
				} finally {
					content.EndLoadOperation ();
				}

			});
		}

		static void CheckModifiedFile (UnresolvedAssemblyProxy context)
		{
			try {
				string cache = GetCacheDirectory (context.FileName);
				if (cache == null)
					return;
				var assemblyDataDirectory = Path.Combine (cache, "assembly.data");
				var writeTime = File.GetLastWriteTimeUtc (context.FileName);
				var cacheTime = File.Exists (assemblyDataDirectory) ? File.GetCreationTimeUtc (assemblyDataDirectory) : writeTime;
				if (writeTime != cacheTime) {
					cache = GetCacheDirectory (context.FileName);
					if (cache != null) {
						context.CtxLoader = new LazyAssemblyLoader (context.FileName, cache);
						try {
							// File is reloaded by the lazy loader
							File.Delete (assemblyDataDirectory);
						} catch {
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

			lock (projectContentLock) {
				list = new Queue<KeyValuePair<Project, ProjectContentWrapper>> (projectContents);
			}

			while (list.Count > 0) {
				var readydb = list.Dequeue ();
				var files = readydb.Key.Files.ToArray ();
				CheckModifiedFiles (readydb.Key, files, readydb.Value);
			}
			
			var assemblyList = new Queue<KeyValuePair<string, UnresolvedAssemblyProxy>> (cachedAssemblyContents);

			while (assemblyList.Count > 0) {
				var readydb = assemblyList.Dequeue ();
				CheckModifiedFile (readydb.Value);
			}
		}

		static void ConsumeParsingQueue ()
		{
			int pending = 0;
			IProgressMonitor monitor = null;
			var token = loadCancellationSource.Token;
			try {
				do {
					if (pending > 5 && monitor == null) {
						monitor = GetParseProgressMonitor ();
						monitor.BeginTask (GettextCatalog.GetString ("Generating database"), 0);
					}
					var job = DequeueParseJob ();
					if (job != null) {
						try {
							job.Run (monitor, token);
						} catch (Exception ex) {
							if (monitor == null)
								monitor = GetParseProgressMonitor ();
							monitor.ReportError (null, ex);
						} finally {
							job.Context.EndLoadOperation ();
						}
					}
					
					if (token.IsCancellationRequested)
						break;
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

	sealed class AssemblyLoadedEventArgs : EventArgs
	{
		public readonly TypeSystemService.LazyAssemblyLoader Assembly;

		public AssemblyLoadedEventArgs (TypeSystemService.LazyAssemblyLoader assembly)
		{
			this.Assembly = assembly;
		}
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


