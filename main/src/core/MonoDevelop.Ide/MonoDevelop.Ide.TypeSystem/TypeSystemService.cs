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
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Threading;
using System.Xml;
using ICSharpCode.NRefactory.Utils;
using System.Threading.Tasks;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Core.Assemblies;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using Microsoft.CodeAnalysis.Text;
using Mono.Posix;

namespace MonoDevelop.Ide.TypeSystem
{
	public static partial class TypeSystemService
	{
		const string CurrentVersion = "1.1.9";
		static IEnumerable<TypeSystemParserNode> parsers;
		static string[] filesSkippedInParseThread = new string[0];
		public static Microsoft.CodeAnalysis.SyntaxAnnotation InsertionModeAnnotation = new Microsoft.CodeAnalysis.SyntaxAnnotation();

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
			CleanupCache ();
			parsers = AddinManager.GetExtensionNodes<TypeSystemParserNode> ("/MonoDevelop/TypeSystem/Parser");
			bool initialLoad = true;
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/Parser", delegate (object sender, ExtensionNodeEventArgs args) {
				//refresh entire list to respect insertbefore/insertafter ordering
				if (!initialLoad)
					parsers = AddinManager.GetExtensionNodes<TypeSystemParserNode> ("/MonoDevelop/TypeSystem/Parser");
			});
			initialLoad = false;

			try {
				emptyWorkspace = new MonoDevelopWorkspace (null);
			} catch (Exception e) {
				LoggingService.LogFatalError ("Can't create roslyn workspace", e); 
			}

			FileService.FileChanged += delegate(object sender, FileEventArgs e) {
				//				if (!TrackFileChanges)
				//					return;

				var filesToUpdate = new List<string> ();
				foreach (var file in e) {
					// Open documents are handled by the Document class itself.
					if (IdeApp.Workbench != null && IdeApp.Workbench.GetDocument (file.FileName) != null)
						continue;
					
					foreach (var w in workspaces) {
						foreach (var p in w.CurrentSolution.ProjectIds) {
							if (w.GetDocumentId (p, file.FileName) != null) {
								filesToUpdate.Add (file.FileName);
								goto found;
							}
						}
					}
				found:;
					
				}
				if (filesToUpdate.Count == 0)
					return;

				Task.Run (delegate {
					try {
						foreach (var file in filesToUpdate) {
							var text = MonoDevelop.Core.Text.StringTextSource.ReadFrom (file).Text;
							foreach (var w in workspaces)
								w.UpdateFileContent (file, text);
						}

						Gtk.Application.Invoke ((o, args) => {
							if (IdeApp.Workbench != null)
								foreach (var w in IdeApp.Workbench.Documents)
									w.StartReparseThread ();
						});
					} catch (Exception) {}
				});
			};

			IntitializeTrackedProjectHandling ();
		}

		public static TypeSystemParser GetParser (string mimeType, string buildAction = BuildAction.Compile)
		{
			var n = GetTypeSystemParserNode (mimeType, buildAction);
			return n != null ? n.Parser : null;
		}

		internal static TypeSystemParserNode GetTypeSystemParserNode (string mimeType, string buildAction)
		{
			foreach (var mt in DesktopService.GetMimeTypeInheritanceChain (mimeType)) {
				var provider = Parsers.FirstOrDefault (p => p.CanParse (mt, buildAction));
				if (provider != null)
					return provider;
			}
			return null;
		}

		public static Task<ParsedDocument> ParseFile (Project project, string fileName, CancellationToken cancellationToken = default(CancellationToken))
		{
			StringTextSource text;

			try {
				if (!File.Exists (fileName))
					return TaskUtil.Default<ParsedDocument>();
				text = StringTextSource.ReadFrom (fileName);
			} catch (Exception) {
				return TaskUtil.Default<ParsedDocument>();
			}

			return ParseFile (project, fileName, DesktopService.GetMimeTypeForUri (fileName), text, cancellationToken);
		}

		public static Task<ParsedDocument> ParseFile (ParseOptions options, string mimeType, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (options == null)
				throw new ArgumentNullException (nameof(options));
			if (options.FileName == null)
				throw new ArgumentNullException ("options.FileName");

			var parser = GetParser (mimeType);
			if (parser == null)
				return TaskUtil.Default<ParsedDocument>();

			var t = Counters.ParserService.FileParsed.BeginTiming (options.FileName);
			try {
				var result = parser.Parse (options, cancellationToken);
				return result ?? TaskUtil.Default<ParsedDocument>();
			} catch (OperationCanceledException) {
				return TaskUtil.Default<ParsedDocument>();
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing: " + e);
				return TaskUtil.Default<ParsedDocument>();
			} finally {
				t.Dispose ();
			}
		}

		internal static bool CanParseProjections (Project project, string mimeType, string fileName)
		{
			var projectFile = project.GetProjectFile (fileName);
			if (projectFile == null)
				return false;
			var parser = GetParser (mimeType, projectFile.BuildAction);
			if (parser == null)
				return false;
			return parser.CanGenerateProjection (mimeType, projectFile.BuildAction, project.SupportedLanguages);
		}

		public static Task<ParsedDocument> ParseFile (Project project, string fileName, string mimeType, ITextSource content, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ParseFile (new ParseOptions { FileName = fileName, Project = project, Content = content }, mimeType, cancellationToken);
		}

		public static Task<ParsedDocument> ParseFile (Project project, string fileName, string mimeType, TextReader content, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ParseFile (project, fileName, mimeType, new StringTextSource (content.ReadToEnd ()), cancellationToken);
		}

		public static Task<ParsedDocument> ParseFile (Project project, IReadonlyTextDocument data, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ParseFile (project, data.FileName, data.MimeType, data, cancellationToken);
		}

		internal static async Task<ParsedDocumentProjection> ParseProjection (ParseOptions options, string mimeType, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (options == null)
				throw new ArgumentNullException (nameof(options));
			if (options.FileName == null)
				throw new ArgumentNullException ("fileName");

			var parser = GetParser (mimeType, options.BuildAction);
			if (parser == null || !parser.CanGenerateProjection (mimeType, options.BuildAction, options.Project?.SupportedLanguages))
				return null;

			var t = Counters.ParserService.FileParsed.BeginTiming (options.FileName);
			try {
				var result = await parser.GenerateParsedDocumentProjection (options, cancellationToken);
				if (options.Project != null) {
					var ws = workspaces.First () ;
					var projectId = ws.GetProjectId (options.Project);

					if (projectId != null) {
						var projectFile = options.Project.GetProjectFile (options.FileName);
						if (projectFile != null) {
							ws.UpdateProjectionEntry (projectFile, result.Projections);
							foreach (var projection in result.Projections) {
								var docId = ws.GetDocumentId (projectId, projection.Document.FileName);
								if (docId != null) {
									ws.InformDocumentTextChange (docId, new MonoDevelopSourceText (projection.Document));
								}
							}
						}
					}
				}
				return result;
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (x => x is OperationCanceledException);
				return null;
			} catch (OperationCanceledException) {
				return null;
			} catch (Exception e) {
				LoggingService.LogError ("Exception while parsing: " + e);
				return null;
			} finally {
				t.Dispose ();
			}
		}

		internal static Task<ParsedDocumentProjection> ParseProjection (Project project, string fileName, string mimeType, ITextSource content, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ParseProjection (new ParseOptions { FileName = fileName, Project = project, Content = content }, mimeType, cancellationToken);
		}

		internal static Task<ParsedDocumentProjection> ParseProjection (Project project, string fileName, string mimeType, TextReader content, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ParseProjection (project, fileName, mimeType, new StringTextSource (content.ReadToEnd ()), cancellationToken);
		}

		internal static Task<ParsedDocumentProjection> ParseProjection (Project project, IReadonlyTextDocument data, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ParseProjection (project, data.FileName, data.MimeType, data, cancellationToken);
		}

	
		#region Folding parsers
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
		#endregion

		#region Parser Database Handling

		static string GetCacheDirectory (TargetFramework framework)
		{
			var derivedDataPath = UserProfile.Current.CacheDir.Combine ("DerivedData");

			var name = StringBuilderCache.Allocate ();
			foreach (var ch in framework.Name) {
				if (char.IsLetterOrDigit (ch)) {
					name.Append (ch);
				} else {
					name.Append ('_');
				}
			}

			string result = derivedDataPath.Combine (StringBuilderCache.ReturnAndFree (name));
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
				throw new ArgumentNullException (nameof(project));
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
				throw new ArgumentNullException (nameof(fileName));
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
			while (i < 999999) {
				yield return Path.Combine (baseName, i.ToString ());
				i++;
			}
			throw new Exception ("Too many cache directories");
		}

		static string EscapeToXml (string txt)
		{
			return new System.Xml.Linq.XText (txt).ToString ();
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
					string.Format ("<DerivedData><File name=\"{0}\" version =\"{1}\"/></DerivedData>", EscapeToXml (fileName), CurrentVersion)
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
				using (var fs = new FileStream (path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan)) {
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
				throw new ArgumentNullException (nameof(obj));

			var t = Counters.ParserService.ObjectSerialized.BeginTiming (path);
			try {
				using (var fs = new FileStream (path, System.IO.FileMode.Create, FileAccess.Write)) {
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
			IEnumerable<string> cacheDirectories;
			
			try {
				if (!Directory.Exists (derivedDataPath))
					return;
				cacheDirectories = Directory.EnumerateDirectories (derivedDataPath);
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting derived data directories.", e);
				return;
			}
			var now = DateTime.Now;
			foreach (var cacheDirectory in cacheDirectories) {
				try {
					foreach (var subDir in Directory.EnumerateDirectories (cacheDirectory)) {
						try {
							var days = Math.Abs ((now - Directory.GetLastWriteTime (subDir)).TotalDays);
							if (days > 30)
								Directory.Delete (subDir, true);
						} catch (Exception e) {
							LoggingService.LogError ("Error while removing outdated cache " + subDir, e);
						}
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting cache directories " + cacheDirectory, e);
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
				throw new ArgumentNullException (nameof(cacheDir));
			if (extensionObject == null)
				throw new ArgumentNullException (nameof(extensionObject));
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

		#endregion

		internal static void InformDocumentClose (Microsoft.CodeAnalysis.DocumentId analysisDocument, FilePath fileName)
		{
			foreach (var w in workspaces) {
				if (w.GetOpenDocumentIds (analysisDocument.ProjectId).Contains (analysisDocument) )
					w.InformDocumentClose (analysisDocument, fileName); 

			}
		}

		internal static void InformDocumentOpen (Microsoft.CodeAnalysis.DocumentId analysisDocument, TextEditor editor)
		{
			foreach (var w in workspaces) {
				if (w.Contains (analysisDocument.ProjectId)) {
					w.InformDocumentOpen (analysisDocument, editor); 
					return;
				}
			}
			if (!gotDocumentRequestError) {
				gotDocumentRequestError = true;
				LoggingService.LogWarning ("Can't open requested document : " + analysisDocument + ":" + editor.FileName);
			}
		}

		internal static void InformDocumentOpen (Microsoft.CodeAnalysis.Workspace ws, Microsoft.CodeAnalysis.DocumentId analysisDocument, TextEditor editor)
		{
			if (ws == null)
				throw new ArgumentNullException (nameof (ws));
			if (analysisDocument == null)
				throw new ArgumentNullException (nameof (analysisDocument));
			if (editor == null)
				throw new ArgumentNullException (nameof (editor));
			((MonoDevelopWorkspace)ws).InformDocumentOpen (analysisDocument, editor); 
		}

		static bool gotDocumentRequestError = false;

		public static Microsoft.CodeAnalysis.ProjectId GetProjectId (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException (nameof(project));
			foreach (var w in workspaces) {
				var projectId = w.GetProjectId (project);
				if (projectId != null) {
					return projectId;
				}
			}
			return null;
		}

		public static Microsoft.CodeAnalysis.Document GetCodeAnalysisDocument (Microsoft.CodeAnalysis.DocumentId docId, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (docId == null)
				throw new ArgumentNullException (nameof(docId));
			foreach (var w in workspaces) {
				var documentId = w.GetDocument (docId, cancellationToken);
				if (documentId != null) {
					return documentId;
				}
			}
			return null;
		}

		public static MonoDevelop.Projects.Project GetMonoProject (Microsoft.CodeAnalysis.Project project)
		{
			if (project == null)
				throw new ArgumentNullException (nameof(project));
			foreach (var w in workspaces) {
				var documentId = w.GetMonoProject (project);
				if (documentId != null) {
					return documentId;
				}
			}
			return null;
		}


		public static MonoDevelop.Projects.Project GetMonoProject (Microsoft.CodeAnalysis.DocumentId documentId)
		{
			foreach (var w in workspaces) {
				var doc = w.GetDocument (documentId);
				if (doc == null)
					continue;

				var p = doc.Project;
				if (p != null)
					return GetMonoProject (p);
			}
			return null;
		}

		static StatusBarIcon statusIcon = null;
		static int workspacesLoading = 0;

		internal static void ShowTypeInformationGatheringIcon ()
		{
			Gtk.Application.Invoke ((o, args) => {
				workspacesLoading++;
				if (statusIcon != null)
					return;
				statusIcon = IdeApp.Workbench?.StatusBar.ShowStatusIcon (ImageService.GetIcon ("md-parser"));
				if (statusIcon != null)
					statusIcon.ToolTip = GettextCatalog.GetString ("Gathering class information");
			});
		}

		internal static void HideTypeInformationGatheringIcon (Action callback = null)
		{
			Gtk.Application.Invoke ((o, args) => {
				workspacesLoading--;
				if (workspacesLoading == 0 && statusIcon != null) {
					statusIcon.Dispose ();
					statusIcon = null;
					if (callback != null)
						callback ();
				}
			});
		}
	}
}
