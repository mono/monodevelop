//
// IProject.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;


using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects
{
	public interface IProject
	{
		/// <summary>This is the language identifier to 
		/// identify the backend binding for the project.</summary>
		string Language {
			get;
		}
		string FileName {
			get;
			set;
		}
		
		string Name {
			get;
		}
		
		string Guid {
			get;
		}
		
		string BasePath {
			get;
		}
		
		System.Collections.ObjectModel.ReadOnlyCollection<ProjectItem> Items {
			get;
		}
		
		string Configuration {
			get;
			set;
		}
		
		string Platform {
			get;
			set;
		}
		
		void Save ();
		
		bool IsFileInProject (string fileName);
		ProjectFile GetFile (string fileName);
		
		void Add (ProjectItem item);
		void Remove (ProjectItem item);
		
		event EventHandler<RenameEventArgs> NameChanged;
		event EventHandler<ProjectItemEventArgs> ItemAdded;
		event EventHandler<ProjectItemEventArgs> ItemRemoved;
		
		event EventHandler ConfigurationChanged;
		event EventHandler PlatformChanged;
	}
}
