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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;
using MonoDevelop.Core.FeatureConfiguration;

namespace MonoDevelop.TextEditor
{
	abstract class TextViewDisplayBinding<TImports> : FileDocumentControllerFactory, IDisposable
		where TImports : TextViewImports
	{
		ThemeToClassification themeToClassification;

		public override string Id => "MonoDevelop.TextEditor.TextViewControllerFactory";

		protected override IEnumerable<DocumentControllerDescription> GetSupportedControllers (FileDescriptor modelDescriptor)
		{
			if (!DefaultSourceEditorOptions.Instance.EnableNewEditor) {
				yield break;
			}

			if (modelDescriptor.FilePath == null || !(IsSupportedFileExtension (modelDescriptor.FilePath) || IsSupportedDesignerFileName (modelDescriptor.FilePath, modelDescriptor.Owner))) {
				yield break;
			}

			bool supported = false;

			if (modelDescriptor.FilePath != null)
				supported = IdeServices.DesktopService.GetFileIsText (modelDescriptor.FilePath, modelDescriptor.MimeType);

			if (!supported && !string.IsNullOrEmpty (modelDescriptor.MimeType))
				supported = IdeServices.DesktopService.GetMimeTypeIsText (modelDescriptor.MimeType);

			if (supported)
				yield return new DocumentControllerDescription (GettextCatalog.GetString ("New Source Code Editor"), true, DocumentControllerRole.Source);
		}

		static HashSet<string> supportedFileExtensions = new HashSet<string> (StringComparer.OrdinalIgnoreCase) {
			".cs",
			".csx"
			//".cshtml",
			//".css",
			//".html",
			//".js",
			//".json",
			//".ts"
		};

		bool IsSupportedFileExtension (FilePath fileName)
		{
			return supportedFileExtensions.Contains (fileName.Extension);
		}

		bool IsSupportedDesignerFileName (FilePath fileName, WorkspaceObject ownerProject)
		{
			if (!FeatureSwitchService.IsFeatureEnabled ("DesignersNewEditor").GetValueOrDefault ())
				return false;

			return fileName.HasExtension (".xaml")
				|| IsSupportedAndroidFileName (fileName, ownerProject);
		}

		bool IsSupportedAndroidFileName (FilePath fileName, WorkspaceObject ownerProject)
		{
			// We only care about .xml and .axml files that are marked as AndroidResource
			if (!(fileName.HasExtension (".xml") || fileName.HasExtension (".axml")))
				return false;

			const string AndroidResourceBuildAction = "AndroidResource";
			var buildAction = (ownerProject as Project)?.GetProjectFile (fileName)?.BuildAction;
			return string.Equals (buildAction, AndroidResourceBuildAction, StringComparison.Ordinal);
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