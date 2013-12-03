using System;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;


namespace MonoDevelop.WebReferences.NodeBuilders
{
	/// <summary>Defines the properties and methods for the ProjectFolderNodeBuilderExtension class.</summary>
	public class ProjectFolderNodeBuilderExtension : NodeBuilderExtension
	{
		/// <summary>Checks if the node can be build for the current data type.</summary>
		/// <param name="dataType">A Type containing the data type of the current node.</param>
		/// <returns>True if the node can be build, otherwise false.</returns>
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType);
		}
		
		/// <summary>Get the attributes for the current node.</summary>
		/// <param name="parentNode">ITreeNavigator containing the tree navigator.</param>
		/// <param name="dataObject">An object containing the value of the current node.</param>
		/// <param name="attributes">A NodeAttributes reference containing all the attribute for the current node.</param>
		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			if (parentNode.Options ["ShowAllFiles"])
				return;
			
			var folder = dataObject as ProjectFolder;
			if (folder == null)
				return;

			var project = folder.Project as DotNetProject;
			if (project == null)
				return;
			
			foreach (var item in WebReferencesService.GetWebReferenceItems (project)) {
				if (folder.Path == item.BasePath.ParentDirectory.CanonicalPath) {
					attributes |= NodeAttributes.Hidden;
					break;
				}
			}
		}
	}
}