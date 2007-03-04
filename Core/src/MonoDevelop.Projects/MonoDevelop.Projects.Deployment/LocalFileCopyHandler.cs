//
// LocalFileCopyHandler.cs
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2006 Michael Hutchinson
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
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects.Deployment.Extensions;

namespace MonoDevelop.Projects.Deployment
{
	public class LocalFileCopyHandler : IFileCopyHandler
	{
		public virtual string Id {
			get { return "MonoDevelop.LocalFileCopyHandler"; }
		}
		
		public virtual string Name {
			get { return GettextCatalog.GetString ("Local Filesystem"); }
		}

		public FileCopyConfiguration CreateConfiguration ()
		{
			return new LocalFileCopyConfiguration ();
		}
		
		public virtual void CopyFiles (IProgressMonitor monitor, IFileReplacePolicy replacePolicy, FileCopyConfiguration copyConfig, DeployFileCollection files)
		{
			LocalFileCopyConfiguration config = (LocalFileCopyConfiguration) copyConfig;
			
			foreach (DeployFile df in files) {
				string destFile = Path.Combine (config.TargetDirectory, df.RelativeTargetPath);
				string sourceFile = df.SourcePath;
				destFile = Runtime.FileService.GetFullPath (destFile);
				
				EnsureDirectoryExists (Path.GetDirectoryName (destFile));
				
				if (FileExists (destFile)) {
					DateTime sourceModified = File.GetLastWriteTime (df.SourcePath);
					DateTime targetModified = GetTargetModificationTime (destFile);
					FileReplaceMode deployMode = replacePolicy.GetReplaceAction (sourceFile, sourceModified, destFile, targetModified);
					
					switch (deployMode) {
						case FileReplaceMode.Skip:
							monitor.Log.WriteLine (GettextCatalog.GetString ("Skipping existing file {0}.", destFile));
							continue; //next file
							
						case FileReplaceMode.Replace:
							monitor.Log.WriteLine (GettextCatalog.GetString ("Replacing existing file {0}.", destFile));
							break;
						
						case FileReplaceMode.ReplaceOlder:
							if (File.GetLastWriteTime (df.SourcePath) > GetTargetModificationTime (destFile)) {
								monitor.Log.WriteLine (GettextCatalog.GetString ("Replacing older existing file {0}.", destFile));
							} else {
								monitor.Log.WriteLine (GettextCatalog.GetString ("Skipping newer existing file {0}.", destFile));
								continue; //next file
							}
							break;
						
						case FileReplaceMode.Abort:
							monitor.Log.WriteLine (GettextCatalog.GetString ("Deployment aborted: file {0} already exists.", destFile));
							return;
					}
				}
				else
					monitor.Log.WriteLine (GettextCatalog.GetString ("Deploying file {0}.", destFile));
				
				CopyFile (df.SourcePath, destFile);
			}
		}
		
		// These simple access routines are used by the base implementation of CopyFiles.
		// They can be overridden so that CopyFiles works with other filesystems.
		// They can be ignored if CopyFiles is overridden.
		
		protected virtual bool FileExists (string file)
		{
			return File.Exists (file);
		}
		
		protected virtual void CopyFile (string source, string target)
		{
			File.Copy (source, target, true);
		}
		 
		protected virtual DateTime GetTargetModificationTime (string targetFile)
		{
			return File.GetLastWriteTime (targetFile);
		}
		
		protected virtual void EnsureDirectoryExists (string directory)
		{
			if (!Directory.Exists (directory))
				Directory.CreateDirectory (directory);
		}
	}
}

