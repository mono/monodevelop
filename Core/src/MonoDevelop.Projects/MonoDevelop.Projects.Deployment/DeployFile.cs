//
// DeployFile.cs: represents a deployable file, its source path, target 
//    directory ID, and and relative target path. Actual target path is 
//    decided by the deploy handler based on the target directory ID. 
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2006 Michael Hutchinson
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

namespace MonoDevelop.Projects.Deployment
{
	public class DeployFile
	{
		string sourcePath;
		string relativeTargetPath;
		string targetDirectoryID;
		
		public DeployFile (string sourcePath, string relativeTargetPath, string targetDirectoryID)
		{
			this.targetDirectoryID = targetDirectoryID;
			this.sourcePath = sourcePath;
			this.relativeTargetPath = relativeTargetPath;
		}
		
		public DeployFile (string sourcePath, string relativeTargetPath)
		 : this (sourcePath, relativeTargetPath, TargetDirectory.ProgramFiles)
		{
		}
		
		public string SourcePath {
			get { return sourcePath; }
			set { sourcePath = value; }
		}
		
		public string RelativeTargetPath {
			get { return relativeTargetPath; }
			set { relativeTargetPath = value; }
		}
		
		public string TargetDirectoryID {
			get { return targetDirectoryID; }
			set { targetDirectoryID = value; }
		}
	}
	
	public static class TargetDirectory
	{
		public static readonly string ProgramFiles = "ProgramFiles";
		public static readonly string Binaries = "Binaries";
		public static readonly string Gac = "Gac";
		public static readonly string ApplicationData = "ApplicationData";
		public static readonly string CommonApplicationData = "CommonApplicationData";
		public static readonly string Personal = "Personal";
	}
	
	public class DeployFileCollection : List<DeployFile>
	{
	}
}
