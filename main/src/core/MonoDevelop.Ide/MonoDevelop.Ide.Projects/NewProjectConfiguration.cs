//
// NewProjectConfiguration.cs
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
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects
{
	public class NewProjectConfiguration
	{
		string projectName = String.Empty;
		string errorMessage;

		public NewProjectConfiguration ()
		{
			SolutionName = String.Empty;
			Location = String.Empty;

			CreateProjectDirectoryInsideSolutionDirectory = true;

			CreateGitIgnoreFile = true;
			UseGit = false;

			Parameters = new ProjectCreateParameters ();
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
		public bool IsNewSolutionWithoutProjects { get; set; }

		public bool UseGit { get; set; }
		public bool CreateGitIgnoreFile { get; set; }

		public string SolutionLocation {
			get {
				if (CreateSeparateSolutionDirectory)
					return Path.Combine (Location, GetValidDir (SolutionName));
				else if (CreateSolution)
					return Path.Combine (Location, GetValidDir (ProjectName));
				else
					return Location;
			}
		}

		public string ProjectLocation {
			get {
				if (IsNewSolutionWithoutProjects) {
					return SolutionLocation;
				}

				string path = Location;
				if (CreateSeparateSolutionDirectory)
					path = Path.Combine (path, GetValidDir (SolutionName));

				if (CreateSeparateProjectDirectory)
					return Path.Combine (path, GetValidDir (ProjectName));

				return path;
			}
		}

		static string GetValidDir (string name)
		{
			name = name.Trim ();
			var sb = new StringBuilder ();
			for (int n = 0; n < name.Length; n++) {
				char c = name [n];
				if (Array.IndexOf (FilePath.GetInvalidPathChars (), c) != -1)
					continue;
				if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || c == Path.VolumeSeparatorChar)
					continue;
				sb.Append (c);
			}
			return sb.ToString ();
		}

		public bool IsValid ()
		{
			return !HasErrors ();
		}

		bool HasErrors ()
		{
			errorMessage = String.Empty;

			string solution = SolutionName;
			string name     = ProjectName;

			if (!FileService.IsValidPath (Location)) {
				errorMessage = GettextCatalog.GetString ("Illegal characters used in location.");
				return true;
			}

			if (CreateSolution && !IsValidSolutionName (solution)) {
				errorMessage = GettextCatalog.GetString ("Illegal solution name.\nOnly use letters, digits, '.' or '_'.");
				return true;
			} else if (IsNewSolutionWithoutProjects) {
				return false;
			}

			if (!IsValidProjectName (name)) {
				errorMessage = GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, '.' or '_'.");
				return true;
			}

			if (!FileService.IsValidPath (ProjectLocation)) {
				errorMessage = GettextCatalog.GetString ("Illegal characters used in project location.");
				return true;
			}

			return false;
		}

		internal string GetErrorMessage ()
		{
			if (errorMessage == null) {
				HasErrors ();
			}

			return errorMessage;
		}

		public static bool IsValidProjectName (string name)
		{
			return IsValidSolutionName (name) &&
				name.IndexOf (' ') < 0;
		}

		public static bool IsValidSolutionName (string name)
		{
			return FileService.IsValidPath (name) &&
				FileService.IsValidFileName (name) &&
				HasValidProjectNameCharacters (name);
		}

		static bool HasValidProjectNameCharacters (string name)
		{
			foreach (char c in name) {
				if (!IsValidProjectNameCharacter (c))
					return false;
			}
			return true;
		}

		static bool IsValidProjectNameCharacter (char c)
		{
			return Char.IsLetterOrDigit (c) || c == '.' || c == '_' || c == '-';
		}

		bool CreateSeparateSolutionDirectory {
			get {
				return (CreateSolution && CreateProjectDirectoryInsideSolutionDirectory) ||
					IsNewSolutionWithoutProjects;
			}
		}

		bool CreateSeparateProjectDirectory {
			get { return CreateProjectDirectoryInsideSolutionDirectory || CreateSolution; }
		}

		public ProjectCreateParameters Parameters { get; private set; }

		public static string GenerateValidProjectName (string name)
		{
			string validName = GetValidDir (name);
			return validName.Replace (" ", String.Empty);
		}
	}
}

