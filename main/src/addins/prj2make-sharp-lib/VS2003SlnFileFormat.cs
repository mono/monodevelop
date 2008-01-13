//
// VS2003SlnFileFormat.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MonoDevelop.Prj2Make
{
	public class VS2003SlnFileFormat : IFileFormat
	{
		public VS2003SlnFileFormat ()
		{
		}

		public string Name {
			get { return "VS2003 solution"; }
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			return fileName;
		}

		public bool CanReadFile (string file)
		{
			if (String.Compare (Path.GetExtension (file), ".sln", true) == 0) {
				string ver = GetSlnFileVersion (file);
				if (ver == "7.00" || ver == "8.00")
					return true;
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

		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
		{
			return null;
		}
		
		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			//if (!CanReadFile (fileName))

			int choice;
			if (IdeApp.Services == null) {
				// HACK, for mdtool
				choice = 0;
			} else {
				choice = IdeApp.Services.MessageService.ShowCustomDialog (GettextCatalog.GetString ("Conversion required"),
					GettextCatalog.GetString (
							"The solution file {0} is a VS2003 solution. It must be converted to either a MonoDevelop " + 
							"or a VS2005 solution. Converting to VS2005 format will overwrite existing files. Convert ?", fileName),
							"MonoDevelop", "VS2005", "Cancel");
			}
		                                                      	
			if (choice == 2)
				throw new InvalidOperationException ("VS2003 solutions are not supported natively.");

			Combine combine = null;
			if (choice == 0) {
				// Convert to MD solution
				combine = ImportSln (fileName, true);
			} else if (choice == 1) {
				// Convert to vs2005 solution
				combine = ImportSlnAsMSBuild (fileName);
				combine.Save (monitor);

				// Re-read to get a MSBuildSolution object
				combine = Services.ProjectService.ReadCombineEntry (combine.FileName, monitor) as Combine;
			}

			return combine;
		}

		// Converts a vs2003 solution to a Combine object
		internal static Combine ImportSln (string fileName, bool save)
		{
			SlnMaker slnmaker = new SlnMaker ();
			Combine combine = null;
			IProgressMonitor m;
			if (IdeApp.Services == null)
				m = new ConsoleProgressMonitor ();
			else
				m = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (
							true, false, true, false);

			try { 
				combine = slnmaker.MsSlnToCmbxHelper (fileName, m, save);
			} catch (Exception e) {
				LoggingService.LogError ("exception while converting : " + e.ToString ());
				throw;
			} finally {
				if (m != null)
					m.Dispose ();
			}

			return combine;
		}

		// Does not save the final combine, useful when this converted
		// combine will be used as a solution folder (saves an extra .sln from being created)
		internal static Combine ImportSlnAsMSBuild (string fileName)
		{
			Combine combine = ImportSln (fileName, false);
			SlnFileFormat.ConvertToMSBuild (combine);
			combine.Save (new NullProgressMonitor ());

			return combine;
		}

		// Utility function to determine the sln file version
		string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			StreamReader reader = new StreamReader(strInSlnFile);
			strInput = reader.ReadLine();

			match = SlnFileFormat.SlnVersionRegex.Match(strInput);
			if (!match.Success) {
				match = SlnFileFormat.SlnVersionRegex.Match (reader.ReadLine ());
			}

			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			return strVersion;
		}

	}
}
