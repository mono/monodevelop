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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.Extensions;
using System.Threading.Tasks;
using System.Linq;

namespace MonoDevelop.Projects.MSBuild
{
	public abstract class MSBuildFileFormat
	{
		readonly SlnFileFormat slnFileFormat;

		internal MSBuildFileFormat ()
		{
			slnFileFormat = new SlnFileFormat (this);
		}

		public static readonly MSBuildFileFormat VS2005 = new MSBuildFileFormatVS05 ();
		public static readonly MSBuildFileFormat VS2008 = new MSBuildFileFormatVS08 ();
		public static readonly MSBuildFileFormat VS2010 = new MSBuildFileFormatVS10 ();
		public static readonly MSBuildFileFormat VS2012 = new MSBuildFileFormatVS12 ();
		public static readonly MSBuildFileFormat VS2017 = new MSBuildFileFormatVS15 ();

		public static IEnumerable<MSBuildFileFormat> GetSupportedFormats ()
		{
			// Return VS2012 format first since this is the default format used.
			// If VS2017 is returned first then since it uses the same solution file
			// version it would be used instead. This would cause the tools version for
			// new projects added to an existing solution to be changed to 15.0 instead
			// of using 4.0 which is the current default.
			yield return VS2012;
			yield return VS2017;
			yield return VS2010;
			yield return VS2008;
			yield return VS2005;
		}

		public static IEnumerable<MSBuildFileFormat> GetSupportedFormats (IMSBuildFileObject targetItem)
		{
			return GetSupportedFormats ().Where (f => f.CanWriteFile (targetItem));
		}

		public static MSBuildFileFormat DefaultFormat {
			get { return VS2012; }
		}
		
		public string Name {
			get { return "MSBuild"; }
		}

		public abstract Version Version { get; }

		internal SlnFileFormat SlnFileFormat {
			get { return slnFileFormat; }
		}
		
		public bool SupportsMonikers { get { return SupportedFrameworks == null; } }

		public static bool ToolsSupportMonikers (string toolsVersion)
		{
			return new Version (toolsVersion) >= new Version ("4.0");
		}
		
		public bool SupportsFramework (TargetFramework fx)
		{
			return SupportsMonikers || ((IList<TargetFrameworkMoniker>)SupportedFrameworks).Contains (fx.Id);
		}

		internal virtual bool SupportsSlnVersion (string version)
		{
			return version == SlnVersion;
		}

		protected virtual bool SupportsToolsVersion (string version)
		{
			return version == DefaultToolsVersion;
		}

		public FilePath GetValidFormatName (object obj, FilePath fileName)
		{
			if (slnFileFormat.CanWriteFile (obj, this))
				return slnFileFormat.GetValidFormatName (obj, fileName, this);
			else {
				string ext = MSBuildProjectService.GetExtensionForItem ((SolutionItem)obj);
				if (!string.IsNullOrEmpty (ext))
					return fileName.ChangeExtension ("." + ext);
				else
					return fileName;
			}
		}

		internal bool CanReadFile (FilePath file, Type expectedType)
		{
			if (expectedType.IsAssignableFrom (typeof(Solution)) && slnFileFormat.CanReadFile (file, this))
				return true;
			else if (expectedType.IsAssignableFrom (typeof(SolutionItem))) {
				if (!MSBuildProjectService.CanReadFile (file))
					return false;
				//TODO: check ProductVersion first
				return SupportsToolsVersion (ReadToolsVersion (file));
			}
			return false;
		}

		public bool CanWriteFile (object obj)
		{
			if (slnFileFormat.CanWriteFile (obj, this)) {
				Solution sol = (Solution) obj;
				foreach (SolutionItem si in sol.GetAllItems<SolutionItem> ())
					if (!CanWriteFile (si))
						return false;
				return true;
			}
			else if (obj is SolutionItem) {
				DotNetProject p = obj as DotNetProject;
				// Check the framework only if the project is not loading, since otherwise the
				// project may not yet have the framework info set.
				if (p != null && !p.Loading && !SupportsFramework (p.TargetFramework))
					return false;
				
				// This file format can write all types of projects. If there isn't a handler for a project,
				// it will use a generic handler.
				return true;
			} else
				return false;
		}

		public virtual IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			if (obj is Solution) {
				List<string> msg = new List<string> ();
				foreach (SolutionItem si in ((Solution)obj).GetAllItems<SolutionItem> ()) {
					IEnumerable<string> ws = GetCompatibilityWarnings (si);
					if (ws != null)
						msg.AddRange (ws);
				}
				return msg;
			}
			var prj = obj as DotNetProject;
			if (prj != null && !SupportsMonikers && !((IList)SupportedFrameworks).Contains (prj.TargetFramework.Id))
				return new [] { GettextCatalog.GetString (
					"The project '{0}' is being saved using the file format '{1}', but this version of Visual Studio " +
					"does not support the framework that the project is targetting ({2})",
					prj.Name, ProductDescription, prj.TargetFramework.Name)
				};
			return null;
		}

		internal async Task WriteFile (FilePath file, object obj, ProgressMonitor monitor)
		{
			if (slnFileFormat.CanWriteFile (obj, this)) {
				await slnFileFormat.WriteFile (file, obj, true, monitor);
			} else {
				throw new NotSupportedException ();
			}
		}

		internal async Task<object> ReadFile (FilePath file, Type expectedType, MonoDevelop.Core.ProgressMonitor monitor)
		{
			if (slnFileFormat.CanReadFile (file, this))
				return await slnFileFormat.ReadFile (file, monitor);
			else
				throw new NotSupportedException (); 
		}

		public abstract string DefaultToolsVersion { get; }

		public abstract string SlnVersion { get; }

		public virtual string DefaultProductVersion { get { return null; } }

		public virtual string DefaultSchemaVersion { get { return null; } }

		public abstract string ProductDescription { get; }

		public virtual TargetFrameworkMoniker[] SupportedFrameworks {
			get { return null; }
		}

		static string ReadToolsVersion (FilePath file)
		{
			try {
				using (XmlTextReader tr = new XmlTextReader (new StreamReader (file))) {
					if (tr.MoveToContent () == XmlNodeType.Element) {
						if (tr.LocalName != "Project" || tr.NamespaceURI != "http://schemas.microsoft.com/developer/msbuild/2003")
							return string.Empty;
						string tv = tr.GetAttribute ("ToolsVersion");
						if (string.IsNullOrEmpty (tv))
							return "2.0"; // Some old VS versions don't specify the tools version, so assume 2.0
						else
							return tv;
					}
				}
			} catch {
				// Ignore
			}
			return string.Empty;
		}
		
		public abstract string Id { get; }
	}
	
	class MSBuildFileFormatVS05: MSBuildFileFormat
	{
		static readonly TargetFrameworkMoniker[] supportedFrameworks = {
			TargetFrameworkMoniker.NET_2_0,
		};

		public override string Id {
			get { return "MSBuild05"; }
		}

		public override Version Version {
			get { return new Version ("2005"); }
		}

		public override string DefaultProductVersion {
			get { return "8.0.50727"; }
		}

		public override string DefaultToolsVersion {
			get { return "2.0"; }
		}

		public override string DefaultSchemaVersion {
			get { return "2.0"; }
		}

		public override string SlnVersion {
			get { return "9.00"; }
		}

		public override string ProductDescription {
			get { return "Visual Studio 2005"; }
		}

		public override TargetFrameworkMoniker[] SupportedFrameworks {
			get { return supportedFrameworks; }
		}
	}
	
	class MSBuildFileFormatVS08: MSBuildFileFormat
	{
		static readonly TargetFrameworkMoniker[] supportedFrameworks = {
			TargetFrameworkMoniker.NET_2_0,
			TargetFrameworkMoniker.NET_3_0,
			TargetFrameworkMoniker.NET_3_5,
			TargetFrameworkMoniker.SL_2_0,
			TargetFrameworkMoniker.SL_3_0,
			TargetFrameworkMoniker.MONOTOUCH_1_0,
		};

		public override string Id {
			get { return "MSBuild08"; }
		}

		public override Version Version {
			get { return new Version ("2008"); }
		}

		public override string DefaultProductVersion {
			get { return "9.0.21022"; }
		}

		public override string DefaultToolsVersion {
			get { return "3.5"; }
		}

		public override string DefaultSchemaVersion {
			get { return "2.0"; }
		}

		public override string SlnVersion {
			get { return "10.00"; }
		}

		public override string ProductDescription {
			get { return "Visual Studio 2008"; }
		}

		public override TargetFrameworkMoniker[] SupportedFrameworks {
			get { return supportedFrameworks; }
		}
	}
	
	class MSBuildFileFormatVS10: MSBuildFileFormat
	{
		public override string Id {
			get { return "MSBuild10"; }
		}

		public override Version Version {
			get { return new Version ("2010"); }
		}

		//WTF VS
		public override string DefaultProductVersion {
			get { return "8.0.30703"; }
		}

		public override string DefaultSchemaVersion {
			get { return "2.0"; }
		}

		public override string DefaultToolsVersion {
			get { return "4.0"; }
		}

		public override string SlnVersion {
			get { return "11.00"; }
		}

		public override string ProductDescription {
			get { return "Visual Studio 2010"; }
		}
	}

	// this is actually VS2010 SP1 and later
	class MSBuildFileFormatVS12: MSBuildFileFormat
	{
		public override string Id {
			get { return "MSBuild12"; }
		}

		public override Version Version {
			get { return new Version ("2012"); }
		}

		public override string DefaultToolsVersion {
			get { return "4.0"; }
		}

		public override string SlnVersion {
			get { return "12.00"; }
		}

		public override string ProductDescription {
			get { return "Visual Studio 2012"; }
		}

		protected override bool SupportsToolsVersion (string version)
		{
			return version == "4.0" || version == "12.0" || version == "14.0";
		}
	}

	class MSBuildFileFormatVS15: MSBuildFileFormat
	{
		public override string Id {
			get { return "MSBuild15"; }
		}

		public override Version Version {
			get { return new Version ("2017"); }
		}

		public override string DefaultToolsVersion {
			get { return "15.0"; }
		}

		public override string SlnVersion {
			get { return "12.00"; }
		}

		public override string ProductDescription {
			get { return "Visual Studio 2017"; }
		}

		protected override bool SupportsToolsVersion (string version)
		{
			return version == "15.0";
		}
	}
}
