
using System;
using Gdk;

using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	class StockIconsNode
	{
		public StockIconsNode (Project project)
		{
			this.Project = project;
		}
		
		public Project Project;
	}
	
	public class StockIconsNodeBuilder: TypeNodeBuilder
	{
		Xwt.Drawing.Image iconsIcon;
		
		public override Type NodeDataType {
			get { return typeof(StockIconsNode); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(StockIconsNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/GtkCore/ContextMenu/ProjectPad.StockIcons"; }
		}
		public StockIconsNodeBuilder ()
		{
			try {
				iconsIcon = Xwt.Drawing.Image.FromResource ("image-x-generic.png");
			} catch (Exception e) {
				Console.WriteLine ("Error while loading pixbuf 'image-x-generic.png': " + e);
			}
		}
		public override int GetSortIndex (ITreeNavigator node)
		{
			return -100;
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "StockIcons";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label = GettextCatalog.GetString ("Stock Icons");
			nodeInfo.Icon = iconsIcon;
		}
	}
	
	public class StockIconsNodeCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			StockIconsNode node = (StockIconsNode) CurrentNode.DataItem;
			GtkDesignInfo info = GtkDesignInfo.FromProject (node.Project);
			GuiBuilderProject gp = info.GuiBuilderProject;
			Stetic.Project sp = gp.SteticProject;
			sp.ImagesRootPath = FileService.AbsoluteToRelativePath (info.GtkGuiFolder, gp.Project.BaseDirectory);
			sp.ImportFileCallback = delegate (string file) {
				return GuiBuilderService.ImportFile (gp.Project, file);
			};
			sp.EditIcons ();
			gp.SaveProject (true);
		}
		
		[CommandHandler (GtkCommands.EditIcons)]
		protected void OnEditIcons ()
		{
			ActivateItem ();
		}
	}
}

