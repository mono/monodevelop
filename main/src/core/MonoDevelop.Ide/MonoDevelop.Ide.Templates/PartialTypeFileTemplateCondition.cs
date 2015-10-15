//
// PartialTypeFileTemplateCondition.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Xml;

using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	
	public class PartialTypeFileTemplateCondition : FileTemplateCondition
	{
		PartialTypeRequirement filter = PartialTypeRequirement.None;

		public override void Load (XmlElement element)
		{
			filter = PartialTypeRequirement.None;
			try {
				filter = (PartialTypeRequirement) Enum.Parse (typeof (PartialTypeRequirement), element.GetAttribute ("Requirement"), true);
			} catch (ArgumentException) {
				throw new InvalidOperationException ("Invalid value for PartialTypeRequirement condition in template.");
			}
		}
		
		public override bool ShouldEnableFor (Project proj, string creationPath, string language)
		{
			if (proj == null)
				//FIXME: check the language's capabilities
				return false;
			
			DotNetProject dnp = proj as DotNetProject;
			if (dnp == null)
				return false;

			bool supported = dnp.SupportsPartialTypes;
			bool enabled = dnp.UsePartialTypes;
			
			switch (filter) {
			case PartialTypeRequirement.None:
				return true;
			case PartialTypeRequirement.Unsupported:
				return (supported == false);
			case PartialTypeRequirement.Supported:
				return (supported == true);
			case PartialTypeRequirement.Disabled:
				return (enabled == false);	
			case PartialTypeRequirement.Enabled:
				return (enabled == true);
			}
			
			return true;
		}
		
		enum PartialTypeRequirement
		{
			None = 0,
			Unsupported = 1,
			Supported = 2,
			Disabled = 3,
			Enabled = 4,
		}
	}
}
