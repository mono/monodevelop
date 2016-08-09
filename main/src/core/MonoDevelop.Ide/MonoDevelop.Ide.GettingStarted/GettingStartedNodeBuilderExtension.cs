using System;
using MonoDevelop.Ide.GettingStarted;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class GettingStartedNodeBuilderExtension : NodeBuilderExtension
	{

		public override bool CanBuildNode (Type dataType)
		{
			return typeof (Project).IsAssignableFrom (dataType);
		}

		public override Type CommandHandlerType {
			get { return typeof (GettingStartedNodeCommandHandler); }
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			if (((Project)dataObject).UserProperties.GetValue ("HideGettingStarted", false))
				return false;

			var gettingStarted = ((Project)dataObject).GetGettingStartedProvider ();
			return gettingStarted != null;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var project = (Project)dataObject;
			if (HasChildNodes (treeBuilder, dataObject)) {
				var node = project.GetGettingStartedNode ();
				treeBuilder.AddChild (node);
			}
		}
	}
}

