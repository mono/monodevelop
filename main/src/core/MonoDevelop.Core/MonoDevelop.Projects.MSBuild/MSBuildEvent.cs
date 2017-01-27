//
// MSBuildEvent.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
namespace MonoDevelop.Projects.MSBuild
{
	[Flags]
	public enum MSBuildEvent
	{
		None = 0,
		BuildStarted = 1 << 0,
		BuildFinished = 1 << 1,
		ProjectStarted = 1 << 2,
		ProjectFinished = 1 << 3,
		TargetStarted = 1 << 4,
		TargetFinished = 1 << 5,
		TaskStarted = 1 << 6,
		TaskFinished = 1 << 7,
		ErrorRaised = 1 << 8,
		WarningRaised = 1 << 9,
		MessageRaised = 1 << 10,
		CustomEventRaised = 1 << 11,
		All = 0xffff
	}
}
