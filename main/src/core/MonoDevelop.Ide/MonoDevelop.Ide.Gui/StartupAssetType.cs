//
// StartupAssetType.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Web.UI;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// Indicates whether a document or solution was opened on starting the IDE.
	/// </summary>
	class StartupAssetType
	{
		public static StartupAssetType None = new StartupAssetType (0, nameof (None));
		public static StartupAssetType Document = new StartupAssetType (1, nameof (Document));
		public static StartupAssetType Solution = new StartupAssetType (2, nameof (Solution));

		StartupAssetType (int id, string name)
		{
			Id = id;
			Name = name;
		}

		public int Id { get; private set; }
		public string Name { get; private set; }

		public static StartupAssetType FromStartupInfo (StartupInfo startupInfo)
		{
			if (startupInfo.OpenedRecentProject || startupInfo.HasSolutionFile) {
				return Solution;
			} else if (startupInfo.OpenedFiles) {
				return Document;
			}
			return None;
		}
	}
}
