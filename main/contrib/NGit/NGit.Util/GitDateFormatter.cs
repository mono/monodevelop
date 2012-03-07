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
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>
	/// A utility for formatting dates according to the Git log.date formats plus
	/// extensions.
	/// </summary>
	/// <remarks>
	/// A utility for formatting dates according to the Git log.date formats plus
	/// extensions. &lt;p&lt; The enum
	/// <see cref="Format">Format</see>
	/// defines the available types
	/// </remarks>
	public class GitDateFormatter
	{
		private DateFormat dateTimeInstance;

		private DateFormat dateTimeInstance2;

		private readonly GitDateFormatter.Format format;

		/// <summary>Git and JGit formats</summary>
		public enum Format
		{
			DEFAULT,
			RELATIVE,
			LOCAL,
			ISO,
			RFC,
			SHORT,
			RAW,
			LOCALE,
			LOCALELOCAL
		}

		/// <summary>Create a new Git oriented date formatter</summary>
		/// <param name="format"></param>
		public GitDateFormatter(GitDateFormatter.Format format)
		{
			this.format = format;
			switch (format)
			{
				default:
				{
					break;
					break;
				}

				case GitDateFormatter.Format.DEFAULT:
				{
					// Not default:
					dateTimeInstance = new SimpleDateFormat("EEE MMM dd HH:mm:ss yyyy Z", CultureInfo
						.InvariantCulture);
					break;
				}

				case GitDateFormatter.Format.ISO:
				{
					dateTimeInstance = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss Z", CultureInfo.InvariantCulture
						);
					break;
				}

				case GitDateFormatter.Format.LOCAL:
				{
					dateTimeInstance = new SimpleDateFormat("EEE MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture
						);
					break;
				}

				case GitDateFormatter.Format.RFC:
				{
					dateTimeInstance = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss Z", CultureInfo
						.InvariantCulture);
					break;
				}

				case GitDateFormatter.Format.SHORT:
				{
					dateTimeInstance = new SimpleDateFormat("yyyy-MM-dd", CultureInfo.InvariantCulture
						);
					break;
				}

				case GitDateFormatter.Format.LOCALE:
				case GitDateFormatter.Format.LOCALELOCAL:
				{
					SystemReader systemReader = SystemReader.GetInstance();
					dateTimeInstance = systemReader.GetDateTimeInstance(DateFormat.DEFAULT, DateFormat
						.DEFAULT);
					dateTimeInstance2 = systemReader.GetSimpleDateFormat("Z");
					break;
				}
			}
		}

		/// <summary>
		/// Format committer, author or tagger ident according to this formatter's
		/// specification.
		/// </summary>
		/// <remarks>
		/// Format committer, author or tagger ident according to this formatter's
		/// specification.
		/// </remarks>
		/// <param name="ident"></param>
		/// <returns>formatted version of date, time and time zone</returns>
		public virtual string FormatDate(PersonIdent ident)
		{
			TimeZoneInfo tz;
			switch (format)
			{
				case GitDateFormatter.Format.RAW:
				{
					int offset = ident.GetTimeZoneOffset();
					string sign = offset < 0 ? "-" : "+";
					int offset2;
					if (offset < 0)
					{
						offset2 = -offset;
					}
					else
					{
						offset2 = offset;
					}
					int hours = offset2 / 60;
					int minutes = offset2 % 60;
					return string.Format("%d %s%02d%02d", ident.GetWhen().GetTime() / 1000, sign, hours
						, minutes);
				}

				case GitDateFormatter.Format.RELATIVE:
				{
					return RelativeDateFormatter.Format(ident.GetWhen());
				}

				case GitDateFormatter.Format.LOCALELOCAL:
				case GitDateFormatter.Format.LOCAL:
				{
					dateTimeInstance.SetTimeZone(SystemReader.GetInstance().GetTimeZone());
					return dateTimeInstance.Format(ident.GetWhen());
				}

				case GitDateFormatter.Format.LOCALE:
				{
					tz = ident.GetTimeZone();
					if (tz == null)
					{
						tz = SystemReader.GetInstance().GetTimeZone();
					}
					dateTimeInstance.SetTimeZone(tz);
					dateTimeInstance2.SetTimeZone(tz);
					return dateTimeInstance.Format(ident.GetWhen()) + " " + dateTimeInstance2.Format(
						ident.GetWhen());
				}

				default:
				{
					tz = ident.GetTimeZone();
					if (tz == null)
					{
						tz = SystemReader.GetInstance().GetTimeZone();
					}
					dateTimeInstance.SetTimeZone(ident.GetTimeZone());
					return dateTimeInstance.Format(ident.GetWhen());
					break;
				}
			}
		}
	}
}
