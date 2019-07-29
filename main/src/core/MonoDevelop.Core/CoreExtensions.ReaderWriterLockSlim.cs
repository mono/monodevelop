//
// CoreExtensions.ReaderWriterLockSlim.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Threading;

namespace System
{
	public static partial class CoreExtensions
	{
		internal static ReadLockExiter Read (this ReaderWriterLockSlim @lock)
			=> new ReadLockExiter (@lock);

		internal readonly struct ReadLockExiter : IDisposable
		{
			readonly ReaderWriterLockSlim _lock;

			internal ReadLockExiter (ReaderWriterLockSlim @lock)
			{
				_lock = @lock;
				@lock.EnterReadLock ();
			}

			public void Dispose () => _lock.ExitReadLock ();
		}

		internal static UpgradeableReadLockExiter UpgradeableRead (this ReaderWriterLockSlim @lock)
			=> new UpgradeableReadLockExiter (@lock);

		internal readonly struct UpgradeableReadLockExiter : IDisposable
		{
			readonly ReaderWriterLockSlim _lock;

			internal UpgradeableReadLockExiter (ReaderWriterLockSlim @lock)
			{
				_lock = @lock;
				@lock.EnterUpgradeableReadLock ();
			}

			public void Dispose ()
			{
				if (_lock.IsWriteLockHeld) {
					_lock.ExitWriteLock ();
				}

				_lock.ExitUpgradeableReadLock ();
			}

			public void EnterWrite () => _lock.EnterWriteLock ();
		}

		internal static WriteLockExiter Write (this ReaderWriterLockSlim @lock)
			=> new WriteLockExiter (@lock);

		internal readonly struct WriteLockExiter : IDisposable
		{
			readonly ReaderWriterLockSlim _lock;

			internal WriteLockExiter (ReaderWriterLockSlim @lock)
			{
				_lock = @lock;
				@lock.EnterWriteLock ();
			}

			public void Dispose () => _lock.ExitWriteLock ();
		}
	}
}
