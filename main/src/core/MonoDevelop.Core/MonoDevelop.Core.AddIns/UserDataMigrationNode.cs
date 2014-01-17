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
		#pragma warning disable 649

		[NodeAttribute (Required=true, Description="The version of the source profile for this migration applies.")]
		string sourceVersion;
		
		[NodeAttribute (Required=true, Description="The relative path of the data")]
		string path;
		
		[NodeAttribute (Description="The relative path of the target, if it differs from the source")]
		string targetPath;
		
		[NodeAttribute (Required=true, Description="The kind of the data to be migrated")]
		UserDataKind kind;
		
		[NodeAttribute (Description="The kind of the target, if it differs from the source")]
		string targetKind;
		
		[NodeAttribute (Name="handler")]
		string handlerTypeName;

		#pragma warning restore 649
		
		public string SourceVersion {
			get { return sourceVersion; }
		}
		
		public FilePath SourcePath {
			get { return FileService.MakePathSeparatorsNative (path); }
		}
		
		public FilePath TargetPath {
			get {
				return FileService.MakePathSeparatorsNative (
					string.IsNullOrEmpty (targetPath)? path : targetPath);
			}
		}
		
		public UserDataKind SourceKind {
			get { return kind; }
		}
		
		public UserDataKind TargetKind {
			get {
				return string.IsNullOrEmpty (targetKind)
					? kind
					: (UserDataKind) Enum.Parse (typeof (UserDataKind), targetKind);
			}
		}
		
		public IUserDataMigrationHandler GetHandler ()
		{
			if (string.IsNullOrEmpty (handlerTypeName))
				return null;
			return (IUserDataMigrationHandler) Activator.CreateInstance (Addin.GetType (handlerTypeName));
		}
	}
}