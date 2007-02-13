using MonoDevelop.Core;
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

		public string GetValidFormatName (string fileName)
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
			return obj is DotNetProject;
		}
		
		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
		}

		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			//if (!CanReadFile (fileName))

			int choice = IdeApp.Services.MessageService.ShowCustomDialog (GettextCatalog.GetString ("Conversion required"),
				GettextCatalog.GetString (
					"The solution file {0} is a VS2003 solution. It must be converted to either a MonoDevelop " + 
					"or a VS2005 solution. Converting to VS2005 format will overwrite existing files. Convert ?", fileName),
					"MonoDevelop", "VS2005", "Cancel");
		                                                      	
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
				combine = IdeApp.Services.ProjectService.ReadCombineEntry (combine.FileName, monitor) as Combine;
			}

			return combine;
		}

		// Converts a vs2003 solution to a Combine object
		internal static Combine ImportSln (string fileName, bool save)
		{
			SlnMaker slnmaker = new SlnMaker ();
			Combine combine = null;
			try { 
				using (IProgressMonitor m = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (
					true, false, true, false)) {
					combine = slnmaker.MsSlnToCmbxHelper (fileName, m, save);
				}
			} catch (Exception e) {
				Console.WriteLine ("exception while converting : " + e.ToString ());
				throw;
			}

			return combine;
		}

		// Does not save the final combine, useful when this converted
		// combine will be used as a solution folder (saves an extra .sln from being created)
		internal static Combine ImportSlnAsMSBuild (string fileName)
		{
			Combine combine = ImportSln (fileName, false);
			MSBuildSolution.ConvertToMSBuild (combine, false);
			MSBuildSolution.UpdateProjectReferences (combine, true);

			return combine;
		}

		// Utility function to determine the sln file version
		string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			StreamReader reader = new StreamReader(strInSlnFile);
			Regex regex = new Regex(@"Microsoft Visual Studio Solution File, Format Version (\d.\d\d)");
			
			strInput = reader.ReadLine();

			match = regex.Match(strInput);
			if (!match.Success) {
				match = regex.Match (reader.ReadLine ());
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
