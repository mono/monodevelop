using AppKit;


namespace MonoDevelop.DesignerSupport.Toolbox
{
	public static class Extensions
	{
		public static NSImage ToNative (this Xwt.Drawing.Image sender)
		{
			var image = Xwt.Toolkit.NativeEngine.GetNativeImage (sender);
			return (NSImage)image;
		}
	}
}
