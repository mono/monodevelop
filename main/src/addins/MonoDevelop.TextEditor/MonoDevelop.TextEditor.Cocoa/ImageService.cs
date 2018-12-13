using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Core.Imaging;

using MDImageService = MonoDevelop.Ide.ImageService;

namespace MonoDevelop.Ide.Text.Cocoa
{
	// Import with AllowDefault:true
	[Export (typeof(IImageService))]
	public class ImageService : IImageService
	{
		public object GetImage (ImageId id)
		{
			// TODO: Add more image IDs (see https://github.com/mono/monodevelop/commit/15f864e5250dd89504f549ae0055622d48334e26 )
			Xwt.Drawing.Image xwtImage = MDImageService.GetImage (id);
			// TODO: Todd^W Kirill^W David, can you sprinkle some magic on this to use Mac engine instead of Gtk?
			return Xwt.Toolkit.CurrentEngine.GetNativeImage (xwtImage);
		}
	}
}