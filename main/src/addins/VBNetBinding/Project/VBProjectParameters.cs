// 
// VBProjectParameters.cs
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.VBNetBinding
{
	public class VBProjectParameters: ProjectParameters
	{
		[ItemProperty ("OptionInfer", DefaultValue="Off")]
		string optionInfer = "Off";
		
		[ItemProperty ("OptionExplicit", DefaultValue="On")]
		string optionExplicit = "On";
		
		[ItemProperty ("OptionCompare", DefaultValue="Binary")]
		string optionCompare = "Binary";
		
		[ItemProperty ("OptionStrict", DefaultValue="Off")]
		string optionStrict = "Off";
		
		[ItemProperty ("MyType", DefaultValue="")]
		string myType = string.Empty;
		
		[ItemProperty ("StartupObject", DefaultValue="")]
		string startupObject = string.Empty;
		
		[ProjectPathItemProperty ("ApplicationIcon", DefaultValue="")]
		string applicationIcon = string.Empty;
		
		[ItemProperty ("CodePage", DefaultValue="")]
		string codePage = string.Empty;
		
		public VBProjectParameters()
		{
		}
		
		public bool OptionInfer {
			get { return optionInfer == "On"; }
			set { optionInfer = value ? "On" : "Off"; }
		}
		
		public bool OptionExplicit {
			get { return optionExplicit == "On"; }
			set { optionExplicit = value ? "On" : "Off"; }
		}

		public bool BinaryOptionCompare {
			get { return optionCompare == "Binary"; }
			set { optionCompare = value ? "Binary" : "Text"; }
		}

		public bool OptionStrict {
			get { return optionStrict == "On"; }
			set { optionStrict = value ? "On" : "Off"; }
		}
		
		public string MyType {
			get { return myType; }
			set { myType = value ?? string.Empty; }
		}
		
		public string StartupObject {
			get { return startupObject; }
			set { startupObject = value ?? string.Empty; }
		}
		
		public string ApplicationIcon {
			get { return applicationIcon; }
			set { applicationIcon = value ?? string.Empty; }
		}
		
		public string CodePage {
			get { return codePage; }
			set { codePage = value ?? string.Empty; }
		}
	}
}
