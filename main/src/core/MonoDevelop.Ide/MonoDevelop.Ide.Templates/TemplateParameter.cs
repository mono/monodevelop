//
// TemplateParameter.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	class TemplateParameter
	{
		public TemplateParameter (string parameter)
		{
			Parse (parameter);
		}

		public string Name { get; private set; }
		public string Value { get; private set; }
		public bool IsValid { get; private set; }

		void Parse (string parameter)
		{
			int index = parameter.IndexOf ('=');
			if (index <= 0) {
				IsValid = false;
				Name = String.Empty;
				Value = String.Empty;
				return;
			}

			IsValid = true;
			Name = parameter.Substring (0, index).Trim ();
			Value = parameter.Substring (index + 1).Trim ();
		}

		public static IEnumerable<TemplateParameter> CreateParameters (string condition)
		{
			string[] parts = condition.Split (new [] {';', ','}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string part in parts) {
				var parameter = new TemplateParameter (part);
				if (!parameter.IsValid) {
					LoggingService.LogWarning ("Invalid template condition '{0}'", condition);
				}
				yield return parameter;
			}
		}
	}
}

