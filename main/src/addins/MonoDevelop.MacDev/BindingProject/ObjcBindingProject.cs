// 
// ObjcBindingProject.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using System.Xml;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Assemblies;

using MonoDevelop.MacDev.NativeReferences;

namespace MonoDevelop.MacDev.BindingProject
{
	static class ObjcBindingBuildAction
	{
		public static readonly string ApiDefinition = "ObjcBindingApiDefinition";
		public static readonly string CoreSource = "ObjcBindingCoreSource";
		public static readonly string NativeLibrary = "ObjcBindingNativeLibrary";
	}
	
	public class ObjcBindingProject : DotNetProject
	{
		public ObjcBindingProject ()
		{
		}
		
		public ObjcBindingProject (string languageName)
			: base (languageName)
		{
		}
		
		public ObjcBindingProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
		}
		
		public override bool IsLibraryBasedProjectType {
			get { return true; }
		}
		
		public override string ProjectType {
			get { return "ObjcBinding"; }
		}
		
		protected override IList<string> GetCommonBuildActions ()
		{
			return new string[] {
				BuildAction.Compile,
				ObjcBindingBuildAction.ApiDefinition,
				ObjcBindingBuildAction.CoreSource,
				ObjcBindingBuildAction.NativeLibrary,
				BuildAction.None,
			};
		}
		
		public override string GetDefaultBuildAction (string fileName)
		{
			// If the file extension is ".a", then it is a NativeLibrary.
			if (fileName.EndsWith (".a"))
				return ObjcBindingBuildAction.NativeLibrary;
			
			var baseAction = base.GetDefaultBuildAction (fileName);
			if (baseAction != BuildAction.Compile)
				return baseAction;
			
			// If the base default BuildAction is Compile, then it's a source file... which means that it can actually
			// be any one of: Compile, CoreSource (enum & struct defs), or an ApiDefinition (although we can fairly
			// safely assume that it's not an ApiDefinition because btouch will only allow one of those, and that will
			// have been created by the template).
			
			return baseAction;
		}
		
		public override bool SupportsFormat (FileFormat format)
		{
			return format.Id == "MSBuild10";
		}
		
		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new ObjcBindingProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));
			return conf;
		}
	}
}
