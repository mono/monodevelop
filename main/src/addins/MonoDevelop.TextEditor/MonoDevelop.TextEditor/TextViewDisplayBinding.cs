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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;
using MonoDevelop.Core.FeatureConfiguration;
using System.Linq;

namespace MonoDevelop.TextEditor
{
	abstract class TextViewDisplayBinding<TImports> : FileDocumentControllerFactory, IDisposable
		where TImports : TextViewImports
	{
		ThemeToClassification themeToClassification;

		public override string Id => "MonoDevelop.TextEditor.TextViewControllerFactory";

		protected override IEnumerable<DocumentControllerDescription> GetSupportedControllers (FileDescriptor modelDescriptor)
		{
			var nodes = Mono.Addins.AddinManager.GetExtensionNodes<SupportedFileTypeExtensionNode> ("/MonoDevelop/TextEditor/SupportedFileTypes");

			bool supported =
				(
					modelDescriptor.FilePath.IsNotNull
					&& IdeServices.DesktopService.GetFileIsText (modelDescriptor.FilePath, modelDescriptor.MimeType)
					&& nodes.Any (n => ExtensionMatch (n) && BuildActionAndFeatureFlagMatch (n))
				) || (
					!string.IsNullOrEmpty (modelDescriptor.MimeType)
					&& IdeServices.DesktopService.GetMimeTypeIsText (modelDescriptor.MimeType)
					&& nodes.Any (n => MimeMatch (n) && BuildActionAndFeatureFlagMatch (n))
				);

			if (supported) {
				yield return new DocumentControllerDescription (GettextCatalog.GetString ("New Source Code Editor"), true, DocumentControllerRole.Source);
			}

			bool ExtensionMatch (SupportedFileTypeExtensionNode node) =>
				node.Extensions != null
				&& node.Extensions.Any (ext => modelDescriptor.FilePath.HasExtension (ext));

			bool MimeMatch (SupportedFileTypeExtensionNode node) =>
				node.MimeTypes != null
				&& node.MimeTypes.Any (
					mime => string.Equals (modelDescriptor.MimeType, mime, StringComparison.OrdinalIgnoreCase)
				);

			bool BuildActionAndFeatureFlagMatch (SupportedFileTypeExtensionNode node)
			{
				if (!string.IsNullOrEmpty (node.FeatureFlag)) {
					if (!(FeatureSwitchService.IsFeatureEnabled (node.FeatureFlag) ?? node.FeatureFlagDefault)) {
						return false;
					}
				}
				if (!string.IsNullOrEmpty (node.BuildAction)) {
					var buildAction = (modelDescriptor.Owner as Project)?.GetProjectFile (modelDescriptor.FilePath)?.BuildAction;
					if (!string.Equals (buildAction, node.BuildAction, StringComparison.OrdinalIgnoreCase)) {
						return false;
					}
				}
				return true;
			}
		}

		public override Task<DocumentController> CreateController (FileDescriptor modelDescriptor, DocumentControllerDescription controllerDescription)
		{
			var imports = Ide.Composition.CompositionManager.Instance.GetExportedValue<TImports> ();
			if (themeToClassification == null)
				themeToClassification = CreateThemeToClassification (imports.EditorFormatMapService);

			return Task.FromResult (CreateContent (imports));
		}

		protected abstract DocumentController CreateContent (TImports imports);

		protected abstract ThemeToClassification CreateThemeToClassification  (Microsoft.VisualStudio.Text.Classification.IEditorFormatMapService editorFormatMapService);

		public void Dispose ()
		{
			themeToClassification?.Dispose ();
			themeToClassification = null;
		}
	}
}
