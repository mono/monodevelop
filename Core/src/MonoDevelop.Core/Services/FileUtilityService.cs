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

using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.Core.Services
{
	public enum FileErrorPolicy {
		Inform,
		ProvideAlternative
	}
	
	public enum FileOperationResult {
		OK,
		Failed,
		SavedAlternatively
	}
	
	public delegate void FileOperationDelegate();
	
	public delegate void NamedFileOperationDelegate(string fileName);
	
	/// <summary>
	/// A utility class related to file utilities.
	/// </summary>
	public class FileUtilityService : AbstractService
	{
		readonly static char[] separators = { Path.DirectorySeparatorChar, Path.VolumeSeparatorChar, Path.AltDirectorySeparatorChar };
		readonly static char[] dir_sep = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		string sharpDevelopRootPath;
		
		public string SharpDevelopRootPath {
			get {
				return sharpDevelopRootPath;
			}
		}
		
		public FileUtilityService()
		{
			sharpDevelopRootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "..";
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
		void SearchDirectory(string directory, string filemask, StringCollection collection, bool searchSubdirectories)
		{
			try {
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
			} catch (Exception e) {
				IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
				messageService.ShowError(e, "Can't access directory " + directory);
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
			
			// platform independend : check for invalid path chars
			foreach (char invalidChar in Path.InvalidPathChars) {
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
		
		public bool TestFileExists(string filename)
		{
			if (!File.Exists(filename)) {
				IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
				messageService.ShowWarning(String.Format (GettextCatalog.GetString ("Can't find file {0}"), filename));
				return false;
			}
			return true;
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
		
		// Observe SAVE functions
		public FileOperationResult ObservedSave(FileOperationDelegate saveFile, string fileName, string message, FileErrorPolicy policy)
		{
			Debug.Assert(IsValidFileName(fileName));
#if false			
			try {
				saveFile();
				return FileOperationResult.OK;
			} catch (Exception e) {
				switch (policy) {
					case FileErrorPolicy.Inform:
						using (SaveErrorInformDialog informDialog = new SaveErrorInformDialog(fileName, message, "Error while saving", e)) {
							informDialog.ShowDialog();
						}
						break;
					case FileErrorPolicy.ProvideAlternative:
						using (SaveErrorChooseDialog chooseDialog = new SaveErrorChooseDialog(fileName, message, "Error while saving", e, false)) {
							switch (chooseDialog.ShowDialog()) {
								case DialogResult.OK: // choose location (never happens here)
								break;
								case DialogResult.Retry:
									return ObservedSave(saveFile, fileName, message, policy);
								case DialogResult.Ignore:
									return FileOperationResult.Failed;
							}
						}
						break;
				}
			}
#else
			try {
				saveFile();
				return FileOperationResult.OK;
			} catch (Exception e) {
				Console.WriteLine("Error while saving : " + e.ToString());
			}
	
#endif
			return FileOperationResult.Failed;
		}
		
		public FileOperationResult ObservedSave(FileOperationDelegate saveFile, string fileName, FileErrorPolicy policy)
		{
			return ObservedSave(saveFile,
			                    fileName,
			                    GettextCatalog.GetString ("Unable to save file."),
			                    policy);
		}
		
		public FileOperationResult ObservedSave(FileOperationDelegate saveFile, string fileName)
		{
			return ObservedSave(saveFile, fileName, FileErrorPolicy.Inform);
		}
		
		public FileOperationResult ObservedSave(NamedFileOperationDelegate saveFileAs, string fileName, string message, FileErrorPolicy policy)
		{
			Debug.Assert(IsValidFileName(fileName));
#if false
			try {
				fileName = System.IO.Path.GetFullPath (fileName);
				saveFileAs(fileName);
				return FileOperationResult.OK;
			} catch (Exception e) {
				switch (policy) {
					case FileErrorPolicy.Inform:
						using (SaveErrorInformDialog informDialog = new SaveErrorInformDialog(fileName, message, "Error while saving", e)) {
							informDialog.ShowDialog();
						}
						break;
					case FileErrorPolicy.ProvideAlternative:
						restartlabel:
							using (SaveErrorChooseDialog chooseDialog = new SaveErrorChooseDialog(fileName, message, "Error while saving", e, true)) {
								switch (chooseDialog.ShowDialog()) {
									case DialogResult.OK:
										using (SaveFileDialog fdiag = new SaveFileDialog()) {
											fdiag.OverwritePrompt = true;
											fdiag.AddExtension    = true;
											fdiag.CheckFileExists = false;
											fdiag.CheckPathExists = true;
											fdiag.Title           = "Choose alternate file name";
											fdiag.FileName        = fileName;
											if (fdiag.ShowDialog() == DialogResult.OK) {
												return ObservedSave(saveFileAs, fdiag.FileName, message, policy);
											} else {
												goto restartlabel;
											}
										}
										case DialogResult.Retry:
											return ObservedSave(saveFileAs, fileName, message, policy);
									case DialogResult.Ignore:
										return FileOperationResult.Failed;
								}
							}
							break;
				}
			}
#else
			try {
				saveFileAs(fileName);
				return FileOperationResult.OK;
			} catch (Exception e) {
				Console.WriteLine("Error while saving as : " + e.ToString());
			}
#endif
			return FileOperationResult.Failed;
		}
		
		public FileOperationResult ObservedSave(NamedFileOperationDelegate saveFileAs, string fileName, FileErrorPolicy policy)
		{
			return ObservedSave(saveFileAs,
			                    fileName,
			                    GettextCatalog.GetString ("Unable to save file."),
			                    policy);
		}
		
		public FileOperationResult ObservedSave(NamedFileOperationDelegate saveFileAs, string fileName)
		{
			return ObservedSave(saveFileAs, fileName, FileErrorPolicy.Inform);
		}
		
		// Observe LOAD functions
		public FileOperationResult ObservedLoad(FileOperationDelegate saveFile, string fileName, string message, FileErrorPolicy policy)
		{
			Debug.Assert(IsValidFileName(fileName));
#if false
			try {
				saveFile();
				return FileOperationResult.OK;
			} catch (Exception e) {
				switch (policy) {
					case FileErrorPolicy.Inform:
						using (SaveErrorInformDialog informDialog = new SaveErrorInformDialog(fileName, message, "Error while loading", e)) {
							informDialog.ShowDialog();
						}
						break;
					case FileErrorPolicy.ProvideAlternative:
						using (SaveErrorChooseDialog chooseDialog = new SaveErrorChooseDialog(fileName, message, "Error while loading", e, false)) {
							switch (chooseDialog.ShowDialog()) {
								case DialogResult.OK: // choose location (never happens here)
								break;
								case DialogResult.Retry:
									return ObservedLoad(saveFile, fileName, message, policy);
								case DialogResult.Ignore:
									return FileOperationResult.Failed;
							}
						}
						break;
				}
			}
#else
			try {
				saveFile();
				return FileOperationResult.OK;
			} catch (Exception e) {
				Console.WriteLine("Error while loading " + e.ToString());
			}
#endif
			return FileOperationResult.Failed;
		}
		
		public FileOperationResult ObservedLoad(FileOperationDelegate saveFile, string fileName, FileErrorPolicy policy)
		{
			return ObservedLoad(saveFile,
			                    fileName,
			                    GettextCatalog.GetString ("Unable to load file."),
			                    policy);
		}
		
		public FileOperationResult ObservedLoad(FileOperationDelegate saveFile, string fileName)
		{
			return ObservedSave(saveFile, fileName, FileErrorPolicy.Inform);
		}
		
		class LoadWrapper
		{
			NamedFileOperationDelegate saveFileAs;
			string fileName;
			
			public LoadWrapper(NamedFileOperationDelegate saveFileAs, string fileName)
			{
				this.saveFileAs = saveFileAs;
				this.fileName   = fileName;
			}
			
			public void Invoke()
			{
				saveFileAs(fileName);
			}
		}
		
		public FileOperationResult ObservedLoad(NamedFileOperationDelegate saveFileAs, string fileName, string message, FileErrorPolicy policy)
		{
			return ObservedLoad(new FileOperationDelegate(new LoadWrapper(saveFileAs, fileName).Invoke), fileName, message, policy);
		}
		
		public FileOperationResult ObservedLoad(NamedFileOperationDelegate saveFileAs, string fileName, FileErrorPolicy policy)
		{
			return ObservedLoad(saveFileAs,
			                    fileName,
			                    GettextCatalog.GetString ("Unable to load file."),
			                    policy);
		}
		
		public FileOperationResult ObservedLoad(NamedFileOperationDelegate saveFileAs, string fileName)
		{
			return ObservedLoad(saveFileAs, fileName, FileErrorPolicy.Inform);
		}
	}
}
