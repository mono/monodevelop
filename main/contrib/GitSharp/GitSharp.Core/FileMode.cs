/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;

namespace GitSharp.Core
{

	public class FileMode : IEquatable<FileMode>
	{
		// [henon] c# does not support octal literals, so every number starting with 0 in java code had to be converted to decimal!
		// Here are the octal literals used by jgit and their decimal counterparts:
		// decimal ... octal
		// 33188 ... 0100644
		// 33261 ... 0100755
		// 61440 ... 0170000
		// 16384 ... 0040000
		// 32768 ... 0100000
		// 40960 ... 0120000
		// 57344 ... 0160000
		// 73 ... 0111

		public delegate bool EqualsDelegate(int bits);

		public const int OCTAL_0100644 = 33188;
		public const int OCTAL_0100755 = 33261;
		public const int OCTAL_0111 = 73;

		/// <summary> Bit pattern for <see cref="TYPE_MASK"/> matching <see cref="RegularFile"/>.</summary>
		public const int TYPE_FILE = 32768;

		/// <summary> Bit pattern for <see cref="TYPE_MASK"/> matching <see cref="GitLink"/>. </summary>
		public const int TYPE_GITLINK = 57344;

		/// <summary>
		/// Mask to apply to a file mode to obtain its type bits.
		/// <para/>
		///  <see cref="TYPE_TREE"/>
		///  <see cref="TYPE_SYMLINK"/>
		///  <see cref="TYPE_FILE"/>
		///  <see cref="TYPE_GITLINK"/>
		///  <see cref="TYPE_MISSING"/>
		/// </summary>
		public const int TYPE_MASK = 61440;

		/// <summary>  Bit pattern for <see cref="TYPE_MASK"/> matching <see cref="Missing"/>. </summary>
		public const int TYPE_MISSING = 0;
		public const int TYPE_SYMLINK = 40960;
		public const int TYPE_TREE = 16384;

		public static readonly FileMode ExecutableFile =
			 new FileMode(OCTAL_0100755, ObjectType.Blob,
				  modeBits => (modeBits & TYPE_MASK) == TYPE_FILE && (modeBits & OCTAL_0111) != 0);

		public static readonly FileMode GitLink =
			 new FileMode(TYPE_GITLINK, ObjectType.Commit,
				  modeBits => (modeBits & TYPE_MASK) == TYPE_GITLINK);

		public static readonly FileMode Missing =
			 new FileMode(0, ObjectType.Bad, modeBits => modeBits == 0);

		public static readonly FileMode RegularFile =
			 new FileMode(OCTAL_0100644, ObjectType.Blob,
				  modeBits => (modeBits & TYPE_MASK) == TYPE_FILE && (modeBits & OCTAL_0111) == 0);

		public static readonly FileMode Symlink =
			 new FileMode(TYPE_SYMLINK, ObjectType.Blob,
				  modeBits => (modeBits & TYPE_MASK) == TYPE_SYMLINK);

		[field: NonSerialized]
		public static readonly FileMode Tree =
			 new FileMode(TYPE_TREE, ObjectType.Tree,
				  modeBits => (modeBits & TYPE_MASK) == TYPE_TREE);

		public static FileMode FromBits(int bits)
		{
			switch (bits & TYPE_MASK) // octal 0170000
			{
				case 0:
					if (bits == 0)
					{
						return Missing;
					}
					break;

				case TYPE_TREE: // octal 0040000
					return Tree;

				case TYPE_FILE: // octal 0100000
					return (bits & OCTAL_0111) != 0 ? ExecutableFile : RegularFile;

				case TYPE_SYMLINK: // octal 0120000
					return Symlink;

				case TYPE_GITLINK: // octal 0160000
					return GitLink;
			}

			return new FileMode(bits, ObjectType.Bad, a => bits == a);
		}

		private readonly byte[] _octalBytes;

		private FileMode(int mode, ObjectType type, Func<int, bool> equalityFunction)
		{
			if (equalityFunction == null)
			{
				throw new ArgumentNullException("equalityFunction");
			}

			EqualityFunction = equalityFunction;

			Bits = mode;
			ObjectType = type;

			if (mode != 0)
			{
				var tmp = new byte[10];
				int p = tmp.Length;

				while (mode != 0)
				{
					tmp[--p] = (byte)((byte)'0' + (mode & 07));
					mode >>= 3;
				}

				_octalBytes = new byte[tmp.Length - p];
				for (int k = 0; k < _octalBytes.Length; k++)
				{
					_octalBytes[k] = tmp[p + k];
				}
			}
			else
			{
				_octalBytes = new[] { (byte)'0' };
			}
		}

		public Func<int, bool> EqualityFunction { get; private set; }

		public int Bits { get; private set; }
		public ObjectType ObjectType { get; private set; }

		public void CopyTo(Stream stream)
		{
			new BinaryWriter(stream).Write(_octalBytes);
		}

		/// <returns>Returns the number of bytes written by <see cref="CopyTo(Stream)"/></returns>
		public int copyToLength()
		{
			return _octalBytes.Length;
		}

		public bool Equals(FileMode other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return EqualityFunction(other.Bits);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() == typeof(int)) return Equals(FromBits((int)obj));
			if (obj.GetType() != typeof(FileMode)) return false;
			return Equals((FileMode)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (EqualityFunction.GetHashCode() * 397) ^ ObjectType.GetHashCode();
			}
		}

		public static bool operator ==(FileMode left, FileMode right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(FileMode left, FileMode right)
		{
			return !Equals(left, right);
		}
	}
}