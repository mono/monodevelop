//
// OptionBinding.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.Refactoring.Options
{
	internal class OptionBinding<T>
	{
		private readonly IOptionService _optionService;
		private readonly Option<T> _key;

		public OptionBinding (IOptionService optionService, Option<T> key)
		{
			_optionService = optionService;
			_key = key;
		}

		public T Value {
			get {
				return _optionService.GetOption (_key);
			}

			set {
				var oldOptions = _optionService.GetOptions ();
				var newOptions = oldOptions.WithChangedOption (_key, value);

				_optionService.SetOptions (newOptions);
				//OptionLogger.Log (oldOptions, newOptions);
			}
		}
	}
}
