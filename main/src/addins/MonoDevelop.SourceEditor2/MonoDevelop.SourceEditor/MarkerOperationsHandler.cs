//
// MarkerOperationsHandler.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	class MarkerOperationsHandler : CommandHandler
	{
		protected override void Run (object data)
		{
			UrlMarker urlMarker = data as UrlMarker;
			if (urlMarker == null)
				return;
			try {
				if (urlMarker.UrlType == UrlType.Email) {
					System.Diagnostics.Process.Start ("mailto:" + urlMarker.Url);
				} else {
					System.Diagnostics.Process.Start (urlMarker.Url);
				}
			} catch (Exception) {
				MessageService.ShowError (GettextCatalog.GetString ("Could not open the url {0}", urlMarker.Url));
			}
		}
		
		protected override void Update (CommandArrayInfo ainfo)
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc != null) {
				SourceEditorView view = IdeApp.Workbench.ActiveDocument.GetContent <SourceEditorView>();
				if (view != null) {
					var location = view.TextEditor.Caret.Location;
					if (location.IsEmpty)
						return;
					var line = view.Document.GetLine (location.Line);
					if (line == null)
						return;
					foreach (TextLineMarker marker in view.Document.GetMarkers (line)) {
						UrlMarker urlMarker = marker as UrlMarker;
						if (urlMarker != null) {
							if (urlMarker.StartColumn <= location.Column && location.Column < urlMarker.EndColumn) {
								ainfo.Add (urlMarker.UrlType == UrlType.Email ? GettextCatalog.GetString ("_Write an e-mail to...") : GettextCatalog.GetString ("_Open URL..."), urlMarker);
								ainfo.AddSeparator ();
							}
						}
					}
				}
			}
		}
	}
}
