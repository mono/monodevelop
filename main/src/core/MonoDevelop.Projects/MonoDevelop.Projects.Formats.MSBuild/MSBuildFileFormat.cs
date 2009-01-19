// MSBuildFileFormat.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class MSBuildFileFormat: IFileFormat
	{
		SlnFileFormat slnFileFormat = new SlnFileFormat ();
		string productVersion;
		string toolsVersion;
		string slnVersion;
		string productDescription;
		string[] frameworkVersions;
		
		public MSBuildFileFormat (string productVersion, string toolsVersion, string slnVersion, string productDescription, string[] frameworkVersions)
		{
			this.productVersion = productVersion;
			this.toolsVersion = toolsVersion;
			this.slnVersion = slnVersion;
			this.productDescription = productDescription;
			this.frameworkVersions = frameworkVersions;
		}
		
		public string Name {
			get {
				return "MSBuild";
			}
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			if (slnFileFormat.CanWriteFile (obj, this))
				return slnFileFormat.GetValidFormatName (obj, fileName, this);
			else {
				ItemTypeNode node = MSBuildProjectService.FindHandlerForItem ((SolutionEntityItem)obj);
				if (node != null)
					return Path.ChangeExtension (fileName, "." + node.Extension);
				else
					return Path.ChangeExtension (fileName, ".mdproj");
			}
		}

		public bool CanReadFile (string file, Type expectedType)
		{
			if (expectedType.IsAssignableFrom (typeof(Solution)) && slnFileFormat.CanReadFile (file, this))
				return true;
			else if (expectedType.IsAssignableFrom (typeof(SolutionEntityItem))) {
				ItemTypeNode node = MSBuildProjectService.FindHandlerForFile (file);
				if (node == null)
					return false;
				return toolsVersion == ReadToolsVersion (file);
			}
			return false;
		}

		public bool CanWriteFile (object obj)
		{
			if (slnFileFormat.CanWriteFile (obj, this))
				return true;
			else if (obj is SolutionEntityItem) {
				ItemTypeNode node = MSBuildProjectService.FindHandlerForItem ((SolutionEntityItem)obj);
				return node != null;
			} else
				return false;
		}

		public void WriteFile (string file, object obj, MonoDevelop.Core.IProgressMonitor monitor)
		{
			if (slnFileFormat.CanWriteFile (obj, this)) {
				slnFileFormat.WriteFile (file, obj, this, monitor);
			} else {
				SolutionEntityItem item = (SolutionEntityItem) obj;
				if (!(item.ItemHandler is MSBuildProjectHandler))
					MSBuildProjectService.InitializeItemHandler (item);
				MSBuildProjectHandler handler = (MSBuildProjectHandler) item.ItemHandler;
				handler.SetTargetFormat (this);
				handler.Save (monitor);
			}
		}

		public object ReadFile (string file, Type expectedType, MonoDevelop.Core.IProgressMonitor monitor)
		{
			if (slnFileFormat.CanReadFile (file, this))
				return slnFileFormat.ReadFile (file, this, monitor);
			else
				return MSBuildProjectService.LoadItem (monitor, file, null, null);
		}

		public List<string> GetItemFiles (object obj)
		{
			return new List<string> ();
		}

		public void InitializeSolutionItem (SolutionItem item)
		{
		}

		public void ConvertToFormat (object obj)
		{
			if (obj == null)
				return;
			
			MSBuildHandler handler;
			SolutionItem item = obj as SolutionItem;
			if (item != null) {
				handler = item.GetItemHandler() as MSBuildHandler;
				if (handler != null) {
					handler.SetTargetFormat (this);
					return;
				}
			}
			
			MSBuildProjectService.InitializeItemHandler (item);
			handler = (MSBuildHandler) item.ItemHandler;
			handler.SetTargetFormat (this);
		}
		
		public bool SupportsMixedFormats {
			get { return false; }
		}

		public string ToolsVersion {
			get {
				return toolsVersion;
			}
		}

		public string SlnVersion {
			get {
				return slnVersion;
			}
		}

		public string ProductVersion {
			get {
				return productVersion;
			}
		}

		public string ProductDescription {
			get {
				return productDescription;
			}
		}

		public string[] FrameworkVersions {
			get {
				return frameworkVersions;
			}
		}

		string ReadToolsVersion (string file)
		{
			try {
				using (XmlTextReader tr = new XmlTextReader (new StreamReader (file))) {
					if (tr.MoveToContent () == XmlNodeType.Element)
						return tr.GetAttribute ("ToolsVersion");
				}
			} catch {
				// Ignore
			}
			return string.Empty;
		}

		public virtual IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			DotNetProject prj = obj as DotNetProject;
			if (!((IList)frameworkVersions).Contains (prj.TargetFramework.Id))
				yield return GettextCatalog.GetString ("The project '{0}' is being saved using the file format '{1}', but this version of Visual Studio does not support the framework that the project is targetting ({2})", prj.Name, productDescription, prj.TargetFramework.Name);
		}
	}
	
	class MSBuildFileFormatVS05: MSBuildFileFormat
	{
		public const string Version = "8.0.50727";
		const string toolsVersion = "2.0";
		const string slnVersion = "9.00";
		const string productComment = "Visual Studio 2005";
		static string[] frameworkVersions = { "2.0" };
		
		public MSBuildFileFormatVS05 (): base (Version, toolsVersion, slnVersion, productComment, frameworkVersions)
		{
		}
	}
	
	class MSBuildFileFormatVS08: MSBuildFileFormat
	{
		public const string Version = "9.0.21022";
		const string toolsVersion = "3.5";
		const string slnVersion = "10.00";
		const string productComment = "Visual Studio 2008";
		static string[] frameworkVersions = { "2.0", "3.0", "3.5" };
		
		public MSBuildFileFormatVS08 (): base (Version, toolsVersion, slnVersion, productComment, frameworkVersions)
		{
		}
	}
	
	class MSBuildFileFormatVS10: MSBuildFileFormat
	{
		public const string Version = "10.0.0";
		const string toolsVersion = "4.0";
		const string slnVersion = "11.00";
		const string productComment = "Visual Studio 2010";
		static string[] frameworkVersions = { "2.0", "3.0", "3.5", "4.0" };
		
		public MSBuildFileFormatVS10 (): base (Version, toolsVersion, slnVersion, productComment, frameworkVersions)
		{
		}
	}
}
