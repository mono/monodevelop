//
// ClrVersionFileTemplateCondition.cs
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

using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	
	public class ClrVersionFileTemplateCondition : FileTemplateCondition
	{
		private ClrVersion clrVersion;
		private ClrVersionCondition condition;
		
		public override void Load (XmlElement element)
		{
			clrVersion = ClrVersion.Default;
			try {
				clrVersion = (ClrVersion) Enum.Parse (typeof (ClrVersion), element.GetAttribute ("ClrVersion"), true);
			} catch (ArgumentException) {
				throw new InvalidOperationException ("Invalid value for ClrVersion condition in template.");
			}
			
			condition = ClrVersionCondition.None;
			try {
				condition = (ClrVersionCondition) Enum.Parse (typeof (ClrVersionCondition), element.GetAttribute ("Condition"), true);
			} catch (ArgumentException) {
				throw new InvalidOperationException ("Invalid value for ClrVersionCondition condition in template.");
			}
		}
		
		public override bool ShouldEnableFor (Project proj, string creationPath)
		{
			if (condition == ClrVersionCondition.None)
				return true;
			
			DotNetProject dnp = proj as DotNetProject;
			if (dnp != null) {
#pragma warning disable CS0618 // Type or member is obsolete
				ClrVersion pver = dnp.TargetFramework.ClrVersion;
#pragma warning restore CS0618 // Type or member is obsolete
				switch (condition) {
				case ClrVersionCondition.Equal:
					return (pver == clrVersion);
				case ClrVersionCondition.NotEqual:
					return (pver != clrVersion);
				case ClrVersionCondition.GreaterThan:
					return (pver > clrVersion);
				case ClrVersionCondition.GreaterThanOrEqual:
					return (pver >= clrVersion);
				case ClrVersionCondition.LessThan:
					return (pver < clrVersion);
				case ClrVersionCondition.LessThanOrEqual:
					return (pver <= clrVersion);							
				}
			}
			
			return false;
		}
		
		enum ClrVersionCondition
		{
			None = 0,
			Equal = 1,
			NotEqual = 2,
			GreaterThan = 3,
			GreaterThanOrEqual = 4,
			LessThan = 5,
			LessThanOrEqual = 6,
		}
	}
}
