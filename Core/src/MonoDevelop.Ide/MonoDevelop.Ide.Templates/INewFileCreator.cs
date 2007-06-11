
using System;

using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Ide.Templates
{
	internal interface INewFileCreator
	{
		bool CreateItem (FileTemplate template, IProject project, string directory, string language, string name);
	}
}
