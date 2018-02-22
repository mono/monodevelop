using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Runtime.InteropServices;
using MonoDevelop.Core;
using System.Text;

using svn_revnum_t = System.IntPtr;
using size_t = System.Int32;
using off_t = System.Int64;
using MonoDevelop.Projects.Text;
using System.Threading;
using System.Linq;
using MonoDevelop.Ide;
using System.Diagnostics;

namespace MonoDevelop.VersionControl.Subversion.Unix
{
	sealed class SvnClient : SubversionVersionControl
	{
		static LibApr apr;
		static readonly Lazy<bool> isInstalled;
		static LibSvnClient svn;
		static readonly Lazy<bool> pre_1_7;

		internal static LibApr Apr {
			get {
				if (apr == null)
					CheckInstalled ();
				return apr;
			}
		}

		internal static LibSvnClient Svn {
			get {
				if (svn == null)
					CheckInstalled ();
				return svn;
			}
		}

		internal static void CheckError (IntPtr error)
		{
			CheckError (error, null);
		}

		public static SubversionException CheckErrorNoThrow (IntPtr error, int? allowedError)
		{
			string msg = null;
			int errorCode = 0; // Innermost error-code.
			while (error != IntPtr.Zero) {
				LibSvnClient.svn_error_t error_t = (LibSvnClient.svn_error_t)Marshal.PtrToStructure (error, typeof(LibSvnClient.svn_error_t));
				if (allowedError.HasValue && error_t.apr_err == allowedError.Value)
					return null;

				errorCode = error_t.apr_err;
				if (msg != null)
					msg += "\n" + GetErrorMessage (error_t);
				else
					msg = GetErrorMessage (error_t);
				error = error_t.svn_error_t_child;

				if (msg == null)
					msg = GettextCatalog.GetString ("Unknown error");
			}
			return msg != null ? new SubversionException (msg, errorCode) : null;
		}

		public static void CheckError (IntPtr error, int? allowedError)
		{
			SubversionException ex = CheckErrorNoThrow (error, allowedError);
			if (ex != null)
				throw ex;
		}
		
		static string GetErrorMessage (LibSvnClient.svn_error_t error)
		{
			if (error.message != null)
				return error.message;
			else {
				byte[] buf = new byte [300];
				// Caller will handle the lock.
				Svn.strerror (error.apr_err, buf, buf.Length);
				return Encoding.UTF8.GetString (buf);
			}
		}

		internal static IntPtr newpool (IntPtr parent)
		{
			IntPtr p;

			// Caller will handle the lock.
			Apr.pool_create_ex (out p, parent, IntPtr.Zero, IntPtr.Zero);
			if (p == IntPtr.Zero)
				throw new InvalidOperationException ("Could not create an APR pool.");
			return p;
		}

		internal static void destroypool (IntPtr pool)
		{
			// Caller will handle lock.
			if (pool != IntPtr.Zero)
				Apr.pool_destroy (pool);
		}
		
		public static string NormalizePath (string pathOrUrl, IntPtr localpool)
		{
			if (pathOrUrl == null)
				return null;
			// Caller will handle the lock.
			IntPtr res = Svn.path_internal_style (pathOrUrl, localpool);
			return Marshal.PtrToStringAnsi (res);
		}

		internal static bool CheckInstalled ()
		{
			// libsvn_client may be linked to libapr-0 or libapr-1, and we need to bind the LibApr class
			// to the same library. The following code detects the required libapr version and loads it. 
			int aprver = GetLoadAprLib (-1);
			svn = LibSvnClient.GetLib ();
			if (svn == null) {
				LoggingService.LogWarning ("Subversion addin could not load libsvn_client, so it will be disabled.");
				return false;
			}
			aprver = GetLoadAprLib (aprver);
			if (aprver != -1)
				LoggingService.LogDebug ("Subversion addin detected libapr-" + aprver);
			apr = LibApr.GetLib (aprver);
			if (apr == null) {
				svn = null;
				LoggingService.LogInfo ("Subversion addin could not load libapr, so it will be disabled.");
				return false;
			}
			return true;
		}

		public override string Version {
			get {
				try {
					return GetVersion ();
				} catch (Exception e) {
					LoggingService.LogError ("Failed to query Subversion version info", e);
					return base.Version;
				}
			}
		}

		internal static bool CheckVersion ()
		{
			return GetVersion ().StartsWith ("1.6", StringComparison.Ordinal);
		}

		public static string GetVersion ()
		{
			IntPtr ptr;
			lock (Svn)
				ptr = Svn.client_version ();
			LibSvnClient.svn_version_t ver = (LibSvnClient.svn_version_t)Marshal.PtrToStructure (ptr, typeof(LibSvnClient.svn_version_t));
			return ver.major + "." + ver.minor + "." + ver.patch;
		}

		static int GetLoadAprLib (int oldVersion)
		{
			// Get the version of the loaded libapr
			string file = "/proc/" + System.Diagnostics.Process.GetCurrentProcess ().Id + "/maps";
			try {
				int newv = oldVersion;
				if (File.Exists (file)) {
					string txt = File.ReadAllText (file);
					if (txt.IndexOf ("libapr-0", StringComparison.Ordinal) != -1 && oldVersion != 0)
						newv = 0;
					if (txt.IndexOf ("libapr-1", StringComparison.Ordinal) != -1 && oldVersion != 1)
						newv = 1;
				}
				return newv;
			} catch {
				// Ignore
				return oldVersion;
			}
		}

		public override bool IsInstalled {
			get { return isInstalled.Value; }
		}

		public static bool Pre_1_7 {
			get { return pre_1_7.Value; }
		}

		static SvnClient ()
		{
			isInstalled = new Lazy<bool> (CheckInstalled);
			pre_1_7 = new Lazy<bool> (CheckVersion);
		}

		public override SubversionBackend CreateBackend ()
		{
			return new UnixSvnBackend ();
		}

		static bool FallbackProbeDirectoryDotSvn (FilePath path)
		{
			while (!path.IsNullOrEmpty) {
				if (Directory.Exists (path.Combine (".svn")))
					return true;
				path = path.ParentDirectory;
			}
			return false;
		}

		public override string GetDirectoryDotSvn (FilePath path)
		{
			if (path.IsNullOrEmpty)
				return string.Empty;
			
			if (Pre_1_7)
				return base.GetDirectoryDotSvn (path);

			UnixSvnBackend backend = CreateBackend () as UnixSvnBackend;
			if (backend == null)
				return String.Empty;

			return backend.GetDirectoryDotSvnInternal (path);
		}
	}

	sealed class UnixSvnBackend : SubversionBackend
	{
		static LibApr apr {
			get {
				return SvnClient.Apr;
			}
		}
		
		static LibSvnClient svn {
			get {
				return SvnClient.Svn;
			}
		}

		static void CheckError (IntPtr error)
		{
			SvnClient.CheckError (error);
		}

		static void CheckError (IntPtr error, int? allowedError)
		{
			SvnClient.CheckError (error, allowedError);
		}

		static SubversionException CheckErrorNoThrow (IntPtr error, int? allowedError)
		{
			return SvnClient.CheckErrorNoThrow (error, allowedError);
		}

		static IntPtr newpool (IntPtr parent)
		{
			return SvnClient.newpool (parent);
		}

		static void destroypool (IntPtr pool)
		{
			SvnClient.destroypool (pool);
		}

		bool disposed;
		readonly IntPtr auth_baton;
		readonly IntPtr pool;
		readonly IntPtr ctx;

		ProgressMonitor updatemonitor;
		ArrayList updateFileList;
		string commitmessage;

		ArrayList lockFileList;
		LibSvnClient.NotifyLockState requiredLockState;

		// retain this so the delegates aren't GC'ed
		readonly LibSvnClient.svn_cancel_func_t cancel_func;
		readonly LibSvnClient.svn_ra_progress_notify_func_t progress_func;
		readonly LibSvnClient.svn_wc_notify_func2_t notify_func;
		readonly LibSvnClient.svn_client_get_commit_log_t log_func;
		readonly IntPtr config_hash;
		readonly IntPtr wc_ctx;

		static bool IsBinary (byte[] buffer, long length)
		{
			length = (int)Math.Min (50 * 1024, length);
			for (int i = 0; i < length; i ++)
				if (buffer [i] == 0)
					return true;
			return false;
		}

		public UnixSvnBackend ()
		{
			lock (svn) {
				// Allocate the APR pool and the SVN client context.
				pool = newpool (IntPtr.Zero);

				// Make sure the config directory is properly created.
				// If the config directory and specifically the subdirectories
				// for the authentication providers don't exist, authentication
				// data won't be saved and no error is given.
				svn.config_ensure (null, pool);

				// Load user and system configuration
				svn.config_get_config (ref config_hash, null, pool);

				if (svn.client_create_context (out ctx, pool) != IntPtr.Zero)
					throw new InvalidOperationException ("Could not create a Subversion client context.");
			
				// Set the callbacks on the client context structure.
				notify_func = new LibSvnClient.svn_wc_notify_func2_t (svn_wc_notify_func_t_impl);
				Marshal.WriteIntPtr (ctx,
				                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "NotifyFunc2"),
				                     Marshal.GetFunctionPointerForDelegate (notify_func));
				log_func = new LibSvnClient.svn_client_get_commit_log_t (svn_client_get_commit_log_impl);
				Marshal.WriteIntPtr (ctx,
				                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "LogMsgFunc"),
				                     Marshal.GetFunctionPointerForDelegate (log_func));
				progress_func = new LibSvnClient.svn_ra_progress_notify_func_t (svn_ra_progress_notify_func_t_impl);
				Marshal.WriteIntPtr (ctx,
				                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "progress_func"),
				                     Marshal.GetFunctionPointerForDelegate (progress_func));
				cancel_func = new LibSvnClient.svn_cancel_func_t (svn_cancel_func_t_impl);
				Marshal.WriteIntPtr (ctx,
				                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "cancel_func"),
				                     Marshal.GetFunctionPointerForDelegate (cancel_func));

				Marshal.WriteIntPtr (ctx,
				                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "config"),
				                     config_hash);

				if (!SvnClient.Pre_1_7) {
					IntPtr scratch = newpool (IntPtr.Zero);
					svn.wc_context_create (out wc_ctx, IntPtr.Zero, pool, scratch);
					Marshal.WriteIntPtr (ctx,
					                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "wc_ctx"),
					                     wc_ctx);
					apr.pool_destroy (scratch);
				}

				IntPtr providers = apr.array_make (pool, 16, IntPtr.Size);
				IntPtr item;
			
				// The main disk-caching auth providers, for both
				// 'username/password' creds and 'username' creds.
			
				item = apr.array_push (providers);
				svn.client_get_simple_provider (item, pool);

				item = apr.array_push (providers);
				svn.client_get_username_provider (item, pool);
			
				// The server-cert, client-cert, and client-cert-password providers
			
				item = apr.array_push (providers);
				svn.client_get_ssl_server_trust_file_provider (item, pool);
			
				item = apr.array_push (providers);
				svn.client_get_ssl_client_cert_file_provider (item, pool);
			
				item = apr.array_push (providers);
				svn.client_get_ssl_client_cert_pw_file_provider (item, pool);

				// Two basic prompt providers: username/password, and just username.

				item = apr.array_push (providers);
				svn.client_get_simple_prompt_provider (item, OnAuthSimplePromptCallback, IntPtr.Zero, 2, pool);
			
				item = apr.array_push (providers);
				svn.client_get_username_prompt_provider (item, OnAuthUsernamePromptCallback, IntPtr.Zero, 2, pool);
			
				// Three ssl prompt providers, for server-certs, client-certs,
				// and client-cert-passphrases.
			
				item = apr.array_push (providers);
				svn.client_get_ssl_server_trust_prompt_provider (item, OnAuthSslServerTrustPromptCallback, IntPtr.Zero, pool);
			
				item = apr.array_push (providers);
				svn.client_get_ssl_client_cert_prompt_provider (item, OnAuthSslClientCertPromptCallback, IntPtr.Zero, 2, pool);
			
				item = apr.array_push (providers);
				svn.client_get_ssl_client_cert_pw_prompt_provider (item, OnAuthSslClientCertPwPromptCallback, IntPtr.Zero, 2, pool);

				// Create the authentication baton			
				svn.auth_open (out auth_baton, providers, pool);

				Marshal.WriteIntPtr (ctx,
				                     (int)Marshal.OffsetOf (typeof(LibSvnClient.svn_client_ctx_t), "auth_baton"),
				                     auth_baton);
			}
		}
		
		public void Dispose ()
		{
			lock (svn) {
				if (!disposed) {
					if (apr != null)
						apr.pool_destroy (pool);
					disposed = true;
				}
			}
		}
		
		~UnixSvnBackend ()
		{
			Dispose ();
		}
		
		static IntPtr GetCancelError ()
		{
			// Subversion destroys the error pool to dispose the error object,
			// so we need to use a non-shared pool.
			IntPtr localpool = newpool (IntPtr.Zero);
			var error = new LibSvnClient.svn_error_t {
				apr_err = LibApr.APR_OS_START_USEERR,
				message = GettextCatalog.GetString ("Operation cancelled."),
				pool = localpool,
			};

			return apr.pcalloc (localpool, error);
		}

		static readonly LibSvnClient.svn_auth_simple_prompt_func_t OnAuthSimplePromptCallback = OnAuthSimplePrompt;
		static IntPtr OnAuthSimplePrompt (ref IntPtr cred, IntPtr baton, string realm, string user_name, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_simple_t data = new LibSvnClient.svn_auth_cred_simple_t ();
			data.username = user_name;
			bool ms;
			if (SimpleAuthenticationPrompt (realm, may_save, ref data.username, out data.password, out ms)) {
				data.may_save = ms;
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			} else {
				data.password = "";
				data.username = "";
				data.may_save = false;
				cred = apr.pcalloc (pool, data);
				return GetCancelError ();
			}
		}

		static readonly LibSvnClient.svn_auth_username_prompt_func_t OnAuthUsernamePromptCallback = OnAuthUsernamePrompt;
		static IntPtr OnAuthUsernamePrompt (ref IntPtr cred, IntPtr baton, string realm, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_username_t data = new LibSvnClient.svn_auth_cred_username_t {
				username = "",
			};
			bool ms;
			if (UserNameAuthenticationPrompt (realm, may_save, ref data.username, out ms)) {
				data.may_save = ms;
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			} else {
				data.username = "";
				data.may_save = false;
				cred = apr.pcalloc (pool, data);
				return GetCancelError ();
			}
		}

		static readonly LibSvnClient.svn_auth_ssl_server_trust_prompt_func_t OnAuthSslServerTrustPromptCallback = OnAuthSslServerTrustPrompt;
		static IntPtr OnAuthSslServerTrustPrompt (ref IntPtr cred, IntPtr baton, string realm, UInt32 failures, ref LibSvnClient.svn_auth_ssl_server_cert_info_t cert_info, bool may_save, IntPtr pool)
		{
			var data = new LibSvnClient.svn_auth_cred_ssl_server_trust_t ();

			var ci = new CertficateInfo {
				AsciiCert = cert_info.ascii_cert,
				Fingerprint = cert_info.fingerprint,
				HostName = cert_info.hostname,
				IssuerName = cert_info.issuer_dname,
				ValidFrom = cert_info.valid_from,
				ValidUntil = cert_info.valid_until,
			};

			SslFailure accepted_failures;
			bool ms;
			if (SslServerTrustAuthenticationPrompt (realm, (SslFailure) failures, may_save, ci, out accepted_failures, out ms) && accepted_failures != SslFailure.None) {
				data.may_save = ms ;
				data.accepted_failures = (uint) accepted_failures;
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			} else {
				data.accepted_failures = 0;
				data.may_save = false;
				cred = apr.pcalloc (pool, data);
				return GetCancelError ();
			}
		}

		static readonly LibSvnClient.svn_auth_ssl_client_cert_prompt_func_t OnAuthSslClientCertPromptCallback = OnAuthSslClientCertPrompt;
		static IntPtr OnAuthSslClientCertPrompt (ref IntPtr cred, IntPtr baton, string realm, bool may_save, IntPtr pool)
		{
			var data = new LibSvnClient.svn_auth_cred_ssl_client_cert_t ();
			bool ms;
			if (SslClientCertAuthenticationPrompt (realm, may_save, out data.cert_file, out ms)) {
				data.may_save = ms;
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			} else {
				data.cert_file = "";
				data.may_save = false;
				cred = apr.pcalloc (pool, data);
				return GetCancelError ();
			}
		}

		static readonly LibSvnClient.svn_auth_ssl_client_cert_pw_prompt_func_t OnAuthSslClientCertPwPromptCallback = OnAuthSslClientCertPwPrompt;
		static IntPtr OnAuthSslClientCertPwPrompt (ref IntPtr cred, IntPtr baton, string realm, bool may_save, IntPtr pool)
		{
			var data = new LibSvnClient.svn_auth_cred_ssl_client_cert_pw_t ();
			bool ms;
			if (SslClientCertPwAuthenticationPrompt (realm, may_save, out data.password, out ms)) {
				data.may_save = ms;
				cred = apr.pcalloc (pool, data);
				return IntPtr.Zero;
			} else {
				data.password = "";
				data.may_save = false;
				cred = apr.pcalloc (pool, data);
				return GetCancelError ();
			}
		}
		
		// Wrappers for native interop
		
		public override string GetVersion ()
		{
			return SvnClient.GetVersion ();
		}

		public override IEnumerable<DirectoryEntry> List (FilePath path, bool recurse, SvnRevision rev)
		{
			return ListUrl (path, recurse, rev);
		}
		
		static string NormalizePath (string pathOrUrl, IntPtr localpool)
		{
			return SvnClient.NormalizePath (pathOrUrl, localpool);
		}

		static IntPtr NormalizePaths (IntPtr pool, params FilePath[] paths)
		{
			IntPtr array = apr.array_make (pool, 0, IntPtr.Size);
			foreach (string path in paths) {
				string pathorurl = NormalizePath (path, pool);
				IntPtr item = apr.array_push (array);
				Marshal.WriteIntPtr (item, apr.pstrdup (pool, pathorurl));
			}
			return array;
		}
		
		public override IEnumerable<DirectoryEntry> ListUrl (string url, bool recurse, SvnRevision rev)
		{
			if (url == null)
				throw new ArgumentNullException ();

			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;
			IntPtr localpool = IntPtr.Zero;
			List<DirectoryEntry> items = new List<DirectoryEntry> ();

			try {
				IntPtr hash;

				localpool = TryStartOperation (null);
				url = NormalizePath (url, localpool);
				
				CheckError (svn.client_ls (out hash, url, ref revision,
				                           recurse, ctx, localpool));
				
				IntPtr item = apr.hash_first (localpool, hash);
				LibSvnClient.svn_dirent_t ent;
				string name;
				while (apr.hash_iterate<LibSvnClient.svn_dirent_t>(ref item, out ent, out name)) {
					var dent = new DirectoryEntry {
						Name = name,
						IsDirectory = ent.kind == LibSvnClient.svn_node_kind_t.Dir,
						Size = ent.size,
						HasProps = ent.has_props,
						CreatedRevision = (int) ent.created_rev,
						Time = new DateTime (1970, 1, 1).AddTicks(ent.time * 10),
						LastAuthor = ent.last_author,
					};
					items.Add (dent);
				}
			} finally {
				TryEndOperation (localpool);
			}
			
			return items;
		}

		public override IEnumerable<VersionInfo> Status (Repository repo, FilePath path, SvnRevision rev, bool descendDirs, bool changedItemsOnly, bool remoteStatus)
		{
			if (path == FilePath.Null)
				throw new ArgumentNullException ();
		
			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;
			ArrayList ret = new ArrayList ();
			
			StatusCollector collector = new StatusCollector (ret);
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				string pathorurl = NormalizePath (path, localpool);
				CheckError (svn.client_status (IntPtr.Zero, pathorurl, ref revision,
				                               collector.Func,
				                               IntPtr.Zero, descendDirs, 
				                               !changedItemsOnly, 
				                               remoteStatus,
				                               false,
				                               false,
				                               ctx, localpool));
			} catch (SubversionException e) {
				// SVN_ERR_WC_NOT_WORKING_COPY and SVN_ERR_WC_NOT_FILE.
				if (e.ErrorCode != 155007 && e.ErrorCode != 155008)
					throw;
			} finally {
				TryEndOperation (localpool);
			}

			List<VersionInfo> nodes = new List<VersionInfo>();
			foreach (LibSvnClient.StatusEnt ent in ret)
				nodes.Add (CreateNode (ent, repo));
			return nodes;
		}

		public override IEnumerable<SvnRevision> Log (Repository repo, FilePath path, SvnRevision revStart, SvnRevision revEnd)
		{
			if (path == FilePath.Null)
				throw new ArgumentNullException ();
			
			LibSvnClient.Rev revisionStart = (LibSvnClient.Rev) revStart;
			LibSvnClient.Rev revisionEnd = (LibSvnClient.Rev) revEnd;

			List<SvnRevision> ret = new List<SvnRevision> ();
			IntPtr strptr = IntPtr.Zero;

			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
				IntPtr first = apr.array_push (array);
				string pathorurl = NormalizePath (path, localpool);
				strptr = Marshal.StringToHGlobalAnsi (pathorurl);
				Marshal.WriteIntPtr (first, strptr);
				
				LogCollector collector = new LogCollector ((SubversionRepository)repo, ret, ctx);
				
				CheckError (svn.client_log (array, ref revisionStart, ref revisionEnd, true, false,
				                            collector.Func,
				                            IntPtr.Zero, ctx, localpool));
			} finally {
				if (strptr != IntPtr.Zero)
					Marshal.FreeHGlobal (strptr);
				TryEndOperation (localpool);
			}
			
			return ret;
		}
		
		public override Annotation[] GetAnnotations (Repository repo, FilePath file, SvnRevision revStart, SvnRevision revEnd)
		{
			if (file == FilePath.Null)
				throw new ArgumentNullException ();
				
			LibSvnClient.Rev revisionStart = (LibSvnClient.Rev) revStart;
			LibSvnClient.Rev revisionEnd = (LibSvnClient.Rev) revEnd;

			MemoryStream data = new MemoryStream ();
			int numAnnotations = 0;
			Cat (file, SvnRevision.Base, data);

			using (StreamReader reader = new StreamReader (data)) {
				reader.BaseStream.Seek (0, SeekOrigin.Begin);
				while (reader.ReadLine () != null)
					numAnnotations++;
			}

			Annotation[] annotations = new Annotation [numAnnotations];
			AnnotationCollector collector = new AnnotationCollector (annotations, repo);

			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				string path = NormalizePath (file.FullPath, localpool);
				CheckError (svn.client_blame (path, ref revisionStart, ref revisionEnd, collector.Func, IntPtr.Zero, ctx, localpool));
			} finally {
				TryEndOperation (localpool);
			}
			
			return annotations;
		}

		public override string GetTextAtRevision (string repositoryPath, Revision revision, string rootPath)
		{
			MemoryStream memstream = new MemoryStream ();
			try {
				Cat (repositoryPath, (SvnRevision) revision, memstream);
			} catch (SubversionException e) {
				// File got added/removed at some point.
				// SVN_ERR_FS_NOT_FOUND
				if (e.ErrorCode == 160013)
					return "";
				// We tried on a directory
				// SVN_ERR_FS_NOT_FILE
				if (e.ErrorCode == 160017)
					return "";
				throw;
			}

			var buffer = memstream.GetBuffer ();
			try {
				if (IsBinary (buffer, memstream.Length))
					return null;
				return Encoding.UTF8.GetString (buffer, 0, (int) memstream.Length);
			} catch {
				return "";
			}
		}
		
		public void Cat (string pathorurl, SvnRevision rev, Stream stream)
		{
			if (pathorurl == null || stream == null)
				throw new ArgumentNullException ();
		
			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;
			
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				pathorurl = NormalizePath (pathorurl, localpool);
				StreamCollector collector = new StreamCollector (stream);
				IntPtr svnstream = svn.stream_create (IntPtr.Zero, localpool);
				svn.stream_set_write (svnstream, collector.Func);
				// Setting peg_revision to revision.
				// Otherwise, it will use Head as peg and it will throw exceptions.
				CheckError (svn.client_cat2 (svnstream, pathorurl, ref revision, ref revision, ctx, localpool), 195007);
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override void Update (FilePath path, bool recurse, ProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException();

			updateFileList = new ArrayList ();
			
			LibSvnClient.Rev rev = LibSvnClient.Rev.Head;
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				string pathorurl = NormalizePath (path, localpool);
				IntPtr result = Marshal.AllocHGlobal (IntPtr.Size);
				CheckError (svn.client_update (result, pathorurl, ref rev, recurse, ctx, localpool));
				Marshal.FreeHGlobal (result);
			} finally {
				TryEndOperation (localpool);

				foreach (string file in updateFileList)
					FileService.NotifyFileChanged (file, true);

				updateFileList = null;
			}
		}

		public override void Revert (FilePath[] paths, bool recurse, ProgressMonitor monitor)
		{
			if (paths == null || monitor == null)
				throw new ArgumentNullException();

			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				IntPtr array = NormalizePaths (localpool, paths);
				CheckError (svn.client_revert (array, recurse, ctx, localpool));
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override void Add (FilePath path, bool recurse, ProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException ();

			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				string pathorurl = NormalizePath (path, localpool);
				CheckError (svn.client_add3 (pathorurl, recurse, true, false, ctx, localpool));
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override void Checkout (string url, FilePath path, Revision revision, bool recurse, ProgressMonitor monitor)
		{
			if (url == null || monitor == null)
				throw new ArgumentNullException ();
			
			if (revision == null)
				revision = SvnRevision.Head;
			var rev = (LibSvnClient.Rev) revision;
			
			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				// Using Uri here because the normalization method doesn't remove the redundant port number when using https
				url = NormalizePath (new Uri(url).ToString(), localpool);
				string npath = NormalizePath (path, localpool);
				CheckError (svn.client_checkout (IntPtr.Zero, url, npath, ref rev, recurse, ctx, localpool));
			} catch (SubversionException e) {
				if (e.ErrorCode != 200015)
					throw;

				if (Directory.Exists (path.ParentDirectory))
					FileService.DeleteDirectory (path.ParentDirectory);
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override void Commit (FilePath[] paths, string message, ProgressMonitor monitor)
		{
			if (paths == null || message == null || monitor == null)
				throw new ArgumentNullException();

			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				IntPtr array = NormalizePaths (localpool, paths);
				IntPtr commit_info = IntPtr.Zero;
				commitmessage = message;
				
				CheckError (svn.client_commit (ref commit_info, array, false, ctx, localpool));
				unsafe {
					if (commit_info != IntPtr.Zero) {
						monitor.Log.WriteLine ();
						monitor.Log.WriteLine (GettextCatalog.GetString ("Revision: {0}", ((LibSvnClient.svn_client_commit_info_t *) commit_info.ToPointer())->revision));
					}
				}
			} finally {
				commitmessage = null;
				TryEndOperation (localpool);
			}
		}

		public override void Mkdir (string[] paths, string message, ProgressMonitor monitor) 
		{
			if (paths == null || monitor == null)
				throw new ArgumentNullException ();

			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				IntPtr array = NormalizePaths (localpool, paths.Select (p => (FilePath)p).ToArray ());
				
				commitmessage = message;

				IntPtr commit_info = IntPtr.Zero;
				CheckError (svn.client_mkdir2 (ref commit_info, array, ctx, localpool));
			} finally {	
				commitmessage = null;
				TryEndOperation (localpool);
			}
		}

		public override void Delete (FilePath path, bool force, ProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException ();

			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				IntPtr array = NormalizePaths (localpool, path);
				IntPtr commit_info = IntPtr.Zero;
				CheckError (svn.client_delete (ref commit_info, array, force, ctx, localpool));
			} finally {
				commitmessage = null;
				TryEndOperation (localpool);
			}
		}

		public override void Move (FilePath srcPath, FilePath destPath, SvnRevision rev, bool force, ProgressMonitor monitor)
		{
			if (srcPath == FilePath.Null || destPath == FilePath.Null || monitor == null)
				throw new ArgumentNullException ();
			
			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;

			nb = new notify_baton ();
			IntPtr commit_info = IntPtr.Zero;
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				string nsrcPath = NormalizePath (srcPath, localpool);
				string ndestPath = NormalizePath (destPath, localpool);
				CheckError (svn.client_move (ref commit_info, nsrcPath, ref revision,
				                             ndestPath, force, ctx, localpool));
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override void Lock (ProgressMonitor monitor, string comment, bool stealLock, params FilePath[] paths)
		{
			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				IntPtr array = NormalizePaths (localpool, paths);
				lockFileList = new ArrayList ();
				requiredLockState = LibSvnClient.NotifyLockState.Locked;
				
				CheckError (svn.client_lock (array, comment, stealLock, ctx, localpool));
				if (paths.Length != lockFileList.Count)
					throw new SubversionException ("Lock operation failed.");
			} finally {
				lockFileList = null;
				TryEndOperation (localpool);
			}
		}
		
		public override void Unlock (ProgressMonitor monitor, bool breakLock, params FilePath[] paths)
		{
			nb = new notify_baton ();
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (monitor);
				IntPtr array = NormalizePaths (localpool, paths);
				lockFileList = new ArrayList ();
				requiredLockState = LibSvnClient.NotifyLockState.Unlocked;
			
				CheckError (svn.client_unlock (array, breakLock, ctx, localpool));
				if (paths.Length != lockFileList.Count)
					throw new SubversionException ("Unlock operation failed.");
			} finally {
				lockFileList = null;
				TryEndOperation (localpool);
			}
		}

		public override string GetUnifiedDiff (FilePath path1, SvnRevision rev1, FilePath path2, SvnRevision rev2, bool recursive)
		{
			IntPtr outfile = IntPtr.Zero;
			IntPtr errfile = IntPtr.Zero;
			string fout = null;
			string ferr = null;
			
			LibSvnClient.Rev revision1 = (LibSvnClient.Rev) rev1;
			LibSvnClient.Rev revision2 = (LibSvnClient.Rev) rev2;
			
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				IntPtr options = apr.array_make (localpool, 0, IntPtr.Size);
				
				fout = Path.GetTempFileName ();
				ferr = Path.GetTempFileName ();
				int res1 = apr.file_open (ref outfile, fout, LibApr.APR_WRITE | LibApr.APR_CREATE | LibApr.APR_TRUNCATE, LibApr.APR_OS_DEFAULT, localpool);
				int res2 = apr.file_open (ref errfile, ferr, LibApr.APR_WRITE | LibApr.APR_CREATE | LibApr.APR_TRUNCATE, LibApr.APR_OS_DEFAULT, localpool);
				
				if (res1 == 0 && res2 == 0) {
					string npath1 = NormalizePath (path1, localpool);
					string npath2 = NormalizePath (path2, localpool);
					CheckError (svn.client_diff (options, npath1, ref revision1, npath2, ref revision2, recursive, false, true, outfile, errfile, ctx, localpool));
					return TextFile.ReadFile (fout).Text;
				} else {
					throw new Exception ("Could not get diff information");
				}
			} catch {
				try {
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile);
					outfile = IntPtr.Zero;
				} catch {}
				throw;
			} finally {
				try {
					// Cleanup
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile); 
					if (errfile != IntPtr.Zero)
						apr.file_close (errfile);
					if (ferr != null)
						FileService.DeleteFile (ferr);
					if (fout != null)
						FileService.DeleteFile (fout);
				} catch {
				} finally {
					TryEndOperation (localpool);
				}
			}
		}

		public override void RevertToRevision (FilePath path, Revision revision, ProgressMonitor monitor)
		{
			Merge (path, LibSvnClient.Rev.Head, (LibSvnClient.Rev) revision);
		}

		public override void RevertRevision (FilePath path, Revision revision, ProgressMonitor monitor)
		{
			SvnRevision srev = (SvnRevision) revision;
			Merge (path, (LibSvnClient.Rev) srev, LibSvnClient.Rev.Number (srev.Rev - 1));
		}
		
		private void Merge (string path, LibSvnClient.Rev revision1, LibSvnClient.Rev revision2)
		{
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				path = NormalizePath (path, localpool);
				LibSvnClient.Rev working = LibSvnClient.Rev.Working;
				CheckError (svn.client_merge_peg2 (path, 
				                                   ref revision1,
				                                   ref revision2,
				                                   ref working,
				                                   path,
				                                   false, false, false, false,
				                                   IntPtr.Zero, //default options is NULL
				                                   ctx, localpool));
			}
			finally {
				TryEndOperation (localpool);
			}
		}

		static void GetProps (StringBuilder props, IntPtr pool, IntPtr result)
		{
			foreach (var new_props in apr.hash_foreach<LibSvnClient.svn_string_t> (pool, result))
				props.Append (Marshal.PtrToStringAnsi (new_props.data));
		}

		public override void Ignore (FilePath[] paths)
		{
			IntPtr result;
			IntPtr props_ptr;
			var props = new StringBuilder ();
			string new_path;
			LibSvnClient.svn_string_t new_props;
			LibSvnClient.Rev rev = LibSvnClient.Rev.Working;

			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				foreach (var path in paths) {
					new_path = NormalizePath (path, localpool);
					CheckError (svn.client_propget (out result, "svn:ignore", Path.GetDirectoryName (new_path),
					                                ref rev, false, ctx, localpool));
					GetProps (props, localpool, result);

					props.AppendLine (Path.GetFileName (new_path));
					new_props = new LibSvnClient.svn_string_t {
						data = Marshal.StringToHGlobalAnsi (props.ToString ()),
						len = props.Length,
					};
					props_ptr = apr.pcalloc (localpool, new_props);
					CheckError (svn.client_propset ("svn:ignore", props_ptr, Path.GetDirectoryName (new_path), false, localpool));
				}
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override void Unignore (FilePath[] paths)
		{
			IntPtr result;
			IntPtr props_ptr;
			var props = new StringBuilder ();
			string new_path;
			LibSvnClient.svn_string_t new_props;
			LibSvnClient.Rev rev = LibSvnClient.Rev.Working;
			int index;
			string props_str;

			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				foreach (var path in paths) {
					new_path = NormalizePath (path, localpool);
					CheckError (svn.client_propget (out result, "svn:ignore", Path.GetDirectoryName (new_path),
					                                ref rev, false, ctx, localpool));
					GetProps (props, localpool, result);

					props_str = props.ToString ();
					index = props_str.IndexOf (Path.GetFileName (new_path) + Environment.NewLine, StringComparison.Ordinal);
					props_str = (index < 0) ? props_str : props_str.Remove (index, Path.GetFileName(new_path).Length+1);

					new_props = new LibSvnClient.svn_string_t {
						data = Marshal.StringToHGlobalAnsi (props_str),
						len = props_str.Length,
					};
					props_ptr = apr.pcalloc (localpool, new_props);
					CheckError (svn.client_propset ("svn:ignore", props_ptr, Path.GetDirectoryName (new_path), false, localpool));
				}
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override bool HasNeedLock (FilePath file)
		{
			IntPtr result;
			var props = new StringBuilder ();
			string new_path;
			LibSvnClient.Rev rev = LibSvnClient.Rev.Working;

			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				new_path = NormalizePath (file, localpool);
				CheckError (svn.client_propget (out result, "svn:needs-lock", new_path,
					ref rev, false, ctx, localpool));
				GetProps (props, localpool, result);

				return props.Length != 0;
			} finally {
				TryEndOperation (localpool);
			}
		}

		public override string GetTextBase (string sourcefile)
		{
			MemoryStream data = new MemoryStream ();
			try {
				Cat (sourcefile, SvnRevision.Base, data);
				return TextFile.ReadFile (sourcefile, data).Text;
				// This outputs the contents of the base revision
				// of a file to a stream.
			} catch (SubversionException) {
				// This occurs when we don't have a base file for
				// the target file. We have no way of knowing if
				// a file has a base version therefore this will do.
				return String.Empty;
			}
		}
		
		IntPtr svn_client_get_commit_log_impl (ref IntPtr log_msg, ref IntPtr tmp_file,
		                                       IntPtr commit_items, IntPtr baton, IntPtr pool)
		{
			log_msg = apr.pstrdup (pool, commitmessage);
			tmp_file = IntPtr.Zero;
			return IntPtr.Zero;
		}

		IntPtr TryStartOperation (ProgressMonitor monitor)
		{
			Monitor.Enter (svn);
			updatemonitor = monitor;
			progressData = new ProgressData ();
			return newpool (pool);
		}

		void TryEndOperation (IntPtr pool)
		{
			destroypool (pool);
			updatemonitor = null;
			progressData.LogTimer.Dispose ();
			progressData = null;
			Monitor.Exit (svn);
		}

		static VersionInfo CreateNode (LibSvnClient.StatusEnt ent, Repository repo) 
		{
			VersionStatus rs = VersionStatus.Unversioned;
			Revision rr = null;
			
			if (ent.RemoteTextStatus != LibSvnClient.svn_wc_status_kind.EMPTY) {
				rs = ConvertStatus (LibSvnClient.NodeSchedule.Normal, ent.RemoteTextStatus);
				rr = new SvnRevision (repo, ent.LastCommitRevision, ent.LastCommitDate,
				                      ent.LastCommitAuthor, GettextCatalog.GetString ("(unavailable)"), null);
			}

			VersionStatus status = ConvertStatus (ent.Schedule, ent.TextStatus);
			
			bool readOnly = File.Exists (ent.LocalFilePath) && (File.GetAttributes (ent.LocalFilePath) & FileAttributes.ReadOnly) != 0;
			
			if (ent.RepoLocked) {
				status |= VersionStatus.LockRequired;
				if (ent.LockOwned)
					status |= VersionStatus.LockOwned;
				else
					status |= VersionStatus.Locked;
			} else if (readOnly)
				status |= VersionStatus.LockRequired;

			VersionInfo ret = new VersionInfo (ent.LocalFilePath, ent.Url, ent.IsDirectory,
			                                   status, new SvnRevision (repo, ent.Revision),
			                                   rs, rr);
			return ret;
		}
		
		static VersionStatus ConvertStatus (LibSvnClient.NodeSchedule schedule, LibSvnClient.svn_wc_status_kind status) {
			switch (schedule) {
				case LibSvnClient.NodeSchedule.Add: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case LibSvnClient.NodeSchedule.Delete: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case LibSvnClient.NodeSchedule.Replace: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
			}
			
			switch (status) {
			case LibSvnClient.svn_wc_status_kind.None: return VersionStatus.Unversioned;
			case LibSvnClient.svn_wc_status_kind.Normal: return VersionStatus.Versioned;
			case LibSvnClient.svn_wc_status_kind.Unversioned: return VersionStatus.Unversioned;
			case LibSvnClient.svn_wc_status_kind.Modified: return VersionStatus.Versioned | VersionStatus.Modified;
			case LibSvnClient.svn_wc_status_kind.Merged: return VersionStatus.Versioned | VersionStatus.Modified;
			case LibSvnClient.svn_wc_status_kind.Conflicted: return VersionStatus.Versioned | VersionStatus.Conflicted;
			case LibSvnClient.svn_wc_status_kind.Ignored: return VersionStatus.Unversioned | VersionStatus.Ignored;
			case LibSvnClient.svn_wc_status_kind.Obstructed: return VersionStatus.Versioned;
			case LibSvnClient.svn_wc_status_kind.Added: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
			case LibSvnClient.svn_wc_status_kind.Deleted: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
			case LibSvnClient.svn_wc_status_kind.Replaced: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
			}
			
			return VersionStatus.Unversioned;
		}
		
		// same logic as svn_mime_type_is_binary, but I don't see a reason for a native call
		static bool MimeTypeIsBinary (string mimeType)
		{
			if (string.IsNullOrEmpty (mimeType))
				return false;
			return !(mimeType.StartsWith ("text/", StringComparison.Ordinal) || 
			         mimeType == "image/x-xbitmap" || 
			         mimeType == "image/x-xpixmap");
		}

		IntPtr svn_cancel_func_t_impl (IntPtr baton)
		{
			if (updatemonitor == null || !updatemonitor.CancellationToken.IsCancellationRequested)
				return IntPtr.Zero;

			IntPtr localpool = newpool (IntPtr.Zero);
			var err = new LibSvnClient.svn_error_t {
				apr_err = 200015,
				message = GettextCatalog.GetString ("The operation was interrupted"),
				pool = localpool
			};

			return apr.pcalloc (localpool, err);
		}

		class ProgressData
		{
			// It's big enough. You don't see repos with more than 5Gb.
			public long Remainder;
			public long SavedProgress;
			public long KBytes;
			public System.Timers.Timer LogTimer = new System.Timers.Timer ();
			public int Seconds;
		}

		static string BytesToSize (long kbytes)
		{
			if (kbytes < 1024)
				return GettextCatalog.GetString ("{0} KBytes", kbytes);
			// 16 * 1024
			if (kbytes < 16384)
				return GettextCatalog.GetString ("{0:0.0} MBytes", kbytes / 1024.0);
			return GettextCatalog.GetString ("{0} MBytes", kbytes / 1024);
		}

		ProgressData progressData;
		void svn_ra_progress_notify_func_t_impl (off_t progress, off_t total, IntPtr baton, IntPtr pool)
		{
			if (updatemonitor == null)
				return;

			long currentProgress = progress;
			if (currentProgress <= progressData.KBytes) {
				if (progressData.SavedProgress < progressData.KBytes) {
					progressData.SavedProgress += progressData.KBytes;
				}
				return;
			}

			long totalProgress = total;
			if (totalProgress != -1 && currentProgress >= totalProgress)
				return;

			progressData.Remainder += currentProgress % 1024;
			if (progressData.Remainder >= 1024) {
				progressData.SavedProgress += progressData.Remainder / 1024;
				progressData.Remainder = progressData.Remainder % 1024;
			}

			progressData.KBytes = progressData.SavedProgress + currentProgress / 1024;
			if (progressData.LogTimer.Enabled)
				return;

			progressData.LogTimer.Interval = 1000;
			progressData.LogTimer.Elapsed += delegate {
				progressData.Seconds += 1;
				Runtime.RunInMainThread (() => {
					updatemonitor?.Log.WriteLine (GettextCatalog.GetString ("Transferred {0} in {1} seconds.", BytesToSize (progressData.KBytes), progressData.Seconds));
				});
			};
			progressData.LogTimer.Start ();
		}
		
		struct notify_baton {
			public bool received_some_change;
			public bool is_checkout;
			public bool is_export;
			public bool suppress_final_line;
			public bool sent_first_txdelta;
			public bool in_external;
			public bool had_print_error;
		}
		notify_baton nb;
		void svn_wc_notify_func_t_impl (IntPtr baton, ref LibSvnClient.svn_wc_notify_t data, IntPtr pool)
		{
			string actiondesc;
			string file = Marshal.PtrToStringAnsi (data.path);
			bool notifyChange = false;
			bool skipEol = false;
//			System.Console.WriteLine(data.action);
			switch (data.action) {
			case LibSvnClient.NotifyAction.Skip: 
				if (data.content_state == LibSvnClient.NotifyState.Missing) {
					actiondesc = string.Format (GettextCatalog.GetString ("Skipped missing target: '{0}'"), file); 
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Skipped '{0}'"), file); 
				}
				break;
			case LibSvnClient.NotifyAction.UpdateDelete: 
				actiondesc = string.Format (GettextCatalog.GetString ("Deleted   '{0}'"), file);
				break;
//			case LibSvnClient.NotifyAction.UpdateReplace: 
//				actiondesc = string.Format (GettextCatalog.GetString ("Replaced  '{0}'"), file);
//				break;
				
			case LibSvnClient.NotifyAction.UpdateAdd: 
				if (data.content_state == LibSvnClient.NotifyState.Conflicted) {
					actiondesc = string.Format (GettextCatalog.GetString ("Conflict {0}"), file); 
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Added   {0}"), file); 
				}
				break;
//			case LibSvnClient.NotifyAction.Exists:
//				// original is untranslated, we'll make it a bit shorter
//				actiondesc = data.content_state == LibSvnClient.NotifyState.Conflicted ? "C" : "E";
//				if (data.prop_state == LibSvnClient.NotifyState.Conflicted) {
//					actiondesc += "C";
//				} else if (data.prop_state == LibSvnClient.NotifyState.Merged) {
//					actiondesc += "G";
//				}
//				actiondesc += " {0}";
//				actiondesc = string.Format (actiondesc, file);
//				actiondesc = string.Format (GettextCatalog.GetString ("Exists   {0}"), file);
//				break;
			case LibSvnClient.NotifyAction.Restore: 
				actiondesc = string.Format (GettextCatalog.GetString ("Restored '{0}'"), file); 
				break;
			case LibSvnClient.NotifyAction.Revert: 
				actiondesc = string.Format (GettextCatalog.GetString ("Reverted '{0}'"), file); 
				break;
			case LibSvnClient.NotifyAction.FailedRevert: 
				actiondesc = string.Format (GettextCatalog.GetString ("Failed to revert '{0}' -- try updating instead."), file);
				break;
			case LibSvnClient.NotifyAction.Resolved:
				actiondesc = string.Format (GettextCatalog.GetString ("Resolved conflict state of '{0}'"), file);
				break;
			case LibSvnClient.NotifyAction.Add: 
				if (MimeTypeIsBinary (Marshal.PtrToStringAuto (data.mime_type))) {
					actiondesc = string.Format (GettextCatalog.GetString ("Add (bin) '{0}'"), file); 
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Add       '{0}'"), file); 
				}
				break;
			case LibSvnClient.NotifyAction.Delete: 
				actiondesc = string.Format (GettextCatalog.GetString ("Delete    '{0}'"), file);
				break;
				
			case LibSvnClient.NotifyAction.UpdateUpdate: 
				// original is untranslated, we'll make it a bit shorter
			/*	actiondesc = "";
				if (data.content_state == LibSvnClient.NotifyState.Conflicted) {
					actiondesc += "C";
				} else if (data.content_state == LibSvnClient.NotifyState.Merged) {
					actiondesc += "G";
				} else if (data.content_state == LibSvnClient.NotifyState.Changed) {
					actiondesc += "U";
				}
				
				if (data.prop_state == LibSvnClient.NotifyState.Conflicted) {
					actiondesc += "C";
				} else if (data.prop_state == LibSvnClient.NotifyState.Merged) {
					actiondesc += "G";
				} else if (data.prop_state == LibSvnClient.NotifyState.Changed) {
					actiondesc += "U";
				}
				if (data.lock_state == LibSvnClient.NotifyLockState.Unlocked)
					actiondesc += "B";
				
				actiondesc += " '{0}'"; 
				actiondesc = string.Format (actiondesc, file); */
				actiondesc = string.Format (GettextCatalog.GetString ("Update '{0}'"), file);
				notifyChange = true;
				break;
			case LibSvnClient.NotifyAction.UpdateExternal: 
				actiondesc = string.Format (GettextCatalog.GetString ("Fetching external item into '{0}'"), file); 
				break;
			case LibSvnClient.NotifyAction.UpdateCompleted:  // TODO
				actiondesc = GettextCatalog.GetString ("Finished"); 
				break;
			case LibSvnClient.NotifyAction.StatusExternal: 
				actiondesc = string.Format (GettextCatalog.GetString ("Performing status on external item at '{0}'"), file);
				break;
			case LibSvnClient.NotifyAction.StatusCompleted: 
				actiondesc = string.Format (GettextCatalog.GetString ("Status against revision: '{0}'"), data.revision);
				break;
				
			case LibSvnClient.NotifyAction.CommitDeleted: 
				actiondesc = string.Format (GettextCatalog.GetString ("Deleting       {0}"), file); 
				break;
			case LibSvnClient.NotifyAction.CommitModified: 
				actiondesc = string.Format (GettextCatalog.GetString ("Sending        {0}"), file);
				notifyChange = true; 
				break;
			case LibSvnClient.NotifyAction.CommitAdded: 
				if (MimeTypeIsBinary (Marshal.PtrToStringAuto (data.mime_type))) {
					actiondesc = string.Format (GettextCatalog.GetString ("Adding  (bin)  '{0}'"), file); 
				} else {
					actiondesc = string.Format (GettextCatalog.GetString ("Adding         '{0}'"), file); 
				}
				break;
			case LibSvnClient.NotifyAction.CommitReplaced: 
				actiondesc = string.Format (GettextCatalog.GetString ("Replacing      {0}"), file);
				notifyChange = true; 
				break;
			case LibSvnClient.NotifyAction.CommitPostfixTxDelta: 
				if (!nb.sent_first_txdelta) {
					actiondesc = GettextCatalog.GetString ("Transmitting file data");
					nb.sent_first_txdelta = true;
				} else {
					actiondesc = ".";
					skipEol = true;
				}
				break;
					
			case LibSvnClient.NotifyAction.Locked: 
				LibSvnClient.svn_lock_t repoLock = (LibSvnClient.svn_lock_t) Marshal.PtrToStructure (data.repo_lock, typeof (LibSvnClient.svn_lock_t));
				actiondesc = string.Format (GettextCatalog.GetString ("'{0}' locked by user '{1}'."), file, repoLock.owner);
				break;
			case LibSvnClient.NotifyAction.Unlocked: 
				actiondesc = string.Format (GettextCatalog.GetString ("'{0}' unlocked."), file);
				break;
			case LibSvnClient.NotifyAction.BlameRevision:
				actiondesc = string.Format (GettextCatalog.GetString ("Get annotations {0}"), file);
				break;
//			case LibSvnClient.NotifyAction.ChangeListSet: 
//				actiondesc = string.Format (GettextCatalog.GetString ("Path '{0}' is now a member of changelist '{1}'."), file, Marshal.PtrToStringAuto (data.changelist_name));
//				break;
//			case LibSvnClient.NotifyAction.ChangeListClear: 
//				actiondesc = string.Format (GettextCatalog.GetString ("Path '{0}' is no longer a member of a changelist."), file);
//				break;
			default:
				LoggingService.LogDebug ("untranslated action:" + data.action);
				actiondesc = data.action.ToString () + " " + file;
				break;
				/*
				 StatusCompleted,
				 StatusExternal,
				 BlameRevision*/
			}
			
			if (updatemonitor != null && !string.IsNullOrEmpty (actiondesc)) {
				Runtime.RunInMainThread (() => {
					if (skipEol) {
						updatemonitor?.Log.Write (actiondesc);
					} else {
						updatemonitor?.Log.WriteLine (actiondesc);
					}
				});
			}
			if (updateFileList != null && notifyChange && File.Exists (file))
				updateFileList.Add (file);
			
			if (lockFileList != null && data.lock_state == requiredLockState)
				lockFileList.Add (file);
		}

		static bool Upgrading;
		static bool TooOld;
		internal string GetDirectoryDotSvnInternal (FilePath path)
		{
			if (Upgrading || TooOld)
				return String.Empty;

			IntPtr result;
			IntPtr scratch = IntPtr.Zero;
			IntPtr localpool = IntPtr.Zero;
			try {
				localpool = TryStartOperation (null);
				scratch = newpool (pool);
				string new_path = NormalizePath (path.FullPath, localpool);
				SubversionException e = CheckErrorNoThrow (svn.client_get_wc_root (out result, new_path, ctx, localpool, scratch), null);
				if (e != null) {
					// SVN_ERR_SVN_ERR_WC_UPGRADE_REQUIRED
					Upgrading = e.ErrorCode == 155036;

					// SVN_ERR_WC_UNSUPPORTED_FORMAT
					TooOld = e.ErrorCode == 155021;

					// We are not in a working copy.
					switch (e.ErrorCode) {
					// SVN_ERR_WC_NOT_DIRECTORY
					case 155007:
					// SVN_ERR_WC_NOT_FILE
					case 155008:
					// SVN_ERR_WC_UNSUPPORTED_FORMAT
					case 155021:
					// SVN_ERR_SVN_ERR_WC_UPGRADE_REQUIRED
					case 155036:
						return String.Empty;
					}
					throw e;
				}
				return Marshal.PtrToStringAnsi (result);
			} finally {
				destroypool (scratch);
				TryEndOperation (localpool);

				if (TooOld)
					WorkingCopyFormatPrompt (false, null);

				if (Upgrading)
					WorkingCopyFormatPrompt (true, delegate {
						Upgrade (path);
					});
			}
		}

		public void Upgrade (FilePath path)
		{
			if (!Upgrading || path.IsNullOrEmpty)
				return;

			IntPtr localpool = IntPtr.Zero;
			bool tryParent = false;
			try {
				localpool = TryStartOperation (null);
				CheckError (svn.client_upgrade (path, ctx, localpool));
			} catch (Exception) {
				tryParent = true;
			} finally {
				TryEndOperation (localpool);
			}

			if (tryParent)
				Upgrade (path.ParentDirectory);
			else
				Upgrading = false;
		}

		public class StatusCollector {
			readonly ArrayList statuses;

			public LibSvnClient.svn_wc_status_func2_t Func {
				get; private set;
			}

			public StatusCollector (ArrayList statuses)
			{
				this.statuses = statuses;
				Func = CollectorFunc;
			}
			
			void CollectorFunc (IntPtr baton, IntPtr path, IntPtr statusPtr)
			{
				string pathstr = Marshal.PtrToStringAnsi (path);
				/*if (status.to_svn_wc_entry_t == IntPtr.Zero)
					return;
				 */
				var status = (LibSvnClient.svn_wc_status2_t) Marshal.PtrToStructure (statusPtr, typeof (LibSvnClient.svn_wc_status2_t));
				statuses.Add (new LibSvnClient.StatusEnt (status, pathstr));
			}
		}
		
		class LogCollector
		{
			static readonly DateTime Epoch = new DateTime (1970, 1, 1);
			
			readonly List<SvnRevision> logs;
			readonly SubversionRepository repo;
			readonly IntPtr ctx;

			public LibSvnClient.svn_log_message_receiver_t Func {
				get; private set;
			}

			public LogCollector (SubversionRepository repo, List<SvnRevision> logs, IntPtr ctx) {
				this.repo = repo;
				this.logs = logs;
				this.ctx = ctx;
				Func = CollectorFunc;
			}

			IntPtr CollectorFunc (IntPtr baton, IntPtr apr_hash_changed_paths, svn_revnum_t revision, IntPtr author, IntPtr date, IntPtr message, IntPtr pool)
			{
				// Taken from https://subversion.apache.org/docs/api/1.8/group__Log.html#ga43d8607236ca1bd5c2d9b41acfb62b7e
				// Don't hash it when it's null. 
				if (apr_hash_changed_paths == IntPtr.Zero)
					return IntPtr.Zero;

				long time;
				svn.time_from_cstring (out time, Marshal.PtrToStringAnsi (date), pool);
				string smessage = "";

				if (message != IntPtr.Zero)
					smessage = Marshal.PtrToStringAnsi (message);
				
				if (smessage != null)
					smessage = smessage.Trim ();
				
				List<RevisionPath> items = new List<RevisionPath>();
				IntPtr item = apr.hash_first (pool, apr_hash_changed_paths);
				LibSvnClient.svn_log_changed_path_t ch;
				string name;

				while (apr.hash_iterate(ref item, out ch, out name)) {
					RevisionAction ac;
					switch (ch.action) {
						case 'A': ac = RevisionAction.Add; break;
						case 'D': ac = RevisionAction.Delete; break;
						case 'R': ac = RevisionAction.Replace; break;
						default: ac = RevisionAction.Modify; break; // should be an 'M'
					}
					
					IntPtr result = IntPtr.Zero;

					SvnClient.CheckError (svn.client_root_url_from_path (ref result, repo.RootPath, ctx, pool));
					if (result == IntPtr.Zero) // Should never happen
						items.Add (new RevisionPath (name, ac, ""));
					else
						items.Add (new RevisionPath (Marshal.PtrToStringAnsi (result) + "/" + name, ac, ""));
				}
				
				SvnRevision ent = new SvnRevision (null, (int) revision, Epoch.AddTicks (time * 10), Marshal.PtrToStringAnsi (author), smessage, items.ToArray ());
				logs.Add (ent);
				
				return IntPtr.Zero;
			}
		}
		
		public class StreamCollector {
			readonly Stream buf;

			public LibSvnClient.svn_readwrite_fn_t Func {
				get; private set;
			}

			public StreamCollector (Stream buf) {
				this.buf = buf;
				Func = CollectorFunc;
			}
			
			IntPtr CollectorFunc (IntPtr baton, IntPtr data, ref size_t len)
			{
				unsafe {
					byte *bp = (byte *) data;
					int max = (int) len;
					for (int i = 0; i < max; i++) {
						buf.WriteByte (*bp);
						bp++;
					}
				}
				return IntPtr.Zero;
			}
		}
		
		/// <summary>
		/// Class for collecting annotations from libsvn_client
		/// </summary>
		private class AnnotationCollector
		{
			readonly Repository repo;
			readonly Annotation[] annotations;
			public LibSvnClient.svn_client_blame_receiver_t Func {
				get; private set;
			}

			/// <summary>
			/// A svn_client_blame_receiver_t implementation.
			/// </summary>
			IntPtr CollectorFunc (IntPtr baton, long line_no, svn_revnum_t revision, string author, string date, string line, IntPtr pool)
			{
				if (line_no < annotations.Length) {
					DateTime tdate;
					try {
						tdate = DateTime.Parse (date);
					} catch {
						tdate = DateTime.MinValue;
					}
					annotations[(int)line_no] = new Annotation (new SvnRevision(repo, (int)revision), author, tdate);
				}
				
				return IntPtr.Zero;
			}
			
			public AnnotationCollector (Annotation[] annotations, Repository repo)
			{
				this.repo = repo;
				this.annotations = annotations;
				Func = CollectorFunc;
			}
		}
		
	}
}
