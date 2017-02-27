//
// DotNetCoreRunConfiguration.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Net;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using System.Net.Sockets;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreRunConfiguration : AssemblyRunConfiguration
	{
		public DotNetCoreRunConfiguration (string name)
			: base (name)
		{
		}

		protected override void Initialize (Project project)
		{
			base.Initialize (project);
			if (string.IsNullOrEmpty (ApplicationURL)) {
				var tcpListner = new TcpListener (IPAddress.Loopback, 0);
				tcpListner.Start ();
				ApplicationURL = $"http://localhost:{((IPEndPoint)tcpListner.LocalEndpoint).Port}";
				tcpListner.Stop ();
			}
		}

		[ItemProperty (DefaultValue = true)]
		public bool LaunchBrowser { get; set; } = true;

		[ItemProperty (DefaultValue = null)]
		public string LaunchUrl { get; set; }

		[ItemProperty (DefaultValue = null)]
		public string ApplicationURL { get; set; }

		protected override void OnCopyFrom (ProjectRunConfiguration config, bool isRename)
		{
			base.OnCopyFrom (config, isRename);

			var other = (DotNetCoreRunConfiguration)config;

			LaunchBrowser = other.LaunchBrowser;
			LaunchUrl = other.LaunchUrl;
			ApplicationURL = other.ApplicationURL;
		}
	}
}
