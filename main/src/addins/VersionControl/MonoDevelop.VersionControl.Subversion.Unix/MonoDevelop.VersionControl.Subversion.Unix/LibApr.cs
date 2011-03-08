
using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.VersionControl.Subversion.Unix
{
	public abstract class LibApr
	{
		public static readonly int APR_OS_DEFAULT = 0xFFF;
		public static readonly int APR_WRITE = 2;
		public static readonly int APR_CREATE = 4;
		public static readonly int APR_TRUNCATE = 16;
		
		public static LibApr GetLib (int ver)
		{
			try {
				if (ver == 0)
					return new LibApr0 ();
				else if (ver == 1)
					return new LibApr1 ();
				
				try {
					return new LibApr0 ();
				} catch {}
				
				return new LibApr1 ();
			}
			catch {
				// Not installed
				return null;
			}
		}
		
		public LibApr ()
		{
			initialize ();
		}
		
		public IntPtr pcalloc (IntPtr pool, object structure)
		{
			IntPtr ptr = pcalloc (pool, Marshal.SizeOf (structure.GetType ()));
			Marshal.StructureToPtr (structure, ptr, false);
			return ptr;
		}
		
		public abstract void initialize();
		public abstract IntPtr pool_create_ex(out IntPtr pool, IntPtr parent, IntPtr abort, IntPtr allocator);
		public abstract void pool_destroy(IntPtr pool);
		public abstract IntPtr hash_first(IntPtr pool, IntPtr hash);
		public abstract IntPtr hash_next(IntPtr hashindex);
		public abstract void hash_this(IntPtr hashindex, out IntPtr key, out int keylen, out IntPtr val);
		public abstract IntPtr array_make(IntPtr pool, int nelts, int elt_size);
		public abstract IntPtr array_push(IntPtr arr);
		public abstract IntPtr pstrdup(IntPtr pool, string s);
		public abstract IntPtr pcalloc (IntPtr pool, [MarshalAs (UnmanagedType.SysInt)] int size);
		public abstract int file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool); 
		public abstract int file_close (IntPtr file);
		
		public const int APR_OS_START_ERROR = 20000;
		public const int APR_OS_ERRSPACE_SIZE = 50000;
		public const int APR_OS_START_STATUS =(APR_OS_START_ERROR + APR_OS_ERRSPACE_SIZE);
		public const int APR_OS_START_USERERR =(APR_OS_START_STATUS + APR_OS_ERRSPACE_SIZE);
		public const int APR_OS_START_USEERR = APR_OS_START_USERERR;
	}

	public class LibApr0: LibApr
	{
		private const string aprlib = "libapr-0.so.0";
		
		public override void initialize() { apr_initialize (); }
		public override IntPtr pool_create_ex (out IntPtr pool, IntPtr parent, IntPtr abort, IntPtr allocator) { return apr_pool_create_ex(out pool, parent, abort, allocator); }
		public override void pool_destroy(IntPtr pool) { apr_pool_destroy (pool); }
		public override IntPtr hash_first(IntPtr pool, IntPtr hash) { return apr_hash_first (pool, hash); }
		public override IntPtr hash_next(IntPtr hashindex) { return apr_hash_next(hashindex); }
		public override void hash_this(IntPtr hashindex, out IntPtr key, out int keylen, out IntPtr val) { apr_hash_this(hashindex, out key, out keylen, out val); }
		public override IntPtr array_make(IntPtr pool, int nelts, int elt_size) { return apr_array_make(pool, nelts, elt_size); }
		public override IntPtr array_push(IntPtr arr) { return apr_array_push (arr); }
		public override IntPtr pstrdup(IntPtr pool, string s) { return apr_pstrdup(pool, s); }
		public override IntPtr pcalloc (IntPtr pool, [MarshalAs (UnmanagedType.SysInt)] int size) { return apr_pcalloc (pool, size); }
		public override int file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool) { return apr_file_open(ref newf, fname, flag, perm, pool); } 
		public override int file_close (IntPtr file) { return apr_file_close (file); } 	

		[DllImport(aprlib)] static extern void apr_initialize();
		[DllImport(aprlib)] static extern IntPtr apr_pool_create_ex(out IntPtr pool, IntPtr parent, IntPtr abort, IntPtr allocator);
		[DllImport(aprlib)] static extern void apr_pool_destroy(IntPtr pool);
		[DllImport(aprlib)] static extern IntPtr apr_hash_first(IntPtr pool, IntPtr hash);
		[DllImport(aprlib)] static extern IntPtr apr_hash_next(IntPtr hashindex);
		[DllImport(aprlib)] static extern void apr_hash_this(IntPtr hashindex, out IntPtr key, out int keylen, out IntPtr val);
		[DllImport(aprlib)] static extern IntPtr apr_array_make(IntPtr pool, int nelts, int elt_size);
		[DllImport(aprlib)] static extern IntPtr apr_array_push(IntPtr arr);
		[DllImport(aprlib)] static extern IntPtr apr_pstrdup(IntPtr pool, string s);
		[DllImport(aprlib)] static extern IntPtr apr_pcalloc(IntPtr pool, [MarshalAs (UnmanagedType.SysInt)] int size);
		[DllImport(aprlib)] static extern int apr_file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool); 
		[DllImport(aprlib)] static extern int apr_file_close (IntPtr file); 	
	}

	public class LibApr1: LibApr
	{
		private const string aprlib = "libapr-1.so.0";
		
		public override void initialize() { apr_initialize (); }
		public override IntPtr pool_create_ex (out IntPtr pool, IntPtr parent, IntPtr abort, IntPtr allocator) { return apr_pool_create_ex(out pool, parent, abort, allocator); }
		public override void pool_destroy(IntPtr pool) { apr_pool_destroy (pool); }
		public override IntPtr hash_first(IntPtr pool, IntPtr hash) { return apr_hash_first (pool, hash); }
		public override IntPtr hash_next(IntPtr hashindex) { return apr_hash_next(hashindex); }
		public override void hash_this(IntPtr hashindex, out IntPtr key, out int keylen, out IntPtr val) { apr_hash_this(hashindex, out key, out keylen, out val); }
		public override IntPtr array_make(IntPtr pool, int nelts, int elt_size) { return apr_array_make(pool, nelts, elt_size); }
		public override IntPtr array_push(IntPtr arr) { return apr_array_push (arr); }
		public override IntPtr pstrdup(IntPtr pool, string s) { return apr_pstrdup(pool, s); }
		public override IntPtr pcalloc (IntPtr pool, [MarshalAs (UnmanagedType.SysInt)] int size) { return apr_pcalloc (pool, size); }
		public override int file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool) { return apr_file_open(ref newf, fname, flag, perm, pool); } 
		public override int file_close (IntPtr file) { return apr_file_close (file); } 	

		[DllImport(aprlib)] static extern void apr_initialize();
		[DllImport(aprlib)] static extern IntPtr apr_pool_create_ex(out IntPtr pool, IntPtr parent, IntPtr abort, IntPtr allocator);
		[DllImport(aprlib)] static extern void apr_pool_destroy(IntPtr pool);
		[DllImport(aprlib)] static extern IntPtr apr_hash_first(IntPtr pool, IntPtr hash);
		[DllImport(aprlib)] static extern IntPtr apr_hash_next(IntPtr hashindex);
		[DllImport(aprlib)] static extern void apr_hash_this(IntPtr hashindex, out IntPtr key, out int keylen, out IntPtr val);
		[DllImport(aprlib)] static extern IntPtr apr_array_make(IntPtr pool, int nelts, int elt_size);
		[DllImport(aprlib)] static extern IntPtr apr_array_push(IntPtr arr);
		[DllImport(aprlib)] static extern IntPtr apr_pstrdup(IntPtr pool, string s);
		[DllImport(aprlib)] static extern IntPtr apr_pcalloc(IntPtr pool, [MarshalAs (UnmanagedType.SysInt)] int size);
		[DllImport(aprlib)] static extern int apr_file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool); 
		[DllImport(aprlib)] static extern int apr_file_close (IntPtr file); 	
	}
}
