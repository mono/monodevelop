#if MAC
using AppKit;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ScrollContainerView : NSScrollView
	{
		public ScrollContainerView ()
		{
			HasVerticalScroller = true;
			HasHorizontalScroller = false;
			BorderType = NSBorderType.NoBorder;
			TranslatesAutoresizingMaskIntoConstraints = false;
		}
	}
}
#endif