// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.PackageManagement.UI
{
	internal class LicenseText : IText
	{
		public LicenseText (string text, Uri link)
		{
			Text = text;
			Link = link;
		}

		public string Text { get; set; }
		public Uri Link { get; set; }
	}
}