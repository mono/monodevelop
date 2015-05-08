//
// IFoldSegment.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;
using System.Web.UI.WebControls;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// Represents the origin for a fold segment
	/// </summary>
	public enum FoldingType {
		Unknown,
		Region,
		TypeDefinition,
		TypeMember,
		Comment
	}

	/// <summary>
	/// A fold segment represents a collapsible region inside the text editor.
	/// </summary>
	public interface IFoldSegment : ISegment
	{
		/// <summary>
		/// Gets or sets a value indicating whether this fold segment is collapsed.
		/// </summary>
		bool IsCollapsed {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the collapsed text. This is displayed when the folding is collapsed instead of the collapsed region.
		/// </summary>
		string CollapsedText {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the type of the folding. This type gives some info about where this folding segment
		/// originates from.
		/// </summary>
		FoldingType FoldingType {
			get;
			set;
		}
	}
}

