// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.VisualStudio
{
	internal static class JObjectExtensions
	{
		/// <summary>
		/// Returns a field value or the empty string. Arrays will become comma delimited strings.
		/// </summary>
		public static string GetString(this JObject json, string property)
		{
			var value = json[property];

			if (value == null)
			{
				return string.Empty;
			}

			var array = value as JArray;

			if (array != null)
			{
				return string.Empty;
			}

			return value.ToString();
		} 
	}
}

