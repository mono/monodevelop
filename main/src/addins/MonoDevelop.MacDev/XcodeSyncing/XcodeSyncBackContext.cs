// 
// XcodeSyncBackContext.cs
// 
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) Xamarin, Inc. (http://xamarin.com)
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
		NSObjectInfoService infoService;
		DotNetProject project;
		NSObjectProjectInfo pinfo;
		List<XcodeSyncObjcBackJob> typeSyncJobs = new List<XcodeSyncObjcBackJob> ();
		List<XcodeSyncFileBackJob> fileSyncJobs = new List<XcodeSyncFileBackJob> ();
		
		public XcodeSyncBackContext (FilePath projectDir, Dictionary<string,DateTime> syncTimes,
			NSObjectInfoService infoService, DotNetProject project)
			: base (projectDir, syncTimes)
		{
			this.project = project;
			this.infoService = infoService;
		}

		public List<XcodeSyncObjcBackJob> TypeSyncJobs {
			get { return typeSyncJobs; }
		}

		public List<XcodeSyncFileBackJob> FileSyncJobs {
			get { return fileSyncJobs; }
		}
		
		public NSObjectProjectInfo ProjectInfo {
			get {
				return pinfo ?? (pinfo = infoService.GetProjectInfo (project));
			}
		}
		
		public void ReportError (string errorFormat, params object[] args)
		{
			string msg = string.Format (errorFormat, args);
			Gtk.Application.Invoke (delegate {
				MonoDevelop.Ide.MessageService.ShowError (msg);
			});
		}
		
		public Dictionary<string, List<NSObjectTypeInfo>> GetTypeUpdates (out Dictionary<string,ProjectFile> newFiles)
		{
			var designerFiles = new Dictionary<string, List<NSObjectTypeInfo>> ();
			AggregateTypeUpdates (designerFiles, out newFiles);
			MergeExistingTypes (designerFiles);
			return designerFiles;
		}
		
		void AggregateTypeUpdates (Dictionary<string, List<NSObjectTypeInfo>> designerFiles,
			out Dictionary<string,ProjectFile> newFiles)
		{
			newFiles = null;
			XC4Debug.Log ("Aggregating {0} type updates", typeSyncJobs.Count);
			foreach (var job in TypeSyncJobs) {
				//generate designer filenames for classes without designer files
				if (job.DesignerFile == null) {
					var df = CreateDesignerFile (job);
					job.DesignerFile = df.FilePath;
					if (newFiles == null)
						newFiles = new Dictionary<string, ProjectFile> ();
					if (!newFiles.ContainsKey (job.DesignerFile))
						newFiles.Add (job.DesignerFile, df);
				}
				//group all the types by designer file
				List<NSObjectTypeInfo> types;
				if (!designerFiles.TryGetValue (job.DesignerFile, out types))
					designerFiles[job.DesignerFile] = types = new List<NSObjectTypeInfo> ();
				XC4Debug.Log ("{0}: {1}", job.DesignerFile, job.Type.ObjCName);
				types.Add (job.Type);
			}
		}
		
		//FIXME: is this overkill?
		ProjectFile CreateDesignerFile (XcodeSyncObjcBackJob job)
		{
			int i = 0;
			FilePath designerFile = null;
			do {
				FilePath f = job.Type.DefinedIn[0];
				string suffix = (i > 0? i.ToString () : "");
				string name = f.FileNameWithoutExtension + suffix + ".designer" + f.Extension;
				designerFile = f.ParentDirectory.Combine (name);
			} while (project.Files.GetFileWithVirtualPath (designerFile.ToRelative (project.BaseDirectory)) != null);
			var dependsOn = ((FilePath)job.Type.DefinedIn[0]).FileName;
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
		public string HFile;
		public string DesignerFile;
		public NSObjectTypeInfo Type;
	}
	
	class XcodeSyncFileBackJob
	{
		public FilePath Original;
		public FilePath SyncedRelative;
	}
}