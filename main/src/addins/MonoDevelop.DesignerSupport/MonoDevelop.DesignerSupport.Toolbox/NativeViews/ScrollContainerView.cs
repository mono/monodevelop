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
			ScrollerStyle = NSScrollerStyle.Overlay;
			TranslatesAutoresizingMaskIntoConstraints = false;
		}
	}
}
#endif