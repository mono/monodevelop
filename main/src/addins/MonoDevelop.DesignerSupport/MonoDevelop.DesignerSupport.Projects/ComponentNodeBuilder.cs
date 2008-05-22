
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.DesignerSupport.PropertyGrid;

namespace MonoDevelop.DesignerSupport.Projects
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
		
		public object GetProvider ()
		{
			return null;
		}

		public void OnEndEditing (object obj)
		{
		}

		public void OnChanged (object obj)
		{
			// Don't use the CurrentNode property here since it may not be properly initialized when the event is fired.
			ITreeNavigator nav = Tree.GetNodeAtObject (obj);
			if (nav != null) {
				IWorkspaceFileObject ce = (IWorkspaceFileObject) nav.GetParentDataItem (typeof(IWorkspaceFileObject), true);
				if (ce != null) {
					IdeApp.ProjectOperations.Save (ce);
					return;
				}
			}
		}
	}
}
