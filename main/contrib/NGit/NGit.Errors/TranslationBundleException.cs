/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Globalization;
using Sharpen;

namespace NGit.Errors
{
	/// <summary>Common base class for all translation bundle related exceptions.</summary>
	/// <remarks>Common base class for all translation bundle related exceptions.</remarks>
	[System.Serializable]
	public abstract class TranslationBundleException : RuntimeException
	{
		private const long serialVersionUID = 1L;

		private readonly Type bundleClass;

		private readonly CultureInfo locale;

		/// <summary>
		/// To construct an instance of
		/// <see cref="TranslationBundleException">TranslationBundleException</see>
		/// </summary>
		/// <param name="message">exception message</param>
		/// <param name="bundleClass">bundle class for which the exception occurred</param>
		/// <param name="locale">locale for which the exception occurred</param>
		/// <param name="cause">
		/// original exception that caused this exception. Usually thrown
		/// from the
		/// <see cref="Sharpen.ResourceBundle">Sharpen.ResourceBundle</see>
		/// class.
		/// </param>
		protected internal TranslationBundleException(string message, Type bundleClass, CultureInfo
			 locale, Exception cause) : base(message, cause)
		{
			this.bundleClass = bundleClass;
			this.locale = locale;
		}

		/// <returns>bundle class for which the exception occurred</returns>
		public Type GetBundleClass()
		{
			return bundleClass;
		}

		/// <returns>locale for which the exception occurred</returns>
		public CultureInfo GetLocale()
		{
			return locale;
		}
	}
}
