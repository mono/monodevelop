// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Utility;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Xml;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Parser;

namespace MonoDevelop.Services
{
	public class DefaultParserService : AbstractService, IParserService
	{
		IParser[] parsers;
		
		public override void InitializeService()
		{
			parsers = (IParser[])(AddInTreeSingleton.AddInTree.GetTreeNode("/Workspace/Parser").BuildChildItems(this)).ToArray(typeof(IParser));
		}
		
		public IParserDatabase CreateParserDatabase ()
		{
			ParserDatabase c = new ParserDatabase (this);
			c.Initialize ();
			return c;
		}
		
		public IExpressionFinder GetExpressionFinder(string fileName)
		{
			IParser parser = GetParser (fileName);
			if (parser != null) {
				return parser.ExpressionFinder;
			}
			return null;
		}
		
		public virtual IParser GetParser (string fileName)
		{
			// HACK: I'm too lazy to do it 'right'
			// HACK: Still a hack, but extensible
			if (fileName != null) {
				foreach(IParser p in parsers) {
					if (p.CanParse(fileName)) {
						return p;
					}
				}
			}
			return null;
		}
		
		public void GenerateAssemblyDatabase (string baseDir, string name)
		{
			AssemblyCodeCompletionDatabase db = new AssemblyCodeCompletionDatabase (baseDir, name, (ParserDatabase) CreateParserDatabase());
			db.ParseInExternalProcess = false;
			db.ParseAll ();
			db.Write ();
		}
	}
	
	internal class ParserContext: IParserContext
	{
		DefaultParserService parserService;
		CodeCompletionDatabase db;
		ParserDatabase pdb;
		
		internal ParserContext (DefaultParserService parserService, ParserDatabase pdb, CodeCompletionDatabase db)
		{
			this.parserService = parserService;
			this.pdb = pdb;
			this.db = db;
		}
		
		public IExpressionFinder GetExpressionFinder (string fileName)
		{
			return pdb.GetExpressionFinder (fileName);
		}
		
		public IParseInformation ParseFile (string fileName)
		{
			return pdb.ParseFile (fileName);
		}
		
		public IParseInformation ParseFile (string fileName, string fileContent)
		{
			return pdb.ParseFile (fileName, fileContent);
		}
		
		public IParseInformation GetParseInformation (string fileName)
		{
			return pdb.GetParseInformation (fileName);
		}
		
		public IClass GetClass (string typeName)
		{
			return pdb.GetClass (db, typeName);
		}
		
		public string[] GetClassList (string subNameSpace, bool includeReferences)
		{
			return pdb.GetClassList (db, subNameSpace, includeReferences);
		}
		
		public string[] GetNamespaceList (string subNameSpace)
		{
			return pdb.GetNamespaceList (db, subNameSpace);
		}
		
		public ArrayList GetNamespaceContents (string subNameSpace, bool includeReferences)
		{
			return pdb.GetNamespaceContents (db, subNameSpace, includeReferences);
		}
		
		public bool NamespaceExists (string name)
		{
			return pdb.NamespaceExists (db, name);
		}
		
		public string SearchNamespace (IUsing iusing, string partitialNamespaceName)
		{
			return pdb.SearchNamespace (db, iusing, partitialNamespaceName);
		}
		
		public IClass SearchType (IUsing iusing, string partitialTypeName)
		{
			return pdb.SearchType (db, iusing, partitialTypeName);
		}
		
		
		public IClass GetClass (string typeName, bool deepSearchReferences, bool caseSensitive)
		{
			return pdb.GetClass (db, typeName, deepSearchReferences, caseSensitive);
		}
		
		public string[] GetClassList (string subNameSpace, bool includeReferences, bool caseSensitive)
		{
			return pdb.GetClassList (db, subNameSpace, includeReferences, caseSensitive);
		}
		
		public string[] GetNamespaceList (string subNameSpace, bool includeReferences, bool caseSensitive)
		{
			return pdb.GetNamespaceList (db, subNameSpace, includeReferences, caseSensitive);
		}
		
		public ArrayList GetNamespaceContents (string subNameSpace, bool includeReferences, bool caseSensitive)
		{
			return pdb.GetNamespaceContents (db, subNameSpace, includeReferences, caseSensitive);
		}
		
		public bool NamespaceExists (string name, bool caseSensitive)
		{
			return pdb.NamespaceExists (db, name, caseSensitive);
		}
		
		public string SearchNamespace (IUsing iusing, string partitialNamespaceName, bool caseSensitive)
		{
			return pdb.SearchNamespace (db, iusing, partitialNamespaceName, caseSensitive);
		}
		
		public IClass SearchType (IUsing iusing, string partitialTypeName, bool caseSensitive)
		{
			return pdb.SearchType (db, iusing, partitialTypeName, caseSensitive);
		}
		
		public IClass SearchType (string name, IClass callingClass, ICompilationUnit unit)
		{
			return pdb.SearchType (db, name, callingClass, unit);
		}
		
		public IEnumerable GetClassInheritanceTree (IClass cls)
		{
			return pdb.GetClassInheritanceTree (db, cls);
		}
		
		public IClass[] GetFileContents (string fileName)
		{
			return pdb.GetFileContents (db, fileName);
		}
		
		public IClass[] GetProjectContents ()
		{
			return pdb.GetProjectContents (db);
		}
		
		public ResolveResult Resolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			// added exception handling here to prevent silly parser exceptions from
			// being thrown and corrupting the textarea control
			try {
				IParser parser = parserService.GetParser (fileName);
				//Runtime.LoggingService.Info("Parse info : " + GetParseInformation(fileName).MostRecentCompilationUnit.Tag);
				if (parser != null) {
					return parser.Resolve (this, expression, caretLineNumber, caretColumn, fileName, fileContent);
				}
				return null;
			} catch {
				return null;
			}
		}
		
		public string MonodocResolver (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			try {
				IParser parser = parserService.GetParser (fileName);
				if (parser != null) {
					return parser.MonodocResolver (this, expression, caretLineNumber, caretColumn, fileName, fileContent);
				}
				return null;
			} catch {
				return null;
			}
		}
		
		public ArrayList IsAsResolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			try {
				IParser parser = parserService.GetParser (fileName);
				if (parser != null) {
					return parser.IsAsResolve (this, expression, caretLineNumber, caretColumn, fileName, fileContent);
				}
				return null;
			} catch {
				return null;
			}
		}
		
		public ArrayList CtrlSpace (int caretLine, int caretColumn, string fileName)
		{
			IParser parser = parserService.GetParser (fileName);
			if (parser != null) {
				return parser.CtrlSpace (this, caretLine, caretColumn, fileName);
			}
			return null;
		}
	}
	
	internal class ParserDatabase : IParserDatabase
	{
		DefaultParserService parserService;
		CodeCompletionDatabase coreDatabase;
		bool threadRunning;
		bool trackingFileChanges;
		IProgressMonitorFactory parseProgressMonitorFactory;
		
		const int MAX_PARSING_CACHE_SIZE = 10;
		const int MAX_SINGLEDB_CACHE_SIZE = 10;
		string CoreDB;

		class ParsingCacheEntry
		{
			   public ParseInformation ParseInformation;
			   public string FileName;
			   public DateTime AccessTime;
		}
		
		class SingleFileCacheEntry
		{
			   public SimpleCodeCompletionDatabase Database;
			   public DateTime AccessTime;
		}
		
		class ParsingJob
		{
			public object Data;
			public JobCallback ParseCallback;
		}

		class CompilationUnitTypeResolver: ITypeResolver
		{
			public IClass CallingClass;
			CodeCompletionDatabase db;
			ICompilationUnit unit;
			ParserDatabase parserDatabase;
			bool allResolved;
			
			public CompilationUnitTypeResolver (CodeCompletionDatabase db, ICompilationUnit unit, ParserDatabase parserDatabase)
			{
				this.db = db;
				this.unit = unit;
				this.parserDatabase = parserDatabase;
			}
			
			public string Resolve (string typeName)
			{
				IClass c = parserDatabase.SearchType (db, typeName, CallingClass, unit);
				if (c != null)
					return c.FullyQualifiedName;
				else {
					allResolved = false;
					return typeName;
				}
			}
			
			public bool AllResolved
			{
				get { return allResolved; }
				set { allResolved = value; }
			}
		}
		
		Hashtable lastUpdateSize = new Hashtable();
		Hashtable parsings = new Hashtable ();
		
		CombineEntryEventHandler combineEntryAddedHandler;
		CombineEntryEventHandler combineEntryRemovedHandler;

		Queue parseQueue = new Queue();
		AutoResetEvent parseEvent = new AutoResetEvent (false);
		
		string codeCompletionPath;

		Hashtable databases = new Hashtable();
		Hashtable singleDatabases = new Hashtable ();
		
		readonly static string[] assemblyList = {
			"Microsoft.VisualBasic",
			"mscorlib",
			"System.Data",
			"System.Design",
			"System.Drawing.Design",
			"System.Drawing",
			"System.Runtime.Remoting",
			"System.Security",
			"System.ServiceProcess",
			"System.Web.Services",
			"System.Web",
			"System",
			"System.Xml",
			"glib-sharp",
			"atk-sharp",
			"pango-sharp",
			"gdk-sharp",
			"gtk-sharp",
			"gnome-sharp",
			"gconf-sharp",
			"gtkhtml-sharp",
			//"System.Windows.Forms",
			//"Microsoft.JScript",
		};
		
		StringNameTable nameTable;
		
		string[] sharedNameTable = new string[] {
			"System.String", "System.Boolean", "System.Int32", "System.Attribute",
			"System.Delegate", "System.Enum", "System.Exception", "System.MarshalByRefObject",
			"System.Object", "SerializableAttribtue", "System.Type", "System.ValueType",
			"System.ICloneable", "System.IDisposable", "System.IConvertible", "System.Byte",
			"System.Char", "System.DateTime", "System.Decimal", "System.Double", "System.Int16",
			"System.Int64", "System.IntPtr", "System.SByte", "System.Single", "System.TimeSpan",
			"System.UInt16", "System.UInt32", "System.UInt64", "System.Void"
		};
		
		public ParserDatabase (DefaultParserService parserService)
		{
			this.parserService = parserService;
			combineEntryAddedHandler = new CombineEntryEventHandler (OnCombineEntryAdded);
			combineEntryRemovedHandler = new CombineEntryEventHandler (OnCombineEntryRemoved);
			nameTable = new StringNameTable (sharedNameTable);
		}
		
		public IParserContext GetProjectParserContext (Project project)
		{
			return new ParserContext (parserService, this, GetProjectDatabase (project));
		}
		
		public IParserContext GetFileParserContext (string file)
		{
			return new ParserContext (parserService, this, GetSingleFileDatabase (file));
		}
		
		public IProgressMonitorFactory ParseProgressMonitorFactory {
			get { return parseProgressMonitorFactory; }
			set { parseProgressMonitorFactory = value; }
		}
		
		
		public bool TrackFileChanges {
			get {
				return trackingFileChanges;
			}
			set {
				if (value == trackingFileChanges)
					return;
				
				lock (this) {
					trackingFileChanges = value;
	
					if (value)
						StartParserThread ();
				}
			}
		}
		
		public IExpressionFinder GetExpressionFinder(string fileName)
		{
			return parserService.GetExpressionFinder (fileName);
		}
		
		private bool ContinueWithProcess(IProgressMonitor progressMonitor)
		{
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();

			if (progressMonitor.IsCancelRequested)
				return false;
			else
				return true;
		}
	
		public void GenerateCodeCompletionDatabase(string createPath, IProgressMonitor progressMonitor)
		{
			if (progressMonitor != null)
				progressMonitor.BeginTask(GettextCatalog.GetString ("Generating database"), assemblyList.Length);

			for (int i = 0; i < assemblyList.Length; ++i)
			{
				try {
					AssemblyCodeCompletionDatabase db = new AssemblyCodeCompletionDatabase (codeCompletionPath, assemblyList[i], this);
					db.ParseAll ();
					db.Write ();
					
					if (progressMonitor != null)
						progressMonitor.Step (1);
						
					if (!ContinueWithProcess (progressMonitor))
						return;
				}
				catch (Exception ex) {
					Runtime.LoggingService.Info (ex);
				}
			}

			if (progressMonitor != null) {
				progressMonitor.Dispose ();
			}
		}
		
		void SetDefaultCompletionFileLocation()
		{
			PropertyService propertyService = Runtime.Properties;
			string path = propertyService.GetProperty("SharpDevelop.CodeCompletion.DataDirectory", String.Empty).ToString();
			if (path == String.Empty) {
				path = Path.Combine (Runtime.FileUtilityService.GetDirectoryNameWithSeparator(propertyService.ConfigDirectory), "CodeCompletionData");
				propertyService.SetProperty ("SharpDevelop.CodeCompletion.DataDirectory", path);
				propertyService.SaveProperties ();
			}
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);

			codeCompletionPath = Runtime.FileUtilityService.GetDirectoryNameWithSeparator(path);
		}

		public void Initialize ()
		{
			SetDefaultCompletionFileLocation();
			DeleteObsoleteDatabases ();

			string coreName = typeof(object).Assembly.GetName().ToString ();
			CoreDB = "Assembly:" + coreName;
			coreDatabase = new AssemblyCodeCompletionDatabase (codeCompletionPath, coreName, this);
			databases [CoreDB] = coreDatabase;
		}
		
		internal IProgressMonitor GetParseProgressMonitor ()
		{
			if (parseProgressMonitorFactory != null)
				return parseProgressMonitorFactory.CreateProgressMonitor ();
			else
				return new NullProgressMonitor ();
		}
			
		internal CodeCompletionDatabase GetDatabase (string uri)
		{
			return GetDatabase (null, uri);
		}
		
		internal ProjectCodeCompletionDatabase GetProjectDatabase (Project project)
		{
			if (project == null) return null;
			return (ProjectCodeCompletionDatabase) GetDatabase (null, "Project:" + project.Name);
		}
		
		internal CodeCompletionDatabase GetDatabase (string baseDir, string uri)
		{
			lock (databases)
			{
				if (baseDir == null) baseDir = codeCompletionPath;
				CodeCompletionDatabase db = (CodeCompletionDatabase) databases [uri];
				if (db == null) 
				{
					// Create/load the database

					if (uri.StartsWith ("Assembly:"))
					{
						string file = uri.Substring (9);
						
						// We may be trying to load an assembly db using a partial name.
						// In this case we get the full name to avoid database conflicts
						file = AssemblyCodeCompletionDatabase.GetFullAssemblyName (file);
						string realUri = "Assembly:" + file;
						db = (CodeCompletionDatabase) databases [realUri];
						if (db != null) {
							databases [uri] = db;
							return db;
						}
						
						AssemblyCodeCompletionDatabase adb;
						db = adb = new AssemblyCodeCompletionDatabase (baseDir, file, this);
						databases [realUri] = adb;
						if (uri != realUri)
							databases [uri] = adb;
						
						// Load referenced databases
						foreach (ReferenceEntry re in db.References)
							GetDatabase (baseDir, re.Uri);
					}
				}
				return db;
			}
		}
		
		internal SimpleCodeCompletionDatabase GetSingleFileDatabase (string file)
		{
			lock (singleDatabases)
			{
				SingleFileCacheEntry entry = singleDatabases [file] as SingleFileCacheEntry;
				if (entry != null) {
					entry.AccessTime = DateTime.Now;
					return entry.Database;
				}
				else 
				{
					if (singleDatabases.Count >= MAX_SINGLEDB_CACHE_SIZE)
					{
						DateTime tim = DateTime.MaxValue;
						string toDelete = null;
						foreach (DictionaryEntry pce in singleDatabases)
						{
							DateTime ptim = ((SingleFileCacheEntry)pce.Value).AccessTime;
							if (ptim < tim) {
								tim = ptim;
								toDelete = pce.Key.ToString();
							}
						}
						singleDatabases.Remove (toDelete);
					}
				
					SimpleCodeCompletionDatabase db = new SimpleCodeCompletionDatabase (file, this);
					entry = new SingleFileCacheEntry ();
					entry.Database = db;
					entry.AccessTime = DateTime.Now;
					singleDatabases [file] = entry;
					return db;
				}
			}
		}
		
		public void Load (CombineEntry entry)
		{
			if (entry is Project)
				LoadProjectDatabase ((Project)entry);
			else if (entry is Combine)
				LoadCombineDatabases ((Combine)entry);
		}
		
		public void Unload (CombineEntry entry)
		{
			if (entry is Project)
				UnloadProjectDatabase ((Project)entry);
			else if (entry is Combine)
				UnloadCombineDatabases ((Combine)entry);
		}
		
		void LoadProjectDatabase (Project project)
		{
			lock (databases)
			{
				string uri = "Project:" + project.Name;
				if (databases.Contains (uri)) return;
				
				ProjectCodeCompletionDatabase db = new ProjectCodeCompletionDatabase (project, this);
				databases [uri] = db;
				
				foreach (ReferenceEntry re in db.References)
					GetDatabase (re.Uri);

				project.NameChanged += new CombineEntryRenamedEventHandler (OnProjectRenamed);
				project.ReferenceAddedToProject += new ProjectReferenceEventHandler (OnProjectReferencesChanged);
				project.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (OnProjectReferencesChanged);
			}
		}
		
		void UnloadDatabase (string uri)
		{
			if (uri == CoreDB) return;

			CodeCompletionDatabase db;
			lock (databases)
			{
				db = databases [uri] as CodeCompletionDatabase;
				if (db != null)
					databases.Remove (uri);
			}
			if (db != null) {
				db.Write ();
				db.Dispose ();
			}
		}
		
		void UnloadProjectDatabase (Project project)
		{
			string uri = "Project:" + project.Name;
			UnloadDatabase (uri);
			project.NameChanged -= new CombineEntryRenamedEventHandler (OnProjectRenamed);
			project.ReferenceAddedToProject -= new ProjectReferenceEventHandler (OnProjectReferencesChanged);
			project.ReferenceRemovedFromProject -= new ProjectReferenceEventHandler (OnProjectReferencesChanged);
		}
		
		void CleanUnusedDatabases ()
		{
			lock (databases)
			{
				Hashtable references = new Hashtable ();
				foreach (CodeCompletionDatabase db in databases.Values)
				{
					if (db is ProjectCodeCompletionDatabase) {
						foreach (ReferenceEntry re in ((ProjectCodeCompletionDatabase)db).References)
							references [re.Uri] = null;
					}
				}
				
				ArrayList todel = new ArrayList ();
				foreach (DictionaryEntry en in databases)
				{
					if (!(en.Value is ProjectCodeCompletionDatabase) && !references.Contains (en.Key))
						todel.Add (en.Key);
				}
				
				foreach (string uri in todel)
					UnloadDatabase (uri);
			}
		}
		
		void LoadCombineDatabases (Combine combine)
		{
			CombineEntryCollection projects = combine.GetAllProjects();
			foreach (Project entry in projects) {
				LoadProjectDatabase (entry);
			}
			combine.EntryAdded += combineEntryAddedHandler;
			combine.EntryRemoved += combineEntryRemovedHandler;
		}
		
		void UnloadCombineDatabases (Combine combine)
		{
			CombineEntryCollection projects = combine.GetAllProjects();
			foreach (Project entry in projects) {
				UnloadProjectDatabase (entry);
			}
			CleanUnusedDatabases ();
			combine.EntryAdded -= combineEntryAddedHandler;
			combine.EntryRemoved -= combineEntryRemovedHandler;
		}
		
		void OnProjectRenamed (object sender, CombineEntryRenamedEventArgs args)
		{
			lock (databases)
			{
				ProjectCodeCompletionDatabase db = GetProjectDatabase ((Project) args.CombineEntry);
				if (db == null) return;
				
				db.Rename (args.NewName);
				databases.Remove ("Project:" + args.OldName);
				databases ["Project:" + args.NewName] = db;
				RefreshProjectDatabases ();
				CleanUnusedDatabases ();
			}
		}
		
		void OnCombineEntryAdded (object sender, CombineEntryEventArgs args)
		{
			if (args.CombineEntry is Project)
				LoadProjectDatabase ((Project)args.CombineEntry);
			else if (args.CombineEntry is Combine)
				LoadCombineDatabases ((Combine)args.CombineEntry);
		}
		
		void OnCombineEntryRemoved (object sender, CombineEntryEventArgs args)
		{
			if (args.CombineEntry is Project)
				UnloadProjectDatabase ((Project) args.CombineEntry);
			else if (args.CombineEntry is Combine)
				UnloadCombineDatabases ((Combine) args.CombineEntry);
			CleanUnusedDatabases ();
		}
		
		void OnProjectReferencesChanged (object sender, ProjectReferenceEventArgs args)
		{
			ProjectCodeCompletionDatabase db = GetProjectDatabase (args.Project);
			if (db != null) {
				db.UpdateFromProject ();
				foreach (ReferenceEntry re in db.References)
				{
					// Make sure the db is loaded
					GetDatabase (re.Uri);
				}
			}
		}
		
		void RefreshProjectDatabases ()
		{
			lock (databases)
			{
				foreach (CodeCompletionDatabase db in databases.Values)
				{
					ProjectCodeCompletionDatabase pdb = db as ProjectCodeCompletionDatabase;
					if (pdb != null)
						pdb.UpdateFromProject ();
				}
			}
		}
		
		internal void QueueParseJob (JobCallback callback, object data)
		{
			ParsingJob job = new ParsingJob ();
			job.ParseCallback = callback;
			job.Data = data;
			lock (parseQueue)
			{
				parseQueue.Enqueue (job);
				parseEvent.Set ();
			}
		}
		
		void DeleteObsoleteDatabases ()
		{
			string[] files = Directory.GetFiles (codeCompletionPath, "*.pidb");
			foreach (string file in files)
			{
				string name = Path.GetFileNameWithoutExtension (file);
				string baseDir = Path.GetDirectoryName (file);
				AssemblyCodeCompletionDatabase.CleanDatabase (baseDir, name);
			}
		}
		
		void StartParserThread()
		{
			lock (this) {
				if (!threadRunning) {
					threadRunning = true;
					Thread t = new Thread(new ThreadStart(ParserUpdateThread));
					t.IsBackground  = true;
					t.Start();
				}
			}
		}
		
		
		void ParserUpdateThread()
		{
			while (trackingFileChanges)
			{
				if (!parseEvent.WaitOne (5000, true))
					CheckModifiedFiles ();
				else if (trackingFileChanges)
					ConsumeParsingQueue ();
			}
			lock (this) {
				threadRunning = false;
				if (trackingFileChanges)
					StartParserThread ();
			}
		}
		
		void CheckModifiedFiles ()
		{
			// Check databases following a bottom-up strategy in the dependency
			// tree. This will help resolving parsed classes.
			
			ArrayList list = new ArrayList ();
			lock (databases) 
			{
				// There may be several uris for the same db
				foreach (object ob in databases.Values)
					if (!list.Contains (ob))
						list.Add (ob);
			}
			
			ArrayList done = new ArrayList ();
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
						CodeCompletionDatabase refdb = GetDatabase (re.Uri);
						if (!done.Contains (refdb)) {
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
		
		void ConsumeParsingQueue ()
		{
			int pending = 0;
			IProgressMonitor monitor = null;
			
			try {
				do {
					if (pending > 5 && monitor == null) {
						monitor = GetParseProgressMonitor ();
						monitor.BeginTask ("Generating database", 0);
					}
					
					ParsingJob job = null;
					lock (parseQueue)
					{
						if (parseQueue.Count > 0)
							job = (ParsingJob) parseQueue.Dequeue ();
					}
					
					if (job != null) {
						try {
							job.ParseCallback (job.Data, monitor);
						} catch (Exception ex) {
							if (monitor == null)
								monitor = GetParseProgressMonitor ();
							monitor.ReportError (null, ex);
						}
					}
					
					lock (parseQueue)
						pending = parseQueue.Count;
					
				}
				while (pending > 0);
			} finally {
				if (monitor != null) monitor.Dispose ();
			}
		}
		
		public void UpdateFile (Project project, string fileName, string fileContent)
		{
			try {
				if (parserService.GetParser (fileName) == null)
					return;
				
				IParseInformation parseInformation = null;
				int contentHash = fileContent.GetHashCode ();
			
				if (lastUpdateSize[fileName] == null || (int)lastUpdateSize[fileName] != contentHash) {
					parseInformation = DoParseFile (fileName, fileContent);
					if (parseInformation == null) return;
					
					if (project != null) {
						ProjectCodeCompletionDatabase db = GetProjectDatabase (project);
						ClassUpdateInformation res = db.UpdateFromParseInfo (parseInformation, fileName);
						if (res != null) NotifyParseInfoChange (fileName, res, project);
					}
					else {
						SimpleCodeCompletionDatabase db = GetSingleFileDatabase (fileName);
						db.UpdateFromParseInfo (parseInformation);
					}

					lastUpdateSize[fileName] = contentHash;
				}
			} catch (Exception e) {
				try {
					Runtime.LoggingService.Info(e.ToString());
				} catch {}
			}
		}
		
#region Default Parser Layer dependent functions

		public IClass GetClass (CodeCompletionDatabase db, string typeName)
		{
			return GetClass (db, typeName, false, true);
		}
		
		public IClass GetClass (CodeCompletionDatabase db, string typeName, bool deepSearchReferences, bool caseSensitive)
		{
			if (deepSearchReferences)
				return DeepGetClass (db, typeName, caseSensitive);
			else
				return GetClass (db, typeName, caseSensitive);
		}
		
		public IClass GetClass (CodeCompletionDatabase db, string typeName, bool caseSensitive)
		{
			if (db != null) {
				IClass c = db.GetClass (typeName, caseSensitive);
				if (c != null) return c;
				foreach (ReferenceEntry re in db.References)
				{
					CodeCompletionDatabase cdb = GetDatabase (re.Uri);
					if (cdb == null) continue;
					c = cdb.GetClass (typeName, caseSensitive);
					if (c != null) return c;
				}
			}
			
			db = GetDatabase (CoreDB);
			return db.GetClass (typeName, caseSensitive);
		}
		
		public IClass DeepGetClass (CodeCompletionDatabase db, string typeName, bool caseSensitive)
		{
			ArrayList visited = new ArrayList ();
			IClass c = DeepGetClassRec (visited, db, typeName, caseSensitive);
			if (c != null) return c;

			db = GetDatabase (CoreDB);
			return db.GetClass (typeName, caseSensitive);
		}
		
		internal IClass DeepGetClassRec (ArrayList visitedDbs, CodeCompletionDatabase db, string typeName, bool caseSensitive)
		{
			if (db == null) return null;
			if (visitedDbs.Contains (db)) return null;
			
			visitedDbs.Add (db);
			
			IClass c = db.GetClass (typeName, caseSensitive);
			if (c != null) return c;
			
			foreach (ReferenceEntry re in db.References)
			{
				CodeCompletionDatabase cdb = GetDatabase (re.Uri);
				if (cdb == null) continue;
				c = DeepGetClassRec (visitedDbs, cdb, typeName, caseSensitive);
				if (c != null) return c;
			}
			return null;
		}
		
		public IClass[] GetProjectContents (CodeCompletionDatabase db)
		{
			if (db != null)
				return db.GetClassList ();
			else
				return new IClass[0];
		}
		
		public string[] GetClassList (CodeCompletionDatabase db, string subNameSpace, bool includeReferences)
		{
			return GetClassList (db, subNameSpace, includeReferences, true);
		}
		
		public string[] GetClassList (CodeCompletionDatabase db, string subNameSpace, bool includeReferences, bool caseSensitive)
		{
			ArrayList contents = new ArrayList ();
			
			if (db != null) {
				db.GetClassList (contents, subNameSpace, caseSensitive);
				if (includeReferences) {
					foreach (ReferenceEntry re in db.References) {
						CodeCompletionDatabase cdb = GetDatabase (re.Uri);
						if (cdb == null) continue;
						cdb.GetClassList (contents, subNameSpace, caseSensitive);
					}
				}
			}
			
			if (includeReferences) {
				db = GetDatabase (CoreDB);
				db.GetClassList (contents, subNameSpace, caseSensitive);
			}
			
			return (string[]) contents.ToArray (typeof(string));
		}

		public string[] GetNamespaceList (CodeCompletionDatabase db, string subNameSpace)
		{
			return GetNamespaceList (db, subNameSpace, true, true);
		}
		
		public string[] GetNamespaceList (CodeCompletionDatabase db, string subNameSpace, bool includeReferences, bool caseSensitive)
		{
			ArrayList contents = new ArrayList ();
			
			if (db != null) {
				db.GetNamespaceList (contents, subNameSpace, caseSensitive);
				if (includeReferences) {
					foreach (ReferenceEntry re in db.References) {
						CodeCompletionDatabase cdb = GetDatabase (re.Uri);
						if (cdb == null) continue;
						cdb.GetNamespaceList (contents, subNameSpace, caseSensitive);
					}
				}
			}
			
			if (includeReferences) {
				db = GetDatabase (CoreDB);
				db.GetNamespaceList (contents, subNameSpace, caseSensitive);
			}
			
			return (string[]) contents.ToArray (typeof(string));
		}
		
		public ArrayList GetNamespaceContents (CodeCompletionDatabase db, string namspace, bool includeReferences)
		{
			return GetNamespaceContents (db, namspace, includeReferences, true);
		}
		
		public ArrayList GetNamespaceContents (CodeCompletionDatabase db, string namspace, bool includeReferences, bool caseSensitive)
		{
			ArrayList contents = new ArrayList ();
			
			if (db != null) {
				db.GetNamespaceContents (contents, namspace, caseSensitive);
				if (includeReferences) {
					foreach (ReferenceEntry re in db.References)
					{
						CodeCompletionDatabase cdb = GetDatabase (re.Uri);
						if (cdb == null) continue;
						cdb.GetNamespaceContents (contents, namspace, caseSensitive);
					}
				}
			}
			
			if (includeReferences) {
				db = GetDatabase (CoreDB);
				db.GetNamespaceContents (contents, namspace, caseSensitive);
			}
			
			return contents;
		}
		
		public bool NamespaceExists (CodeCompletionDatabase db, string name)
		{
			return NamespaceExists (db, name, true);
		}
		
		public bool NamespaceExists (CodeCompletionDatabase db, string name, bool caseSensitive)
		{
			if (db != null) {
				if (db.NamespaceExists (name, caseSensitive)) return true;
				foreach (ReferenceEntry re in db.References)
				{
					CodeCompletionDatabase cdb = GetDatabase (re.Uri);
					if (cdb == null) continue;
					if (cdb.NamespaceExists (name, caseSensitive)) return true;
				}
			}
			
			db = GetDatabase (CoreDB);
			return db.NamespaceExists (name, caseSensitive);
			}

		public string SearchNamespace (CodeCompletionDatabase db, IUsing usin, string partitialNamespaceName)
		{
			return SearchNamespace (db, usin, partitialNamespaceName, true);
		}
		
		public string SearchNamespace (CodeCompletionDatabase db, IUsing usin, string partitialNamespaceName, bool caseSensitive)
		{
//			Runtime.LoggingService.Info("SearchNamespace : >{0}<", partitialNamespaceName);
			if (NamespaceExists (db, partitialNamespaceName, caseSensitive)) {
				return partitialNamespaceName;
			}
			
			// search for partitial namespaces
			string declaringNamespace = (string)usin.Aliases[""];
			if (declaringNamespace != null) {
				while (declaringNamespace.Length > 0) {
					if ((caseSensitive ? declaringNamespace.EndsWith(partitialNamespaceName) : declaringNamespace.ToLower().EndsWith(partitialNamespaceName.ToLower()) ) && NamespaceExists (db, declaringNamespace, caseSensitive)) {
						return declaringNamespace;
					}
					int index = declaringNamespace.IndexOf('.');
					if (index > 0) {
						declaringNamespace = declaringNamespace.Substring(0, index);
					} else {
						break;
					}
				}
			}
			
			// Remember:
			//     Each namespace has an own using object
			//     The namespace name is an alias which has the key ""
			foreach (DictionaryEntry entry in usin.Aliases) {
				string aliasString = entry.Key.ToString();
				if (caseSensitive ? partitialNamespaceName.StartsWith(aliasString) : partitialNamespaceName.ToLower().StartsWith(aliasString.ToLower())) {
					if (aliasString.Length >= 0) {
						string nsName = nsName = String.Concat(entry.Value.ToString(), partitialNamespaceName.Remove(0, aliasString.Length));
						if (NamespaceExists (db, nsName, caseSensitive)) {
							return nsName;
						}
					}
				}
			}
			return null;
		}

		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchType (CodeCompletionDatabase db, string name, IClass callingClass, ICompilationUnit unit)
		{
			if (name == null || name == String.Empty)
				return null;
				
			IClass c;
			c = GetClass (db, name);
			if (c != null)
				return c;

			if (unit != null) {
				foreach (IUsing u in unit.Usings) {
					if (u != null) {
						c = SearchType (db, u, name);
						if (c != null) {
							return c;
						}
					}
				}
			}
			if (callingClass == null) {
				return null;
			}
			string fullname = callingClass.FullyQualifiedName;
			string[] namespaces = fullname.Split(new char[] {'.'});
			string curnamespace = "";
			int i = 0;
			
			do {
				curnamespace += namespaces[i] + '.';
				c = GetClass (db, curnamespace + name);
				if (c != null) {
					return c;
				}
				i++;
			}
			while (i < namespaces.Length);
			
			return null;
		}
		
		public IClass SearchType (CodeCompletionDatabase db, IUsing iusing, string partitialTypeName)
		{
			return SearchType (db, iusing, partitialTypeName, true);
		}
		
		public IClass SearchType (CodeCompletionDatabase db, IUsing iusing, string partitialTypeName, bool caseSensitive)
		{
//			Runtime.LoggingService.Info("Search type : >{0}<", partitialTypeName);
			IClass c = GetClass (db, partitialTypeName, caseSensitive);
			if (c != null) {
				return c;
			}
			
			foreach (string str in iusing.Usings) {
				string possibleType = String.Concat(str, ".", partitialTypeName);
//				Runtime.LoggingService.Info("looking for " + possibleType);
				c = GetClass (db, possibleType, caseSensitive);
				if (c != null) {
//					Runtime.LoggingService.Info("Found!");
					return c;
				}
			}
			
			// search class in partitial namespaces
			string declaringNamespace = (string)iusing.Aliases[""];
			if (declaringNamespace != null) {
				while (declaringNamespace.Length > 0) {
					string className = String.Concat(declaringNamespace, ".", partitialTypeName);
//					Runtime.LoggingService.Info("looking for " + className);
					c = GetClass (db, className, caseSensitive);
					if (c != null) {
//						Runtime.LoggingService.Info("Found!");
						return c;
					}
					int index = declaringNamespace.IndexOf('.');
					if (index > 0) {
						declaringNamespace = declaringNamespace.Substring(0, index);
					} else {
						break;
					}
				}
			}
			
			foreach (DictionaryEntry entry in iusing.Aliases) {
				string aliasString = entry.Key.ToString();
				if (caseSensitive ? partitialTypeName.StartsWith(aliasString) : partitialTypeName.ToLower().StartsWith(aliasString.ToLower())) {
					string className = null;
					if (aliasString.Length > 0) {
						className = String.Concat(entry.Value.ToString(), partitialTypeName.Remove(0, aliasString.Length));
//						Runtime.LoggingService.Info("looking for " + className);
						c = GetClass (db, className, caseSensitive);
						if (c != null) {
//							Runtime.LoggingService.Info("Found!");
							return c;
						}
					}
				}
			}
			
			return null;
		}
		
		public bool ResolveTypes (Project project, ICompilationUnit unit, ClassCollection types, out ClassCollection result)
		{
			CompilationUnitTypeResolver tr = new CompilationUnitTypeResolver (GetProjectDatabase (project), unit, this);
			
			bool allResolved = true;
			result = new ClassCollection ();
			foreach (IClass c in types) {
				tr.CallingClass = c;
				tr.AllResolved = true;
				result.Add (PersistentClass.Resolve (c, tr));
				allResolved = allResolved && tr.AllResolved;
			}
				
			return allResolved;
		}
		
		public IEnumerable GetClassInheritanceTree (CodeCompletionDatabase db, IClass cls)
		{
			return new ClassInheritanceEnumerator (this, db, cls);
		}
		
		public IClass[] GetFileContents (CodeCompletionDatabase db, string fileName)
		{
			return db.GetFileContents (fileName);
		}
		
#endregion
		
		public IParseInformation ParseFile(string fileName)
		{
			return ParseFile(fileName, null);
		}
		
		public IParseInformation ParseFile (string fileName, string fileContent)
		{
			return DoParseFile (fileName, fileContent);
		}
		
		public IParseInformation DoParseFile (string fileName, string fileContent)
		{
			IParser parser = parserService.GetParser (fileName);
			
			if (parser == null) {
				return null;
			}
			
			parser.LexerTags = new string[] { "HACK", "TODO", "UNDONE", "FIXME" };
			
			ICompilationUnitBase parserOutput = null;
			
			if (fileContent == null) {
				lock (databases) {
					foreach (object ob in databases.Values) {
						ProjectCodeCompletionDatabase db = ob as ProjectCodeCompletionDatabase;
						if (db != null) {
							if (db.Project.IsFileInProject (fileName))
								fileContent = db.Project.GetParseableFileContent(fileName);
						}
					}
				}
			}
			
			if (fileContent != null) {
				parserOutput = parser.Parse(fileName, fileContent);
			} else {
				parserOutput = parser.Parse(fileName);
			}
			
			ParseInformation parseInformation = GetCachedParseInformation (fileName);
			bool newInfo = false;
			
			if (parseInformation == null) {
				parseInformation = new ParseInformation();
				newInfo = true;
			}
			
			if (parserOutput.ErrorsDuringCompile) {
				parseInformation.DirtyCompilationUnit = parserOutput;
			} else {
				parseInformation.ValidCompilationUnit = parserOutput;
				parseInformation.DirtyCompilationUnit = null;
			}
			
			if (newInfo) {
				AddToCache (parseInformation, fileName);
			}
			
			OnParseInformationChanged (new ParseInformationEventArgs (fileName, parseInformation));
			return parseInformation;
		}
		
		ParseInformation GetCachedParseInformation (string fileName)
		{
			lock (parsings) 
			{
				ParsingCacheEntry en = parsings [fileName] as ParsingCacheEntry;
				if (en != null) {
					en.AccessTime = DateTime.Now;
					return en.ParseInformation;
				}
				else
					return null;
			}
		}
		
		void AddToCache (ParseInformation info, string fileName)
		{
			lock (parsings) 
			{
				if (parsings.Count >= MAX_PARSING_CACHE_SIZE)
				{
					DateTime tim = DateTime.MaxValue;
					string toDelete = null;
					foreach (DictionaryEntry pce in parsings)
					{
						DateTime ptim = ((ParsingCacheEntry)pce.Value).AccessTime;
						if (ptim < tim) {
							tim = ptim;
							toDelete = pce.Key.ToString();
						}
					}
					parsings.Remove (toDelete);
				}
				
				ParsingCacheEntry en = new ParsingCacheEntry();
				en.ParseInformation = info;
				en.AccessTime = DateTime.Now;
				parsings [fileName] = en;
			}
		}

		public IParseInformation GetParseInformation(string fileName)
		{
			if (fileName == null || fileName.Length == 0) {
				return null;
			}
			
			IParseInformation info = GetCachedParseInformation (fileName);
			if (info != null) return info;
			else return ParseFile(fileName);
		}
		
		////////////////////////////////////
		
		internal INameEncoder DefaultNameEncoder {
			get { return nameTable; }
		}

		internal INameDecoder DefaultNameDecoder {
			get { return nameTable; }
		}
		
		public void NotifyParseInfoChange (string file, ClassUpdateInformation res, Project project)
		{
			ClassInformationEventArgs args = new ClassInformationEventArgs (file, res, project);
			OnClassInformationChanged (args);
		}

		protected virtual void OnParseInformationChanged(ParseInformationEventArgs e)
		{
			if (ParseInformationChanged != null) {
				ParseInformationChanged(this, e);
			}
		}
		
		protected virtual void OnClassInformationChanged(ClassInformationEventArgs e)
		{
			if (ClassInformationChanged != null) {
				ClassInformationChanged(this, e);
			}
		}
		
		public event ParseInformationEventHandler ParseInformationChanged;
		public event ClassInformationEventHandler ClassInformationChanged;
	}
	
	[Serializable]
	internal class DummyCompilationUnit : AbstractCompilationUnit
	{
		CommentCollection miscComments = new CommentCollection();
		CommentCollection dokuComments = new CommentCollection();
		TagCollection     tagComments  = new TagCollection();
		
		public override CommentCollection MiscComments {
			get {
				return miscComments;
			}
		}
		
		public override CommentCollection DokuComments {
			get {
				return dokuComments;
			}
		}
		
		public override TagCollection TagComments {
			get {
				return tagComments;
			}
		}
	}
	
	internal class ClassInheritanceEnumerator : IEnumerator, IEnumerable
	{
		ParserDatabase parserDatabase;
		IClass topLevelClass;
		IClass currentClass  = null;
		Queue  baseTypeQueue = new Queue();
		CodeCompletionDatabase db;

		internal ClassInheritanceEnumerator (ParserDatabase parserDatabase, CodeCompletionDatabase db, IClass topLevelClass)
		{
			this.parserDatabase = parserDatabase;
			this.db = db;
			this.topLevelClass = topLevelClass;
			baseTypeQueue.Enqueue(topLevelClass.FullyQualifiedName);
			PutBaseClassesOnStack(topLevelClass);
			baseTypeQueue.Enqueue("System.Object");
		}
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		void PutBaseClassesOnStack(IClass c)
		{
			foreach (string baseTypeName in c.BaseTypes)
				baseTypeQueue.Enqueue(baseTypeName);
		}

		public IClass Current {
			get {
				return currentClass;
			}
		}

		object IEnumerator.Current {
			get {
				return currentClass;
			}
		}

		public bool MoveNext()
		{
			if (baseTypeQueue.Count == 0) {
				return false;
			}
			string baseTypeName = baseTypeQueue.Dequeue().ToString();

			IClass baseType = parserDatabase.DeepGetClass (db, baseTypeName, true);
			if (baseType == null) {
				ICompilationUnit unit = currentClass == null ? null : currentClass.CompilationUnit;
				if (unit != null) {
					foreach (IUsing u in unit.Usings) {
						baseType = parserDatabase.SearchType (db, u, baseTypeName);
						if (baseType != null) {
							break;
						}
					}
				}
			}

			if (baseType != null) {
				currentClass = baseType;
				PutBaseClassesOnStack(currentClass);
			}
			
			return baseType != null;
		}

		public void Reset()
		{
			baseTypeQueue.Clear();
			baseTypeQueue.Enqueue(topLevelClass.FullyQualifiedName);
			PutBaseClassesOnStack(topLevelClass);
			baseTypeQueue.Enqueue("System.Object");
		}
	}	
	
	public class ClassUpdateInformation
	{
		ClassCollection added = new ClassCollection ();
		ClassCollection removed = new ClassCollection ();
		ClassCollection modified = new ClassCollection ();
		
		public ClassCollection Added
		{
			get { return added; }
		}
		
		public ClassCollection Removed
		{
			get { return removed; }
		}
		
		public ClassCollection Modified
		{
			get { return modified; }
		}
	}
	
	public interface ITypeResolver
	{
		string Resolve (string typeName);
	}
	
	public delegate void JobCallback (object data, IProgressMonitor monitor);
}
