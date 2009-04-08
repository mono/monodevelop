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
using System.IO;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor
{
	public class AutoSave : IDisposable
	{
		static string GetAutoSaveFileName (string fileName)
		{
			if (fileName == null)
				return null;
			return Path.Combine (Path.GetDirectoryName (fileName),
			                     "." + Path.GetFileNameWithoutExtension (fileName) + "~" + Path.GetExtension (fileName));
		}
		public static bool AutoSaveExists (string fileName)
		{
			return File.Exists (GetAutoSaveFileName (fileName));
		}
		
		
		static void CreateAutoSave (string fileName, string content)
		{
			File.WriteAllText (GetAutoSaveFileName (fileName), content);
		}
		
#region AutoSave 
		class FileContent 
		{
			public string FileName;
			public string Content;

			public FileContent (string fileName, string content)
			{
				this.FileName = fileName;
				this.Content = content;
			}
		}
		
		public bool Running {
			get {
				return autoSaveThreadRunning;
			}
		}
		
		public bool IsDirty {
			get;
			set;
		}
		
		public string FileName {
			get;
			set;
		}
		
		public AutoSave ()
		{
			IsDirty = false;
		}
		AutoResetEvent resetEvent = new AutoResetEvent (false);
		bool autoSaveThreadRunning = false;
		Thread autoSaveThread;
		bool fileChanged   = false;
		FileContent content = null;
		object contentLock = new object ();
		
		public void StartAutoSaveThread ()
		{
			autoSaveThreadRunning = true;
			if (autoSaveThread == null) {
				autoSaveThread = new Thread (AutoSaveThread);
				autoSaveThread.IsBackground = true;
				autoSaveThread.Start ();
			}
		}
		
		void AutoSaveThread ()
		{
			while (autoSaveThreadRunning) {
				resetEvent.WaitOne ();
				lock (contentLock) {
					if (fileChanged) {
						CreateAutoSave (content.FileName, content.Content);
						fileChanged = false;
					}
				}
			}
		}
		
		public string LoadAutoSave ()
		{
			string autoSaveFileName = GetAutoSaveFileName (FileName);
			return File.ReadAllText (autoSaveFileName);
		}
		
		public void RemoveAutoSaveFile ()
		{
			IsDirty = false;
			if (AutoSaveExists (FileName)) {
				string autoSaveFileName = GetAutoSaveFileName (FileName);
				try {
					lock (contentLock) {
						File.Delete (autoSaveFileName);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't delete auto save file: " + autoSaveFileName, e);
				}
			}
		}
		
		public void InformAutoSaveThread (string content)
		{
			if (FileName == null || content == null)
				return;
			if (!autoSaveThreadRunning)
				StartAutoSaveThread ();
			
			IsDirty = true;
			lock (contentLock) {
				fileChanged = true;
				this.content = new FileContent (FileName, content);
				
				resetEvent.Set ();
			}
		}
		
		public void Dispose ()
		{
			autoSaveThreadRunning = false;
			if (autoSaveThread != null) {
				resetEvent.Set ();
				autoSaveThread.Join ();
				autoSaveThread = null;
			}
		}
#endregion
	
	}
}
