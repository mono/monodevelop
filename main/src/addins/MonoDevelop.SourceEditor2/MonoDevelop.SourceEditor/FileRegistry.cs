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

namespace MonoDevelop.SourceEditor
{
	/// <summary>
	/// The File registry handles events that are affecting all open source views to allow the 
	/// operations to 'do action on all'/'ignore action on all'. (think of 50 files that needs to be reloaded)
	/// </summary>
	static class FileRegistry
	{
		readonly static List<SourceEditorView> openFiles = new List<SourceEditorView> ();
		readonly static FileSystemWatcher fileSystemWatcher;

		static FileRegistry ()
		{
			fileSystemWatcher = new FileSystemWatcher ();
			fileSystemWatcher.Created += (s, e) => Runtime.RunInMainThread (() => OnFileChanged (s, e));
			fileSystemWatcher.Changed += (s, e) => Runtime.RunInMainThread (() => OnFileChanged (s, e));

			FileService.FileCreated += HandleFileServiceChange;
			FileService.FileChanged += HandleFileServiceChange;

		}

		public static void Add (SourceEditorView sourceEditorView)
		{
			if (sourceEditorView.TextEditorType == TextEditorType.Projection)
				return;
			openFiles.Add (sourceEditorView);
		}

		public static void Remove (SourceEditorView sourceEditorView)
		{
			if (sourceEditorView.TextEditorType == TextEditorType.Projection)
				return;
			openFiles.Remove (sourceEditorView);
			UpdateEolMessages ();
		}

		static bool SkipView (SourceEditorView view)
		{
			return view.Document == null || !view.IsFile || view.IsUntitled || view.TextEditorType == TextEditorType.Projection;
		}

		static void HandleFileServiceChange (object sender, FileEventArgs e)
		{
			bool foundOneChange = false;
			foreach (var file in e) {
				if (skipFiles.Contains (file.FileName)) {
					skipFiles.Remove (file.FileName);
					continue;
				}
				foreach (var view in openFiles) {
					if (SkipView (view) || !string.Equals (view.ContentName, file.FileName, FilePath.PathComparison))
						continue;
					if (!view.IsDirty/* && (IdeApp.Workbench.AutoReloadDocuments || file.AutoReload)*/)
						view.SourceEditorWidget.Reload ();
					else
						foundOneChange = true;
				}
			}

			if (foundOneChange)
				CommitViewChange (GetAllChangedFiles ());
		}

		static List<SourceEditorView> GetAllChangedFiles ()
		{
			var changedViews = new List<SourceEditorView> ();
			foreach (var view in openFiles) {
				if (SkipView (view))
					continue;
				if (view.LastSaveTimeUtc == File.GetLastWriteTimeUtc (view.ContentName))
					continue;
				if (!view.IsDirty/* && IdeApp.Workbench.AutoReloadDocuments*/)
					view.SourceEditorWidget.Reload ();
				else
					changedViews.Add (view);
			}
			return changedViews;
		}


		static void OnFileChanged (object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
				CheckFileChange (e.FullPath);
		}

		static void CheckFileChange (string fileName)
		{
			if (skipFiles.Contains (fileName)) {
				skipFiles.Remove (fileName);
				return;
			}

			var changedViews = new List<SourceEditorView> ();
			foreach (var view in openFiles) {
				if (SkipView (view))
					continue;
				if (string.Equals (view.ContentName, fileName, FilePath.PathComparison)) {
					if (view.LastSaveTimeUtc == File.GetLastWriteTimeUtc (fileName))
						continue;
					if (!view.IsDirty/* && IdeApp.Workbench.AutoReloadDocuments*/)
						view.SourceEditorWidget.Reload ();
					else
						changedViews.Add (view);
				}
			}
			CommitViewChange (changedViews);
		}

		static void CommitViewChange (List<SourceEditorView> changedViews)
		{
			if (changedViews.Count == 0)
				return;
			if (changedViews.Count == 1) {
				changedViews [0].SourceEditorWidget.ShowFileChangedWarning (false);
			} else {
				foreach (var view in changedViews) {
					view.SourceEditorWidget.ShowFileChangedWarning (true);
				}
				if (!changedViews.Contains (IdeApp.Workbench.ActiveDocument.PrimaryView.GetContent<SourceEditorView> ()))
					changedViews [0].WorkbenchWindow.SelectWindow ();
			}
		}

		public static void IgnoreAllChangedFiles ()
		{
			foreach (var view in GetAllChangedFiles ()) {
				view.LastSaveTimeUtc = File.GetLastWriteTime (view.ContentName);
				view.SourceEditorWidget.RemoveMessageBar ();
				view.WorkbenchWindow.ShowNotification = false;
			}
		}

		public static void ReloadAllChangedFiles ()
		{
			foreach (var view in GetAllChangedFiles ()) {
				view.SourceEditorWidget.RemoveMessageBar ();
				view.SourceEditorWidget.Reload ();
				view.WorkbenchWindow.ShowNotification = false;
			}
		}

		#region EOL markers
		public static bool HasMultipleIncorrectEolMarkers {
			get {
				int count = 0;
				foreach (var view in openFiles) {
					if (SkipView (view) || !view.SourceEditorWidget.HasIncorrectEolMarker)
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
			foreach (var view in openFiles) {
				if (SkipView (view) || !view.SourceEditorWidget.HasIncorrectEolMarker)
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

			foreach (var view in openFiles) {
				if (SkipView (view) || !view.SourceEditorWidget.HasIncorrectEolMarker)
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
			foreach (var view in openFiles) {
				if (SkipView (view) || !view.SourceEditorWidget.HasIncorrectEolMarker)
					continue;
				view.SourceEditorWidget.UpdateEolMarkerMessage (multiple);
			}
		}

		static List<string> skipFiles = new List<string> ();
		internal static void SkipNextChange (string fileName)
		{
			if (!skipFiles.Contains (fileName))
				skipFiles.Add (fileName);
		}
		#endregion
	}
}
