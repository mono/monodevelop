// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.CodeDom.Compiler;

using MonoDevelop.Core.Properties;
using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Templates;

namespace MonoDevelop.Gui
{
	public class WorkbenchSingleton
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
			Runtime.Properties.PropertyChanged += (PropertyEventHandler) Runtime.DispatchService.GuiDispatch (new PropertyEventHandler(TrackPropertyChanges));
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
					case "MonoDevelop.Gui.VisualStyle":
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
	}
}
