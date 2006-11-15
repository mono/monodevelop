
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace VersionControl.Service.Cvs
{
	public class CvsRepository: UrlBasedRepository
	{
		string moduleName;
		
		public CvsRepository ()
		{
		}
		
		public CvsRepository (VersionControlSystem vcs, string moduleName): base (vcs)
		{
			this.moduleName = moduleName;
		}
		
		public override string Url
		{
			get {
				if (Method.Length == 0)
					return "";
				string login = "";
				if (this.User != "") {
					login += this.User;
					if (this.Pass != "")
						login += ":" + this.Pass;
					login += "@";
				}
				if (!Dir.StartsWith ("/"))
					Dir = "/" + Dir;
				return ":" + Method + ":" + login + Server + (Port > 0 ? ":" + Port.ToString() : "") + Dir;
			}
			set {
				Method = Server = User = Pass = Dir = string.Empty;
				Port = 0;
				
				int p = 0, i = 0;
				
				// Method
				if (value.Length > 0 && value[0] == ':') {
					i = value.IndexOf (':', 1);
					if (i == -1) return;
					Method = value.Substring (1, i-1);
					p = i+1;
				}
				i = value.IndexOf ('@', p);
				if (i != -1) {
					// User and password
					User = value.Substring (p, i-p);
					p = i + 1;
					i = User.IndexOf (':');
					if (i != -1) {
						Pass = User.Substring (i+1);
						User = User.Substring (0, i);
					}
				}
				
				// Host name
				i = value.IndexOf (':', p);
				if (i != -1) {
					Server = value.Substring (p, i-p);
					p = i + 1;
					i = value.IndexOf ('/', p);
					if (i == -1) return;
					if (i > p+1) {
						int pt;
						if (int.TryParse (value.Substring (p, i-p), out pt))
							Port = pt;
					}
					p = i;
				} else {
					i = value.IndexOf ('/');
					if (i == -1) return;
					Server = value.Substring (p, i-p);
					p = i;
				}
				Dir = value.Substring (p);
			}
		}
		
		public override bool HasChildRepositories {
			get { return moduleName == null; }
		}
		
		public override IEnumerable<Repository> ChildRepositories {
			get {
				List<Repository> list = new List<Repository> ();
				if (moduleName != null)
					return list;

				TextReader tr = RunCommand ("co", "-c");
				string rep;
				while ((rep = tr.ReadLine()) != null) {
					int i = rep.IndexOf (' ');
					if (i != -1)
						rep = rep.Substring (0, i);
					if (rep.Length == 0)
						continue;
					CvsRepository cr = new CvsRepository (base.VersionControlSystem, rep);
					cr.Name = rep;
					list.Add (cr);
				}
				return list;
			}
		}
		
		public override string GetPathToBaseText (string sourcefile)
		{
			return null;
		}
		
		public override string GetTextAtRevision (string repositoryPath, Revision revision)
		{
			return null;
		}
		
		public override Revision[] GetHistory (string sourcefile, Revision since)
		{
			return null;
		}
		
		public override VersionInfo GetVersionInfo (string localPath, bool getRemoteStatus)
		{
			return null;
		}
		
		public override VersionInfo[] GetDirectoryVersionInfo (string sourcepath, bool getRemoteStatus, bool recursive)
		{
			return null;
		}
		
		
		public override Repository Publish (string serverPath, string localPath, string[] files, string message, IProgressMonitor monitor)
		{
			return null;
		}
		
		public override void Update (string path, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
		}
		
		public override void Checkout (string path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Revert (string localPath, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Add (string path, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Move (string srcPath, string destPath, Revision revision, bool force, IProgressMonitor monitor)
		{
		}
		
		public override void Delete (string path, bool force, IProgressMonitor monitor)
		{
		}
		
		TextReader RunCommand (string command, string options)
		{
			StringWriter ow = new StringWriter ();
			StringWriter ew = new StringWriter ();
			
			ProcessWrapper pw = Runtime.ProcessService.StartProcess ("cvs", "-d " + Url + " " + command + " " + options, ".", ow, ew, null);
			pw.WaitForOutput ();
			
			return new StringReader (ow.ToString ());
		}
	}
}
