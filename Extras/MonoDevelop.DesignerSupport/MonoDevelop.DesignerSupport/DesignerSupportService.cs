
using System;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class DesignerSupportService : AbstractService
	{
		PropertyPad propertyPad = null;
		ToolboxService toolboxService = null;
		CodeBehindService codeBehindService = new CodeBehindService ();
		BindingService bindingService = new BindingService ();
		
		public BindingService BindingService {
			get { return bindingService; }
		}
		
		#region PropertyPad
		
		public PropertyPad PropertyPad {
			get {
				return propertyPad;
			}
		}
		
		internal void SetPropertyPad (PropertyPad pad)
		{
			propertyPad = pad;
		}
		
		#endregion
		
		#region Toolbox
		
		public ToolboxService ToolboxService {
			get{
				//lazy load of toolbox contents
				if (toolboxService == null) {
					toolboxService = new ToolboxService ();
										
					string path = System.IO.Path.Combine (Runtime.Properties.ConfigDirectory, "Toolbox.xml");
					if (System.IO.File.Exists (path))
						toolboxService.LoadContents (path);
				}
				
				return toolboxService;
			}
		}
		
		#endregion
		
		#region CodeBehind
		
		public CodeBehindService CodeBehindService {
			get { return codeBehindService; }
		}
		
		#endregion
		
		#region IService implementations
		
		public override void InitializeService()
		{
			base.InitializeService ();
		}
		
		public override void UnloadService()
		{
			if (toolboxService != null)
				toolboxService.SaveContents (System.IO.Path.Combine (Runtime.Properties.ConfigDirectory, "Toolbox.xml"));
		}
		
		#endregion
	}
	
	public static class DesignerSupport
	{
		static DesignerSupportService designerSupportService;
		
		public static DesignerSupportService Service {
			get {
				if (designerSupportService == null)
					designerSupportService = (DesignerSupportService) ServiceManager.GetService (typeof(DesignerSupportService));
				return designerSupportService;
			}
		}
	}
}
