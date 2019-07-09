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
using System.Collections.Generic;
using MonoDevelop.Projects.Text;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.TextEditing
{
	/// <summary>
	/// Offers several methods for tracking changes being done in edited files
	/// </summary>
	[DefaultServiceImplementation]
	public class TextEditorService: Service
	{
		Dictionary<FilePath,List<FileExtension>> fileExtensions = new Dictionary<FilePath,List<FileExtension>> ();

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
		public void NotifyLineCountChanged (ITextFile textFile, int lineNumber, int lineCount, int column)
		{
			foreach (var ext in GetFileLineExtensions (textFile.Name).Where (ex => ex.TrackLinePosition)) {
				if (ext.Line > lineNumber) {
					if (ext.Line + lineCount < lineNumber)
						ext.NotifyDeleted ();
					if (ext.OriginalLine == -1)
						ext.OriginalLine = ext.Line;
					ext.Line += lineCount;
					ext.Refresh ();
				} else if (ext.Line == lineNumber && lineCount < 0) {
					ext.NotifyDeleted ();
					if (ext.OriginalLine == -1)
						ext.OriginalLine = ext.Line;
					ext.Line += lineCount;
					ext.Refresh ();
				}
			}

			if (LineCountChanged != null)
				LineCountChanged (textFile, new LineCountEventArgs (textFile, lineNumber, lineCount, column));
		}

		/// <summary>
		/// Notifies to the text editor service that all previous line change notifications for a file have to be discarded
		/// </summary>
		/// <param name='textFile'>
		/// Text file.
		/// </param>
		public void NotifyLineCountChangesReset (ITextFile textFile)
		{
			foreach (var ext in GetFileLineExtensions (textFile.Name).Where (ex => ex.TrackLinePosition)) {
				if (ext.OriginalLine != -1) {
					ext.Line = ext.OriginalLine;
					ext.OriginalLine = -1;
					ext.Refresh ();
				}
			}
			if (LineCountChangesReset != null)
				LineCountChangesReset (textFile, new TextFileEventArgs (textFile));
		}

		/// <summary>
		/// Notifies to the text editor service that all previous line change notifications for a file have to be committed
		/// </summary>
		/// <param name='textFile'>
		/// Text file.
		/// </param>
		public void NotifyLineCountChangesCommitted (ITextFile textFile)
		{
			foreach (var ext in GetFileLineExtensions (textFile.Name).Where (ex => ex.TrackLinePosition))
				ext.OriginalLine = -1;

			if (LineCountChangesCommitted != null)
				LineCountChangesCommitted (textFile, new TextFileEventArgs (textFile));
		}
		
		IEnumerable<FileLineExtension> GetFileLineExtensions (FilePath file)
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
		public void RegisterExtension (FileExtension extension)
		{
			extension.TextEditorService = this;
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
		public void UnregisterExtension (FileExtension extension)
		{
			List<FileExtension> list;
			if (!fileExtensions.TryGetValue (extension.File, out list))
				return;
			if (list.Remove (extension))
				NotifyExtensionRemoved (extension);
		}

		void NotifyExtensionAdded (FileExtension extension)
		{
			if (FileExtensionAdded != null)
				FileExtensionAdded (null, new FileExtensionEventArgs () { Extension = extension });
		}

		void NotifyExtensionRemoved (FileExtension extension)
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
		public FileExtension[] GetFileExtensions (FilePath file)
		{
			List<FileExtension> list;
 			if (!fileExtensions.TryGetValue (file, out list))
				return new FileExtension[0];
			else
				return list.ToArray ();
		}

		internal void Refresh (FileExtension ext)
		{
			NotifyExtensionRemoved (ext);
			NotifyExtensionAdded (ext);
		}

		/// <summary>
		/// Occurs when there has been a line count change in a file being edited
		/// </summary>
		public event EventHandler<LineCountEventArgs> LineCountChanged;

		/// <summary>
		/// Occurs when all previous line change notifications for a file have to be discarded
		/// </summary>
		public event EventHandler<TextFileEventArgs> LineCountChangesReset;

		/// <summary>
		/// Occurs when all previous line change notifications for a file have to be committed
		/// </summary>
		public event EventHandler<TextFileEventArgs> LineCountChangesCommitted;

		/// <summary>
		/// Occurs when a text editor extension has been added
		/// </summary>
		public event EventHandler<FileExtensionEventArgs> FileExtensionAdded;

		/// <summary>
		/// Occurs when a text editor extension has been removed
		/// </summary>
		public event EventHandler<FileExtensionEventArgs> FileExtensionRemoved;
	}
}

