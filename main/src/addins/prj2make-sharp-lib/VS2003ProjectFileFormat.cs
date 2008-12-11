//
// VS2003ProjectFileFormat.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
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

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Threading;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Prj2Make
{
	public class VS2003ProjectFileFormat : IFileFormat
	{
		GuiHelper helper;
		
		public VS2003ProjectFileFormat ()
		{
		}

		public string Name {
			get { return "Visual Studio 2003"; }
		}

		GuiHelper GuiHelper {
			get {
				if (helper == null)
					helper = new GuiHelper ();
				return helper;
			}
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			return fileName;
		}

		public bool CanReadFile (string file, Type expectedObjectType)
		{
			if (expectedObjectType.IsAssignableFrom (typeof(Solution)) && String.Compare (Path.GetExtension (file), ".sln", true) == 0) {
				string ver = GetSlnFileVersion (file);
				if (ver == "7.00" || ver == "8.00")
					return true;
			}
			
			if (!expectedObjectType.IsAssignableFrom (typeof(DotNetProject)))
				return false;
			
			if (String.Compare (Path.GetExtension (file), ".csproj", true) != 0 &&
				String.Compare (Path.GetExtension (file), ".vbproj", true) != 0)
				return false;

			try {
				using (XmlReader xr = XmlReader.Create (file)) {
					xr.MoveToContent ();
					if (xr.NodeType == XmlNodeType.Element && String.Compare (xr.LocalName, "VisualStudioProject") == 0)
						return true;
				}
			} catch {
				return false;
			}

			return false;
		}

		public bool CanWriteFile (object obj)
		{
			return false;
		}

		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
		}

		public object ReadFile (string file, Type expectedType, IProgressMonitor monitor)
		{
			if (expectedType.IsAssignableFrom (typeof(DotNetProject)))
				return ReadProjectFile (file, monitor);
			else
				return ReadSolutionFile (file, monitor);
		}

		public object ReadProjectFile (string fileName, IProgressMonitor monitor)
		{
			TargetConvert choice = IdeApp.IsInitialized ? GuiHelper.QueryProjectConversion (fileName) : TargetConvert.None;
			if (choice == TargetConvert.None)
				throw new InvalidOperationException ("VS2003 projects are not supported natively.");

			DotNetProject project = ImportCsproj (fileName);
			project.FileName = fileName;
			
			if (choice == TargetConvert.MonoDevelop) {
				project.FileFormat = IdeApp.Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			} else if (choice == TargetConvert.VisualStudio) {
				project.FileFormat = IdeApp.Services.ProjectService.FileFormats.GetFileFormat ("MSBuild05");
			}

			project.Save (monitor);
			return project;
		}
		
		public object ReadSolutionFile (string fileName, IProgressMonitor monitor)
		{
			TargetConvert choice = IdeApp.IsInitialized ? GuiHelper.QuerySolutionConversion (fileName) : TargetConvert.None;
			if (choice == TargetConvert.None)
				throw new InvalidOperationException ("VS2003 solutions are not supported natively.");
			
			Solution solution = ImportSln (fileName);
			
			if (choice == TargetConvert.MonoDevelop) {
				solution.ConvertToFormat (IdeApp.Services.ProjectService.FileFormats.GetFileFormat ("MD1"), true);
			} else if (choice == TargetConvert.VisualStudio) {
				solution.ConvertToFormat (IdeApp.Services.ProjectService.FileFormats.GetFileFormat ("MSBuild05"), true);
			}
			
			solution.Save (monitor);
			return solution;
		}
		
		internal DotNetProject ImportCsproj (string fileName)
		{
			DotNetProject project = null;
			SlnMaker slnmaker = new SlnMaker ();
			try { 
				using (IProgressMonitor m = GuiHelper.CreateProgressMonitor ()) {
					project = slnmaker.CreatePrjxFromCsproj (fileName, m);
				}
			} catch (Exception e) {
				LoggingService.LogError ("exception while converting: " + e.ToString ());
				throw;
			}

			return project;
		}

		public void ConvertToFormat (object obj)
		{
			throw new NotSupportedException ();
		}

		public List<string> GetItemFiles (object obj)
		{
			return new List<string> ();
		}

		// Converts a vs2003 solution to a Combine object
		internal Solution ImportSln (string fileName)
		{
			SlnMaker slnmaker = new SlnMaker ();
			Solution solution = null;
			IProgressMonitor m = GuiHelper.CreateProgressMonitor ();

			try { 
				solution = slnmaker.MsSlnToCmbxHelper (fileName, m);
			} catch (Exception e) {
				LoggingService.LogError ("exception while converting : " + e.ToString ());
				throw;
			} finally {
				if (m != null)
					m.Dispose ();
			}

			return solution;
		}

		// Utility function to determine the sln file version
		string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			StreamReader reader = new StreamReader(strInSlnFile);
			strInput = reader.ReadLine();

			match = SlnMaker.SlnVersionRegex.Match(strInput);
			if (!match.Success) {
				match = SlnMaker.SlnVersionRegex.Match (reader.ReadLine ());
			}

			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}
		
		public bool SupportsMixedFormats {
			get { return false; }
		}

		public IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			yield break;
		}
	}

	enum TargetConvert
	{
		None,
		VisualStudio,
		MonoDevelop
	}

	class GuiHelper: GuiSyncObject
	{
		TargetConvert QueryConversion (string text)
		{
			AlertButton monodevelop = new AlertButton (GettextCatalog.GetString ("Convert to MonoDevelop"));
			AlertButton vs2005      = new AlertButton (GettextCatalog.GetString ("Convert to Visual Studio 2005"));

			AlertButton choice = MessageService.AskQuestion (text,
			                                                 GettextCatalog.GetString ("Converting to Visual Studio 2005 format will overwrite existing files."),
			                                                 AlertButton.Cancel, vs2005, monodevelop);
			if (choice == monodevelop)
				return TargetConvert.MonoDevelop;
			else if (choice == vs2005)
				return TargetConvert.VisualStudio;
			else
				return TargetConvert.None;
		}
		
		public TargetConvert QueryProjectConversion (string file)
		{
			string text = GettextCatalog.GetString ("The project file {0} is a Visual Studio 2003 project. It must be converted to either a MonoDevelop or a Visual Studio 2005 project.", file);
			return QueryConversion (text);
		}
		
		public TargetConvert QuerySolutionConversion (string file)
		{
			string text = GettextCatalog.GetString ("The solution file {0} is a Visual Studio 2003 solution. It must be converted to either a MonoDevelop or a Visual Studio 2005 project.", file);
			return QueryConversion (text);
		}

		public IProgressMonitor CreateProgressMonitor ()
		{
			return new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (true, false, true, false);
		}
	}
}
