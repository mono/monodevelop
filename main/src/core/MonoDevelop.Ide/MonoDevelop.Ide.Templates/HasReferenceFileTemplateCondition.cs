﻿//
// HasReferenceFileTemplateCondition.cs
//
// Author:
//       therzok <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2017 (c) Marius Ungureanu
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
using System.Xml;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	public class HasReferenceFileTemplateCondition : FileTemplateCondition
	{
		string reference;
		public override void Load (XmlElement element)
		{
			reference = element.GetAttribute ("Assembly");
			if (string.IsNullOrWhiteSpace (reference))
				throw new InvalidOperationException ("Invalid value for Assembly condition in template.");
		}

		public override bool ShouldEnableFor (Project proj, string projectPath)
		{
			var dnp = proj as DotNetProject;
			if (dnp != null) {
				return dnp.References.Where (x => x.ReferenceType != ReferenceType.Project).Any (x => {
					var trimmed = x.StoredReference.TrimStart ();
					int commaIndex = trimmed.IndexOf (',');
					return trimmed.IndexOf (reference, 0, commaIndex, StringComparison.Ordinal) == 0;
				});
			}
			return false;
		}
	}
}
