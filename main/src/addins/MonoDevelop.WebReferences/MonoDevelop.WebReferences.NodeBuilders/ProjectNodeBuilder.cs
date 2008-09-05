using System;
using System.Collections;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.WebReferences.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.WebReferences.NodeBuilders
{
	/// <summary>Defines the properties and methods for the ProjectFolderNodeBuilderExtension class.</summary>
	public class ProjectNodeBuilder: NodeBuilderExtension
	{
		#region Properties
		/// <summary>Gets the type of the CommandHandler with the ProjectFolderNodeBuilderExtension.</summary>
		/// <value>A Type containing the reference for the CommandHandlerType for the ProjectFolderNodeBuilderExtension.</value>
		public override Type CommandHandlerType 
		{
			get { return typeof(WebReferenceCommandHandler); }
		}
		#endregion
		
		/// <summary>Checks if the node can be build for the current data type.</summary>
		/// <param name="Type">A Type containing the data type of the current node.</param>
		/// <returns>True if the node can be build, otherwise false.</returns>
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(DotNetProject).IsAssignableFrom (dataType);
		}
		
		/// <summary>Adds the WebReferencesFolder to the tree builder for all the DotNet projects.</summary>
		/// <param name="builder">An ITreeBuilder containing all the data for the current DotNet project.</param>
		/// <param name="dataObject">An object containing the data for the current node in the tree.</param>
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project) dataObject;
			if (Library.ProjectContainsWebReference(project))
				builder.AddChild (new WebReferenceFolder(project));
		}
	}
}