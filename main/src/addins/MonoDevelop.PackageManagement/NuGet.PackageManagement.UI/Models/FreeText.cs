// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.PackageManagement.UI
{
	internal class FreeText : IText
	{
		public FreeText (string text)
		{
			Text = text;
		}

		public string Text { get; }
	}
}