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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var folder = (WebReferenceFolder) dataObject;
			nodeInfo.Label = folder.IsWCF ? GettextCatalog.GetString ("Web Services") : GettextCatalog.GetString ("Web References");
			nodeInfo.Icon = Context.GetIcon ("md-webreference-folder");
			nodeInfo.ClosedIcon = Context.GetIcon ("md-webreference-folder");
			
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
			if (folder.IsWCF)
				treeBuilder.AddChildren (WebReferencesService.GetWebReferenceItemsWCF (folder.Project));
			else
				treeBuilder.AddChildren (WebReferencesService.GetWebReferenceItemsWS (folder.Project));
		}
		
		public override int GetSortIndex (ITreeNavigator node)
		{
			return -200;
		}
	}
}
