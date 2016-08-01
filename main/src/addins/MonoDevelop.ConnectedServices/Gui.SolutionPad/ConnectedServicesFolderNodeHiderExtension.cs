using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace MonoDevelop.ConnectedServices.Gui.SolutionPad
{
	/// <summary>
	/// Hides the folders from the solution pad that match the folder that is used to store service state
	/// </summary>
	sealed class ConnectedServicesFolderNodeHiderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (ProjectFolder).IsAssignableFrom (dataType);
		}

		/// <summary>
		/// Gets the attributes of the given node. In this case, sets the hidden attribute if the node matches the Connected Services folder in the project root
		/// </summary>
		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			var folder = (ProjectFolder)dataObject;

			if (parentNode != null && parentNode.DataItem is DotNetProject && folder.Path.FileName == ConnectedServices.ProjectStateFolderName)
			{
				attributes |= NodeAttributes.Hidden;
			}
		}
	}
}
