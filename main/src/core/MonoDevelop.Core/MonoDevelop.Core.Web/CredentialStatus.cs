// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client

namespace MonoDevelop.Core.Web
{
	/// <summary>
	/// Result of an attempt to acquire credentials.
	/// Keep in sync with NuGet.VisualStudio.VsCredentialStatus
	/// </summary>
	enum CredentialStatus
	{
		Success,
		ProviderNotApplicable,
		UserCanceled
	}
}