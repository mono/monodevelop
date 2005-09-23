// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Services
{
	public delegate void FileEventHandler(object sender, FileEventArgs e);
	
	public class FileEventArgs : EventArgs
	{
		string fileName   = null;
		string sourceFile = null;
		string targetFile = null;
		
		bool   isDirectory;
		
		public string FileName {
			get {
				return fileName;
			}
		}
		
		public string SourceFile {
			get {
				return sourceFile;
			}
		}
		
		public string TargetFile {
			get {
				return targetFile;
			}
		}
		
		
		public bool IsDirectory {
			get {
				return isDirectory;
			}
		}
		
		public FileEventArgs(string fileName, bool isDirectory)
		{
			this.fileName = fileName;
			this.isDirectory = isDirectory;
		}
		
		public FileEventArgs(string sourceFile, string targetFile, bool isDirectory)
		{
			this.sourceFile = sourceFile;
			this.targetFile = targetFile;
			this.isDirectory = isDirectory;
		}
	}
}
