//
// DocumentControllerSupport.cs
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
namespace MonoDevelop.Ide.Gui.Documents
{
	public class DocumentControllerSupport
	{
		/// <summary>
		/// Whether the display binding can be used as the default handler for the content types
		/// that it handles. If this is false, the factory is only used when the user explicitly picks it.
		/// </summary>
		public bool CanUseAsDefault { get; set; }

		/// <summary>
		/// Role that the new controller will have
		/// </summary>
		public DocumentControllerRole Role { get; set; }

		/// <summary>
		/// Name to show in visualizer selectors
		/// </summary>
		/// <value>The display name.</value>
		public string DisplayName { get; set; }
	}
}
