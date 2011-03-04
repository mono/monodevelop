// 
// FileTypeCondition.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;

namespace MonoDevelop.Ide.Extensions
{
	public class FileTypeCondition: ConditionType
	{
		string fileName;
		string mimeType;
		
		public void SetFileName (string file)
		{
			if (file != fileName) {
				fileName = file;
				if (fileName != null)
					mimeType = DesktopService.GetMimeTypeForUri (fileName);
				NotifyChanged ();
			}
		}
		
		public override bool Evaluate (NodeElement conditionNode)
		{
			if (fileName == null)
				return false;
			
			string[] fileExtensions = conditionNode.GetAttribute ("fileExtensions").Split (new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
			string[] mimeTypes = conditionNode.GetAttribute ("mimeTypes").Split (new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
			string[] names = conditionNode.GetAttribute ("name").Split (new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
			
			if (fileExtensions.Length > 0) {
				string ext = System.IO.Path.GetExtension (fileName);
				if (fileExtensions.Any (fe => string.Compare (fe, ext, StringComparison.OrdinalIgnoreCase) == 0))
					return true;
			}
			if (mimeTypes.Length > 0) {
				if (mimeTypes.Any (t => DesktopService.GetMimeTypeIsSubtype (mimeType, t)))
					return true;
			}
			if (names.Length > 0) {
				string name = System.IO.Path.GetFileName (fileName);
				if (names.Any (fn => string.Compare (fn, name, StringComparison.OrdinalIgnoreCase) == 0))
					return true;
			}
			return false;
		}
	}
}
