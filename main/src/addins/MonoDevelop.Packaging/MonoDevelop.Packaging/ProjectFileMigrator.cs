//
// ProjectFileMigrator.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Collections.Generic;

namespace MonoDevelop.Packaging
{
	/// <summary>
	/// Migrates all files apart from AssemblyInfo.cs from one project to another.
	/// </summary>
	class ProjectFileMigrator
	{
		public void MigrateFiles (Project source, Project target)
		{
			foreach (ProjectFile file in GetFilesToMigrate (source).ToArray ()) {

				FilePath destination = GetFileDestinationPath (file, source, target);

				Directory.CreateDirectory (destination.ParentDirectory);
				FileService.MoveFile (file.FilePath, destination);

				var movedProjectFile = new ProjectFile (destination, file.BuildAction);
				target.AddFile (movedProjectFile);

				source.Files.Remove (file);
			}
		}

		IEnumerable<ProjectFile> GetFilesToMigrate (Project source)
		{
			return source.Files.Where (file => !IsExcluded (file)).ToArray ();
		}

		bool IsExcluded (ProjectFile file)
		{
			return string.Equals (file.FilePath.FileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
		}

		FilePath GetFileDestinationPath (ProjectFile file, Project source, Project target)
		{
			FilePath relativePath = source.GetRelativeChildPath (file.FilePath);
			return target.BaseDirectory.Combine (relativePath);
		}
	}
}
