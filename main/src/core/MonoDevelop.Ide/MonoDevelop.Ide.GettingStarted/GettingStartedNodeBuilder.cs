using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.GettingStarted;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class GettingStartedNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof (GettingStartedNode);
			}
		}

		public override Type CommandHandlerType {
			get {
				return typeof (GettingStartedNodeCommandHandler);
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "GettingStarted";
		}

		public override object GetParentObject (object dataObject)
		{
			var node = dataObject as GettingStartedNode;
			return node?.Project ?? base.GetParentObject (dataObject);
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label = GettextCatalog.GetString ("Getting Started");
			nodeInfo.Icon = Context.GetIcon ("md-getting-started");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -2000;
		}

		public override void OnNodeAdded (object dataObject)
		{
			var node = dataObject as GettingStartedNode;
			node.Removed += OnNodeHidden;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			var node = dataObject as GettingStartedNode;
			node.Removed -= OnNodeHidden;
		}

		void OnNodeHidden (object sender, EventArgs args)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (sender);
			if (builder != null) {
				builder.MoveToParent ();
				builder.UpdateAll ();
			}
		}
	}
}

