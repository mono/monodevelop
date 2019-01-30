//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text;

namespace MDBuildTasks
{
	public class FilterLocalCopyReferences : Task
	{
		[Required]
		public ITaskItem[] ReferenceCopyLocalPaths { get; set; }

		[Required]
		public ITaskItem[] IncludeList { get; set; }
		public ITaskItem[] SuppressList { get; set; }

		public ITaskItem[] ReferencedProjects { get; set; }

		public string Configuration { get; set; }
		public string Platform { get; set; }
		public string ProjectDir { get; set; }
		public string ProjectName { get; set; }

		public bool DebugCopies { get; set; }

		[Output]
		public ITaskItem[] ItemsToRemove { get; set; }

		public override bool Execute ()
		{
			HashSet<string> localCopyCone = null;
			HashSet<string> importedLocalCopyConeNames = null;
			List<ITaskItem> localCopyKeep = null;
			HashSet<string> unusedIncludeKeys = null;
			HashSet<string> suppressKeyHash = null;

			if (DebugCopies) {
				localCopyKeep = new List<ITaskItem> ();
				localCopyCone = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
				unusedIncludeKeys = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
				importedLocalCopyConeNames = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
				suppressKeyHash = new HashSet<string> (StringComparer.OrdinalIgnoreCase);

				// we can't invoke custom targets on the referenced project to get the output dir
				// as some projects will not have these targets.
				// instead, just poke in some standard location and hope it works. worse case, our diagnostics are poorer.
				if (ReferencedProjects != null) {
					foreach (var projectRef in ReferencedProjects) {
						if (string.Equals (projectRef.GetMetadata ("BuildReference"), "true", StringComparison.OrdinalIgnoreCase)) {
							var projectLocalCopyConeFile = GetLocalCopyConeFile (Path.GetDirectoryName (projectRef.GetMetadata ("FullPath")), projectRef.GetMetadata ("Configuration"), projectRef.GetMetadata ("Platform"));
							if (File.Exists (projectLocalCopyConeFile)) {
								foreach (var line in File.ReadAllLines (projectLocalCopyConeFile)) {
									localCopyCone.Add (line);
								}
							}
						}
					}
				}
				foreach (var f in localCopyCone) {
					importedLocalCopyConeNames.Add (Path.GetFileName (f));
				}
				if (SuppressList != null) {
					foreach (var include in SuppressList) {
						suppressKeyHash.Add (include.ItemSpec);
					}
				}
			}

			var toRemove = new List<ITaskItem> ();

			var includeKeyHash = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
			foreach (var include in IncludeList) {
				includeKeyHash.Add (include.ItemSpec);
			}

			foreach (var item in ReferenceCopyLocalPaths) {
				var filename = Path.GetFileName (item.ItemSpec);
				var includeKey = filename;
				switch (Path.GetExtension (includeKey).ToLowerInvariant ()) {
				case ".dll":
					// Only make this change if the user hasn't explicitly put a *.resources.dll file in the include list (e.g. AndroidDeviceManager.Resources.dll should be treated like a normal DLL)
					if (!includeKeyHash.Contains(includeKey) && includeKey.EndsWith (".resources.dll", StringComparison.OrdinalIgnoreCase)) {
						includeKey = includeKey.Substring (0, includeKey.Length - ".resources.dll".Length) + ".dll";
					}
					break;
				case ".config":
					includeKey = Path.GetFileNameWithoutExtension (includeKey);
					break;
				case ".pdb":
				case ".xml":
					includeKey = Path.ChangeExtension (includeKey, ".dll");
					break;
				}

				if (!includeKeyHash.Contains (includeKey)) {
					toRemove.Add (item);
					if (DebugCopies) {
						// Log a message that this may need to be imported.
						// If the file was not in the cone of files that referenced projects have already local copied.
						// Check the filename, not just the full path, because there are some edge cases like SharpZipLib
						// where there are multiple different assemblies with the same name and we don't want copies of
						// both floating around.
						if (!importedLocalCopyConeNames.Contains (filename) && !suppressKeyHash.Contains (includeKey)) {
							unusedIncludeKeys.Add (includeKey);
						}
					}
				} else if (DebugCopies) {
					localCopyCone.Add (item.ItemSpec);
					localCopyKeep.Add (item);
				}
			}

			ItemsToRemove = toRemove.ToArray ();

			if (!DebugCopies) {
				return true;
			}

			string localCopyConeFile = GetLocalCopyConeFile (ProjectDir, Configuration, Platform);
			if (localCopyCone.Count > 0) {
				File.WriteAllLines (localCopyConeFile, localCopyCone);
			} else if (File.Exists (localCopyConeFile)) {
				File.Delete (localCopyConeFile);
			}

			if ((toRemove.Count + localCopyKeep.Count + unusedIncludeKeys.Count) == 0) {
				return true;
			}

			var message = new StringBuilder ();
			message.AppendLine ($"====== FilterLocalCopyReferences {ProjectName} Debug ======");

			if (toRemove.Count> 0) {
				message.AppendLine ("Removing items:");
				foreach (var item in toRemove) {
					message.Append ("  ");
					message.AppendLine (item.ItemSpec);
				}
			}
			if (localCopyKeep.Count > 0) {
				message.AppendLine ("Keeping items:");
				foreach (var item in localCopyKeep) {
					message.Append ("  ");
					message.AppendLine (item.ItemSpec);
				}
			}
			if (unusedIncludeKeys.Count > 0) {
				message.AppendLine ("Unused keys:");
				foreach (var key in unusedIncludeKeys) {
					message.Append ("  <IncludeCopyLocal Include=\"");
					message.Append (key);
					message.AppendLine ("\" />");
				}
			}
			message.AppendLine ("=============================================");
			Log.LogMessage (MessageImportance.High, message.ToString ());

			return true;
		}

		static string GetLocalCopyConeFile (string projectDir, string configuration, string platform)
		{
			var objDir = Path.Combine (projectDir, "obj");
			if (!string.IsNullOrEmpty (platform) && !(string.Equals (platform, "anycpu", StringComparison.OrdinalIgnoreCase) || string.Equals (platform, "any cpu", StringComparison.OrdinalIgnoreCase))) {
				objDir = Path.Combine (objDir, platform);
			}
			objDir = Path.Combine (objDir, configuration);
			return Path.Combine (objDir, "LocalCopyCone.txt");
		}
	}
}
