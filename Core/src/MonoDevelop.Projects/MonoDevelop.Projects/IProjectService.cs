// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// This interface describes the basic functions of the 
	/// SharpDevelop project service.
	/// </summary>
	public interface IProjectService
	{
		bool IsCombineEntryFile (string filename);
		
		DataContext DataContext {
			get;
		}
		
		FileFormatManager FileFormats {
			get;
		}
		
		CombineEntry ReadCombineEntry (string file, IProgressMonitor monitor);

		string Export (IProgressMonitor monitor, string rootSourceFile, string targetPath, IFileFormat format);
		string Export (IProgressMonitor monitor, string rootSourceFile, string[] childEnryFiles, string targetPath, IFileFormat format);

		bool CanCreateSingleFileProject (string file);
		Project CreateSingleFileProject (string file);
		
		Project CreateProject (string type, MonoDevelop.Ide.Projects.NewSolutionData info, XmlElement projectOptions);
	}
}
