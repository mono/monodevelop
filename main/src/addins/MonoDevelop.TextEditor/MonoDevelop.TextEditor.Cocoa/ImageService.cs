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
			return Xwt.Toolkit.NativeEngine.GetNativeImage (xwtImage);
		}
	}
}