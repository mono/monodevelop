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
using System.Linq;

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
		
		public string[] DefinedIn { get; internal set; }
		
		public HashSet<string> UserTypeReferences { get; private set; }
		
		public void GenerateObjcType (string directory)
		{
			if (IsModel)
				throw new ArgumentException ("Cannot generate definition for model");
			
			string hFilePath = System.IO.Path.Combine (directory, ObjCName + ".h");
			string mFilePath = System.IO.Path.Combine (directory, ObjCName + ".m");
			
			using (var sw = System.IO.File.CreateText (hFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				sw.WriteLine ("#import <UIKit/UIKit.h>");
				foreach (var reference in UserTypeReferences) {
					sw.WriteLine ("#import \"{0}.h\"", reference);
				}
				sw.WriteLine ();
				
				sw.WriteLine ("@interface {0} : {1} {{", ObjCName, BaseIsModel? "NSObject" : BaseObjCType);
				foreach (var outlet in Outlets) {
					sw.WriteLine ("\t{0} *_{1};", outlet.ObjCType, outlet.ObjCName);
				}
				sw.WriteLine ("}");
				sw.WriteLine ();
				
				foreach (var outlet in Outlets) {
					sw.WriteLine ("@property (nonatomic, retain) IBOutlet {0} *{1};", outlet.ObjCType, outlet.ObjCName);
					sw.WriteLine ();
				}
				
				foreach (var action in Actions) {
					if (action.Parameters.Any (p => p.ObjCType == null))
						continue;
					WriteActionSignature (action, sw);
					sw.WriteLine (";");
					sw.WriteLine ();
				}
				
				sw.WriteLine ("@end");
			}
			
			using (var sw = System.IO.File.CreateText (mFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				sw.WriteLine ("#import \"{0}.h\"", ObjCName);
				sw.WriteLine ();
				
				sw.WriteLine ("@implementation {0}", ObjCName);
				sw.WriteLine ();
				
				bool hasOutlet = false;
				foreach (var outlet in Outlets) {
					sw.WriteLine ("@synthesize {0} = _{0};", outlet.ObjCName);
					hasOutlet = true;
				}
				if (hasOutlet)
					sw.WriteLine ();
				
				foreach (var action in Actions) {
					if (action.Parameters.Any (p => p.ObjCType == null))
						continue;
					WriteActionSignature (action, sw);
					sw.WriteLine (" {");
					sw.WriteLine ("}");
					sw.WriteLine ();
				}
				
				sw.WriteLine ("@end");
			}
		}
		
		static string modificationWarning =
			"// WARNING\n" +
			"// This file has been generated automatically by MonoDevelop to\n" +
			"// mirror C# types. Changes in this file made by drag-connecting\n" +
			"// from the UI designer will be synchronized back to C#, but\n" +
			"// more complex manual changes may not transfer correctly.\n";
		
		void WriteActionSignature (IBAction action, System.IO.TextWriter writer)
		{
			writer.Write ("- (IBAction){0}", action.ObjCName);
			bool isFirst = true;
			
			foreach (var param in action.Parameters) {
				string paramType = param.ObjCType;
				if (isFirst && paramType == "NSObject")
					paramType = "id";
				else
					paramType = paramType + " *";
				
				if (isFirst) {
					isFirst = false;
					writer.Write (":({0}){1}", paramType, param.Name);
				} else {
					writer.Write (" {0}:({1}){2}", param.Label, paramType, param.Name);
				}
			}	
		}
	}
}