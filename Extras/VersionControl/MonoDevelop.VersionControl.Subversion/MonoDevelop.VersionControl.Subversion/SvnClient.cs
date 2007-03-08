using System;
using System.Collections;
using System.IO;
using System.Threading;

using System.Runtime.InteropServices;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Subversion.Gui;

namespace MonoDevelop.VersionControl.Subversion
{
	public class SvnClient
	{
		IntPtr pool;
		IntPtr ctx;
		IntPtr auth_baton;
		static LibApr apr;
		
		object sync = new object();
		bool inProgress = false;
		
		IProgressMonitor updatemonitor;
		string commitmessage = null;
		
		// retain this so the delegates aren't GC'ed
		svn_client_ctx_t ctxstruct;
		
		static SvnClient() {
			apr = LibApr.GetLib ();
		}

		private IntPtr newpool(IntPtr parent) {
			IntPtr p;
			apr.pool_create_ex(out p, parent, IntPtr.Zero, IntPtr.Zero);
			if (p == IntPtr.Zero)
				throw new InvalidOperationException("Could not create an APR pool.");
			return p;
		}
		
		public SvnClient() {
			// Allocate the APR pool and the SVN client context.
			
			pool = newpool(IntPtr.Zero);
		
			if (svn_client_create_context(out ctx, pool) != IntPtr.Zero)
				throw new InvalidOperationException("Could not create a Subversion client context.");
				
			// Set the callbacks on the client context structure.
		
			// This is quite a roudabout way of doing this.  The task
			// is to set the notify field of the unmanaged structure
			// at the address 'ctx' -- with the address of a delegate.
			// There's no way to get an address for the delegate directly,
			// as far as I could figure out, so instead I create a managed
			// structure that mirrors the start of the unmanaged structure
			// I want to modify.  Then I marshal the managed structure
			// *onto* to unmanaged one, overwriting fields in the process.
			// I don't use references to the structure itself in the API
			// calls because it needs to be allocated by SVN.  I hope
			// this doesn't cause any memory leaks.
			ctxstruct = new svn_client_ctx_t();
			ctxstruct.NotifyFunc = new svn_wc_notify_func_t(svn_wc_notify_func_t_impl);
			ctxstruct.LogMsgFunc = new svn_client_get_commit_log_t(svn_client_get_commit_log_impl);

			IntPtr providers = apr.array_make (pool, 1, IntPtr.Size);
			IntPtr item;
			
			item = apr.array_push (providers);
			svn_client_get_simple_prompt_provider (item, new svn_auth_simple_prompt_func_t (OnAuthSimplePrompt), IntPtr.Zero, 2, pool);
			
			item = apr.array_push (providers);
			svn_client_get_username_prompt_provider (item, new svn_auth_username_prompt_func_t (OnAuthUsernamePrompt), IntPtr.Zero, 2, pool);
			
			item = apr.array_push (providers);
			svn_client_get_ssl_server_trust_prompt_provider (item, new svn_auth_ssl_server_trust_prompt_func_t (OnAuthSslServerTrustPrompt), IntPtr.Zero, pool);
			
			item = apr.array_push (providers);
			svn_client_get_ssl_client_cert_prompt_provider (item, new svn_auth_ssl_client_cert_prompt_func_t (OnAuthSslClientCertPrompt), IntPtr.Zero, 2, pool);

			item = apr.array_push (providers);
			svn_client_get_ssl_client_cert_pw_prompt_provider (item, new svn_auth_ssl_client_cert_pw_prompt_func_t (OnAuthSslClientCertPwPrompt), IntPtr.Zero, 2, pool);

			item = apr.array_push (providers);
			svn_client_get_simple_provider (item, pool);
			
			item = apr.array_push (providers);
			svn_client_get_username_provider (item, pool);
			
			item = apr.array_push (providers);
			svn_client_get_ssl_client_cert_file_provider (item, pool);
			
			item = apr.array_push (providers);
			svn_client_get_ssl_client_cert_pw_file_provider (item, pool);

			item = apr.array_push (providers);
			svn_client_get_ssl_server_trust_file_provider (item, pool);
			
			svn_auth_open (out auth_baton, providers, pool); 
			ctxstruct.auth_baton = auth_baton;

			Marshal.StructureToPtr(ctxstruct, ctx, false);
		}
		
		[StructLayout(LayoutKind.Sequential)]
		private struct svn_client_ctx_t {
			public IntPtr auth_baton;
			public svn_wc_notify_func_t NotifyFunc;
			public IntPtr notify_baton;
			public svn_client_get_commit_log_t LogMsgFunc;
			public IntPtr logmsg_baton;
		}

		~SvnClient() {
			apr.pool_destroy(pool);
		}
		
		static IntPtr GetCancelError (IntPtr pool)
		{
			svn_error_t error = new svn_error_t ();
			error.apr_err = LibApr.APR_OS_START_USEERR;
			error.message = "Operation cancelled.";
			error.pool = pool;
			return apr.pcalloc (pool, error);
		}
		
		static IntPtr OnAuthSimplePrompt (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.LPStr)] string user_name, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool)
		{
			svn_auth_cred_simple_t data; 
			if (UserPasswordDialog.Show (user_name, realm, may_save != 0, out data)) {
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			}
			else {
				data.password = "";
				data.username = "";
				data.may_save = 0;
				cred = apr.pcalloc (pool, data);
				return GetCancelError (pool);
			}
		}
		
		static IntPtr OnAuthUsernamePrompt (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool)
		{
			svn_auth_cred_username_t data;
			if (UserPasswordDialog.Show (realm, may_save != 0, out data)) {
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			}
			else {
				data.username = "";
				data.may_save = 0;
				cred = apr.pcalloc (pool, data);
				return GetCancelError (pool);
			}
		}

		static IntPtr OnAuthSslServerTrustPrompt (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, uint failures, ref svn_auth_ssl_server_cert_info_t cert_info, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool)
		{
			svn_auth_cred_ssl_server_trust_t data;
			SslServerTrustDialog.Show (realm, failures, may_save, cert_info, out data);
			cred = apr.pcalloc (pool, data);
			return IntPtr.Zero;
		}
		
		static IntPtr OnAuthSslClientCertPrompt (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool)
		{
			svn_auth_cred_ssl_client_cert_t data;
			if (ClientCertificateDialog.Show (realm, may_save, out data)) {
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			}
			else {
				data.cert_file = "";
				data.may_save = 0;
				cred = apr.pcalloc (pool, data);
				return GetCancelError (pool);
			}
		}
		
		static IntPtr OnAuthSslClientCertPwPrompt (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool)
		{
			svn_auth_cred_ssl_client_cert_pw_t data;
			if (ClientCertificatePasswordDialog.Show (realm, may_save, out data)) {
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			}
			else {
				data.password = "";
				data.may_save = 0;
				cred = apr.pcalloc (pool, data);
				return GetCancelError (pool);
			}
		}
		
		// Wrappers for native interop
		
		public string Version {
			get {
				IntPtr ptr = svn_client_version();
				svn_version_t ver = (svn_version_t)Marshal.PtrToStructure(ptr, typeof(svn_version_t));				
				return ver.major + "." + ver.minor + "." + ver.patch;
			}
		}
		
		public IList List(string pathorurl, bool recurse, Rev revision) {
			if (pathorurl == null) throw new ArgumentException();
			
			IntPtr localpool = newpool(pool);
			ArrayList items = new ArrayList();
			try {
				IntPtr hash;
			
				CheckError(svn_client_ls(out hash, pathorurl, ref revision,
	               recurse ? 1 : 0, ctx, localpool));
	               
	             IntPtr item = apr.hash_first(localpool, hash);
	             while (item != IntPtr.Zero) {
	             	IntPtr nameptr, val;
	             	int namelen;
	             	apr.hash_this(item, out nameptr, out namelen, out val);
	             
	             	string name = Marshal.PtrToStringAnsi(nameptr);
					svn_dirent_t ent = (svn_dirent_t)Marshal.PtrToStructure(val, typeof(svn_dirent_t));				
	             	item = apr.hash_next(item);
	             	
	             	items.Add(new DirEnt(name, ent));
	             }	             
	             
			} finally {
				apr.pool_destroy(localpool);
			}
			return items;
		}

		public IList Status(string path, Rev revision) {
			return Status(path, revision, false, false, false);
		}
		
		public IList Status(string path, Rev revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus) {
			if (path == null) throw new ArgumentException();
		
			ArrayList ret = new ArrayList();

			StatusCollector collector = new StatusCollector(ret);

			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_status (IntPtr.Zero, path, ref revision,
					new svn_wc_status_func_t(collector.Func),
					IntPtr.Zero,
					descendDirs ? 1 : 0, 
					changedItemsOnly ? 0 : 1, 
					remoteStatus ? 1 : 0,
					1,
					ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
			}
				
			return ret;
		}
		
		public IList Log(string path, Rev revisionStart, Rev revisionEnd) {
			if (path == null) throw new ArgumentException();
			
			ArrayList ret = new ArrayList();
			IntPtr localpool = newpool(pool);
			IntPtr strptr = IntPtr.Zero;
			try {
				IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
				IntPtr first = apr.array_push(array);
				strptr = Marshal.StringToHGlobalAnsi(path);
				Marshal.WriteIntPtr(first, strptr);

				LogCollector collector = new LogCollector(ret);
				
				CheckError(svn_client_log (
					array,
					ref revisionStart, ref revisionEnd,
					1,
		            0,
		        	new svn_log_message_receiver_t(collector.Func),
		            IntPtr.Zero,
		            ctx, localpool));
			} finally {
				if (strptr != IntPtr.Zero)
					Marshal.FreeHGlobal(strptr);
				apr.pool_destroy(localpool);
			}
			return ret;
		}
		
		public string GetPathUrl(string path) {
			if (path == null) throw new ArgumentNullException();
			
			IntPtr ret = IntPtr.Zero;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_url_from_path(ref ret, path, localpool));
			} finally {
				apr.pool_destroy(localpool);
			}
			if (ret == IntPtr.Zero) return null;
			return Marshal.PtrToStringAnsi(ret);
		}
		
		public string Cat(string pathorurl, Rev revision) {
			MemoryStream memstream = new MemoryStream();
			Cat(pathorurl, revision, memstream);
			try {
				return System.Text.Encoding.UTF8.GetString(memstream.GetBuffer());
			} catch {
			}
			return System.Text.Encoding.ASCII.GetString(memstream.GetBuffer());
		}

		public void Cat(string pathorurl, Rev revision, Stream stream) {
			if (pathorurl == null) throw new ArgumentException();
			if (stream == null) throw new ArgumentException();
			
			IntPtr localpool = newpool(pool);
			try {
				StreamCollector collector = new StreamCollector(stream);
				IntPtr svnstream = svn_stream_create(IntPtr.Zero, localpool);
				svn_stream_set_write(svnstream, new svn_readwrite_fn_t(collector.Func));
				Rev peg_revision = Rev.Blank;
				CheckError(svn_client_cat2(svnstream, pathorurl, ref peg_revision, ref revision, ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
			}
		}

		public void Update(string path, bool recurse, IProgressMonitor monitor)
		{
			if (path == null || monitor == null)
				throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			
			Rev rev = Rev.Head;
			IntPtr localpool = newpool(pool);
			try {
				CheckError (svn_client_update (IntPtr.Zero, path, ref rev, recurse ? 1 : 0, ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public void Revert (string[] paths, bool recurse, IProgressMonitor monitor)
		{
			if (paths == null || monitor == null)
				throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			IntPtr localpool = newpool(pool);
			
			// Put each item into an APR array.
			IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
			foreach (string path in paths) {
				IntPtr item = apr.array_push(array);
				Marshal.WriteIntPtr (item, apr.pstrdup (localpool, path));
			}
			
			try {
				CheckError (svn_client_revert (array, recurse ? 1 : 0, ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public void Add(string path, bool recurse, IProgressMonitor monitor) {
			if (path == null || monitor == null) throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_add(path, (recurse ? 1 :0), ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public void Checkout (string url, string path, Rev rev, bool recurse, IProgressMonitor monitor) {
			if (url == null || monitor == null) throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			Rev revision;
			IntPtr localpool = newpool(pool);
			try {
				CheckError(svn_client_checkout(out revision, url, path, ref rev, (recurse ? 1 :0), ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}

		public void Commit(string[] paths, string message, IProgressMonitor monitor) {
			if (paths == null || message == null || monitor == null)
				throw new ArgumentNullException();
		
			lock (sync) {
				if (inProgress) throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}

			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
				foreach (string path in paths) {
					IntPtr item = apr.array_push(array);
					Marshal.WriteIntPtr(item, apr.pstrdup(localpool, path));
				}

				IntPtr commit_info = IntPtr.Zero;
				
				commitmessage = message;
		
				CheckError(svn_client_commit (
					ref commit_info, array,
					0, ctx, localpool));
			} finally {
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy(localpool);
				inProgress = false;
			}
		}
		
		public void Mkdir (string[] paths, string message, IProgressMonitor monitor) 
		{
			if (paths == null || monitor == null)
				throw new ArgumentNullException();
		
			lock (sync) {
				if (inProgress) throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}

			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make(localpool, paths.Length, IntPtr.Size);
				foreach (string path in paths) {
					IntPtr item = apr.array_push(array);
					Marshal.WriteIntPtr(item, apr.pstrdup(localpool, path));
				}
				
				commitmessage = message;

				IntPtr commit_info = IntPtr.Zero;
				IntPtr pp = svn_client_mkdir2 (ref commit_info, array, ctx, localpool); 
				CheckError(pp);
			} finally {	
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy(localpool);
				inProgress = false;
			}
		}
		
		public void Delete(string path, bool force, IProgressMonitor monitor) {
			if (path == null || monitor == null)
				throw new ArgumentNullException();
		
			lock (sync) {
				if (inProgress) throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}

			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make(localpool, 0, IntPtr.Size);
				//foreach (string path in paths) {
					IntPtr item = apr.array_push(array);
					Marshal.WriteIntPtr(item, apr.pstrdup(localpool, path));
				//}
				IntPtr commit_info = IntPtr.Zero;
				CheckError (svn_client_delete (ref commit_info, array, (force ? 1 : 0), ctx, localpool));
			} finally {
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy(localpool);
				inProgress = false;
			}
		}
		
		public void Move(string srcPath, string destPath, Rev revision, bool force, IProgressMonitor monitor) {
			if (srcPath == null || destPath == null || monitor == null) throw new ArgumentException();
			
			lock (sync) {
				if (inProgress)
					throw new SubversionException("Another Subversion operation is already in progress.");
				inProgress = true;
			}
			
			updatemonitor = monitor;
			IntPtr commit_info = IntPtr.Zero;
			IntPtr localpool = newpool(pool);
			try {
				CheckError (svn_client_move (ref commit_info, srcPath, ref revision,
											   destPath, (force ? 1 : 0), ctx, localpool));
			} finally {
				apr.pool_destroy(localpool);
				updatemonitor = null;
				inProgress = false;
			}
		}
		
		public string PathDiff (string path1, Rev revision1, string path2, Rev revision2, bool recursive)
		{
			IntPtr outfile = IntPtr.Zero;
			IntPtr errfile = IntPtr.Zero;
			string fout = null;
			string ferr = null;
			IntPtr localpool = newpool(pool);
			
			try {
				IntPtr options = apr.array_make (localpool, 0, IntPtr.Size);
				
				fout = Path.GetTempFileName ();
				ferr = Path.GetTempFileName ();
				int res1 = apr.file_open (ref outfile, fout, APR_WRITE | APR_CREATE | APR_TRUNCATE, APR_OS_DEFAULT, localpool);
				int res2 = apr.file_open (ref errfile, ferr, APR_WRITE | APR_CREATE | APR_TRUNCATE, APR_OS_DEFAULT, localpool);
				
				if (res1==0 && res2==0) {
					CheckError (svn_client_diff (options, path1, ref revision1, path2, ref revision2, (recursive ? 1 : 0), 0, 1, outfile, errfile, ctx, localpool));
					return fout;
				} else {
					throw new Exception ("Could not get diff information");
				}
			} catch {
				try {
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile);
					if (fout != null)
						Runtime.FileService.DeleteFile (fout);
					outfile = IntPtr.Zero;
					fout = null;
				} catch {}
				throw;
			} finally {
				try {
					// Cleanup
					apr.pool_destroy (localpool);
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile); 
					if (errfile != IntPtr.Zero)
						apr.file_close (errfile);
					if (ferr != null)
						Runtime.FileService.DeleteFile (ferr);
				} catch {}
			}
		}
		
		IntPtr svn_client_get_commit_log_impl(ref IntPtr log_msg,
			ref IntPtr tmp_file, IntPtr commit_items, IntPtr baton,
			IntPtr pool) {
			log_msg = apr.pstrdup(pool, commitmessage);
			tmp_file = IntPtr.Zero;
			return IntPtr.Zero;
		}

		private void CheckError(IntPtr error) {
			if (error == IntPtr.Zero) return;
			string msg = null;
			while (error != IntPtr.Zero) {
				svn_error_t error_t = (svn_error_t)Marshal.PtrToStructure(error, typeof(svn_error_t));
				if (msg != null)
					msg += "\n" + error_t.message;
				else
					msg = error_t.message;
				error = error_t.svn_error_t_child;
			}
			if (msg == null)
				msg = "Unknown error";
			throw new SubversionException (msg);
		}
		
		void svn_wc_notify_func_t_impl(IntPtr baton, IntPtr path,
			NotifyAction action, NodeKind kind, IntPtr mime_type,
			NotifyState content_state, NotifyState prop_state, long revision) {
				string actiondesc = action.ToString();
				switch (action) {
					case NotifyAction.UpdateAdd: actiondesc = "Added"; break;
					case NotifyAction.UpdateDelete: actiondesc = "Deleted"; break;
					case NotifyAction.UpdateUpdate: actiondesc = "Updating"; break;
					case NotifyAction.UpdateExternal: actiondesc = "External Updated"; break;
					case NotifyAction.UpdateCompleted: actiondesc = "Finished"; break;
					
					case NotifyAction.CommitAdded: actiondesc = "Added"; break;
					case NotifyAction.CommitDeleted: actiondesc = "Deleted"; break;
					case NotifyAction.CommitModified: actiondesc = "Modified"; break;
					case NotifyAction.CommitReplaced: actiondesc = "Replaced"; break;
					case NotifyAction.CommitPostfixTxDelta: actiondesc = "Sending Content"; break;
					/*Add,
					Copy,
					Delete,
					Restore,
					Revert,
					FailedRevert,
					Resolved,
					Skip,
					StatusCompleted,
					StatusExternal,
					BlameRevision*/
				}
			
				if (updatemonitor != null) {
					updatemonitor.Log.WriteLine (actiondesc + " " + Marshal.PtrToStringAnsi (path));
				}
		}
		
		private class StatusCollector {
			ArrayList statuses;
			public StatusCollector(ArrayList statuses) { this.statuses = statuses; }
			public void Func(IntPtr baton, IntPtr path, ref svn_wc_status_t status) {
				string pathstr = Marshal.PtrToStringAnsi(path);
/*				if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
*/
				statuses.Add(new StatusEnt(status, pathstr));
			}
  
		}

		private class LogCollector {
			static readonly DateTime Epoch = new DateTime(1970,1,1);

			ArrayList logs;
			public LogCollector(ArrayList logs) { this.logs = logs; }
			public IntPtr Func(IntPtr baton, IntPtr apr_hash_changed_paths, int revision, IntPtr author, IntPtr date, IntPtr message, IntPtr pool) {
				long time;
				svn_time_from_cstring(out time, Marshal.PtrToStringAnsi(date), pool);
				string smessage = "";
				if (message != IntPtr.Zero) smessage = Marshal.PtrToStringAnsi(message).Trim();
			
				ArrayList items = new ArrayList();

				IntPtr item = apr.hash_first(pool, apr_hash_changed_paths);
				while (item != IntPtr.Zero) {
					IntPtr nameptr, val;
					int namelen;
					apr.hash_this(item, out nameptr, out namelen, out val);
	             
					string name = Marshal.PtrToStringAnsi(nameptr);
					svn_log_changed_path_t ch = (svn_log_changed_path_t)Marshal.PtrToStructure(val, typeof(svn_log_changed_path_t));				
					item = apr.hash_next(item);
	             	
					items.Add(new LogEntChangedPath(name, ch));
	             }	             

				logs.Add(new LogEnt(revision, Marshal.PtrToStringAnsi(author), Epoch.AddTicks(time*10), smessage, 
					(LogEntChangedPath[])items.ToArray(typeof(LogEntChangedPath))));
				
				return IntPtr.Zero;
			}
		}
		
		private class StreamCollector {
			Stream buf;
			public StreamCollector(Stream buf) { this.buf = buf; }
			public IntPtr Func(IntPtr baton, IntPtr data, ref IntPtr len) {
				unsafe {
					byte* bp = (byte*) data;
					int max = (int) len;
					for (int i = 0; i < max; i++) {
						buf.WriteByte (*bp);
						bp++;
					}
				}
				return IntPtr.Zero;
			}
		}
		
		public class DirEnt {
			public readonly string Name;
			public readonly bool IsDirectory;
			public readonly long Size;
			public readonly bool HasProps;
			public readonly int CreatedRevision;
			public readonly DateTime Time;
			public readonly string LastAuthor;
			
			static readonly DateTime Epoch = new DateTime(1970,1,1);
			
			internal DirEnt(string name, svn_dirent_t ent) {
				Name = name;
				IsDirectory = (ent.kind == (int)NodeKind.Dir);
				Size = ent.size;
				HasProps = ent.has_props != 0;
				CreatedRevision = ent.created_rev;
				Time = Epoch.AddTicks(ent.time*10);
				LastAuthor = ent.last_author;
			}
			
		}
		
		public class StatusEnt {
			public readonly string LocalFilePath;
			public readonly string Name;
			public readonly int Revision;
  			public readonly string Url;
  			public readonly string Repository;
  			public readonly string RepositoryUuid;
  			public readonly bool IsDirectory;
  			public readonly NodeSchedule Schedule;
  			public readonly bool Copied;
  			public readonly bool Deleted;
  			public readonly bool Absent;
  			public readonly bool Incomplete;
  			public readonly string CopiedFrom;
  			public readonly int CopiedFromRevision;
  			public readonly string ConflictOld;
  			public readonly string ConflictNew;
  			public readonly string ConflictWorking;
  			public readonly string PropertyRejectFile;
  			public readonly DateTime TextTime;
  			public readonly DateTime PropTime;
  			public readonly string Checksum; //(base64 for text-base, or NULL);
  			public readonly int LastCommitRevision;
  			public readonly DateTime LastCommitDate;
  			public readonly string LastCommitAuthor;	
 			
			public readonly VersionStatus TextStatus;
			public readonly VersionStatus PropsStatus;
			public readonly bool Locked;
			public readonly bool Switched;
			public readonly VersionStatus RemoteTextStatus;
			public readonly VersionStatus RemotePropsStatus;
			
			static readonly DateTime Epoch = new DateTime(1970,1,1);

			internal StatusEnt(svn_wc_status_t status, string localpath) {
				LocalFilePath = localpath;
				TextStatus = (VersionStatus)status.svn_wc_status_kind_text;
				PropsStatus = (VersionStatus)status.svn_wc_status_kind_props;
				Locked = status.locked != 0;
				Copied = status.copied != 0;
				Switched = status.switched != 0;
				RemoteTextStatus = (VersionStatus)status.svn_wc_status_kind_text_repo;
				RemotePropsStatus = (VersionStatus)status.svn_wc_status_kind_props_repo;
				
				if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
					
				svn_wc_entry_t ent = (svn_wc_entry_t)Marshal.PtrToStructure(status.to_svn_wc_entry_t, typeof(svn_wc_entry_t));
				Name = ent.name;
				Revision = ent.revision;
	  			Url = ent.url;
	  			Repository = ent.repos;
	  			RepositoryUuid = ent.repos_uuid;
	  			IsDirectory = (ent.node_kind == (int)NodeKind.Dir);
	  			Schedule = (NodeSchedule)ent.schedule;
	  			Copied = ent.copied != 0;
	  			Deleted = ent.deleted != 0;
	  			Absent = ent.absent != 0;
	  			Incomplete = ent.incomplete != 0;
	  			CopiedFrom = ent.copied_from;
	  			CopiedFromRevision = ent.copied_rev;
	  			ConflictOld = ent.conflict_old;
	  			ConflictNew = ent.conflict_new;
	  			ConflictWorking = ent.conflict_working;
	  			PropertyRejectFile = ent.property_reject_file;
	  			TextTime = Epoch.AddTicks(ent.text_time*10);
	  			PropTime = Epoch.AddTicks(ent.prop_time*10);
	  			Checksum = ent.checksum;
	  			LastCommitRevision = ent.last_commit_rev;
	  			LastCommitDate = Epoch.AddTicks(ent.last_commit_date*10);
	  			LastCommitAuthor = ent.last_commit_author;	
			}

		}
		
		public class LogEnt {
			public readonly int Revision;
			public readonly string Author;
			public readonly DateTime Time;
			public readonly string Message;
			public readonly LogEntChangedPath[] ChangedPaths;
			
			internal LogEnt(int rev, string author, DateTime time, string msg, LogEntChangedPath[] changes) {
				Revision = rev;
				Author = author;
				Time = time;
				Message = msg;
				ChangedPaths = changes;
			}
		}
		
		public class LogEntChangedPath {
			public readonly string Path;
			public readonly RevisionAction Action;
			public readonly string ActionDesc;
			public readonly string CopyFromPath;
			public readonly int CopyFromRevision;
			
			internal LogEntChangedPath(string path, svn_log_changed_path_t info) {
				Path = path;
				CopyFromPath = info.copy_from_path;
				CopyFromRevision = info.copy_from_rev;
				
				switch (info.action) {
				case 'A': Action = RevisionAction.Add; break;
				case 'D': Action = RevisionAction.Delete; break;
				case 'R': Action = RevisionAction.Replace; break;
				default: Action = RevisionAction.Modify; break; // should be an 'M'
				}
			}
		}
		
		// Native Interop
		
		
		const int APR_OS_DEFAULT = 0xFFF;
		const int APR_WRITE = 2;
		const int APR_CREATE = 4;
		const int APR_TRUNCATE = 16;

		private const string svnclientlib = "libsvn_client-1.so.0";
		
		private struct svn_error_t {
			[MarshalAs (UnmanagedType.SysInt)] public int apr_err;
			[MarshalAs (UnmanagedType.LPStr)] public string message;
			public IntPtr svn_error_t_child;
			public IntPtr pool;
			[MarshalAs (UnmanagedType.LPStr)] public string file;
			[MarshalAs (UnmanagedType.SysInt)] public int line;
		}
		
		private struct svn_version_t {
  			public int major;
  			public int minor;
  			public int patch;
			public string tag;	
  		}
  		
  		enum NodeKind {
  			None,
  			File,
  			Dir,
  			Unknown
  		}

		internal struct svn_dirent_t {
			public int kind;
			public long size;
			public int has_props;
			public int created_rev;
			public long time; // created
			public string last_author;
		}
		
		public enum NodeSchedule {
			Normal,
			Add,
			Delete,
			Replace
		}
		
		public enum VersionStatus {
			EMPTY,
			None,
			Unversioned,
			Normal,
			Added,
			Missing,
			Deleted,
			Replaced,
			Modified,
			Merged,
			Conflicted,
			Ignored,
			Obstructed,
			External,
			Incomplete
		}
		
		internal struct svn_wc_entry_t {
			public string name;
			public int revision;
  			public string url;
  			public string repos;
  			public string repos_uuid;
  			public int node_kind;
  			public int schedule;
  			public int copied;
  			public int deleted;
  			public int absent;
  			public int incomplete;
  			public string copied_from;
  			public int copied_rev;
  			public string conflict_old;
  			public string conflict_new;
  			public string conflict_working;
  			public string property_reject_file;
  			public long text_time; // or zero
  			public long prop_time;
  			public string checksum; //(base64 for text-base, or NULL);
  			public int last_commit_rev;
  			public long last_commit_date;
  			public string last_commit_author;
  		}
  				
		internal struct svn_wc_status_t {
			public IntPtr to_svn_wc_entry_t;
			public int svn_wc_status_kind_text;
			public int svn_wc_status_kind_props;
			public int locked;
			public int copied;
			public int switched;
			public int svn_wc_status_kind_text_repo;
			public int svn_wc_status_kind_props_repo;
		}
		
		public struct Rev {
			public IntPtr kind;
			public svn_opt_revision_value_t value;
			
			Rev (int kind) {
				this.kind = (IntPtr) kind;
				value = new svn_opt_revision_value_t ();
			}
			
			public static Rev Number (int rev)
			{
				Rev r = new Rev(1);
				r.value.number = (IntPtr) rev;
				return r;
			}
			
			public readonly static Rev Blank = new Rev(0);
			public readonly static Rev First = new Rev(1);
			public readonly static Rev Committed = new Rev(3);
			public readonly static Rev Previous = new Rev(4);
			public readonly static Rev Base = new Rev(5);
			public readonly static Rev Working = new Rev(6);
			public readonly static Rev Head = new Rev(7);
		}
		
		public struct svn_opt_revision_value_t {
			public IntPtr number;
			public IntPtr date1;
			public IntPtr date2;
		}
		
		internal struct svn_log_changed_path_t {
			public char action;
			public string copy_from_path;
			public int copy_from_rev;
		}
		
		internal struct svn_auth_cred_simple_t {
			[MarshalAs (UnmanagedType.LPStr)] public string username;
			[MarshalAs (UnmanagedType.LPStr)] public string password;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		internal struct svn_auth_cred_username_t {
			[MarshalAs (UnmanagedType.LPStr)] public string username;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		internal struct svn_auth_cred_ssl_server_trust_t {
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
			public uint accepted_failures;
		}
		
		internal struct svn_auth_cred_ssl_client_cert_t {
			[MarshalAs (UnmanagedType.LPStr)] public string cert_file;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		internal struct svn_auth_cred_ssl_client_cert_pw_t {
			[MarshalAs (UnmanagedType.LPStr)] public string password;
			[MarshalAs (UnmanagedType.SysInt)] public int may_save;
		}
		
		internal struct svn_auth_ssl_server_cert_info_t {
			[MarshalAs (UnmanagedType.LPStr)] public string hostname;
			[MarshalAs (UnmanagedType.LPStr)] public string fingerprint;
			[MarshalAs (UnmanagedType.LPStr)] public string valid_from;
			[MarshalAs (UnmanagedType.LPStr)] public string valid_until;
			[MarshalAs (UnmanagedType.LPStr)] public string issuer_dname;
			[MarshalAs (UnmanagedType.LPStr)] public string ascii_cert;
		}
		
		internal enum NotifyAction {
			Add,
			Copy,
			Delete,
			Restore,
			Revert,
			FailedRevert,
			Resolved,
			Skip,
			UpdateDelete,
			UpdateAdd,
			UpdateUpdate,
			UpdateCompleted,
			UpdateExternal,
			StatusCompleted,
			StatusExternal,
			CommitModified,
			CommitAdded,
			CommitDeleted,
			CommitReplaced,
			CommitPostfixTxDelta,
			BlameRevision
		}
		
		internal enum NotifyState {
			Inapplicable,
			Unknown,
			Unchanged,
			Missing,
			Obstructed,
			Changed,
			Merged,
			Conflicted
		}
		
		delegate void svn_wc_status_func_t(IntPtr baton, IntPtr path,
			ref svn_wc_status_t status);
		
		// Certificate is not yet valid.
		public const uint SVN_AUTH_SSL_NOTYETVALID = 0x00000001;
		// Certificate has expired.
		public const uint SVN_AUTH_SSL_EXPIRED = 0x00000002;
		// Certificate's CN (hostname) does not match the remote hostname.
		public const uint SVN_AUTH_SSL_CNMISMATCH = 0x00000004;
		// Certificate authority is unknown (i.e. not trusted)
		public const uint SVN_AUTH_SSL_UNKNOWNCA = 0x00000008;
		// Other failure. This can happen if neon has introduced a new failure bit that we do not handle yet. */
		public const uint SVN_AUTH_SSL_OTHER = 0x40000000;
		
		delegate IntPtr svn_auth_simple_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.LPStr)] string user_name, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		delegate IntPtr svn_auth_username_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		delegate IntPtr svn_auth_ssl_server_trust_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, uint failures, ref svn_auth_ssl_server_cert_info_t cert_info, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		delegate IntPtr svn_auth_ssl_client_cert_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		delegate IntPtr svn_auth_ssl_client_cert_pw_prompt_func_t (ref IntPtr cred, IntPtr baton, [MarshalAs (UnmanagedType.LPStr)] string realm, [MarshalAs (UnmanagedType.SysInt)] int may_save, IntPtr pool);
		
		delegate IntPtr svn_log_message_receiver_t(IntPtr baton,
			IntPtr apr_hash_changed_paths, int revision, IntPtr author,
			IntPtr date, IntPtr message, IntPtr pool);
		
		delegate IntPtr svn_readwrite_fn_t(IntPtr baton, IntPtr data, ref IntPtr len);
		
		delegate void svn_wc_notify_func_t(IntPtr baton, IntPtr path,
			NotifyAction action, NodeKind kind, IntPtr mime_type,
			NotifyState content_state, NotifyState prop_state, long revision);
			
		delegate IntPtr svn_client_get_commit_log_t(ref IntPtr log_msg,
			ref IntPtr tmp_file, IntPtr commit_items, IntPtr baton,
			IntPtr pool);

		[DllImport(svnclientlib)] static extern void svn_auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, [MarshalAs (UnmanagedType.SysInt)] int retry_limit, IntPtr pool);

		[DllImport(svnclientlib)] static extern IntPtr svn_client_version();
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_create_context(out IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_ls (
				out IntPtr dirents, string path_or_url, ref Rev revision,
               int recurse, IntPtr ctx, IntPtr pool);

		[DllImport(svnclientlib)] static extern IntPtr svn_client_status (
			IntPtr result_rev, string path, ref Rev revision,
			svn_wc_status_func_t status_func, IntPtr status_baton,
			int descend, int get_all, int update, int no_ignore,
			IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_log (
			IntPtr apr_array_header_t_targets,
			ref Rev rev_start, ref Rev rev_end,
			int discover_changed_paths,
            int strict_node_history,
            svn_log_message_receiver_t receiver,
            IntPtr receiver_baton,
            IntPtr ctx, IntPtr pool);
            
        [DllImport(svnclientlib)] static extern IntPtr svn_time_from_cstring (
        	out long aprtime, string time, IntPtr pool);
        	
		[DllImport(svnclientlib)] static extern IntPtr svn_client_url_from_path (
			ref IntPtr url, string path_or_url, IntPtr pool);

		[DllImport(svnclientlib)] static extern IntPtr svn_client_cat2 (
			IntPtr stream, string path_or_url,
			ref Rev peg_revision,
			ref Rev revision,
			IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_create (
			IntPtr baton, IntPtr pool);
		
		//[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_read (
		//	IntPtr stream, svn_readwrite_fn_t reader);

		[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_write (
			IntPtr stream, svn_readwrite_fn_t writer);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_update (
			IntPtr result_rev,
			string path, ref Rev revision,
			int recurse, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_delete (
			ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
			int force, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_add (
			string path, int recurse, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_commit (
			ref IntPtr svn_client_commit_info_t_commit_info,
			IntPtr apr_array_header_t_targets, int nonrecursive,
			IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_revert (
			IntPtr apr_array_header_t_targets, int recursive,
			IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)] static extern IntPtr svn_client_move(
			ref IntPtr commit_info_p, string srcPath, ref Rev rev,
			string destPath, int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_checkout(
			out Rev revision, string url, string path, ref Rev rev, 
			int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_mkdir2(
			ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool);
			
		[DllImport(svnclientlib)]
		static extern IntPtr svn_client_diff (
			IntPtr diff_options,
			string path1,
			ref Rev revision1,
			string path2,
			ref Rev revision2,
			int recurse,
			int ignore_ancestry,
			int no_diff_deleted,
			IntPtr outfile,
			IntPtr errfile,
			IntPtr ctx,
			IntPtr pool
		);
	
	}
}
