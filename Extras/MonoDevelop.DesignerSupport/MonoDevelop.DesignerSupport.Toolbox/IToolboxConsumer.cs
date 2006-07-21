
using System;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	public interface IToolboxConsumer
	{
		/*todo: drag/drop stuff */
		void Use (ItemToolboxNode item);
		
		//used to filter toolbox items
		System.ComponentModel.ToolboxItemFilterAttribute[] ToolboxFilterAttributes {
			get;
		}
		
		//Used if ToolboxItemFilterAttribute demands ToolboxItemFilterType.Custom
		//If not expecting it, should just return false
		bool CustomFilterSupports (ItemToolboxNode item);
	}
}
