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
using System.Linq;
using System.Threading.Tasks;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;
using MonoDevelop.Core.FeatureConfiguration;

namespace MonoDevelop.TextEditor
{
	abstract class TextViewDisplayBinding<TImports> : FileDocumentControllerFactory, IDisposable
		where TImports : TextViewImports
	{
		ThemeToClassification themeToClassification;
		readonly Dictionary<(string addinId, string providerType), ILegacyEditorSupportProvider> legacyEditorSupportProviders =
			new Dictionary<(string addinId, string providerType), ILegacyEditorSupportProvider> ();

		readonly Lazy<bool> alwaysUseLegacyEditor = new Lazy<bool> (
			() => FeatureSwitchService.IsFeatureEnabled ("AlwaysUseLegacyEditor").GetValueOrDefault ());

		public override string Id => "MonoDevelop.TextEditor.TextViewControllerFactory";

		protected override IEnumerable<DocumentControllerDescription> GetSupportedControllers (FileDescriptor modelDescriptor)
		{
			var isTextByFilePath = modelDescriptor.FilePath.IsNotNull && IdeServices.DesktopService.GetFileIsText (modelDescriptor.FilePath, modelDescriptor.MimeType);
			var isTextByMimeType = !string.IsNullOrEmpty (modelDescriptor.MimeType) && IdeServices.DesktopService.GetMimeTypeIsText (modelDescriptor.MimeType);

			if (!isTextByFilePath && !isTextByMimeType)
				yield break;

			// If feature switch is enabled, don't even bother checking for modern editor support.
			// This should only be used by automatic testing tooling that hasn't updated to support modern editor yet.
			var preferLegacy = alwaysUseLegacyEditor.Value;
			if (!preferLegacy) {
				// First, check if legacy editor even has support for the file. If not, always use modern editor.
				var legacySupportNodes = Mono.Addins.AddinManager.GetExtensionNodes<LegacyEditorSupportExtensionNode> ("/MonoDevelop/TextEditor/LegacyEditorSupport");
				foreach (var node in legacySupportNodes.OrderByDescending (n => string.IsNullOrEmpty (n.ProviderType))) {
					if ((isTextByFilePath && ExtensionMatch (node)) || (isTextByMimeType && MimeMatch (node))) {
						preferLegacy = true;
						break;
					}

					// Only attempt a provider check if extension/mimetype checks already failed, as it's more expensive.
					if (PrefersLegacyEditor (node)) {
						preferLegacy = true;
						break;
					}
				}

				// Next, check if there is an explicit directive to prefer the modern editor even if legacy is supported.
				if (preferLegacy) {
					var explicitModernSupportNodes = Mono.Addins.AddinManager.GetExtensionNodes<SupportedFileTypeExtensionNode> ("/MonoDevelop/TextEditor/SupportedFileTypes");
					foreach (var node in explicitModernSupportNodes) {
						if (((isTextByFilePath && ExtensionMatch (node)) || (isTextByMimeType && MimeMatch (node))) && BuildActionAndFeatureFlagMatch (node)) {
							preferLegacy = false;
							break;
						}
					}
				}
			}

			yield return new EditorDocumentControllerDescription (GettextCatalog.GetString ("Source Code Editor"), true, DocumentControllerRole.Source, preferLegacy);

			bool ExtensionMatch (MatchingFileTypeExtensionNode node) =>
				node.Extensions != null
				&& node.Extensions.Any (ext => modelDescriptor.FilePath.HasExtension (ext));

			bool MimeMatch (MatchingFileTypeExtensionNode node) =>
				node.MimeTypes != null
				&& node.MimeTypes.Any (
					mime => string.Equals (modelDescriptor.MimeType, mime, StringComparison.OrdinalIgnoreCase)
				);

			bool PrefersLegacyEditor (LegacyEditorSupportExtensionNode node)
			{
				if (string.IsNullOrEmpty (node.ProviderType))
					return false;

				try {
					var key = (node.Addin.Id, node.ProviderType);
					if (!legacyEditorSupportProviders.TryGetValue (key, out var provider)) {
						provider = (ILegacyEditorSupportProvider)Activator.CreateInstance (node.Addin.GetType (node.ProviderType));
						legacyEditorSupportProviders [key] = provider;
					}
					return provider.PreferLegacyEditor (modelDescriptor);
				} catch (Exception e) {
					LoggingService.LogError ("Error loading legacy editor support provider", e);
				}

				return false;
			}

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
			if (controllerDescription is EditorDocumentControllerDescription editorControllerDescription && editorControllerDescription.IsLegacy)
				return Task.FromResult<DocumentController> (new Ide.Editor.TextEditorViewContent ());

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

		class EditorDocumentControllerDescription : DocumentControllerDescription
		{
			public bool IsLegacy { get; }

			public EditorDocumentControllerDescription (string name, bool canUseAsDefault = true, DocumentControllerRole role = DocumentControllerRole.Source, bool isLegacy = false)
				: base (name, canUseAsDefault, role)
			{
				IsLegacy = isLegacy;
			}
		}
	}
}
