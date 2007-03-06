
using System;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.DesignerSupport.PropertyGrid
{
	class ComponentNodeBuilder: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return true;
		}

		public override Type CommandHandlerType {
			get { return typeof(ComponentNodeCommandHandler); }
		}
	}
	
	class ComponentNodeCommandHandler: NodeCommandHandler, IPropertyPadProvider
	{
		public object GetActiveComponent ()
		{
			return CurrentNode.DataItem;
		}
	}
}
