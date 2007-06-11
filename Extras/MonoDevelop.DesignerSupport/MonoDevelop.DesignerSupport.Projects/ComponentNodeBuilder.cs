
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
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
		
		public object GetPropertyProvider ()
		{
			return null;
		}

		public void OnEndEditing (object obj)
		{
		}

		public void OnChanged (object obj)
		{
			SolutionProject ce = (SolutionProject) CurrentNode.GetParentDataItem (typeof(SolutionProject), true);
			if (ce != null) {
				using (IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (false)) {
					ProjectService.SaveProject (ce.Project, mon);
				}
			}
		}
	}
}
