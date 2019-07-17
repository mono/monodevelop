// 
// Scope.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.FindInFiles
{

	abstract partial class Scope
	{
		sealed class DirectoryScope : Scope
		{
			readonly string path;
			readonly bool recurse;

			public override PathMode PathMode {
				get { return PathMode.Absolute; }
			}

			[Obsolete ("Unused - will be removed")]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public bool IncludeHiddenFiles {
				get;
				set;
			}
			bool fileNamesLoaded;
			ImmutableArray<FileProvider> fileNames;

			private async Task EnsureFileNamesLoaded (FindInFilesModel filterOptions, CancellationToken cancellationToken)
			{
				if (fileNamesLoaded)
					return;
				fileNamesLoaded = true;
				fileNames = await Task.Run (delegate {
					return ImmutableArray.CreateRange (GetFileNames (filterOptions, cancellationToken).Select (file => new FileProvider (file)));
				});
			}

			public DirectoryScope (string path, bool recurse)
			{
				this.path = path;
				this.recurse = recurse;
			}

			public override bool ValidateSearchOptions (FindInFilesModel filterOptions)
			{
				if (!Directory.Exists (path)) {
					MessageService.ShowError (string.Format (GettextCatalog.GetString ("Directory not found: {0}"), path));
					return false;
				}
				return true;
			}

			IEnumerable<string> GetFileNames (FindInFilesModel filterOptions, CancellationToken cancellationToken)
			{
				if (string.IsNullOrEmpty (path))
					yield break;
				var directoryStack = new Stack<string> ();
				directoryStack.Push (path);

				while (directoryStack.Count > 0) {
					var curPath = directoryStack.Pop ();
					if (cancellationToken.IsCancellationRequested)
						yield break;
					if (!Directory.Exists (curPath))
						yield break;
					try {
						var readPermission = new FileIOPermission (FileIOPermissionAccess.Read, curPath);
						readPermission.Demand ();
					} catch (Exception e) {
						LoggingService.LogError ("Can't access path " + curPath, e);
						yield break;
					}

					foreach (string fileName in Directory.EnumerateFiles (curPath)) {
						if (cancellationToken.IsCancellationRequested)
							yield break;
						if (Platform.IsWindows) {
							var attr = File.GetAttributes (fileName);
							if (attr.HasFlag (FileAttributes.Hidden))
								continue;
						}
						if (Path.GetFileName (fileName).StartsWith (".", StringComparison.Ordinal))
							continue;
						if (!filterOptions.IsFileNameMatching (fileName))
							continue;
						if (!IdeServices.DesktopService.GetFileIsText (fileName))
							continue;
						yield return fileName;
					}

					if (recurse) {
						foreach (string directoryName in Directory.EnumerateDirectories (curPath)) {
							if (cancellationToken.IsCancellationRequested)
								yield break;
							if (Platform.IsWindows) {
								var attr = File.GetAttributes (directoryName);
								if (attr.HasFlag (FileAttributes.Hidden))
									continue;
							}
							if (Path.GetFileName (directoryName).StartsWith (".", StringComparison.Ordinal))
								continue;
							directoryStack.Push (directoryName);
						}
					}

				}
			}

			public override async Task<ImmutableArray<FileProvider>> GetFilesAsync (FindInFilesModel filterOptions, CancellationToken cancellationToken)
			{
				await EnsureFileNamesLoaded (filterOptions, cancellationToken);
				return fileNames;
			}

			public override string GetDescription (FindInFilesModel model)
			{
				return model.InReplaceMode
					? GettextCatalog.GetString ("Replacing '{0}' in directory '{1}'", model.FindPattern, path)
					: GettextCatalog.GetString ("Looking for '{0}' in directory '{1}'", model.FindPattern, path);
			}
		}
	}
}