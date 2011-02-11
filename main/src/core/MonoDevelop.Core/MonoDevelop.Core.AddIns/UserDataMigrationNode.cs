// 
// UserDataMigrationNode.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Core.AddIns
{
	class UserDataMigrationNode: ExtensionNode
	{
		[NodeAttribute (Required=true)]
		string sourceVersion;
		
		[NodeAttribute (Required=true)]
		string sourcePath;
		
		[NodeAttribute ()]
		string targetPath;
		
		[NodeAttribute (Required=true)]
		UserDataKind kind;
		
		[NodeAttribute (Name="handler")]
		string handlerTypeName;
		
		public string SourceVersion {
			get { return sourceVersion; }
		}
		
		public FilePath SourcePath {
			get { return FileService.MakePathSeparatorsNative (sourcePath); }
		}
		
		public FilePath TargetPath {
			get {
				return FileService.MakePathSeparatorsNative (
					string.IsNullOrEmpty (targetPath)? sourcePath : targetPath);
			}
		}
		
		public UserDataKind Kind {
			get { return kind; }
		}
		
		public IUserDataMigrationHandler GetHandler ()
		{
			if (string.IsNullOrEmpty (handlerTypeName))
				return null;
			return (IUserDataMigrationHandler) Activator.CreateInstance (Addin.GetType (handlerTypeName));
		}
	}
}

