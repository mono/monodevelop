//
// SourceCodeLookup.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	static class SourceCodeLookup
	{
		readonly static List<Tuple<FilePath,FilePath>> possiblePaths = new List<Tuple<FilePath, FilePath>> ();
		readonly static Dictionary<FilePath,FilePath> directMapping = new Dictionary<FilePath, FilePath> ();
		const string DebugSourceFoldersKey = "Debugger.DebugSourceFolders";

		/// <summary>
		/// Finds the source file.
		/// </summary>
		/// <returns>The source file.</returns>
		/// <param name="originalFile">File from .mdb/.pdb.</param>
		/// <param name="hash">Hash of original file stored in .mdb/.pdb.</param>
		public static FilePath FindSourceFile (FilePath originalFile, byte[] hash)
		{
			if (directMapping.ContainsKey (originalFile))
				return directMapping [originalFile];
			foreach (var folder in possiblePaths) {
				//file = /tmp/ci_build/mono/System/Net/Http/HttpClient.cs
				var relativePath = originalFile.ToRelative (folder.Item1);
				//relativePath = System/Net/Http/HttpClient.cs
				var newFile = folder.Item2.Combine (relativePath);
				//newPossiblePath = C:\GIT\mono_source\System\Net\Http\HttpClient.cs
				if (CheckFileHash (newFile, hash)) {
					directMapping.Add (originalFile, newFile);
					return newFile;
				}
			}
			foreach (var document in IdeApp.Workbench.Documents.Where((d) => d.FileName.FileName == originalFile.FileName)) {
				//Check if it's already added to avoid MD5 checking
				if (!directMapping.ContainsKey (originalFile)) {
					if (CheckFileHash (document.FileName, hash)) {
						AddLoadedFile (document.FileName, originalFile);
						return document.FileName;
					}
				}
			}
			foreach (var bp in DebuggingService.Breakpoints.GetBreakpoints().Where((bp) => Path.GetFileName(bp.FileName) == originalFile.FileName)) {
				//Check if it's already added to avoid MD5 checking
				if (!directMapping.ContainsKey (originalFile)) {
					if (CheckFileHash (bp.FileName, hash)) {
						AddLoadedFile (bp.FileName, originalFile);
						return bp.FileName;
					}
				}
			}
			var debugSourceFolders = IdeApp.Workspace.GetAllSolutions ().SelectMany (s => s.UserProperties.GetValue (DebugSourceFoldersKey, Array.Empty<string> ()));
			if (debugSourceFolders.Any ()) {
				var result = TryDebugSourceFolders (originalFile, hash, debugSourceFolders);
				if (result.IsNotNull)
					return result;
			}
			//Attempt to find source code inside solution, this is mostly useful for Docker which moves code to container and compiles there 
			//so when user debug application we want to make connection to code opened inside IDE automaticlly
			debugSourceFolders = IdeApp.Workspace.GetAllSolutions ().Select (s => s.BaseDirectory.ToString ());
			if (debugSourceFolders.Any ()) {
				var result = TryDebugSourceFolders (originalFile, hash, debugSourceFolders);
				if (result.IsNotNull)
					return result;
			}
			return FilePath.Null;
		}

		public static FilePath TryDebugSourceFolders (FilePath originalFile, byte[] hash, IEnumerable<string> debugSourceFolders)
		{
			var folders = ((string)originalFile).Split ('/', '\\');
			//originalFile=/tmp/ci_build/mono/System/Net/Http/HttpClient.cs
			for (int i = 0; i < folders.Length; i++) {
				var partiallyCombined = Path.Combine (folders.Skip (i).ToArray ());
				//i=0 partiallyCombined=tmp/ci_build/mono/System/Net/Http/HttpClient.cs
				//i=1 partiallyCombined=ci_build/mono/System/Net/Http/HttpClient.cs
				//i=2 partiallyCombined=mono/System/Net/Http/HttpClient.cs
				//i=3 partiallyCombined=System/Net/Http/HttpClient.cs
				//...
				//Idea here is... Try with combining longest possbile path 1st
				foreach (var debugSourceFolder in debugSourceFolders) {
					var potentialPath = Path.Combine (debugSourceFolder, partiallyCombined);
					if (CheckFileHash (potentialPath, hash)) {
						AddLoadedFile (potentialPath, originalFile);
						return potentialPath;
					}
				}
			}
			return FilePath.Null;
		}

		public static bool CheckFileHash (FilePath file, byte[] hash)
		{
			if (hash == null)
				return false;
			if (File.Exists (file)) {
				using (var fs = File.OpenRead (file)) {
					// Roslyn SHA1 checksum always starts with 20
					if (hash.Length > 0 && hash [0] == 20)
						using (var sha1 = SHA1.Create ()) {
							if (sha1.ComputeHash (fs).Take (15).SequenceEqual (hash.Skip (1))) {
								return true;
							}
						}
					if (hash.Length == 20) {
						using (var sha1 = SHA1.Create ()) {
							fs.Position = 0;
							if (sha1.ComputeHash (fs).SequenceEqual (hash)) {
								return true;
							}
						}
					}
					fs.Position = 0;
					using (var md5 = MD5.Create ()) {
						if (md5.ComputeHash (fs).SequenceEqual (hash)) {
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Call this method when user succesfully opens file so we can reuse this path
		/// for other files from same project.
		/// Notice that it's caller job to verify hash matches for performance reasons.
		/// </summary>
		/// <param name="file">File path which user picked.</param>
		/// <param name = "originalFile">Original file path from .pdb/.mdb.</param>
		public static void AddLoadedFile (FilePath file, FilePath originalFile)
		{
			if (directMapping.ContainsKey (originalFile))
				return;
			directMapping.Add (originalFile, file);
			//file = C:\GIT\mono_source\System\Text\UTF8Encoding.cs
			//originalFile = /tmp/ci_build/mono/System/Text/UTF8Encoding.cs
			var fileParent = file.ParentDirectory;
			var originalParent = originalFile.ParentDirectory;
			if (fileParent == originalParent) {
				//This can happen if file was renamed
				possiblePaths.Add (new Tuple<FilePath, FilePath> (originalParent, fileParent));
				AddPathToDebugSourceFolders (fileParent);
			} else {
				while (fileParent.FileName == originalParent.FileName) {
					fileParent = fileParent.ParentDirectory;
					originalParent = originalParent.ParentDirectory;
				}
				//fileParent = C:\GIT\mono_source\
				//originalParent = /tmp/ci_build/mono/
				possiblePaths.Add (new Tuple<FilePath, FilePath> (originalParent, fileParent));
				AddPathToDebugSourceFolders (fileParent);
			}
		}

		static void AddPathToDebugSourceFolders (string path)
		{
			foreach (var sol in IdeApp.Workspace.GetAllSolutions ()) {
				var debugSourceFolders = sol.UserProperties.GetValue (DebugSourceFoldersKey, Array.Empty<string> ());
				if (debugSourceFolders.Contains (path))
					continue;
				sol.UserProperties.SetValue (DebugSourceFoldersKey, debugSourceFolders.Union (new [] { path }).ToArray ());
				sol.SaveUserProperties ().Ignore ();
			}
		}

		public static string [] GetDebugSourceFolders (Solution solution)
		{
			return solution.UserProperties.GetValue (DebugSourceFoldersKey, Array.Empty<string> ());
		}

		public static void SetDebugSourceFolders (Solution solution, string [] folders)
		{
			// Invalidate existing mappings so new DebugSourceFolders are used.
			directMapping.Clear ();
			possiblePaths.Clear ();
			solution.UserProperties.SetValue (DebugSourceFoldersKey, folders);
		}
	}
}

