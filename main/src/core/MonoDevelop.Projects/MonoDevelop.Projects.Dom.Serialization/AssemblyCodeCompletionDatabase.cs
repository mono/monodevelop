//
// AssemblyCodeCompletionDatabase.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using Mono.Cecil;

using MonoDevelop.Projects;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom.Serialization
{	
	internal class AssemblyCodeCompletionDatabase: SerializationCodeCompletionDatabase
	{
		bool useExternalProcess = true;
		string baseDir;
		bool loadError;
		bool isPackageAssembly;
		bool parsing;
		string assemblyFile;
		
		// This is the package version of the assembly. It is serialized.
		string packageVersion;
		
		public AssemblyCodeCompletionDatabase (string assemblyFile, ParserDatabase pdb): this (assemblyFile, pdb, false)
		{
		}
		
		public AssemblyCodeCompletionDatabase (string assemblyFile, ParserDatabase pdb, bool isTempDatabase): base (pdb, false)
		{
			this.assemblyFile = assemblyFile;
			
			if (!File.Exists (assemblyFile)) {
				loadError = true;
				return;
			}
			
			string name = assemblyFile.Replace(',','_').Replace(" ","").Replace(Path.DirectorySeparatorChar,'_');
			
			SystemPackage package = Runtime.SystemAssemblyService.GetPackageFromPath (assemblyFile);
			if (package != null) {
				isPackageAssembly = true;
				packageVersion = package.Name + " " + package.Version;
			}
			else
				isPackageAssembly = false;

			this.baseDir = ProjectDomService.CodeCompletionPath;

			if (isTempDatabase)
				SetFile (Path.GetTempFileName ());
			else {
				SetLocation (baseDir, name);
				Read ();
			}
			
			ArrayList oldFiles = new ArrayList ();
			foreach (FileEntry e in GetAllFiles ()) {
				if (e.FileName != assemblyFile)
					oldFiles.Add (e);
			}
			
			foreach (FileEntry e in oldFiles)
				RemoveFile (e.FileName);
			
			if (files [assemblyFile] == null) {
				AddFile (assemblyFile);
				headers ["CheckFile"] = assemblyFile;
			}
			
			FileEntry fe = GetFile (assemblyFile);
			if (IsFileModified (fe)) {
				// Update references to other assemblies
				Hashtable rs = new Hashtable ();
				foreach (string uri in ReadAssemblyReferences ()) {
					rs[uri] = null;
					if (!HasReference (uri))
						AddReference (uri);
				}
				
				ArrayList keys = new ArrayList ();
				keys.AddRange (references);
				foreach (ReferenceEntry re in keys)
				{
					if (!rs.Contains (re.Uri))
						RemoveReference (re.Uri);
				}
			}
		}
		
		public override void Write ()
		{
			base.Write ();
			Read ();
		}
		
		protected override bool IsFileModified (FileEntry file)
		{
			if (parsing)
				return false;
			
			if (!isPackageAssembly)
				return base.IsFileModified (file);

			// Don't check timestamps for packaged assemblies.
			// Just check if the package has changed.
			SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromPath (assemblyFile);
			bool versionMismatch = pkg != null && packageVersion != pkg.Name + " " + pkg.Version;
			return (!file.DisableParse && (versionMismatch || file.LastParseTime == DateTime.MinValue || file.ParseErrorRetries > 0));
		}
		
		public static string GetFullAssemblyName (string s)
		{
			return Runtime.SystemAssemblyService.GetAssemblyFullName (s);
		}
		
		internal bool LoadError {
			get { return loadError; }
		}
		
		protected override void SerializeData (Queue dataQueue)
		{
			base.SerializeData (dataQueue);
			dataQueue.Enqueue (packageVersion);
		}
		
		protected override void DeserializeData (Queue dataQueue)
		{
			base.DeserializeData (dataQueue);
			if (isPackageAssembly) {
				if (dataQueue.Count > 0) {
					string ver = (string) dataQueue.Dequeue ();
					if (ver != null) {
						packageVersion = ver;
						return;
					}
				}
				
				// Package version not set, assume current version 
			}
		}
		
		protected override void ParseFile (string fileName, IProgressMonitor parentMonitor)
		{
			if (!File.Exists (fileName))
				return;
			IProgressMonitor monitor = parentMonitor;
			
			// Update the package version
			SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromPath (assemblyFile);
			if (pkg != null)
				packageVersion = pkg.Name + " " + pkg.Version;
				
			try {
				if (parentMonitor == null)
					monitor = ProjectDomService.GetParseProgressMonitor ();
				parsing = true;
				if (monitor != null)
					monitor.BeginTask ("Parsing assembly: " + Path.GetFileName (fileName), 1);
				if (useExternalProcess)
				{
					using (DatabaseGenerator helper = GetGenerator (true))
					{
						string tmpDbFile = helper.GenerateDatabase (baseDir, assemblyFile);
						if (Disposed) {
							if (tmpDbFile != null)
								File.Delete (tmpDbFile);
							return;
						}
						if (tmpDbFile != null) {
							// GenerateDatabase generates the info in a temp file. Now we have to
							// unlock the current database file, copy the new file over it, and
							// read (and lock) the database again.
							UnlockDatabaseFile ();
							File.Delete (RealDataFile);
							File.Move (tmpDbFile, RealDataFile);
							Read ();
						}
					}
				} else {
					ICompilationUnit ainfo = DomCecilCompilationUnit.Load (fileName, false, false);
					
					UpdateTypeInformation (ainfo.Types, fileName);
					
					// Reset the error retry count, since the file has been
					// successfully parsed.
					FileEntry e = GetFile (fileName);
					e.ParseErrorRetries = 0;
					
					Flush ();
				}
			} catch (Exception ex) {
				FileEntry e = GetFile (fileName);
				e.LastParseTime = DateTime.MinValue;
				if (e.ParseErrorRetries > 0) {
					if (--e.ParseErrorRetries == 0) {
						e.DisableParse = true;
					}
				}
				else
					e.ParseErrorRetries = 3;
				if (monitor != null)
					monitor.ReportError ("Error parsing assembly: " + fileName, ex);
				throw;
			} finally {
				parsing = false;
				if (monitor != null) {
					monitor.EndTask ();
					if (parentMonitor == null) 
						monitor.Dispose ();
				}
			}
			ProjectDomService.NotifyAssemblyInfoChange (fileName, assemblyFile);
		}
		
		public bool ParseInExternalProcess
		{
			get { return useExternalProcess; }
			set { useExternalProcess = value; }
		}
		
		public static void CleanDatabase (string baseDir, string name)
		{
			try {
				// Read the headers of the file without fully loading the database
				Hashtable headers = ReadHeaders (baseDir, name);
				string checkFile = (string) headers ["CheckFile"];
				int version = (int) headers ["Version"];
				if (!File.Exists (checkFile) || version != FORMAT_VERSION) {
					string dataFile = Path.Combine (baseDir, name + ".pidb");
					FileService.DeleteFile (dataFile);
					LoggingService.LogInfo ("Deleted " + dataFile);
				}
			} catch {
				// Ignore errors while cleaning
			}
		}
		
		static DatabaseGenerator GetGenerator (bool share)
		{
			if (Runtime.ProcessService != null)
				return (DatabaseGenerator) Runtime.ProcessService.CreateExternalProcessObject (typeof(DatabaseGenerator), share);
			else
				return new DatabaseGenerator ();
		}
		
		public IEnumerable<string> ReadAssemblyReferences ()
		{
			AssemblyDefinition asm = AssemblyFactory.GetAssemblyManifest (assemblyFile);
		
			AssemblyNameReferenceCollection names = asm.MainModule.AssemblyReferences;
			foreach (AssemblyNameReference aname in names) {
				string afile = Runtime.SystemAssemblyService.GetAssemblyLocation (aname.FullName);
				if (afile != null)
					yield return "Assembly:" + Path.GetFullPath (afile);
			}
		}
	}
	
	internal class DatabaseGenerator: RemoteProcessObject
	{
		public string GenerateDatabase (string baseDir, string assemblyFile)
		{
			try {
				Runtime.Initialize (false);
				ParserDatabase pdb = new ParserDatabase ();

				// Generate the new db in a temp file. The main process will move the file if required.
				using (AssemblyCodeCompletionDatabase db = new AssemblyCodeCompletionDatabase (assemblyFile, pdb, true)) {
					if (db.LoadError)
						throw new InvalidOperationException ("Could find assembly: " + assemblyFile);
					db.ParseInExternalProcess = false;
					db.ParseAll ();
					db.Write ();
					return db.RealDataFile;
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return null;
			}
		}
	}
	
}
