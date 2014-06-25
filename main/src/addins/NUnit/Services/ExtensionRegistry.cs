//
// ProviderRegistry.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.NUnit
{
	/// <summary>
	/// The class helps to manage, register and unregister discoverers and executors
	/// </summary>
	public class ExtensionRegistry
	{
		List<Tuple<string, string, Type>> discoverers = new List<Tuple<string, string, Type>>();
		List<Tuple<string, string, string, Type>> executors = new List<Tuple<string, string, string, Type>>();

		public void RegisterTestDiscoverer (string providerId, string id, Type type)
		{
			discoverers.Add (Tuple.Create (providerId, id, type));
		}

		public void UnregisterTestDiscoverer (string providerId, string id)
		{
			discoverers = discoverers.Where (t => t.Item1 != providerId && t.Item2 != id).ToList ();
		}

		public void RegisterTestExecutor (string providerId, string discovererId, string id, Type type)
		{
			executors.Add (Tuple.Create (providerId, discovererId, id, type));
		}

		public void UnregisterTestExecutor (string providerId, string discovererId, string id)
		{
			executors = executors.Where (t => t.Item1 != providerId && t.Item2 != discovererId && t.Item3 != id).ToList ();
		}

		public IEnumerable<Tuple<string, Type>> GetDiscoverers (string providerId)
		{
			return discoverers.Where (t => t.Item1 == providerId)
				.Select (t => Tuple.Create (t.Item2, t.Item3));
		}

		public Type GetExecutor (string providerId, string discovererId, string id)
		{
			return executors.Where (t => t.Item1 == providerId && t.Item2 == discovererId && t.Item3 == id)
				.Select (t => t.Item4)
				.Single ();
		}

		public Type GetDefaultExecutor (string providerId, string discovererId)
		{
			return executors.Where (t => t.Item1 == providerId && t.Item2 == discovererId)
				.Select (t => t.Item4)
				.First ();
		}
	}
}

