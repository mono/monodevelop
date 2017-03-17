// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace NuGet.Credentials
{
	[Serializable]
	class ProviderException : Exception
	{
		public ProviderException ()
		{
		}

		public ProviderException (string message) : base (message)
		{
		}

		public ProviderException (string message, Exception inner) : base (message, inner)
		{
		}

		protected ProviderException (
			SerializationInfo info,
			StreamingContext context) : base (info, context)
		{
		}
	}
}