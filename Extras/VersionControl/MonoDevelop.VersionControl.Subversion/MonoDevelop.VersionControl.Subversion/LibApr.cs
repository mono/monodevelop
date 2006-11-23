
using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.VersionControl.Subversion
{
	public abstract class LibApr
	{
		public static LibApr GetLib ()
		{
			try {
				return new LibApr0 ();
			} catch {}

			return new LibApr1 ();
		}
		
		public LibApr ()
		{
			initialize ();
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
		public abstract int file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool); 
		public abstract int file_close (IntPtr file);
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
		[DllImport(aprlib)] static extern int apr_file_open(ref IntPtr newf, string fname, int flag, int perm, IntPtr pool); 
		[DllImport(aprlib)] static extern int apr_file_close (IntPtr file); 	
	}
}
