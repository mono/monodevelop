// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.CodeDom.Compiler;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui
{
/*	internal class WorkbenchSingleton
	{
		static IWorkbench workbench    = null;
		
		public static IWorkbench Workbench {
			get {
				return workbench;
			}
			set {
				workbench = value;
			}
		}
		
		static WorkbenchSingleton()
		{
			Runtime.Properties.PropertyChanged += (PropertyEventHandler) Services.DispatchService.GuiDispatch (new PropertyEventHandler(TrackPropertyChanges));
		}
		
		static void SetWbLayout()
		{
			//FIXME: I should be doing this here, but im doing it in the WorkbenchLayout property, which seems wrong
			workbench.WorkbenchLayout = new SdiWorkbenchLayout();
		}
		
		/// <remarks>
		/// This method handles the redraw all event for specific changed IDE properties
		/// </remarks>
		static void TrackPropertyChanges(object sender, MonoDevelop.Core.Properties.PropertyEventArgs e)
		{
			if (e.OldValue != e.NewValue) {
				switch (e.Key) {
					case "MonoDevelop.Core.Gui.VisualStyle":
					case "CoreProperties.UILanguage":
						workbench.RedrawAllComponents();
						break;
				}
			}
		}
		
		public static void CreateWorkspace()
		{
			SetWbLayout();
			workbench.RedrawAllComponents();
		}
	}*/
}
