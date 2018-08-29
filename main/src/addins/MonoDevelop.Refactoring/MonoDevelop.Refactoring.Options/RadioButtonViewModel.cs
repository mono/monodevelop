//
// RadioButtonViewModel.cs
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
	internal class RadioButtonViewModel<TOption> : AbstractRadioButtonViewModel
	{
		private readonly Option<TOption> _option;
		private readonly TOption _value;

		public RadioButtonViewModel (string description, string preview, string group, TOption value, Option<TOption> option, AbstractOptionPreviewViewModel info, OptionSet options)
			: base (description, preview, info, options, isChecked: options.GetOption (option).Equals (value), group: group)
		{
			_value = value;
			_option = option;
		}

		internal override void SetOptionAndUpdatePreview (AbstractOptionPreviewViewModel info, string preview)
		{
			info.SetOptionAndUpdatePreview (_value, _option, preview);
		}
	}
}
