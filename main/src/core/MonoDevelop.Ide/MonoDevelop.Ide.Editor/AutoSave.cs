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
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Core.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// This class handles the auto save mechanism for open files.
	/// It should only be used by editor implementations.
	/// </summary>
	static class AutoSave
	{
		//FIXME: is this path a good one? wouldn't it be better to put autosaves beside the files anyway?
		static string autoSavePath = UserProfile.Current.CacheDir.Combine ("AutoSave");
		static bool autoSaveEnabled;
		
		static AutoSave ()
		{
			try {
				if (!Directory.Exists (autoSavePath))
					Directory.CreateDirectory (autoSavePath);
			} catch (Exception e) {
				LoggingService.LogError ("Can't create auto save path:" + autoSavePath +". Auto save is disabled.", e);
				autoSaveEnabled = false;
				return;
			}
			autoSaveEnabled = true;
		}

		static string GetAutoSaveFileName (string fileName)
		{
			if (fileName == null)
				return null;
			return Path.Combine (autoSavePath, GetMD5 (fileName) + ".sav");
		}

		static MD5 md5 = MD5.Create ();
		static string GetMD5 (string data)
		{
			var result = StringBuilderCache.Allocate();
			foreach (var b in md5.ComputeHash (Encoding.ASCII.GetBytes (data))) {
				result.Append(b.ToString("X2"));
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		/// <summary>
		/// Returns true if an auto save exists for the given file name.
		/// </summary>
		public static bool AutoSaveExists (string fileName)
		{
			if (!autoSaveEnabled)
				return false;
			try {
				var autoSaveFilename = GetAutoSaveFileName (fileName);
				bool autoSaveExists = File.Exists (autoSaveFilename);
				if (autoSaveExists) {
					if (File.GetLastWriteTimeUtc (autoSaveFilename) < File.GetLastWriteTimeUtc (fileName)) {
						File.Delete (autoSaveFilename);
						return false;
					}
				}
				return autoSaveExists;
			} catch (Exception e) {
				LoggingService.LogError ("Error in auto save - disableing.", e);
				DisableAutoSave ();
				return false;
			}
		}

		static void CreateAutoSave (string fileName, ITextSource content)
		{
			if (!autoSaveEnabled)
				return;
			try {
				// Directory may have removed/unmounted. Therefore this operation is not guaranteed to work.
				var autosaveFileName = GetAutoSaveFileName (fileName);
				if (File.Exists (autosaveFileName))
					File.Delete (autosaveFileName);
				content.WriteTextTo (autosaveFileName);
				Counters.AutoSavedFiles++;
			} catch (Exception e) {
				LoggingService.LogError ("Error in auto save while creating: " + fileName +". Disableing auto save.", e);
				DisableAutoSave ();
			}
		}

		#region AutoSave

		/// <summary>
		/// Loads the content from an auto save file and removes the auto save file.
		/// </summary>
		public static ITextSource LoadAndRemoveAutoSave (string fileName)
		{
			string autoSaveFileName = GetAutoSaveFileName (fileName);
			var result = StringTextSource.ReadFrom (autoSaveFileName);
			AutoSave.RemoveAutoSaveFile (fileName);
			return result;
		}

		/// <summary>
		/// Loads the content from an auto save file.
		/// </summary>
		public static ITextSource LoadAutoSave (string fileName)
		{
			string autoSaveFileName = GetAutoSaveFileName (fileName);
			return StringTextSource.ReadFrom (autoSaveFileName);
		}

		/// <summary>
		/// Removes the auto save file.
		/// </summary>
		/// <param name="fileName">The file name for which the auto save file should be removed.</param>
		public static void RemoveAutoSaveFile (string fileName)
		{
			if (!autoSaveEnabled)
				return;
			if (AutoSaveExists (fileName)) {
				string autoSaveFileName = GetAutoSaveFileName (fileName);
				try {
					File.Delete (autoSaveFileName);
				} catch (Exception e) {
					LoggingService.LogError ("Can't delete auto save file: " + autoSaveFileName +". Disableing auto save.", e);
					DisableAutoSave ();
				}
			}
		}
		static Task finishedTask = Task.FromResult (true);
		internal static Task InformAutoSaveThread (ITextSource content, string fileName, bool isDirty)
		{
			if (content == null)
				throw new ArgumentNullException (nameof (content));
			if (!autoSaveEnabled || string.IsNullOrEmpty (fileName))
				return finishedTask;
			if (isDirty) {
				return Task.Run (() => {
					CreateAutoSave (fileName, content);
				});
			} else {
				RemoveAutoSaveFile (fileName);
				return finishedTask;
			}
		}

		static void DisableAutoSave ()
		{
			autoSaveEnabled = false;
		}
#endregion
	}
}
