// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Services;

namespace MonoDevelop.Gui.ErrorHandlers
{
	internal class CombineLoadError
	{
		CombineLoadError()
		{
			
		}
		
		public static void HandleError(Exception e, string filename)
		{
			if (e is DirectoryNotFoundException || e is FileNotFoundException) {
				bool isProject = filename.ToLower().EndsWith(".prjx");
				
				string errorMessage = string.Format
					(String.Format (GettextCatalog.GetString ("Could not load the {0} '{1}'.\n\n{2}"),
					 isProject ? "project" : "combine",
					 filename, e.Message));
				
				GenericError.DisplayError(errorMessage);
			} else {
				GenericError.DisplayError(e.ToString());
				throw e;
			}
		}
	}
}
