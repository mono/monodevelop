// 
// NSObjectInfo.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectTypeInfo
	{
		public NSObjectTypeInfo (string objcName, string cliName, string baseObjCName, string baseCliName, bool isModel)
		{
			this.ObjCName = objcName;
			this.CliName = cliName;
			this.BaseObjCType = baseObjCName;
			this.BaseCliType = baseCliName;
			this.IsModel = isModel;
			
			Outlets = new List<IBOutlet> ();
			Actions = new List<IBAction> ();
			UserTypeReferences = new HashSet<string> ();
		}
		
		public string ObjCName { get; private set; }
		public string CliName { get; private set; }
		public bool IsModel { get; internal set; }
		
		public string BaseObjCType { get; internal set; }
		public string BaseCliType { get; internal set; } 
		public bool BaseIsModel { get; internal set; }
		
		public bool IsUserType { get; internal set; }
		
		public List<IBOutlet> Outlets { get; private set; }
		public List<IBAction> Actions { get; private set; }
		
		public HashSet<string> UserTypeReferences { get; private set; }
	}
}