
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
		Pixbuf iconsIcon = Pixbuf.LoadFromResource ("image-x-generic.png");
		
		public override Type NodeDataType {
			get { return typeof(StockIconsNode); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(StockIconsNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/GtkCore/ContextMenu/ProjectPad.StockIcons"; }
		}

		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return -1;
		}


		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "StockIcons";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Stock Icons");
			icon = iconsIcon;
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
			gp.Save (true);
		}
		
		[CommandHandler (GtkCommands.EditIcons)]
		protected void OnEditIcons ()
		{
			ActivateItem ();
		}
	}
}

