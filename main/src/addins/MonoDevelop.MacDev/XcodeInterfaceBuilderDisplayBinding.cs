// 
// XcodeInterfaceBuilderDisplayBinding.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.MacDev.XcodeSyncing;

namespace MonoDevelop.MacDev
{
	public class XcodeInterfaceBuilderDisplayBinding : IExternalDisplayBinding
	{
		public DesktopApplication GetApplication (FilePath fileName, string mimeType, Project ownerProject)
		{
			return new XcodeInterfaceBuilderDesktopApplication ((IXcodeTrackedProject)ownerProject);
		}
		
		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			if (!AppleSdkSettings.IsXcode4)
				return false;
			if (fileName.IsNullOrEmpty)
				return false;
			var xcp = ownerProject as IXcodeTrackedProject;
			if (xcp == null)
				return false;
			return xcp.XcodeProjectTracker.ShouldOpenInXcode (fileName);
		}

		public bool CanUseAsDefault {
			get { return true; }
		}
	}
	
	class XcodeInterfaceBuilderDesktopApplication : DesktopApplication
	{
		IXcodeTrackedProject project;
		
		public XcodeInterfaceBuilderDesktopApplication (IXcodeTrackedProject project)
			: base (AppleSdkSettings.XcodePath, "Xcode Interface Builder", true)
		{
			this.project = project;
		}
		
		public override void Launch (params string[] files)
		{
			project.XcodeProjectTracker.OpenDocument (files[0]);
		}
	}
}