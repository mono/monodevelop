//
// TemplateConditionEvaluator.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014-2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.Templates
{
	static class TemplateConditionEvaluator
	{
		public static bool EvaluateCondition (IStringTagModel model, string condition)
		{
			if (string.IsNullOrWhiteSpace (condition))
				return true;

			condition = condition.Trim ();

			string parameter = GetNotConditionParameterName (condition);
			if (parameter != null) {
				return !model.GetBoolValue (parameter);
			}

			return model.GetBoolValue (condition);
		}

		static string GetNotConditionParameterName (string createCondition)
		{
			if (createCondition.StartsWith ("!", StringComparison.Ordinal)) {
				return createCondition.Substring (1).TrimStart ();
			}

			return null;
		}
	}
}

