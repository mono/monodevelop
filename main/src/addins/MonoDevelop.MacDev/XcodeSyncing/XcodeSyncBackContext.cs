// 
// XcodeSyncBackContext.cs
// 
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.IO;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.MacDev.ObjCIntegration;
using System.Threading.Tasks;
using MonoDevelop.MacDev.XcodeIntegration;

namespace MonoDevelop.MacDev.XcodeSyncing
{
	class XcodeSyncBackContext : XcodeSyncContext
	{
		List<XcodeSyncObjcBackJob> typeSyncJobs = new List<XcodeSyncObjcBackJob> ();
		List<XcodeSyncFileBackJob> fileSyncJobs = new List<XcodeSyncFileBackJob> ();
		NSObjectProjectInfo pinfo;
		
		public XcodeSyncBackContext (FilePath projectDir, Dictionary<string,DateTime> syncTimes,
			NSObjectInfoService infoService, DotNetProject project)
			: base (projectDir, syncTimes)
		{
			InfoService = infoService;
			Project = project;
		}
		
		public NSObjectInfoService InfoService {
			get; private set;
		}
		
		public DotNetProject Project {
			get; private set;
		}

		public List<XcodeSyncObjcBackJob> TypeSyncJobs {
			get { return typeSyncJobs; }
		}

		public List<XcodeSyncFileBackJob> FileSyncJobs {
			get { return fileSyncJobs; }
		}
		
		public NSObjectProjectInfo ProjectInfo {
			get {
				if (pinfo == null)
					pinfo = InfoService.GetProjectInfo (Project);
				
				if (pinfo != null)
					pinfo.Update (true);
				
				return pinfo;
			}
		}
		
		public void ReportError (string errorFormat, params object[] args)
		{
			string msg = string.Format (errorFormat, args);
			Gtk.Application.Invoke (delegate {
				MonoDevelop.Ide.MessageService.ShowError (msg);
			});
		}
		
		public Dictionary<string, List<NSObjectTypeInfo>> GetTypeUpdates (IProgressMonitor monitor, CodeDomProvider provider,
			out Dictionary<string, NSObjectTypeInfo> newTypes,
			out Dictionary<string, ProjectFile> newFiles)
		{
			Dictionary<string, List<NSObjectTypeInfo>> designerFiles = new Dictionary<string, List<NSObjectTypeInfo>> ();
			string defaultNamespace;
			
			// First, we need to name any new user-defined types.
			foreach (var job in TypeSyncJobs) {
				if (!job.IsFreshlyAdded) {
					monitor.Log.WriteLine ("Found updated class: {0}", job.Type.CliName);
					continue;
				}
				
				defaultNamespace = Project.GetDefaultNamespace (job.RelativePath);
				job.Type.CliName = defaultNamespace + "." + provider.CreateValidIdentifier (job.Type.ObjCName);
				monitor.Log.WriteLine ("Found newly-added class: {0}", job.Type.CliName);
				ProjectInfo.InsertUpdatedType (job.Type);
			}
			
			// Next we can resolve base-types, outlet types, and action parameter types for each of our user-defined types.
			foreach (var job in TypeSyncJobs) {
				defaultNamespace = Project.GetDefaultNamespace (job.RelativePath);
				ProjectInfo.ResolveObjcToCli (monitor, job.Type, provider, defaultNamespace);
			}
			
			AggregateTypeUpdates (monitor, provider, designerFiles, out newTypes, out newFiles);
			MergeExistingTypes (designerFiles);
			
			return designerFiles;
		}
		
		void AggregateTypeUpdates (IProgressMonitor monitor, CodeDomProvider provider, Dictionary<string, List<NSObjectTypeInfo>> designerFiles,
			out Dictionary<string, NSObjectTypeInfo> newTypes,
			out Dictionary<string, ProjectFile> newFiles)
		{
			newFiles = null;
			newTypes = null;
			
			foreach (var job in TypeSyncJobs) {
				if (job.IsFreshlyAdded) {
					// Need to define what file this new type is defined in
					string filename = job.Type.ObjCName + "." + provider.FileExtension;
					string path;
					
					if (job.RelativePath != null)
						path = Path.Combine (Project.BaseDirectory, job.RelativePath, filename);
					else
						path = Path.Combine (Project.BaseDirectory, filename);
					
					job.Type.DefinedIn = new string[] { path };
					
					if (newFiles == null)
						newFiles = new Dictionary<string, ProjectFile> ();
					if (!newFiles.ContainsKey (path))
						newFiles.Add (path, new ProjectFile (path));
					
					if (newTypes == null)
						newTypes = new Dictionary<string, NSObjectTypeInfo> ();
					if (!newTypes.ContainsKey (path))
						newTypes.Add (path, job.Type);
				}
				
				// generate designer filenames for classes without designer files
				if (job.DesignerFile == null) {
					var df = CreateDesignerFile (job);
					job.DesignerFile = df.FilePath;
					if (newFiles == null)
						newFiles = new Dictionary<string, ProjectFile> ();
					if (!newFiles.ContainsKey (job.DesignerFile))
						newFiles.Add (job.DesignerFile, df);
				}
				
				// group all the types by designer file
				List<NSObjectTypeInfo> types;
				if (!designerFiles.TryGetValue (job.DesignerFile, out types))
					designerFiles[job.DesignerFile] = types = new List<NSObjectTypeInfo> ();
				
				types.Add (job.Type);
			}
		}
		
		ProjectFile CreateDesignerFile (XcodeSyncObjcBackJob job)
		{
			FilePath designerFile = null;
			
			FilePath f = job.Type.DefinedIn[0];
			string name = f.FileNameWithoutExtension + ".designer" + f.Extension;
			designerFile = f.ParentDirectory.Combine (name);
			
			var dependsOn = ((FilePath) job.Type.DefinedIn[0]).FileName;
			
			return new ProjectFile (designerFile, BuildAction.Compile) { DependsOn = dependsOn };
		}
		
		void MergeExistingTypes (Dictionary<string, List<NSObjectTypeInfo>> designerFiles)
		{
			//add in other designer types that exist in the designer files
			foreach (var ut in ProjectInfo.GetTypes ()) {
				if (!ut.IsUserType)
					continue;
				var df = ut.GetDesignerFile ();
				List<NSObjectTypeInfo> types;
				if (df != null && designerFiles.TryGetValue (df, out types))
					if (!types.Any (t => t.ObjCName == ut.ObjCName))
						types.Add (ut);
			}
		}
	}
	
	class XcodeSyncObjcBackJob
	{
		public string DesignerFile;
		public NSObjectTypeInfo Type;
		public bool IsFreshlyAdded;
		public string RelativePath;
		
		public static XcodeSyncObjcBackJob NewType (NSObjectTypeInfo type, string relativePath)
		{
			return new XcodeSyncObjcBackJob () {
				RelativePath = relativePath,
				IsFreshlyAdded = true,
				Type = type,
			};
		}
		
		public static XcodeSyncObjcBackJob UpdateType (NSObjectTypeInfo type, string designerFile)
		{
			return new XcodeSyncObjcBackJob () {
				DesignerFile = designerFile,
				Type = type,
			};
		}
	}
	
	class XcodeSyncFileBackJob
	{
		public FilePath Original;
		public FilePath SyncedRelative;
		public bool IsFreshlyAdded;
		
		public XcodeSyncFileBackJob (FilePath original, FilePath syncedRelative, bool isFreshlyAdded)
		{
			IsFreshlyAdded = isFreshlyAdded;
			SyncedRelative = syncedRelative;
			Original = original;
		}
	}
}