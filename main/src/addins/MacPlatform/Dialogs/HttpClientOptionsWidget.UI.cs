//
// HttpClientOptionsWidget.UI.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.MacIntegration
{
	partial class HttpClientOptionsWidget : Widget
	{
		ComboBox httpClientHandlerComboBox;
		HttpClientImplementation nsUrlSessionHttpClientImplementation = new HttpClientImplementation {
			Name = GettextCatalog.GetString ("NSUrlSession (default)")
		};
		HttpClientImplementation managedHttpClientImplementation = new HttpClientImplementation {
			Name = GettextCatalog.GetString ("Managed")
		};

		void Build ()
		{
			var mainHBox = new HBox ();

			var httpClientHandlerLabel = new Label ();
			httpClientHandlerLabel.Name = nameof (httpClientHandlerLabel);
			httpClientHandlerLabel.Text = GettextCatalog.GetString ("HttpClient implementation:");
			mainHBox.PackStart (httpClientHandlerLabel);

			httpClientHandlerComboBox = new ComboBox ();
			httpClientHandlerComboBox.Name = nameof (httpClientHandlerComboBox);
			httpClientHandlerComboBox.SetCommonAccessibilityAttributes (
				nameof (httpClientHandlerComboBox),
				httpClientHandlerLabel,
				GettextCatalog.GetString ("Select HttpClient implementation"));
			mainHBox.PackStart (httpClientHandlerComboBox);

			httpClientHandlerComboBox.Items.Add (nsUrlSessionHttpClientImplementation);
			httpClientHandlerComboBox.Items.Add (managedHttpClientImplementation);

			Content = mainHBox;
		}

		class HttpClientImplementation
		{
			public string Name;

			public override string ToString ()
			{
				return Name;
			}
		}
	}
}
