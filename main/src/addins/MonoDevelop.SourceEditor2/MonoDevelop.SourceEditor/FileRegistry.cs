// SourceEditorView.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.IO;

using MonoDevelop.Core;
using Services = MonoDevelop.Projects.Services;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor
{
	/// <summary>
	/// The File registry handles events that are affecting all open source views to allow the 
	/// operations to 'do action on all'/'ignore action on all'. (think of 50 files that needs to be reloaded)
	/// </summary>
	static class FileRegistry
	{

		#region EOL markers
		public static bool HasMultipleIncorrectEolMarkers {
			get {
				int count = 0;
				foreach (var doc in DocumentRegistry.OpenFiles) {
					if (DocumentRegistry.SkipView (doc))
						continue;
					var view = doc.GetContent<SourceEditorView> ();
					if (!view.SourceEditorWidget.HasIncorrectEolMarker)
						continue;
					count++;
					if (count > 1)
						return true;
				}
				return false;
			}
		}

		public static void ConvertLineEndingsInAllFiles ()
		{
			DefaultSourceEditorOptions.Instance.LineEndingConversion = LineEndingConversion.ConvertAlways;
			foreach (var doc in DocumentRegistry.OpenFiles) {
				if (DocumentRegistry.SkipView (doc))
					continue;
				var view = doc.GetContent<SourceEditorView> ();
				if (!view.SourceEditorWidget.HasIncorrectEolMarker)
					continue;

				view.SourceEditorWidget.ConvertLineEndings ();
				view.SourceEditorWidget.RemoveMessageBar ();
				view.WorkbenchWindow.ShowNotification = false;
				view.Save ();
			}
		}

		public static void IgnoreLineEndingsInAllFiles ()
		{
			DefaultSourceEditorOptions.Instance.LineEndingConversion = LineEndingConversion.LeaveAsIs;

			foreach (var doc in DocumentRegistry.OpenFiles) {
				if (DocumentRegistry.SkipView (doc))
					continue;
				var view = doc.GetContent<SourceEditorView> ();
				if (!view.SourceEditorWidget.HasIncorrectEolMarker)
					continue;

				view.SourceEditorWidget.UseIncorrectMarkers = true;
				view.SourceEditorWidget.RemoveMessageBar ();
				view.WorkbenchWindow.ShowNotification = false;
				view.Save ();
			}
		}

		public static void UpdateEolMessages ()
		{
			var multiple = HasMultipleIncorrectEolMarkers;
			foreach (var doc in DocumentRegistry.OpenFiles) {
				if (DocumentRegistry.SkipView (doc))
					continue;
				var view = doc.GetContent<SourceEditorView> ();
				if (!view.SourceEditorWidget.HasIncorrectEolMarker)
					continue;
				view.SourceEditorWidget.UpdateEolMarkerMessage (multiple);
			}
		}
		#endregion
	}
}
