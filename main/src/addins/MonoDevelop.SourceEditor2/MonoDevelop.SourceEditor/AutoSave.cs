//
// AutoSave.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor
{
	static class AutoSave
	{
		static string autoSavePath = Path.Combine (PropertyService.Get<string> ("MonoDevelop.CodeCompletion.DataDirectory", String.Empty), "AutoSave");

		static AutoSave ()
		{
			if (!Directory.Exists (autoSavePath))
				Directory.CreateDirectory (autoSavePath);
			StartAutoSaveThread ();
		}

		static string GetAutoSaveFileName (string fileName)
		{
			if (fileName == null)
				return null;
			string newFileName = Path.Combine (Path.GetDirectoryName (fileName), Path.GetFileNameWithoutExtension (fileName) + Path.GetExtension (fileName) + "~");
			newFileName = Path.Combine (autoSavePath, newFileName.Replace(',','_').Replace(" ","").Replace (":","").Replace (Path.DirectorySeparatorChar, '_').Replace (Path.AltDirectorySeparatorChar, '_'));
			return newFileName;
		}

		public static bool AutoSaveExists (string fileName)
		{
			return File.Exists (GetAutoSaveFileName (fileName));
		}

		static void CreateAutoSave (string fileName, string content)
		{
			try {
				// Directory may have removed/unmounted. Therefore this operation is not guaranteed to work.
				File.WriteAllText (GetAutoSaveFileName (fileName), content);
				Counters.AutoSavedFiles++;
			} catch (Exception) {
			}
		}

#region AutoSave
		class FileContent
		{
			public string FileName;
			public Mono.TextEditor.Document Content;

			public FileContent (string fileName, Mono.TextEditor.Document content)
			{
				this.FileName = fileName;
				this.Content = content;
			}
		}

		public static bool Running {
			get {
				return autoSaveThreadRunning;
			}
		}

		static AutoResetEvent resetEvent = new AutoResetEvent (false);
		static bool autoSaveThreadRunning = false;
		static Thread autoSaveThread;
		static Queue<FileContent> queue = new Queue<FileContent> ();
		static object contentLock = new object ();

		static void StartAutoSaveThread ()
		{
			autoSaveThreadRunning = true;
			if (autoSaveThread == null) {
				autoSaveThread = new Thread (AutoSaveThread);
				autoSaveThread.Name = "Autosave";
				autoSaveThread.IsBackground = true;
				autoSaveThread.Start ();
			}
		}

		static void AutoSaveThread ()
		{
			while (autoSaveThreadRunning) {
				resetEvent.WaitOne ();
				while (queue.Count > 0) {
					var content = queue.Dequeue ();
					lock (contentLock) {
						CreateAutoSave (content.FileName, content.Content.Text);
					}
				}
			}
		}

		public static string LoadAutoSave (string fileName)
		{
			string autoSaveFileName = GetAutoSaveFileName (fileName);
			return File.ReadAllText (autoSaveFileName);
		}

		public static void RemoveAutoSaveFile (string fileName)
		{
			if (AutoSaveExists (fileName)) {
				string autoSaveFileName = GetAutoSaveFileName (fileName);
				try {
					lock (contentLock) {
						File.Delete (autoSaveFileName);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't delete auto save file: " + autoSaveFileName, e);
				}
			}
		}

		public static void InformAutoSaveThread (Mono.TextEditor.Document content)
		{
			if (content == null)
				return;
			if (content.IsDirty) {
				queue.Enqueue (new FileContent (content.FileName, content));
				resetEvent.Set ();
			} else {
				RemoveAutoSaveFile (content.FileName);
			}
		}

/*		public static void Dispose ()
		{
			autoSaveThreadRunning = false;
			if (autoSaveThread != null) {
				resetEvent.Set ();
				autoSaveThread.Join ();
				autoSaveThread = null;
			}
		}*/
#endregion
	}
}
