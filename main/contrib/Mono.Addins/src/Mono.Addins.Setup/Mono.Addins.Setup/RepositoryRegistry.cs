//
// RepositoryRegistry.cs
//
// Author:
//   Lluis Sanchez Gual
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
//


using System;
using System.IO;
using System.Collections;
using Mono.Addins.Setup.ProgressMonitoring;

namespace Mono.Addins.Setup
{
	public class RepositoryRegistry
	{
		ArrayList repoList;
		SetupService service;
		
		internal RepositoryRegistry (SetupService service)
		{
			this.service = service;
		}
		
		public AddinRepository RegisterRepository (IProgressStatus monitor, string url)
		{
			return RegisterRepository (monitor, url, false);
		}
		
		public AddinRepository RegisterRepository (IProgressStatus monitor, string url, bool updateNow)
		{
			if (!url.EndsWith (".mrep"))
				url = url + "/main.mrep";
			
			RepositoryRecord rr = FindRepositoryRecord (url);
			if (rr != null)
				return rr;

			rr = RegisterRepository (url, false);
			
			try {
				if (updateNow) {
					UpdateRepository (monitor, url);
					rr = FindRepositoryRecord (url);
					Repository rep = rr.GetCachedRepository ();
					rr.Name = rep.Name;
				}
				service.SaveConfiguration ();
				return rr;
			} catch (Exception ex) {
				if (monitor != null)
					monitor.ReportError ("The repository could not be registered", ex);
				if (ContainsRepository (url))
					RemoveRepository (url);
				return null;
			}
		}
		
		internal RepositoryRecord RegisterRepository (string url, bool isReference)
		{
			RepositoryRecord rr = FindRepositoryRecord (url);
			if (rr != null) {
				if (rr.IsReference && !isReference) {
					rr.IsReference = false;
					service.SaveConfiguration ();
				}
				return rr;
			}
			
			rr = new RepositoryRecord ();
			rr.Url = url;
			rr.IsReference = isReference;
			
			string name = service.RepositoryCachePath;
			if (!Directory.Exists (name))
				Directory.CreateDirectory (name);
			string host = new Uri (url).Host;
			if (host.Length == 0)
				host = "repo";
			name = Path.Combine (name, host);
			rr.File = name + "_" + service.Configuration.RepositoryIdCount + ".mrep";
			
			rr.Id = "rep" + service.Configuration.RepositoryIdCount;
			service.Configuration.Repositories.Add (rr);
			service.Configuration.RepositoryIdCount++;
			service.SaveConfiguration ();
			repoList = null;
			return rr;
		}
		
		internal RepositoryRecord FindRepositoryRecord (string url)
		{
			foreach (RepositoryRecord rr in service.Configuration.Repositories)
				if (rr.Url == url) return rr;
			return null;
		}
		
		public void RemoveRepository (string url)
		{
			RepositoryRecord rep = FindRepositoryRecord (url);
			if (rep == null)
				throw new InstallException ("The repository at url '" + url + "' is not registered");
			
			foreach (RepositoryRecord rr in service.Configuration.Repositories) {
				if (rr == rep) continue;
				Repository newRep = rr.GetCachedRepository ();
				if (newRep == null) continue;
				foreach (ReferenceRepositoryEntry re in newRep.Repositories) {
					if (re.Url == url) {
						rep.IsReference = true;
						return;
					}
				}
			}
			
			// There are no other repositories referencing this one, so we can safely delete
			
			Repository delRep = rep.GetCachedRepository ();
			service.Configuration.Repositories.Remove (rep);
			rep.ClearCachedRepository ();
			
			if (delRep != null) {
				foreach (ReferenceRepositoryEntry re in delRep.Repositories)
					RemoveRepository (new Uri (new Uri (url), re.Url).ToString ());
			}

			service.SaveConfiguration ();
			repoList = null;
		}
		
		public bool ContainsRepository (string url)
		{
			return FindRepositoryRecord (url) != null;
		}
		
		ArrayList RepositoryList {
			get {
				if (repoList == null) {
					ArrayList list = new ArrayList ();
					foreach (RepositoryRecord rep in service.Configuration.Repositories) {
						if (!rep.IsReference)
							list.Add (rep);
					}
					repoList = list;
				}
				return repoList;
			}
		}
		
		public AddinRepository[] GetRepositories ()
		{
			return (AddinRepository[]) RepositoryList.ToArray (typeof(AddinRepository));
		}
			                     
		
		public void UpdateAllRepositories (IProgressStatus monitor)
		{
			UpdateRepository (monitor, (string)null);
		}
		
		public void UpdateRepository (IProgressStatus statusMonitor, string url)
		{
			repoList = null;
			
			IProgressMonitor monitor = ProgressStatusMonitor.GetProgressMonitor (statusMonitor);
		
			monitor.BeginTask ("Updating repositories", service.Configuration.Repositories.Count);
			try {
				int num = service.Configuration.Repositories.Count;
				for (int n=0; n<num; n++) {
					RepositoryRecord rr = (RepositoryRecord) service.Configuration.Repositories [n];
					if ((url == null || rr.Url == url) && !rr.IsReference)
						UpdateRepository (monitor, new Uri (rr.Url), rr);
					monitor.Step (1);
				}
			} finally {
				monitor.EndTask ();
			}
			service.SaveConfiguration ();
		}

		void UpdateRepository (IProgressMonitor monitor, Uri baseUri, RepositoryRecord rr)
		{
			Uri absUri = new Uri (baseUri, rr.Url);
			monitor.BeginTask ("Updating from " + absUri.ToString (), 2);
			Repository newRep;
			try {
				newRep = (Repository) service.Store.DownloadObject (monitor, absUri.ToString (), typeof(Repository));
			} catch (Exception ex) {
				monitor.ReportError ("Could not get information from repository" + ": " + absUri.ToString (), ex);
				return;
			}
			
			monitor.Step (1);
			
			foreach (ReferenceRepositoryEntry re in newRep.Repositories) {
				Uri refRepUri = new Uri (absUri, re.Url);
				string refRepUrl = refRepUri.ToString ();
				RepositoryRecord refRep = FindRepositoryRecord (refRepUrl);
				if (refRep == null)
					refRep = RegisterRepository (refRepUrl, true);
				if (refRep.LastModified < re.LastModified) {
					UpdateRepository (monitor, refRepUri, refRep);
				}
			}
			monitor.EndTask ();
			rr.UpdateCachedRepository (newRep);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates ()
		{
			return GetAvailableAddin (null, null, null, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates (string repositoryUrl)
		{
			return GetAvailableAddin (repositoryUrl, null, null, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates (string id, string version)
		{
			return GetAvailableAddin (null, id, version, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableUpdates (string repositoryUrl, string id, string version)
		{
			return GetAvailableAddin (repositoryUrl, id, version, true);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins ()
		{
			return GetAvailableAddin (null, null, null, false);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddins (string repositoryUrl)
		{
			return GetAvailableAddin (repositoryUrl, null, null);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddin (string id, string version)
		{
			return GetAvailableAddin (null, id, version);
		}
		
		public AddinRepositoryEntry[] GetAvailableAddin (string repositoryUrl, string id, string version)
		{
			return GetAvailableAddin (repositoryUrl, id, version, false);
		}
		
		PackageRepositoryEntry[] GetAvailableAddin (string repositoryUrl, string id, string version, bool updates)
		{
			ArrayList list = new ArrayList ();
			
			IEnumerable ee;
			if (repositoryUrl != null) {
				ArrayList repos = new ArrayList ();
				GetRepositoryTree (repositoryUrl, repos);
				ee = repos;
			} else
				ee = service.Configuration.Repositories;
			
			foreach (RepositoryRecord rr in ee) {
				Repository rep = rr.GetCachedRepository();
				if (rep == null) continue;
				foreach (PackageRepositoryEntry addin in rep.Addins) {
					if ((id == null || Addin.GetIdName (addin.Addin.Id) == id) && (version == null || addin.Addin.Version == version)) {
						if (updates) {
							Addin ainfo = service.Registry.GetAddin (addin.Addin.Id);
							if (ainfo == null || Addin.CompareVersions (ainfo.Version, addin.Addin.Version) <= 0)
								continue;
						}
						list.Add (addin);
					}
				}
			}
			// Old versions are returned first
			list.Sort ();
			return (PackageRepositoryEntry[]) list.ToArray (typeof(PackageRepositoryEntry));
		}
		
		void GetRepositoryTree (string url, ArrayList list)
		{
			RepositoryRecord rr = FindRepositoryRecord (url);
			if (rr == null) return;
			
			if (list.Contains (rr))
				return;
				
			list.Add (rr);
			Repository rep = rr.GetCachedRepository ();
			if (rep == null)
				return;
			
			Uri absUri = new Uri (url);
			foreach (ReferenceRepositoryEntry re in rep.Repositories) {
				Uri refRepUri = new Uri (absUri, re.Url);
				GetRepositoryTree (refRepUri.ToString (), list);
			}
		}
	}
}
