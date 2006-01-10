
using System;

using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	internal interface INewFileCreator
	{
		bool CreateItem (FileTemplate template, Project project, string directory, string language, string name);
	}
}
