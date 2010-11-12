// SshFuseFileCopyHandler.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System;

using MonoDevelop.Core;

namespace MonoDevelop.Deployment.Targets
{
	
	
	public class SshFuseFileCopyHandler : BaseFuseFileCopyHandler
	{
		public override string Id {
			get { return "MonoDevelop.Deployment.SshFuseFileCopyHandler"; }
		}
		
		public override string Name {
			get { return "SSH (FUSE)"; }
		}
		
		public override void MountTempDirectory (IProgressMonitor monitor, FileCopyConfiguration copyConfig, string tempPath)
		{
			SshFuseFileCopyConfiguration config = (SshFuseFileCopyConfiguration) copyConfig;
			string fuseArgs = string.Format ("{0} {1} {2}", config.TargetDirectory, tempPath, config.ExtraMountArguments);
			RunFuseCommand (monitor, "sshfs", fuseArgs); 
		}
		
		public override FileCopyConfiguration CreateConfiguration ()
		{
			return new SshFuseFileCopyConfiguration ();
		}

	}
	
	public class SshFuseFileCopyConfiguration : BaseFuseFileCopyConfiguration
	{
		string hostname;
		string username;
		string directory;
		
		public override string FriendlyLocation {
			get {
				ParseTargetDirectory ();
				if (!string.IsNullOrEmpty (HostName))
					return "sshfs " + TargetDirectory;
				return null;
			}
		}
		
		//some code (serialisation) accesses the base TargetDirectory directly, so we have to sync it
		//in the HostName, UserName, and Directory accessors to be usre it's up-to-date.
		//These are helper methods to do just that.
		void SetTargetDirectory ()
		{
			if (!string.IsNullOrEmpty (username))
				TargetDirectory = string.Format ("{0}@{1}:{2}", username, hostname, directory);
			else
				TargetDirectory = string.Format ("{0}:{1}", hostname, directory);
		}
		
		void ParseTargetDirectory ()
		{
			if (TargetDirectory == null) {
				hostname = username = directory = null;
				return;
			}
			int indexOfAt = TargetDirectory.IndexOf ('@');
			int indexOfColon = TargetDirectory.IndexOf (':');
			if (indexOfColon < 1) {
				LoggingService.LogWarning ("Ignoring invalid SSHFS host path \"{0}\" in configuration.", TargetDirectory);
				return;
			}
			if (indexOfAt > 0 && indexOfColon > indexOfAt) {
				username = TargetDirectory.Substring (0, indexOfAt);
				hostname = TargetDirectory.Substring (indexOfAt + 1, indexOfColon - indexOfAt - 1);
			} else {
				hostname = TargetDirectory.Substring (0, indexOfColon);
			}
			directory = TargetDirectory.Substring (indexOfColon + 1);
		}
		
		public string HostName {
			get {
				ParseTargetDirectory ();
				return hostname;
			}
			set {
				ParseTargetDirectory ();
				hostname = value;
				SetTargetDirectory ();
			}
		}
	
		public string UserName {
			get {
				ParseTargetDirectory ();
				return username;
			}
			set {
				ParseTargetDirectory ();
				username = value;
				SetTargetDirectory ();
			}
		}
		
		public string Directory {
			get {
				ParseTargetDirectory ();
				return directory;
			}
			set {
				ParseTargetDirectory ();
				directory = value;
				SetTargetDirectory ();
			}
		}
	}
}
