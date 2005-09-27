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
		
		CombineEntry ReadFile (string file, IProgressMonitor monitor);

		void WriteFile (string file, CombineEntry entry, IProgressMonitor monitor);
		
		Project CreateSingleFileProject (string file);
		
		Project CreateProject (string type, ProjectCreateInformation info, XmlElement projectOptions);
	}
}
