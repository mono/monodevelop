//
// TemplateCondition.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	public class TemplateCondition
	{
		public static readonly TemplateCondition Null = new TemplateCondition (null);

		string condition;
		TemplateParameter parameter;

		public TemplateCondition (string condition)
		{
			this.condition = condition;
		}

		public bool IsExcluded (ProjectCreateParameters parameters)
		{
			if (String.IsNullOrEmpty (condition)) {
				return false;
			}

			if (parameter == null) {
				parameter = new TemplateParameter (condition);
				if (!parameter.IsValid) {
					LoggingService.LogWarning ("Invalid template condition '{0}'", condition);
				}
			}

			return !String.Equals (parameters [parameter.Name], parameter.Value, StringComparison.OrdinalIgnoreCase);
		}

		public override string ToString ()
		{
			return condition;
		}
	}
}

