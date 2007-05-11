using System;

namespace MonoDevelop.Ide.Projects.Item
{
	public static class ProjectItemFactory
	{
		public static ProjectItem Create(string itemType)
		{
			switch (itemType) {
			case "Reference":
				return new UnknownProjectItem(itemType);
			case "ProjectReference":
				return new UnknownProjectItem(itemType);
			case "Import":
				return new UnknownProjectItem(itemType);
					
			case "None":
			case "Compile":
			case "EmbeddedResource":
			case "Resource":
			case "Content":
			case "Folder":
				return new ProjectFile(ProjectFile.GetFileType (itemType));
/*
				case "WebReferences":
					return ;
				case "WebReferenceUrl":
					return ;
				case "COMReference":
					return ;
*/
			default:
				return new UnknownProjectItem(itemType);
			}
		}
	}
}
