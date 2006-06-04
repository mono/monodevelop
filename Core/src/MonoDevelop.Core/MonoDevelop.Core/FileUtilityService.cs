// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core
{
	/// <summary>
	/// A utility class related to file utilities.
	/// </summary>
	public class FileUtilityService : AbstractService
	{
		readonly static char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
		//readonly static char[] dir_sep = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		string sharpDevelopRootPath;
		
		public string SharpDevelopRootPath {
			get {
				return sharpDevelopRootPath;
			}
		}
		
		public FileUtilityService()
		{
			sharpDevelopRootPath = PropertyService.EntryAssemblyDirectory + Path.DirectorySeparatorChar + "..";
		}
		
		public override void UnloadService()
		{
			base.UnloadService();
		}

		public StringCollection SearchDirectory(string directory, string filemask, bool searchSubdirectories)
		{
			StringCollection collection = new StringCollection();
			SearchDirectory(directory, filemask, collection, searchSubdirectories);
			return collection;
		}
		
		public StringCollection SearchDirectory(string directory, string filemask)
		{
			return SearchDirectory(directory, filemask, true);
		}
		
		/// <summary>
		/// Finds all files which are valid to the mask <code>filemask</code> in the path
		/// <code>directory</code> and all subdirectories (if searchSubdirectories
		/// is true. The found files are added to the StringCollection 
		/// <code>collection</code>.
		/// </summary>
		void SearchDirectory (string directory, string filemask, StringCollection collection, bool searchSubdirectories)
		{
			string[] file = Directory.GetFiles(directory, filemask);
			foreach (string f in file) {
				collection.Add(f);
			}
			
			if (searchSubdirectories) {
				string[] dir = Directory.GetDirectories(directory);
				foreach (string d in dir) {
					SearchDirectory(d, filemask, collection, searchSubdirectories);
				}
			}
		}
		
		/// <summary>
		/// Converts a given absolute path and a given base path to a path that leads
		/// from the base path to the absoulte path. (as a relative path)
		/// </summary>
		public string AbsoluteToRelativePath(string baseDirectoryPath, string absPath)
		{
			if (! Path.IsPathRooted (absPath))
				return absPath;
			
			absPath = Path.GetFullPath (absPath);
			baseDirectoryPath = Path.GetFullPath (baseDirectoryPath);
			
			string[] bPath = baseDirectoryPath.Split (separators);
			string[] aPath = absPath.Split (separators);
			int indx = 0;
			for(; indx < Math.Min(bPath.Length, aPath.Length); ++indx){
				if(!bPath[indx].Equals(aPath[indx]))
					break;
			}
			
			if (indx == 0) {
				return absPath;
			}
			
			string erg = "";
			
			if(indx == bPath.Length) {
				erg += "." + Path.DirectorySeparatorChar;
			} else {
				for (int i = indx; i < bPath.Length; ++i) {
					erg += ".." + Path.DirectorySeparatorChar;
				}
			}
			erg += String.Join(Path.DirectorySeparatorChar.ToString(), aPath, indx, aPath.Length-indx);
			
			return erg;
		}
		
		/// <summary>
		/// Converts a given relative path and a given base path to a path that leads
		/// to the relative path absoulte.
		/// </summary>
		public string RelativeToAbsolutePath(string baseDirectoryPath, string relPath)
		{			
			return Path.GetFullPath (baseDirectoryPath + Path.DirectorySeparatorChar + relPath);
		}
		
		/// <summary>
		/// This method checks the file fileName if it is valid.
		/// </summary>
		public bool IsValidFileName(string fileName)
		{
			// Fixme: 260 is the hardcoded maximal length for a path on my Windows XP system
			//        I can't find a .NET property or method for determining this variable.
			if (fileName == null || fileName.Length == 0 || fileName.Length >= 260) {
				return false;
			}
			
			// platform independent : check for invalid path chars
			foreach (char invalidChar in Path.GetInvalidPathChars()) {
				if (fileName.IndexOf(invalidChar) >= 0) {
					return false;
				}
			}
			
			// platform dependend : Check for invalid file names (DOS)
			// this routine checks for follwing bad file names :
			// CON, PRN, AUX, NUL, COM1-9 and LPT1-9
			
			string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
			if (nameWithoutExtension != null) {
				nameWithoutExtension = nameWithoutExtension.ToUpper();
			}
			
			if (nameWithoutExtension == "CON" ||
			    nameWithoutExtension == "PRN" ||
			    nameWithoutExtension == "AUX" ||
			    nameWithoutExtension == "NUL") {
		    	
		    	return false;
		    }
			    
		    char ch = nameWithoutExtension.Length == 4 ? nameWithoutExtension[3] : '\0';
			
			return !((nameWithoutExtension.StartsWith("COM") ||
			          nameWithoutExtension.StartsWith("LPT")) &&
			          Char.IsDigit(ch));
		}
		
		public bool IsDirectory(string filename)
		{
			if (!Directory.Exists(filename)) {
				return false;
			}
			FileAttributes attr = File.GetAttributes(filename);
			return (attr & FileAttributes.Directory) != 0;
		}
		
		/// <summary>
		/// Returns directoryName + "\\" (Win32) when directoryname doesn't end with
		/// "\\"
		/// </summary>
		public string GetDirectoryNameWithSeparator(string directoryName)
		{
			if (directoryName == null) return "";
			
			if (directoryName.EndsWith(Path.DirectorySeparatorChar.ToString())) {
				return directoryName;
			}
			return directoryName + Path.DirectorySeparatorChar;
		}
	}
}
