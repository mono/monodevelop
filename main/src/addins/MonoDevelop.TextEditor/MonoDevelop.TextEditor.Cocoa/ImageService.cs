//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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