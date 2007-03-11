using System;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;

namespace CSharpBinding.FormattingStrategy.Properties {
	public enum GotoLabelIndentStyle {
		// Place goto labels in the leftmost column
		LeftJustify,
		
		// Place goto labels one indent less than current
		OneLess,
		
		// Indent goto labels normally
		Normal
	}
	
	public class FormattingProperties {
		static PropertyService propertyService;
		static IProperties properties;
		
		static FormattingProperties ()
		{
			propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));
			properties = ((IProperties) propertyService.GetProperty ("CSharpBinding.FormattingProperties",
			                                                         new DefaultProperties ()));
		}
		
		public static bool IndentCaseLabels {
			get {
				return (bool) properties.GetProperty ("IndentCaseLabels", false);
			}
			set {
				properties.SetProperty ("IndentCaseLabels", value);
			}
		}
		
		public static GotoLabelIndentStyle GotoLabelIndentStyle {
			get {
				return (GotoLabelIndentStyle)
					properties.GetProperty ("GotoLabelIndentStyle", GotoLabelIndentStyle.OneLess);
			}
			set {
				properties.SetProperty ("GotoLabelIndentStyle", value);
			}
		}
	}
}
