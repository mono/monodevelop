//
// TextEditorService.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using MonoDevelop.Core;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.TextEditing
{
	/// <summary>
	/// Offers several methods for tracking changes being done in edited files
	/// </summary>
	public static class TextEditorService
	{
		static Dictionary<FilePath,List<FileExtension>> fileExtensions = new Dictionary<FilePath,List<FileExtension>> ();

		static TextEditorService ()
		{
			LineCountChanged += delegate(object sender, LineCountEventArgs e) {
				foreach (var ext in GetFileLineExtensions (e.TextFile.Name).Where (ex => ex.TrackLinePosition).ToList ()) {
					if (ext.Line > e.LineNumber) {
						if (ext.Line + e.LineCount < e.LineNumber)
							ext.NotifyDeleted ();
						if (ext.OriginalLine == -1)
							ext.OriginalLine = ext.Line;
						ext.Line += e.LineCount;
						ext.Refresh ();
					}
					else if (ext.Line == e.LineNumber && e.LineCount < 0) {
						ext.NotifyDeleted ();
						if (ext.OriginalLine == -1)
							ext.OriginalLine = ext.Line;
						ext.Line += e.LineCount;
						ext.Refresh ();
					}
				}
			};
			LineCountChangesCommitted += delegate(object sender, TextFileEventArgs e) {
				foreach (var ext in GetFileLineExtensions (e.TextFile.Name).Where (ex => ex.TrackLinePosition))
					ext.OriginalLine = -1;
			};
			LineCountChangesReset += delegate(object sender, TextFileEventArgs e) {
				foreach (var ext in GetFileLineExtensions (e.TextFile.Name).Where (ex => ex.TrackLinePosition)) {
					if (ext.OriginalLine != -1) {
						ext.Line = ext.OriginalLine;
						ext.OriginalLine = -1;
						ext.Refresh ();
					}
				}
			};
		}

		/// <summary>
		/// Notifies to the text editor service that there has been a line count change in a file being edited
		/// </summary>
		/// <param name='textFile'>
		/// File that changed
		/// </param>
		/// <param name='lineNumber'>
		/// Line number
		/// </param>
		/// <param name='lineCount'>
		/// Number of lines added (or removed if negative)
		/// </param>
		/// <param name='column'>
		/// Column.
		/// </param>
		public static void NotifyLineCountChanged (ITextFile textFile, int lineNumber, int lineCount, int column)
		{
			if (LineCountChanged != null)
				LineCountChanged (textFile, new LineCountEventArgs (textFile, lineNumber, lineCount, column));
		}

		/// <summary>
		/// Notifies to the text editor service that all previous line change notifications for a file have to be discarded
		/// </summary>
		/// <param name='textFile'>
		/// Text file.
		/// </param>
		public static void NotifyLineCountChangesReset (ITextFile textFile)
		{
			if (LineCountChangesReset != null)
				LineCountChangesReset (textFile, new TextFileEventArgs (textFile));
		}

		/// <summary>
		/// Notifies to the text editor service that all previous line change notifications for a file have to be committed
		/// </summary>
		/// <param name='textFile'>
		/// Text file.
		/// </param>
		public static void NotifyLineCountChangesCommitted (ITextFile textFile)
		{
			if (LineCountChangesCommitted != null)
				LineCountChangesCommitted (textFile, new TextFileEventArgs (textFile));
		}
		
		static IEnumerable<FileLineExtension> GetFileLineExtensions (FilePath file)
		{
			file = file.CanonicalPath;
			return fileExtensions.Values.SelectMany (e => e).OfType<FileLineExtension> ().Where (e => e.File.CanonicalPath == file);
		}

		/// <summary>
		/// Registers a text editor extension.
		/// </summary>
		/// <param name='extension'>
		/// The extension.
		/// </param>
		public static void RegisterExtension (FileExtension extension)
		{
			List<FileExtension> list;
			if (!fileExtensions.TryGetValue (extension.File, out list))
				list = fileExtensions [extension.File] = new List<FileExtension> ();
			list.Add (extension);
			NotifyExtensionAdded (extension);
		}

		/// <summary>
		/// Unregisters a text editor extension.
		/// </summary>
		/// <param name='extension'>
		/// Extension.
		/// </param>
		public static void UnregisterExtension (FileExtension extension)
		{
			List<FileExtension> list;
			if (!fileExtensions.TryGetValue (extension.File, out list))
				return;
			if (list.Remove (extension))
				NotifyExtensionRemoved (extension);
		}

		static void NotifyExtensionAdded (FileExtension extension)
		{
			if (FileExtensionAdded != null)
				FileExtensionAdded (null, new FileExtensionEventArgs () { Extension = extension });
		}

		static void NotifyExtensionRemoved (FileExtension extension)
		{
			if (FileExtensionRemoved != null)
				FileExtensionRemoved (null, new FileExtensionEventArgs () { Extension = extension });
		}

		/// <summary>
		/// Gets the text editor extensions for a file
		/// </summary>
		/// <returns>
		/// The file extensions.
		/// </returns>
		/// <param name='file'>
		/// File.
		/// </param>
		public static FileExtension[] GetFileExtensions (FilePath file)
		{
			List<FileExtension> list;
 			if (!fileExtensions.TryGetValue (file, out list))
				return new FileExtension[0];
			else
				return list.ToArray ();
		}

		internal static void Refresh (FileExtension ext)
		{
			NotifyExtensionRemoved (ext);
			NotifyExtensionAdded (ext);
		}

		/// <summary>
		/// Occurs when there has been a line count change in a file being edited
		/// </summary>
		public static event EventHandler<LineCountEventArgs> LineCountChanged;

		/// <summary>
		/// Occurs when all previous line change notifications for a file have to be discarded
		/// </summary>
		public static event EventHandler<TextFileEventArgs> LineCountChangesReset;

		/// <summary>
		/// Occurs when all previous line change notifications for a file have to be committed
		/// </summary>
		public static event EventHandler<TextFileEventArgs> LineCountChangesCommitted;

		/// <summary>
		/// Occurs when a text editor extension has been added
		/// </summary>
		public static event EventHandler<FileExtensionEventArgs> FileExtensionAdded;

		/// <summary>
		/// Occurs when a text editor extension has been removed
		/// </summary>
		public static event EventHandler<FileExtensionEventArgs> FileExtensionRemoved;
	}








	
}

