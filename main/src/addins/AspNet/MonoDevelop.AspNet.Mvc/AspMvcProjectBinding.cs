// 
// AspMvcProjectBinding.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNet.Mvc
{
	
	
	public class AspMvc1ProjectBinding : IProjectBinding
	{
		public Project CreateProject (ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return new AspMvc1Project (lang, info, projectOptions);
		}
		
		public Project CreateSingleFileProject (string sourceFile)
		{
			throw new InvalidOperationException ();
		}
		
		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return false;
		}
		
		public string Name {
			get { return "AspNetMvc1"; }
		}
	}

	public class AspMvc2ProjectBinding : IProjectBinding
	{
		public Project CreateProject (ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return new AspMvc2Project (lang, info, projectOptions);
		}

		public Project CreateSingleFileProject (string sourceFile)
		{
			throw new InvalidOperationException ();
		}

		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return false;
		}

		public string Name
		{
			get { return "AspNetMvc2"; }
		}
	}

	public class AspMvc3ProjectBinding : IProjectBinding
	{
		public Project CreateProject (ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return new AspMvc3Project (lang, info, projectOptions);
		}

		public Project CreateSingleFileProject (string sourceFile)
		{
			throw new InvalidOperationException ();
		}

		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return false;
		}

		public string Name
		{
			get { return "AspNetMvc3"; }
		}
	}

	public class AspMvc4ProjectBinding : IProjectBinding
	{
		public Project CreateProject (ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			string lang = projectOptions.GetAttribute ("language");
			return new AspMvc4Project (lang, info, projectOptions);
		}
		
		public Project CreateSingleFileProject (string sourceFile)
		{
			throw new InvalidOperationException ();
		}
		
		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return false;
		}
		
		public string Name
		{
			get { return "AspNetMvc4"; }
		}
	}
}
