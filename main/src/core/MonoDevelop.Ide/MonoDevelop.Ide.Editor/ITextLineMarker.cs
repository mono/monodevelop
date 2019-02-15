//
// ITextLineMarker.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor
{
	[Obsolete ("Old editor")]
	public interface ITextLineMarker
	{
		IDocumentLine Line {
			get;
		}

		bool IsVisible {
			get;
			set;
		}

		object Tag {
			get;
			set;
		}
	}

	[Obsolete ("Old editor")]
	public enum UrlType
	{
		Unknown,
		Url,
		Email
	}

	[Obsolete ("Old editor")]
	public interface IUrlTextLineMarker : ITextLineMarker
	{
		UrlType UrlType {
			get;
		}

		string Url {
			get;
		}
	}

	[Obsolete ("Old editor")]
	public interface ICurrentDebugLineTextMarker :  ITextLineMarker
	{

	}

	[Obsolete ("Old editor")]
	public interface IMessageBubbleLineMarker : ITextLineMarker
	{
		int TaskCount { get; }

		MonoDevelop.Ide.Tasks.TaskListEntry PrimaryTask { get; set; }

		IEnumerable<MonoDevelop.Ide.Tasks.TaskListEntry> Tasks { get; }

		void AddTask (MonoDevelop.Ide.Tasks.TaskListEntry task);
	}
}