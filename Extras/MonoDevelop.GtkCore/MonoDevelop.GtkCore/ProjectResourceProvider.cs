//
// ProjectResourceProvider.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;
using MonoDevelop.Ide.Projects;

namespace MonoDevelop.GtkCore
{
	public class ProjectResourceProvider: MarshalByRefObject, Stetic.IResourceProvider
	{
		IProject project;
		
		public ProjectResourceProvider (IProject project)
		{
			this.project = project;
		}
		
		public Stetic.ResourceInfo[] GetResources ()
		{
			ArrayList list = new ArrayList ();
			foreach (ProjectItem item in project.Items) {
				ProjectFile file = item as ProjectFile;
				if (file == null)
					continue;
				list.Add (new Stetic.ResourceInfo (Path.GetFileName (file.FullPath), file.FullPath));
			}
			return (Stetic.ResourceInfo[]) list.ToArray (typeof(Stetic.ResourceInfo));
		}
		
		public Stream GetResourceStream (string resourceName)
		{
			foreach (ProjectItem item in project.Items) {
				ProjectFile file = item as ProjectFile;
				if (file == null)
					continue;
				if (resourceName == Path.GetFileName (file.FullPath))
					return File.OpenRead (file.FullPath);
			}
			return null;
		}
		
		public Stetic.ResourceInfo AddResource (string fileName)
		{
			project.Add (new ProjectFile (fileName, FileType.EmbeddedResource));
			ProjectService.SaveProject (project);
			return new Stetic.ResourceInfo (Path.GetFileName (fileName), fileName);
		}
		
		public void RemoveResource (string resourceName)
		{
			foreach (ProjectItem item in project.Items) {
				ProjectFile file = item as ProjectFile;
				if (file == null)
					continue;
				if (resourceName == Path.GetFileName (file.FullPath)) {
					project.Remove (file);
					ProjectService.SaveProject (project);
					return;
				}
			}
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
	
}
