//
// CheckBoxOptionViewModel.cs
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
	internal class CheckBoxOptionViewModel : AbstractCheckBoxViewModel
	{
		public CheckBoxOptionViewModel (IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options)
			: this (option, description, preview, preview, info, options)
		{
		}

		public CheckBoxOptionViewModel (IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options)
			: base (option, description, truePreview, falsePreview, info)
		{
			SetProperty (ref _isChecked, (bool)options.GetOption (new OptionKey (option, option.IsPerLanguage ? info.Language : null)));
		}

		public override bool IsChecked {
			get {
				return _isChecked;
			}

			set {
				SetProperty (ref _isChecked, value);
				Info.SetOptionAndUpdatePreview (_isChecked, Option, GetPreview ());
			}
		}
	}
}
