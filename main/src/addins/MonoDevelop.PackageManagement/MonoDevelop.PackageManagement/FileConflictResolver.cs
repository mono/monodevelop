// 
// FileConflictResolver.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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
using MonoDevelop.Ide;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class FileConflictResolver : IFileConflictResolver
	{
		static AlertButton YesToAllButton = new AlertButton ("Yes to All");
		static AlertButton NoToAllButton = new AlertButton ("No to All");
		
		AlertButton[] buttons = new AlertButton[] {
			AlertButton.Yes,
			YesToAllButton,
			AlertButton.No,
			NoToAllButton
		};
		
		const int YesButtonIndex = 0;
		const int YesToAllButtonIndex = 1;
		const int NoButtonIndex = 2;
		const int NoToAllButtonIndex = 3;
		
		public FileConflictResolution ResolveFileConflict(string message)
		{
			AlertButton result = MessageService.AskQuestion(
				"File Conflict",
				message,
				NoButtonIndex, // "No" is default accept button.
				buttons);
			return MapResultToFileConflictResolution(result);
		}
		
		FileConflictResolution MapResultToFileConflictResolution(AlertButton result)
		{
			if (result == AlertButton.Yes) {
				return FileConflictResolution.Overwrite;
			} else if (result == YesToAllButton) {
				return FileConflictResolution.OverwriteAll;
			} else if (result == NoToAllButton) {
				return FileConflictResolution.IgnoreAll;
			} else {
				return FileConflictResolution.Ignore;
			}
		}
	}
}
