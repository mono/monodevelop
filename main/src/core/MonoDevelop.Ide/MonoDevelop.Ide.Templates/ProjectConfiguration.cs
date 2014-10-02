//
// ProjectConfiguration.cs
//
// Author:
//       Todd Berman  <tberman@off.net>
//       Lluis Sanchez Gual <lluis@novell.com>
//       Viktoria Dudka  <viktoriad@remobjects.com>
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
//

using System;
using System.IO;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	public class ProjectConfiguration
	{
		string projectName = String.Empty;

		public ProjectConfiguration ()
		{
			SolutionName = String.Empty;
			Location = String.Empty;
			ProjectFileExtension = String.Empty;

			CreateProjectDirectoryInsideSolutionDirectory = true;

			CreateGitIgnoreFile = true;
			UseGit = true;
		}

		public string ProjectName {
			get { return projectName; }
			set {
				if (CreateSolution && (projectName == SolutionName)) {
					SolutionName = value;
				}
				projectName = value;
			}
		}

		public string ProjectFileExtension { get; set; }
		public string SolutionName { get; set; }
		public string Location { get; set; }

		public string GetValidProjectName ()
		{
			return GetValidDir (ProjectName);
		}

		public string GetValidSolutionName ()
		{
			return GetValidDir (SolutionName);
		}

		public bool CreateProjectDirectoryInsideSolutionDirectory { get; set; }
		public bool CreateSolution { get; set; }

		public bool UseGit { get; set; }
		public bool CreateGitIgnoreFile { get; set; }

		public string SolutionLocation {
			get {
				if (CreateSeparateSolutionDirectory)
					return Path.Combine (Location, GetValidDir (SolutionName));
				else
					return Path.Combine (Location, GetValidDir (ProjectName));
			}
		}

		public string ProjectLocation {
			get {
				string path = Location;
				if (CreateSeparateSolutionDirectory)
					path = Path.Combine (path, GetValidDir (SolutionName));

				if (CreateSeparateProjectDirectory)
					return Path.Combine (path, GetValidDir (ProjectName));

				return path;
			}
		}

		string GetValidDir (string name)
		{
			name = name.Trim ();
			var sb = new StringBuilder ();
			for (int n = 0; n < name.Length; n++) {
				char c = name [n];
				if (Array.IndexOf (Path.GetInvalidPathChars(), c) != -1)
					continue;
				if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || c == Path.VolumeSeparatorChar)
					continue;
				sb.Append (c);
			}
			return sb.ToString ();
		}

		public bool IsValid ()
		{
			string solution = SolutionName;
			string name     = ProjectName;
			string location = ProjectLocation;

			if (solution.Equals ("")) solution = name; //This was empty when adding after first combine

			return !((CreateSeparateSolutionDirectory && !FileService.IsValidPath (solution)) ||
				!FileService.IsValidFileName (name) ||
				name.IndexOf (' ') >= 0 ||
				!FileService.IsValidPath (location));
		}

		bool CreateSeparateSolutionDirectory {
			get { return CreateSolution && CreateProjectDirectoryInsideSolutionDirectory; }
		}

		bool CreateSeparateProjectDirectory {
			get { return CreateProjectDirectoryInsideSolutionDirectory || CreateSolution; }
		}
	}
}

