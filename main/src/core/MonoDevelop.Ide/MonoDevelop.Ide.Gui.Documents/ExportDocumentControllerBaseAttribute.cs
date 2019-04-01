//
// ExportDocumentControllerBaseAttribute.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide.Gui.Documents
{
	[AttributeUsage (AttributeTargets.Class)]
	public class ExportDocumentControllerBaseAttribute : CustomExtensionAttribute
	{
		string [] fileExtensions;
		string [] mimeTypes;
		FileNameEvalutor patternEvaluator;

		/// <summary>
		/// File extension to which the controller applies. Several extensions can be provided using comma as separator.
		/// </summary>
		[NodeAttribute ("fileExtension")]
		public string FileExtension { get; set; }

		/// <summary>
		/// Mime types to which the controller applies. Several extensions can be provided using comma as separator.
		/// </summary>
		[NodeAttribute ("mimeType")]
		public string MimeType { get; set; }

		/// <summary>
		/// File pattern to which the controller applies. Several patterns can be provided using comma as separator, for example "foo.*, *.txt"
		/// </summary>
		[NodeAttribute ("filePattern")]
		public string FilePattern { get; set; }

		public bool HasFileFilter {
			get {
				return !string.IsNullOrEmpty (FileExtension) ||
					!string.IsNullOrEmpty (MimeType) ||
					!string.IsNullOrEmpty (FilePattern);
			}
		}

		string [] ParseList (string arg)
		{
			if (!string.IsNullOrEmpty (arg)) {
				return arg.Split (',').Select (e => e.Trim ()).Where (e => e.Length > 0).ToArray ();
			}
			return Array.Empty<string> ();
		}

		public bool CanHandle (FilePath filePath, string mimeType)
		{
			if (fileExtensions == null) {
				fileExtensions = ParseList (FileExtension);
				mimeTypes = ParseList (MimeType);
				var filePatterns = ParseList (FilePattern);
				if (filePatterns.Length > 0)
					patternEvaluator = FileNameEvalutor.CreateFileNameEvaluator (filePatterns, ',');
			}

			if (fileExtensions.Contains ("*") || mimeTypes.Contains ("*") || FilePattern == "*")
				return true;

			if (fileExtensions.Length > 0) {
				string ext = System.IO.Path.GetExtension (filePath);
				foreach (var allowedExtension in fileExtensions) {
					if (string.Equals (ext, allowedExtension, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}

			if (mimeTypes.Length > 0) {
				IEnumerable<string> mimeTypeChain;
				if (!string.IsNullOrEmpty (mimeType))
					mimeTypeChain = IdeServices.DesktopService.GetMimeTypeInheritanceChain (mimeType);
				else
					mimeTypeChain = IdeServices.DesktopService.GetMimeTypeInheritanceChainForFile (filePath);
				foreach (var mt in mimeTypeChain) {
					foreach (var allowedMime in mimeTypes) {
						if (mt == allowedMime)
							return true;
					}
				}
			}

			if (patternEvaluator != null) {
				string name = filePath.FileName;
				if (patternEvaluator.SupportsFile (name))
					return true;
			}

			return false;
		}
	}
}
