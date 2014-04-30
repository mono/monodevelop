//
// From NuGet src/Core
//
// Copyright (c) 2010-2014 Outercurve Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	/// <summary>
	/// This interface represents the basic interface that one needs to implement in order to
	/// support repository authentication. 
	/// </summary>
	public interface ICredentialProvider
	{
		/// <summary>
		/// Returns CredentialState state that let's the consumer know if ICredentials
		/// were discovered by the ICredentialProvider. The credentials argument is then
		/// populated with the discovered valid credentials that can be used for the given Uri.
		/// The proxy instance if passed will be used to ensure that the request goes through the proxy
		/// to ensure successful connection to the destination Uri.
		/// </summary>
		ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying);
	}
}
