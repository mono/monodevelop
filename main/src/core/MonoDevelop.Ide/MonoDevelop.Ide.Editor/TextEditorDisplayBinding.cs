//
// TextEditorDisplayBinding.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.IO;
using MonoDevelop.Projects;
using System.ComponentModel;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Editor
{
	public class TextEditorDisplayBinding : IViewDisplayBinding
	{
		static bool IsInitialized = false;

		public static FilePath SyntaxModePath {
			get {
				return UserProfile.Current.UserDataRoot.Combine ("ColorThemes");
			}
		}

		static TextEditorDisplayBinding ()
		{
			InitSourceEditor ();
		}

		public static void InitSourceEditor ()
		{
			if (IsInitialized)
				return;
			IsInitialized = true;

			// MonoDevelop.SourceEditor.Extension.TemplateExtensionNodeLoader.Init ();
			DefaultSourceEditorOptions.Init ();
			// SyntaxModeService.EnsureLoad ();
			LoadCustomStylesAndModes ();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void LoadCustomStylesAndModes ()
		{
			bool success = true;
			if (!Directory.Exists (SyntaxModePath)) {
				try {
					Directory.CreateDirectory (SyntaxModePath);
				} catch (Exception e) {
					success = false;
					LoggingService.LogError ("Can't create syntax mode directory", e);
				}
			}
			if (success)
				SyntaxHighlightingService.LoadStylesAndModesInPath (SyntaxModePath);
		}

		public string Name {
			get {
				return GettextCatalog.GetString ("Source Code Editor");
			}
		}

		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			if (fileName != null)
				return DesktopService.GetFileIsText (fileName, mimeType);

			if (!string.IsNullOrEmpty (mimeType))
				return DesktopService.GetMimeTypeIsText (mimeType);

			return false;
		}

		public ViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			TextEditor editor;

			// HACK: this is a very poor test for whether to load the document. Maybe add an IsPlaceholder property to FilePath.
			// Another alternative would be to add a parameter to CreateContent.
			if (File.Exists(fileName))
			{
				editor = TextEditorFactory.CreateNewEditor(fileName, mimeType);
			}
			else
			{
				editor = TextEditorFactory.CreateNewEditor();
				editor.FileName = fileName;
				editor.MimeType = mimeType;
			}

			editor.GetViewContent ().Project = ownerProject;
			return editor.GetViewContent (); 
		}

		public bool CanHandleFile (string fileName)
		{
			return DesktopService.GetFileIsText (fileName);
		}

		public bool CanUseAsDefault {
			get { return true; }
		}
	}
}