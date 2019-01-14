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
using Microsoft.VisualStudio.Imaging;

using MonoDevelop.Core;
using MDImageService = MonoDevelop.Ide.ImageService;

namespace MonoDevelop.Ide.Text.Cocoa
{
	// Import with AllowDefault:true
	[Export (typeof(IImageService))]
	public class ImageService : IImageService
	{
		public object GetImage (ImageId imageId)
		{
			// TODO: Add more image IDs (see https://github.com/mono/monodevelop/commit/15f864e5250dd89504f549ae0055622d48334e26 )
			// TODO: May need to adjust StockIconCodon to support multiple ImageIds per StockIcon element. For now, using
			//       giant switch statement for easier iteration back-and-forth with standalone.
			//var xwtImage = MDImageService.GetImage (imageId);
			//return Xwt.Toolkit.NativeEngine.GetNativeImage (xwtImage);

			var stockId = GetStockIconId (imageId);
			if (stockId == null) {
				// Try to return based on the ID directly
				var nativeIcon = MDImageService.GetImage (imageId);
				if (nativeIcon == null) {
					LoggingService.LogInfo ("ImageService missing image with id: {0} , guid: {1}", imageId.Id, imageId.Guid);
					return null;
				}
				return Xwt.Toolkit.NativeEngine.GetNativeImage (nativeIcon);
			}
			return Xwt.Toolkit.NativeEngine.GetNativeImage (MDImageService.GetIcon (stockId));
		}

		string GetStockIconId (ImageId imageId)
		{
			switch (imageId.Id) {
			case KnownImageIds.Class:
			case KnownImageIds.ClassPublic:
			case KnownImageIds.ClassPrivate:
			case KnownImageIds.ClassProtected:
			case KnownImageIds.ClassSealed:
			case KnownImageIds.ClassInternal:
				return "md-class";
			case KnownImageIds.Constant:
			case KnownImageIds.ConstantPublic:
			case KnownImageIds.ConstantPrivate:
			case KnownImageIds.ConstantProtected:
			case KnownImageIds.ConstantSealed:
			case KnownImageIds.ConstantInternal:
			case KnownImageIds.EnumerationItemPublic:
			case KnownImageIds.EnumerationItemSealed:
			case KnownImageIds.EnumerationItemPrivate:
			case KnownImageIds.EnumerationItemSnippet:
			case KnownImageIds.EnumerationItemInternal:
			case KnownImageIds.EnumerationItemShortcut:
			case KnownImageIds.EnumerationItemProtected:
				return "md-literal";
			case KnownImageIds.Delegate:
			case KnownImageIds.DelegatePublic:
			case KnownImageIds.DelegatePrivate:
			case KnownImageIds.DelegateProtected:
			case KnownImageIds.DelegateSealed:
			case KnownImageIds.DelegateInternal:
				return "md-delegate";
			case KnownImageIds.Enumeration:
			case KnownImageIds.EnumerationPublic:
			case KnownImageIds.EnumerationPrivate:
			case KnownImageIds.EnumerationProtected:
			case KnownImageIds.EnumerationSealed:
			case KnownImageIds.EnumerationInternal:
				return "md-enum";
			case KnownImageIds.Event:
			case KnownImageIds.EventPublic:
			case KnownImageIds.EventPrivate:
			case KnownImageIds.EventProtected:
			case KnownImageIds.EventSealed:
			case KnownImageIds.EventInternal:
				return "md-event";
			case KnownImageIds.ExceptionPublic:
			case KnownImageIds.ExceptionPrivate:
			case KnownImageIds.ExceptionProtected:
			case KnownImageIds.ExceptionSealed:
			case KnownImageIds.ExceptionInternal:
				return "md-exception";
			case KnownImageIds.ExtensionMethod:
				return "md-extensionmethod";
			case KnownImageIds.Field:
			case KnownImageIds.FieldPublic:
			case KnownImageIds.FieldPrivate:
			case KnownImageIds.FieldProtected:
			case KnownImageIds.FieldSealed:
			case KnownImageIds.FieldInternal:
				return "md-field";
			case KnownImageIds.Interface:
			case KnownImageIds.InterfacePublic:
			case KnownImageIds.InterfacePrivate:
			case KnownImageIds.InterfaceProtected:
			case KnownImageIds.InterfaceSealed:
			case KnownImageIds.InterfaceInternal:
				return "md-interface";
			case KnownImageIds.IntellisenseKeyword:
				return "md-keyword";
			case KnownImageIds.Method:
			case KnownImageIds.MethodPublic:
			case KnownImageIds.MethodPrivate:
			case KnownImageIds.MethodProtected:
			case KnownImageIds.MethodSealed:
			case KnownImageIds.MethodInternal:
				return "md-method";
			case KnownImageIds.Module:
			case KnownImageIds.ModulePublic:
			case KnownImageIds.ModulePrivate:
			case KnownImageIds.ModuleProtected:
			case KnownImageIds.ModuleSealed:
			case KnownImageIds.ModuleInternal:
				return "md-module";
			case KnownImageIds.Namespace:
			case KnownImageIds.NamespacePublic:
			case KnownImageIds.NamespacePrivate:
			case KnownImageIds.NamespaceProtected:
			case KnownImageIds.NamespaceSealed:
			case KnownImageIds.NamespaceInternal:
				return "md-name-space";
			case KnownImageIds.Property:
			case KnownImageIds.PropertyPublic:
			case KnownImageIds.PropertyPrivate:
			case KnownImageIds.PropertyProtected:
			case KnownImageIds.PropertySealed:
			case KnownImageIds.PropertyInternal:
				return "md-property";
			case KnownImageIds.Structure:
			case KnownImageIds.StructurePublic:
			case KnownImageIds.StructurePrivate:
			case KnownImageIds.StructureProtected:
			case KnownImageIds.StructureSealed:
			case KnownImageIds.StructureInternal:
			case KnownImageIds.ValueType:
			case KnownImageIds.ValueTypePublic:
			case KnownImageIds.ValueTypeSealed:
			case KnownImageIds.ValueTypePrivate:
			case KnownImageIds.ValueTypeInternal:
			case KnownImageIds.ValueTypeShortcut:
			case KnownImageIds.ValueTypeProtected:
				return "md-struct";
			case KnownImageIds.Type:
			case KnownImageIds.TypePublic:
			case KnownImageIds.TypePrivate:
			case KnownImageIds.TypeProtected:
			case KnownImageIds.TypeSealed:
			case KnownImageIds.TypeInternal:
				return "md-type";
			case KnownImageIds.MemberVariable:
			case KnownImageIds.GlobalVariable:
			case KnownImageIds.LocalVariable:
			case KnownImageIds.Parameter:
				return "md-variable";
			case KnownImageIds.Snippet:
				return "md-template"; // TODO: Differentiate surrounds-with?
			default:
				return null;
			}
		}
	}
}