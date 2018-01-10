//
// StartupInfo.cs:
//
// Authors:
//   Christian Hergert <christian.hergert@gmail.com>
//   Todd Berman <tberman@off.net>
//   John Luke <john.luke@gmail.com>
//
// Copyright (C) 2005, Christian Hergert
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// Software), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace MonoDevelop.Ide.Gui
{
	internal class StartupInfo
	{
		List<FileOpenInformation> requestedFileList = new List<FileOpenInformation> ();
		List<string> parameterList = new List<string> ();

		public IList<string> ParameterList {
			get { return parameterList; }
		}
		
		public IEnumerable<FileOpenInformation> RequestedFileList {
			get { return requestedFileList; }
		}
		
		public bool HasFiles {
			get { return requestedFileList.Count > 0; }
		}

		public bool HasSolutionFile {
			get {
				return requestedFileList.Any (f => File.Exists (f.FileName) && (Services.ProjectService.IsWorkspaceItemFile (f.FileName) || Services.ProjectService.IsSolutionItemFile (f.FileName)));
			}
		}

		/// <summary>
		/// Set to true if a project was opened on startup.
		/// </summary>
		internal bool OpenedRecentProject { get; set; }

		/// <summary>
		/// Set to true if files were opened on startup.
		/// </summary>
		internal bool OpenedFiles { get; set; }
		
		/// <summary>
		/// Matches a filename string with optional line and column 
		/// (/foo/bar/blah.cs;22;31)
		/// </summary>
		public static readonly Regex FileExpression = new Regex (@"^(?<filename>[^;]+)(;(?<line>\d+))?(;(?<column>\d+))?$", RegexOptions.Compiled);
		
		public StartupInfo (IEnumerable<string> args)
		{
			foreach (string arg in args) {
				string a = arg;
				Match fileMatch = FileExpression.Match (a);
				
				// this does not yet work with relative paths
				if (a[0] == '~') {
					var sf = MonoDevelop.Core.Platform.IsWindows ? Environment.SpecialFolder.UserProfile : Environment.SpecialFolder.Personal;
					a = Path.Combine (Environment.GetFolderPath (sf), a.Substring (1));
				}

				if (fileMatch != null && fileMatch.Success) {
					string filename = fileMatch.Groups["filename"].Value;
					if (File.Exists (filename)) {
						int line = 1, column = 1;
						filename = Path.GetFullPath (filename);
						if (fileMatch.Groups["line"].Success)
							int.TryParse (fileMatch.Groups["line"].Value, out line);
						if (fileMatch.Groups["column"].Success)
							int.TryParse (fileMatch.Groups["column"].Value, out column);
						var file = new FileOpenInformation (filename, null, line, column, OpenDocumentOptions.Default);
						requestedFileList.Add (file);
					}
				} else if (a[0] == '-' || a[0] == '/') {
					int markerLength = 1;
					
					if (a.Length >= 2 && a[0] == '-' && a[1] == '-') {
						markerLength = 2;
					}
					
					parameterList.Add(a.Substring (markerLength));
				}
			}
		}
	}
}
