//
// ProjectFile.cs
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
using System.IO;

namespace MonoDevelop.Ide.Projects
{
	public enum FileType {
		None,
		Compile,
		EmbeddedResource,
		Resource,
		Content,
		Folder
	}
	
	public class ProjectFile : ProjectItem
	{
		FileType fileType;
		
		public FileType FileType {
			get { return this.fileType; }
			set { this.fileType = value; }
		}
		
		public string FullPath {
			get {
				return Path.GetFullPath (Path.Combine (Project.BasePath, Include.Replace ('\\', Path.DirectorySeparatorChar)));
			}
		}
		
		public string Name {
			get {
				return Path.GetFileName (Include);
			}
		}
		
		protected override string Tag {
			get {
				return this.FileType.ToString ();
			}
		}
		
		public static FileType GetFileType (string str)
		{
			try {
				Console.WriteLine ((FileType)Enum.Parse (typeof(FileType), str));
				return (FileType)Enum.Parse (typeof(FileType), str);
			} catch (Exception) {
				return FileType.None;
			}
		}
		
		public string DependentUpon {
			get {
				return base.GetMetadata ("DependentUpon");
			}
			set {
				base.SetMetadata ("DependentUpon", value);
			}
		}
		
		public string SubType {
			get {
				return base.GetMetadata ("SubType");
			}
			set {
				base.SetMetadata ("SubType", value);
			}
		}
		
//		Never,
//		Always,
//		PreserveNewest
		public string CopyToOutputDirectory {
			get {
				return base.GetMetadata ("CopyToOutputDirectory");
			}
			set {
				base.SetMetadata ("CopyToOutputDirectory", value);
			}
		}
		
		public string CustomTool {
			get {
				return base.GetMetadata ("Generator");
			}
			set {
				base.SetMetadata ("Generator", value);
			}
		}
		
		public string CustomToolNamespace {
			get {
				return base.GetMetadata ("CustomToolNamespace");
			}
			set {
				base.SetMetadata ("CustomToolNamespace", value);
			}
		}
		
		public ProjectFile (FileType fileType)
		{
			this.FileType = fileType;
		}
		
		public ProjectFile (string fileName, FileType fileType) : base (fileName)
		{
			this.FileType = fileType;
		}
	}
}
