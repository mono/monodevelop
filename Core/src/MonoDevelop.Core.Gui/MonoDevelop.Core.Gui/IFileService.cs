// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// This interface describes the basic functions of the 
	/// SharpDevelop file service.
	/// </summary>
	public interface IFileService
	{
		/// <remarks>
		/// Removes a file physically
		/// CAUTION : Use only this file for a remove operation, because it is important
		/// to know for other parts of the IDE when a file is removed.
		/// </remarks>
		void RemoveFile(string fileName);
		
		/// <remarks>
		/// Renames a file physically
		/// CAUTION : Use only this file for a rename operation, because it is important
		/// to know for other parts of the IDE when a file is renamed.
		/// </remarks>
		void RenameFile(string oldName, string newName);
		
		void CopyFile (string sourcePath, string destPath);

		void MoveFile (string sourcePath, string destPath);
		
		void CreateDirectory (string path);

		/// <remarks>
		/// Is called, when a file is renamed.
		/// </remarks>
		event FileEventHandler FileRenamed;
		
		/// <remarks>
		/// Is called, when a file is removed.
		/// </remarks>
		event FileEventHandler FileRemoved;
		
		/// <remarks>
		/// Is called, when a file is created.
		/// </remarks>
		event FileEventHandler FileCreated;
	}
}
