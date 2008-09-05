using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;


namespace MonoDevelop.WebReferences.NodeBuilders
{
	/// <summary>Defines the properties and methods for the ProjectFolderNodeBuilderExtension class.</summary>
	public class ProjectFolderNodeBuilderExtension : NodeBuilderExtension
	{
		/// <summary>Checks if the node can be build for the current data type.</summary>
		/// <param name="Type">A Type containing the data type of the current node.</param>
		/// <returns>True if the node can be build, otherwise false.</returns>
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFolder).IsAssignableFrom (dataType);
		}
		
		/// <summary>Get the attributes for the current node.</summary>
		/// <param name="treeNavigator">ITreeNavigator containing the tree navigator.</param>
		/// <param name="dataObject">An object containing the value of the current node.</param>
		/// <param name="attributes">A NodeAttributes reference containing all the attribute for the current node.</param>
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			if (treeNavigator.Options ["ShowAllFiles"])
				return;
				
			ProjectFolder folder = dataObject as ProjectFolder;
			if (folder != null && folder.Project != null && Library.GetWebReferencePath(folder.Project) == folder.Path)
				attributes |= NodeAttributes.Hidden;
		}
	}
}