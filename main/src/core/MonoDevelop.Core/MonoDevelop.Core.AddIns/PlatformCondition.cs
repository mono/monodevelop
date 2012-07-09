// 
// PlatformCondition.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using Mono.Addins;

namespace MonoDevelop.Core.AddIns
{
	class PlatformCondition: ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			string plat = conditionNode.GetAttribute ("value");
			bool negate = false;
			if (plat.StartsWith ("!")) {
				plat = plat.Substring (1);
				negate = true;
			}
			bool result;
			switch (plat.ToLower ()) {
				case "windows":
				case "win32":
					result = Platform.IsWindows; break;
				case "mac":
				case "macos":
				case "macosx":
					result = Platform.IsMac; break;
				case "unix":
				case "linux":
					result = !Platform.IsMac && !Platform.IsWindows; break;
				default:
					result = false; break;
			}
			return negate ? !result : result;
		}
	}
}
