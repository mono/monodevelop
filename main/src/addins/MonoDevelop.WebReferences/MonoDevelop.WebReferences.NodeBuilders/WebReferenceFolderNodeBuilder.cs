using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.WebReferences.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.WebReferences.NodeBuilders
{
	/// <summary>Defines the properties and methods for the WebReferenceFolderNodeBuilder class.</summary>
	public class WebReferenceFolderNodeBuilder : TypeNodeBuilder
	{
		#region Properties
		/// <summary>Gets the data type for the WebReferenceFolderNodeBuilder.</summary>
		/// <value>A Type containing the data type for WebReferenceFolderNodeBuilder.</value>
		public override Type NodeDataType 
		{
			get { return typeof(WebReferenceFolder); }
		}
		
		/// <summary>Gets the type of the CommandHandler with the WebReferenceFolderNodeBuilder.</summary>
		/// <value>A Type containing the reference for the CommandHandlerType for the WebReferenceFolderNodeBuilder.</value>
		public override Type CommandHandlerType 
		{
			get { return typeof(WebReferenceCommandHandler); }
		}
		
		/// <summary>Gets the Addin path for the context menu for the WebReferenceFolderNodeBuilder.</summary>
		/// <value>A string containing the AddIn path for the context menu for the WebReferenceFolderNodeBuilder.</value>
		public override string ContextMenuAddinPath 
		{
			get { return "/MonoDevelop/WebReferences/ContextMenu/ProjectPad/WebReferenceFolder"; }
		}
		#endregion
		
		/// <summary>Gets the node name for the current node.</summary>
		/// <param name="thisNode">An ITreeNavigator containing the current node settings.</param>
		/// <param name="dataObject">An object containing the data for the current object.</param>
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "WebReferences";
		}
		
		/// <summary>Build the node in the project tree.</summary>
		/// <param name="treeBuilder">An ITreeBuilder containing the project tree builder.</param>
		/// <param name="dataObject">An object containing the current builder child.</param>
		/// <param name="label">A string containing the label of the node.</param>
		/// <param name="icon">A Pixbif containing the icon for the node.</param>
		/// <param name="closedIcon">A Pixbif containing the closed icon for the node.</param>
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Web References");
			icon = Context.GetIcon (Stock.OpenReferenceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedReferenceFolder);
			
		}
		
		/// <summary>Checks if the node builder has contains any child nodes.</summary>
		/// <param name="builder">An ITreeBuilder containing all the node builder information.</param>
		/// <param name="dataObject">An object containing the current activated node.</param> 
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		/// <summary>Add entries for all the web references in the project to the tree builder.</summary>
		/// <param name="treeBuilder">An ITreeBuilder containing all the data for the current DotNet project.</param>
		/// <param name="dataObject">An object containing the data for the current node in the tree.</param>
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var folder = (WebReferenceFolder) dataObject;
			foreach (WebReferenceItem item in WebReferencesService.GetWebReferenceItems (folder.Project))
				treeBuilder.AddChild(item);
		}
		
		/// <summary>Compare two object with one another and returns a number based on their sort order.</summary>
		/// <returns>An integer containing the sort order for the objects.</returns>
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return (otherNode.DataItem is ProjectReferenceCollection) ? 1 : -1;
		}
	}
}
