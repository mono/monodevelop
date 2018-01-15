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
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public static class HelpService
	{
		static RootTree helpTree;
		static bool helpTreeInitialized;
		static object helpTreeLock = new object ();
		static HashSet<string> sources = new HashSet<string> ();
		
		/// <summary>
		/// Starts loading the MonoDoc tree in the background.
		/// </summary>
		public static void AsyncInitialize ()
		{
			lock (helpTreeLock) {
				if (helpTreeInitialized)
					return;
			}
			ThreadPool.QueueUserWorkItem (delegate {
				// Load the help tree asynchronously. Reduces startup time.
				InitializeHelpTree ();
			});
		}
		
		//FIXME: allow adding sources without restart when extension installed (will need to be async)
		// will also be tricky we cause we'll also have update any running MonoDoc viewer
		static void InitializeHelpTree ()
		{
			lock (helpTreeLock) {
				if (helpTreeInitialized)
					return;

				// Only attempt on Windows if we can find monodoc.xml (currently not the case).
				// This avoids a first-chance FileNotFoundException in LoadTree.
				if (Platform.IsWindows && !File.Exists ("monodoc.xml")) {
					LoggingService.LogError ("Monodoc documentation tree could not be loaded because monodoc.xml was not found.");
					helpTreeInitialized = true;
					return;
				}

				Counters.HelpServiceInitialization.BeginTiming ();
				
				try {
					helpTree = RootTree.LoadTree ();
					
					foreach (var node in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/MonoDocSources"))
						sources.Add (((MonoDocSourceNode)node).Directory);

					if (Platform.IsWindows) {
						// windoc defines a special external directory used by XA. we need to read these docs too.
						// Not sure why it wasn't defined in monodoc.dll
						var commonAppData = Environment.GetFolderPath (Environment.SpecialFolder.CommonApplicationData);
						sources.Add (Path.Combine (commonAppData, "Monodoc"));
					}
					
					//remove nonexistent sources
					foreach (var s in sources.ToList ().Where (d => !Directory.Exists (d)))
						sources.Remove (s);
					
					foreach (var s in sources)
						helpTree.AddSource (s);
							
				} catch (Exception ex) {
					if (!(ex is ThreadAbortException) && !(ex.InnerException is ThreadAbortException))
						LoggingService.LogError ("Monodoc documentation tree could not be loaded.", ex);
				} finally {
					helpTreeInitialized = true;
					Counters.HelpServiceInitialization.EndTiming ();
				}
			}
		}
		
		/// <summary>
		/// A MonoDoc docs tree.
		/// </summary>
		/// <remarks>
		/// The tree is background-loaded the help service, and accessing the property will block until it is finished 
		/// loading. If you don't wish to block, check the <see cref="TreeInitialized"/> property first.
		///  </remarks>
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
		
		public static IEnumerable<string> Sources {
			get { return sources; }
		}
		
		//note: this method is very careful to check that the generated URLs exist in MonoDoc
		//because if we send nonexistent URLS to MonoDoc, it shows empty pages
		public static string GetMonoDocHelpUrl (Microsoft.CodeAnalysis.ISymbol result)
		{
			if (result == null)
				return null;
			return result.GetDocumentationCommentId ();
		}
	}
}

