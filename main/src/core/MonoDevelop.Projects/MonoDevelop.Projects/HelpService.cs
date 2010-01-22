// 
// HelpService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Monodoc;
using System.Threading;
using MonoDevelop.Core;
using Mono.Addins;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public static class HelpService
	{
		static RootTree helpTree;
		static bool helpTreeInitialized, cacheInitialized;
		static object helpTreeLock = new object ();
		static object cacheLock = new object ();
		static FilePath cacheRoot;
		
		/// <summary>
		/// Starts loading the MonoDoc tree in the background.
		/// </summary>
		public static void AsyncInitialize ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				// Load the help tree asynchronously. Reduces startup time.
				InitializeHelpTree ();
			});
		}
		
		static void InitializeHelpTree ()
		{
			lock (helpTreeLock) {
				if (helpTreeInitialized)
					return;
				try {
					var cacheDir = GetMonoDocCacheRoot ();
					if (!cacheDir.IsNullOrEmpty)
						helpTree = RootTree.LoadTree (cacheDir);
					else
						helpTree = RootTree.LoadTree (); 
				} catch (Exception ex) {
					if (!(ex is ThreadAbortException) && !(ex.InnerException is ThreadAbortException))
						LoggingService.LogError ("Monodoc documentation tree could not be loaded.", ex);
				} finally {
					helpTreeInitialized = true;
				}
			}
		}
		
		/// <summary>
		/// A MonoDoc docs tree.
		/// </summary>
		/// <remarks>
		/// The tree is background-loaded the help service, and accessing the property will block until it is finished 
		/// loading. If you don't wish to block, check the <see cref="TreeInitialized"/> property first.
		//  </remarks>
		public static RootTree HelpTree {
			get {
				lock (helpTreeLock) {
					if (!helpTreeInitialized)
						InitializeHelpTree ();
					return helpTree;
				}
			}
		}
		
		/// <summary>
		/// Whether the MonoDoc docs tree has finished loading.
		/// </summary>
		public static bool TreeInitialized {
			get {
				return helpTreeInitialized;
			}
		}
		
		/// <summary>
		/// If non-null, this is the path of an MonoDoc root directory that contains a merged "cache" of the main 
		/// system doc source and the doc sources provided by addins. This root is used by MonoDevelop itself and by 
		/// the instance of the MonoDoc browser that MonoDevelop launches.
		/// </summary>
		/// <remarks>
		/// The cache is background-initialized by the help service, and accessing this method will block until it 
		/// is initialized. If you don't wish to block, check the <see cref="CacheInitialized"/> property first.
		//  </remarks>
		public static FilePath GetMonoDocCacheRoot ()
		{
			if (cacheInitialized)
				return cacheRoot;
			
			lock (cacheLock) {
				if (cacheInitialized)
					return cacheRoot;
				
				//FIXME; the help cache doesn't work on Windows because it uses symlinks
				if (!PropertyService.IsWindows)
					cacheRoot = PropertyService.ConfigPath.Combine ("MonoDocCache");
				else
					cacheRoot = null;
				
				var sources = GetSources ();
				if (sources == null)
					cacheRoot = null;
				else
					UpdateHelpCache (cacheRoot, sources);
				cacheInitialized = true;
				return cacheRoot;
			}
		}
		
		static IList<string> GetSources ()
		{
			var sources = new List<string> ();
			
			if (Type.GetType ("Mono.Runtime") != null) {
				FilePath corlib = typeof (object).Assembly.Location;
				sources.Add (corlib.ParentDirectory.Combine ("..", "..", "monodoc", "sources").FullPath);
			}
			
			foreach (MonoDocSourceNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/MonoDocSources"))
				sources.Add (node.Directory);
			
			sources = sources.Select (p => Path.GetFullPath (p)).Where (Directory.Exists).Distinct ().ToList ();
			if (sources.Count > 0)
				return sources;
			else
				return null;
		}
		
		static void UpdateHelpCache (FilePath cacheDir, IList<string> mergeSources)
		{
			//build a map of all doc files. later merge sources override earlier ones
			var map = new Dictionary<string,string> ();
			foreach (var dir in mergeSources) {
				foreach (var file in Directory.GetFiles (dir)) {
					if (file.EndsWith (".source") || file.EndsWith (".zip") || file.EndsWith (".source"))
						map[Path.GetFileName (file)] = file;
				}
			}
			
			var cacheSourcesDir = cacheDir.Combine ("sources");
			if (Directory.Exists (cacheDir)) {
				string[] existing = Directory.GetFiles (cacheSourcesDir);
				for (int i = 0; i < existing.Length; i++)
					existing[i] = Path.GetFileName (existing[i]);
				
				DateTime lastModified = Directory.GetLastWriteTime (cacheDir);
				if (existing.Length == map.Count
				    && existing.All (map.ContainsKey)
				    && map.Values.All (f => File.GetLastWriteTime (f) < lastModified))
					return;
				Directory.Delete (cacheDir, true);
			}
			
			Directory.CreateDirectory (cacheSourcesDir);
			
			foreach (var kvp in map)
				new Mono.Unix.UnixFileInfo (kvp.Value).CreateSymbolicLink (Path.Combine (cacheSourcesDir, kvp.Key));
			
			File.WriteAllText (Path.Combine (cacheDir, "monodoc.xml"), @"<?xml version=""1.0""?>
<node label=""Mono Documentation"" name=""libraries"">
  <node label=""Commands and Files"" name=""man"" />
  <node label=""Languages"" name=""languages"" />
  <node label=""Tools"" name=""tools"" />
  <node label=""Various"" name=""various"" />
</node>");
			
			Directory.SetLastWriteTime (cacheDir, DateTime.Now);
		}
		
		/// <summary>
		/// Whether the MonoDoc cache directory has been initialized yet.
		/// </summary>
		public static bool CacheInitialized {
			get {
				return cacheInitialized;
			}
		}
		
		//note: this method is very careful to check that the generated URLs exist in MonoDoc
		//because if we send nonexistent URLS to MonoDoc, it shows empty pages
		public static string GetMonoDocHelpUrl (ResolveResult result)
		{
			if (result == null)
				return null;
			
			if (result is AggregatedResolveResult) 
				result = ((AggregatedResolveResult)result).PrimaryResult;
			
			
			if (result is NamespaceResolveResult)
			{
				string namespc = ((NamespaceResolveResult)result).Namespace;
				//verify that the namespace exists in the help tree
				//FIXME: GetHelpXml doesn't seem to work for namespaces, so forced to do full render
				Monodoc.Node dummy;
				if (!String.IsNullOrEmpty (namespc) && HelpTree != null && HelpTree.RenderUrl ("N:" + namespc, out dummy) != null)
					return "N:" + namespc;
				else return null;
			}
			
			IMember member = null;
			if (result is MethodResolveResult)
				member = ((MethodResolveResult)result).MostLikelyMethod;
			else if (result is MemberResolveResult)
				member = ((MemberResolveResult)result).ResolvedMember;
			
			if (member != null && member.GetMonodocDocumentation () != null)
				return member.HelpUrl;
			
			IReturnType type = result.ResolvedType;
			if (type != null && !String.IsNullOrEmpty (type.FullName)) {
				string t = "T:" + type.FullName;
				if (HelpTree != null && HelpTree.GetHelpXml (t) != null)
					return t;
			}
			
			return null;
		}
	}
}

