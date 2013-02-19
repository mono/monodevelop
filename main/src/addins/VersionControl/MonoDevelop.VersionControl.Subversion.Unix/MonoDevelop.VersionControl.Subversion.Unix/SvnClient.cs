using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using System.Runtime.InteropServices;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Subversion.Gui;
using System.Text;

using svn_revnum_t = System.Int32;
using size_t = System.Int32;

namespace MonoDevelop.VersionControl.Subversion.Unix
{
	class SvnClient : SubversionVersionControl
	{
		internal static LibApr apr;
		static Lazy<bool> isInstalled;
		internal static LibSvnClient svn;

		internal static void CheckError (IntPtr error)
		{
			CheckError (error, null);
		}

		public static void CheckError (IntPtr error, int? allowedError)
		{
			string msg = null;
			while (error != IntPtr.Zero) {
				LibSvnClient.svn_error_t error_t = (LibSvnClient.svn_error_t) Marshal.PtrToStructure (error, typeof (LibSvnClient.svn_error_t));
				if (allowedError.HasValue && error_t.apr_err == allowedError.Value)
					return;

				if (msg != null)
					msg += "\n" + GetErrorMessage (error_t);
				else
					msg = GetErrorMessage (error_t);
				error = error_t.svn_error_t_child;

				if (msg == null)
					msg = GettextCatalog.GetString ("Unknown error");

				throw new SubversionException (msg);
			}
		}
		
		static string GetErrorMessage (LibSvnClient.svn_error_t error)
		{
			if (error.message != null)
				return error.message;
			else {
				byte[] buf = new byte [300];
				svn.strerror (error.apr_err, buf, buf.Length);
				return Encoding.UTF8.GetString (buf);
			}
		}

		internal static IntPtr newpool (IntPtr parent)
		{
			IntPtr p;
			apr.pool_create_ex (out p, parent, IntPtr.Zero, IntPtr.Zero);
			if (p == IntPtr.Zero)
				throw new InvalidOperationException ("Could not create an APR pool.");
			return p;
		}
		
		public static string NormalizePath (string pathOrUrl, IntPtr localpool)
		{
			if (pathOrUrl == null)
				return null;
			IntPtr res = svn.path_internal_style (pathOrUrl, localpool);
			return Marshal.PtrToStringAnsi (res);
		}

		static bool CheckInstalled ()
		{
			// libsvn_client may be linked to libapr-0 or libapr-1, and we need to bind the LibApr class
			// the the same library. The following code detects the required libapr version and loads it. 
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
		
		static int GetLoadAprLib (int oldVersion)
		{
			// Get the version of the loaded libapr
			string file = "/proc/" + System.Diagnostics.Process.GetCurrentProcess ().Id + "/maps";
			try {
				int newv = oldVersion;
				if (File.Exists (file)) {
					string txt = File.ReadAllText (file);
					if (txt.IndexOf ("libapr-0") != -1 && oldVersion != 0)
						newv = 0;
					if (txt.IndexOf ("libapr-1") != -1 && oldVersion != 1)
						newv = 1;
				}
				return newv;
			} catch {
				// Ignore
				return oldVersion;
			}
		}

		public override string Id {
			get {
				return "MonoDevelop.VersionControl.Subversion.SubversionVersionControl";
			}
		}

		public override bool IsInstalled {
			get { return isInstalled.Value; }
		}

		static SvnClient ()
		{
			isInstalled = new Lazy<bool> (CheckInstalled);
		}

		public override SubversionBackend CreateBackend ()
		{
			return new UnixSvnBackend ();
		}

		public override string GetPathUrl (FilePath path)
		{
			if (path == FilePath.Null)
				throw new ArgumentNullException();

			IntPtr ret = IntPtr.Zero;
			IntPtr localpool = newpool (IntPtr.Zero);
			try {
				string npath = NormalizePath (path, localpool);
				CheckError (svn.client_url_from_path (ref ret, npath, localpool));
			} finally {
				apr.pool_destroy (localpool);
			}

			if (ret == IntPtr.Zero)
				return null;

			return Marshal.PtrToStringAnsi (ret);
		}
	}

	class UnixSvnBackend : SubversionBackend
	{
		protected static LibApr apr {
			get { return SvnClient.apr; }
		}
		
		protected static LibSvnClient svn {
			get { return SvnClient.svn; }
		}

		static void CheckError (IntPtr error)
		{
			SvnClient.CheckError (error);
		}

		static void CheckError (IntPtr error, int? allowedError)
		{
			SvnClient.CheckError (error, allowedError);
		}

		static IntPtr newpool (IntPtr parent)
		{
			return SvnClient.newpool (parent);
		}

		bool disposed  = false;
		IntPtr auth_baton;
		IntPtr pool;
		IntPtr ctx;

		object sync = new object();
		bool inProgress = false;

		IProgressMonitor updatemonitor;
		ArrayList updateFileList;
		string commitmessage = null;

		ArrayList lockFileList;
		LibSvnClient.NotifyLockState requiredLockState;

		// retain this so the delegates aren't GC'ed
		LibSvnClient.svn_client_ctx_t ctxstruct;

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
			// Allocate the APR pool and the SVN client context.
			pool = newpool (IntPtr.Zero);

			// Make sure the config directory is properly created.
			// If the config directory and specifically the subdirectories
			// for the authentication providers don't exist, authentication
			// data won't be saved and no error is given.
			svn.config_ensure (null, pool);
			
			if (svn.client_create_context (out ctx, pool) != IntPtr.Zero)
				throw new InvalidOperationException ("Could not create a Subversion client context.");
			
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
			ctxstruct = new LibSvnClient.svn_client_ctx_t ();
			ctxstruct.NotifyFunc2 = new LibSvnClient.svn_wc_notify_func2_t (svn_wc_notify_func_t_impl);
			ctxstruct.LogMsgFunc = new LibSvnClient.svn_client_get_commit_log_t (svn_client_get_commit_log_impl);
			
			// Load user and system configuration
			svn.config_get_config (ref ctxstruct.config, null, pool);
			
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
			ctxstruct.auth_baton = auth_baton;
			
			Marshal.StructureToPtr (ctxstruct, ctx, false);
		}
		
		public void Dispose ()
		{
			if (!disposed) {
				if (apr != null)
					apr.pool_destroy(pool);
				disposed = true;
			}
		}
		
		~UnixSvnBackend ()
		{
			Dispose ();
		}
		
		static IntPtr GetCancelError ()
		{
			LibSvnClient.svn_error_t error = new LibSvnClient.svn_error_t ();
			error.apr_err = LibApr.APR_OS_START_USEERR;
			error.message = "Operation cancelled.";
			
			// Subversion destroys the error pool to dispose the error object,
			// so we need to use a non-shared pool.
			IntPtr localpool = newpool (IntPtr.Zero);
			error.pool = localpool;
			return apr.pcalloc (localpool, error);
		}

		static LibSvnClient.svn_auth_simple_prompt_func_t OnAuthSimplePromptCallback = OnAuthSimplePrompt;
		static IntPtr OnAuthSimplePrompt (ref IntPtr cred, IntPtr baton, string realm, string user_name, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_simple_t data = new LibSvnClient.svn_auth_cred_simple_t ();
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

		static LibSvnClient.svn_auth_username_prompt_func_t OnAuthUsernamePromptCallback = OnAuthUsernamePrompt;
		static IntPtr OnAuthUsernamePrompt (ref IntPtr cred, IntPtr baton, string realm, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_username_t data = new LibSvnClient.svn_auth_cred_username_t ();
			data.username = "";
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

		static LibSvnClient.svn_auth_ssl_server_trust_prompt_func_t OnAuthSslServerTrustPromptCallback = OnAuthSslServerTrustPrompt;
		static IntPtr OnAuthSslServerTrustPrompt (ref IntPtr cred, IntPtr baton, string realm, uint failures, ref LibSvnClient.svn_auth_ssl_server_cert_info_t cert_info, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_ssl_server_trust_t data = new LibSvnClient.svn_auth_cred_ssl_server_trust_t ();
			
			CertficateInfo ci = new CertficateInfo ();
			ci.AsciiCert = cert_info.ascii_cert;
			ci.Fingerprint = cert_info.fingerprint;
			ci.HostName = cert_info.hostname;
			ci.IssuerName = cert_info.issuer_dname;
			ci.ValidFrom = cert_info.valid_from;
			ci.ValidUntil = cert_info.valid_until;

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

		static LibSvnClient.svn_auth_ssl_client_cert_prompt_func_t OnAuthSslClientCertPromptCallback = OnAuthSslClientCertPrompt;
		static IntPtr OnAuthSslClientCertPrompt (ref IntPtr cred, IntPtr baton, string realm, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_ssl_client_cert_t data = new LibSvnClient.svn_auth_cred_ssl_client_cert_t ();
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

		static LibSvnClient.svn_auth_ssl_client_cert_pw_prompt_func_t OnAuthSslClientCertPwPromptCallback = OnAuthSslClientCertPwPrompt;
		static IntPtr OnAuthSslClientCertPwPrompt (ref IntPtr cred, IntPtr baton, string realm, bool may_save, IntPtr pool)
		{
			LibSvnClient.svn_auth_cred_ssl_client_cert_pw_t data;
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
			IntPtr ptr = svn.client_version ();
			LibSvnClient.svn_version_t ver = (LibSvnClient.svn_version_t) Marshal.PtrToStructure (ptr, typeof (LibSvnClient.svn_version_t));				
			return ver.major + "." + ver.minor + "." + ver.patch;
		}

		public override IEnumerable<DirectoryEntry> List (FilePath path, bool recurse, SvnRevision rev)
		{
			return ListUrl (path, recurse, rev);
		}
		
		static string NormalizePath (string pathOrUrl, IntPtr localpool)
		{
			return SvnClient.NormalizePath (pathOrUrl, localpool);
		}
		
		public override IEnumerable<DirectoryEntry> ListUrl (string pathorurl, bool recurse, SvnRevision rev)
		{
			if (pathorurl == null)
				throw new ArgumentNullException ();

			TryStartOperation ();
			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;
			IntPtr localpool = newpool (pool);
			List<DirectoryEntry> items = new List<DirectoryEntry> ();
			try {
				IntPtr hash;
				
				pathorurl = NormalizePath (pathorurl, localpool);
				
				CheckError (svn.client_ls (out hash, pathorurl, ref revision,
				                           recurse ? 1 : 0, ctx, localpool));
				
				IntPtr item = apr.hash_first (localpool, hash);
				while (item != IntPtr.Zero) {
					IntPtr nameptr, val;
					int namelen;
					apr.hash_this (item, out nameptr, out namelen, out val);
					
					string name = Marshal.PtrToStringAnsi (nameptr);
					LibSvnClient.svn_dirent_t ent = (LibSvnClient.svn_dirent_t) Marshal.PtrToStructure (val, typeof (LibSvnClient.svn_dirent_t));				
					item = apr.hash_next (item);
					
					DirectoryEntry dent = new DirectoryEntry ();
					dent.Name = name;
					dent.IsDirectory = ent.kind == LibSvnClient.svn_node_kind_t.Dir;
					dent.Size = ent.size;
					dent.HasProps = ent.has_props;
					dent.CreatedRevision = (int) ent.created_rev;
					dent.Time = new DateTime (1970, 1, 1).AddTicks(ent.time * 10);
					dent.LastAuthor = ent.last_author;
					items.Add (dent);
				}
			} finally {
				apr.pool_destroy (localpool);
				TryEndOperation ();
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

			TryStartOperation ();
			IntPtr localpool = newpool (pool);
			try {
				string pathorurl = NormalizePath (path, localpool);
				CheckError (svn.client_status (IntPtr.Zero, pathorurl, ref revision,
				                               collector.Func,
				                               IntPtr.Zero, descendDirs, 
				                               !changedItemsOnly, 
				                               remoteStatus,
				                               false,
				                               false,
				                               ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				TryEndOperation ();
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

			TryStartOperation ();

			List<SvnRevision> ret = new List<SvnRevision> ();
			IntPtr localpool = newpool (pool);
			IntPtr strptr = IntPtr.Zero;
			try {
				IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
				IntPtr first = apr.array_push (array);
				string pathorurl = NormalizePath (path, localpool);
				strptr = Marshal.StringToHGlobalAnsi (pathorurl);
				Marshal.WriteIntPtr (first, strptr);
				
				LogCollector collector = new LogCollector ((SubversionRepository)repo, ret);
				
				CheckError (svn.client_log (array, ref revisionStart, ref revisionEnd, 1, 0,
				                            collector.Func,
				                            IntPtr.Zero, ctx, localpool));
			} finally {
				if (strptr != IntPtr.Zero)
					Marshal.FreeHGlobal (strptr);
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
			
			return ret;
		}
		
		public override Annotation[] GetAnnotations (Repository repo, FilePath file, SvnRevision revStart, SvnRevision revEnd)
		{
			if (file == FilePath.Null)
				throw new ArgumentNullException ();
				
			LibSvnClient.Rev revisionStart = (LibSvnClient.Rev) revStart;
			LibSvnClient.Rev revisionEnd = (LibSvnClient.Rev) revEnd;
			
			int numAnnotations = File.ReadAllLines (((SubversionRepository)repo).GetPathToBaseText(file)).Length;
			Annotation[] annotations = new Annotation [numAnnotations];
			
			AnnotationCollector collector = new AnnotationCollector (annotations);
			
			IntPtr localpool = newpool (pool);

			TryStartOperation ();
			try {
				string path = NormalizePath (file.FullPath, localpool);
				CheckError (svn.client_blame (path, ref revisionStart, ref revisionEnd, collector.Func, IntPtr.Zero, ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
			
			return annotations;
		}

		public override string GetTextAtRevision (string pathorurl, Revision revision)
		{
			MemoryStream memstream = new MemoryStream ();
			Cat (pathorurl, (SvnRevision) revision, memstream);

			var buffer = memstream.GetBuffer ();
			try {
				if (IsBinary (buffer, memstream.Length))
					return null;
				return System.Text.Encoding.UTF8.GetString (buffer, 0, (int) memstream.Length);
			} catch {
			}
			
			return System.Text.Encoding.ASCII.GetString (buffer, 0, (int) memstream.Length);
		}
		
		public void Cat (string pathorurl, SvnRevision rev, Stream stream)
		{
			if (pathorurl == null || stream == null)
				throw new ArgumentNullException ();
		
			TryStartOperation ();
			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;
			
			IntPtr localpool = newpool (pool);
			try {
				pathorurl = NormalizePath (pathorurl, localpool);
				StreamCollector collector = new StreamCollector (stream);
				IntPtr svnstream = svn.stream_create (IntPtr.Zero, localpool);
				svn.stream_set_write (svnstream, collector.Func);
				LibSvnClient.Rev peg_revision = LibSvnClient.Rev.Blank;
				CheckError (svn.client_cat2 (svnstream, pathorurl, ref peg_revision, ref revision, ctx, localpool), 195007);
			} finally {
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
		}

		public override void Update (FilePath path, bool recurse, IProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException();
			
			TryStartOperation ();
			
			updatemonitor = monitor;
			updateFileList = new ArrayList ();
			
			LibSvnClient.Rev rev = LibSvnClient.Rev.Head;
			IntPtr localpool = newpool (pool);
			try {
				string pathorurl = NormalizePath (path, localpool);
				CheckError (svn.client_update (IntPtr.Zero, pathorurl, ref rev, recurse, ctx, localpool));
			} finally {
				foreach (string file in updateFileList)
					FileService.NotifyFileChanged (file);
				updateFileList = null;
				
				apr.pool_destroy (localpool);
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override void Revert (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
			if (paths == null || monitor == null)
				throw new ArgumentNullException();
			
			TryStartOperation ();
			
			updatemonitor = monitor;
			IntPtr localpool = newpool (pool);
			
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
				foreach (string path in paths) {
					string pathorurl = NormalizePath (path, localpool);
					IntPtr item = apr.array_push (array);
					Marshal.WriteIntPtr (item, apr.pstrdup (localpool, pathorurl));
				}
			
				CheckError (svn.client_revert (array, recurse ? 1 : 0, ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override void Resolve (FilePath path, bool recurse, IProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException();
			
			TryStartOperation ();
			
			updatemonitor = monitor;
			IntPtr localpool = newpool (pool);
			
			try {
				string pathorurl = NormalizePath (path, localpool);
				CheckError (svn.client_resolved (pathorurl, recurse ? 1 : 0, ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override void Add (FilePath path, bool recurse, IProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException ();

			TryStartOperation ();

			nb = new notify_baton ();
			updatemonitor = monitor;
			IntPtr localpool = newpool (pool);
			try {
				string pathorurl = NormalizePath (path, localpool);
				CheckError (svn.client_add3 (pathorurl, (recurse ? 1 : 0), 1, 0, ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override void Checkout (string url, FilePath path, Revision revision, bool recurse, IProgressMonitor monitor)
		{
			if (url == null || monitor == null)
				throw new ArgumentNullException ();
			
			if (revision == null)
				revision = SvnRevision.Head;
			LibSvnClient.Rev rev = (LibSvnClient.Rev) revision;
			
			TryStartOperation ();
			nb = new notify_baton ();
			updatemonitor = monitor;
			IntPtr localpool = newpool (pool);
			try {
				// Using Uri here because the normalization method doesn't remove the redundant port number when using https
				url = NormalizePath (new Uri(url).ToString(), localpool);
				string npath = NormalizePath (path, localpool);
				CheckError (svn.client_checkout (IntPtr.Zero, url, npath, ref rev, recurse, ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override void Commit (FilePath[] paths, string message, IProgressMonitor monitor)
		{
			if (paths == null || message == null || monitor == null)
				throw new ArgumentNullException();
			
			TryStartOperation ();
			
			nb = new notify_baton ();
			updatemonitor = monitor;
			
			IntPtr localpool = newpool (pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
				foreach (string path in paths) {
					string npath = NormalizePath (path, localpool);
					IntPtr item = apr.array_push (array);
					Marshal.WriteIntPtr (item, apr.pstrdup (localpool, npath));
				}
				
				IntPtr commit_info = IntPtr.Zero;
				
				commitmessage = message;
				
				CheckError (svn.client_commit (ref commit_info, array, 0, ctx, localpool));
				unsafe {
					if (commit_info != IntPtr.Zero) {
						monitor.Log.WriteLine ();
						monitor.Log.WriteLine (GettextCatalog.GetString ("Revision: {0}", ((LibSvnClient.svn_client_commit_info_t *) commit_info.ToPointer())->revision));
					}
				}
			} finally {
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
		}

		public override void Mkdir (string[] paths, string message, IProgressMonitor monitor) 
		{
			if (paths == null || monitor == null)
				throw new ArgumentNullException ();
		
			TryStartOperation ();

			nb = new notify_baton ();
			updatemonitor = monitor;
			
			IntPtr localpool = newpool(pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make (localpool, paths.Length, IntPtr.Size);
				foreach (string path in paths) {
					string npath = NormalizePath (path, localpool);
					IntPtr item = apr.array_push (array);
					Marshal.WriteIntPtr (item, apr.pstrdup (localpool, npath));
				}
				
				commitmessage = message;

				IntPtr commit_info = IntPtr.Zero;
				CheckError (svn.client_mkdir2 (ref commit_info, array, ctx, localpool));
			} finally {	
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
		}

		public override void Delete (FilePath path, bool force, IProgressMonitor monitor)
		{
			if (path == FilePath.Null || monitor == null)
				throw new ArgumentNullException ();
			
			TryStartOperation ();
			
			nb = new notify_baton ();
			updatemonitor = monitor;
			
			IntPtr localpool = newpool (pool);
			try {
				// Put each item into an APR array.
				IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
				//foreach (string path in paths) {
					string npath = NormalizePath (path, localpool);
					IntPtr item = apr.array_push (array);
					Marshal.WriteIntPtr (item, apr.pstrdup (localpool, npath));
				//}
				IntPtr commit_info = IntPtr.Zero;
				CheckError (svn.client_delete (ref commit_info, array, (force ? 1 : 0), ctx, localpool));
			} finally {
				commitmessage = null;
				updatemonitor = null;
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
		}

		public override void Move (FilePath srcPath, FilePath destPath, SvnRevision rev, bool force, IProgressMonitor monitor)
		{
			if (srcPath == FilePath.Null || destPath == FilePath.Null || monitor == null)
				throw new ArgumentNullException ();
			
			LibSvnClient.Rev revision = (LibSvnClient.Rev) rev;
			
			TryStartOperation ();
			
			nb = new notify_baton ();
			updatemonitor = monitor;
			IntPtr commit_info = IntPtr.Zero;
			IntPtr localpool = newpool (pool);
			try {
				string nsrcPath = NormalizePath (srcPath, localpool);
				string ndestPath = NormalizePath (destPath, localpool);
				CheckError (svn.client_move (ref commit_info, nsrcPath, ref revision,
				                             ndestPath, (force ? 1 : 0), ctx, localpool));
			} finally {
				apr.pool_destroy (localpool);
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override void Lock (IProgressMonitor monitor, string comment, bool stealLock, params FilePath[] paths)
		{
			TryStartOperation ();
			
			IntPtr localpool = newpool (pool);
			IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
			updatemonitor = monitor;
			nb = new notify_baton ();

			try {
				foreach (string path in paths) {
					string npath = NormalizePath (path, localpool);
					IntPtr item = apr.array_push (array);
					Marshal.WriteIntPtr (item, apr.pstrdup (localpool, npath));
				}
				lockFileList = new ArrayList ();
				requiredLockState = LibSvnClient.NotifyLockState.Locked;
				
				CheckError (svn.client_lock (array, comment, stealLock ? 1 : 0, ctx, localpool));
				if (paths.Length != lockFileList.Count)
					throw new SubversionException ("Lock operation failed.");
			} finally {
				apr.pool_destroy (localpool);
				lockFileList = null;
				updatemonitor = null;
				TryEndOperation ();
			}
		}
		
		public override void Unlock (IProgressMonitor monitor, bool breakLock, params FilePath[] paths)
		{
			TryStartOperation ();
			
			IntPtr localpool = newpool (pool);
			IntPtr array = apr.array_make (localpool, 0, IntPtr.Size);
			updatemonitor = monitor;
			nb = new notify_baton ();
			
			try {
				foreach (string path in paths) {
					string npath = NormalizePath (path, localpool);
					IntPtr item = apr.array_push (array);
					Marshal.WriteIntPtr (item, apr.pstrdup (localpool, npath));
				}
				lockFileList = new ArrayList ();
				requiredLockState = LibSvnClient.NotifyLockState.Unlocked;
			
				CheckError (svn.client_unlock (array, breakLock ? 1 : 0, ctx, localpool));
				if (paths.Length != lockFileList.Count)
					throw new SubversionException ("Lock operation failed.");
			} finally {
				apr.pool_destroy (localpool);
				lockFileList = null;
				updatemonitor = null;
				TryEndOperation ();
			}
		}

		public override string GetUnifiedDiff (FilePath path1, SvnRevision rev1, FilePath path2, SvnRevision rev2, bool recursive)
		{
			TryStartOperation ();

			IntPtr localpool = newpool (pool);
			IntPtr outfile = IntPtr.Zero;
			IntPtr errfile = IntPtr.Zero;
			string fout = null;
			string ferr = null;
			
			LibSvnClient.Rev revision1 = (LibSvnClient.Rev) rev1;
			LibSvnClient.Rev revision2 = (LibSvnClient.Rev) rev2;
			
			try {
				IntPtr options = apr.array_make (localpool, 0, IntPtr.Size);
				
				fout = Path.GetTempFileName ();
				ferr = Path.GetTempFileName ();
				int res1 = apr.file_open (ref outfile, fout, LibApr.APR_WRITE | LibApr.APR_CREATE | LibApr.APR_TRUNCATE, LibApr.APR_OS_DEFAULT, localpool);
				int res2 = apr.file_open (ref errfile, ferr, LibApr.APR_WRITE | LibApr.APR_CREATE | LibApr.APR_TRUNCATE, LibApr.APR_OS_DEFAULT, localpool);
				
				if (res1 == 0 && res2 == 0) {
					string npath1 = NormalizePath (path1, localpool);
					string npath2 = NormalizePath (path2, localpool);
					CheckError (svn.client_diff (options, npath1, ref revision1, npath2, ref revision2, (recursive ? 1 : 0), 0, 1, outfile, errfile, ctx, localpool));
					return MonoDevelop.Projects.Text.TextFile.ReadFile (fout).Text;
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
					apr.pool_destroy (localpool);
					if (outfile != IntPtr.Zero)
						apr.file_close (outfile); 
					if (errfile != IntPtr.Zero)
						apr.file_close (errfile);
					if (ferr != null)
						FileService.DeleteFile (ferr);
					if (fout != null)
						FileService.DeleteFile (fout);

					TryEndOperation ();
				} catch {}
			}
		}

		public override void RevertToRevision (FilePath path, Revision revision, IProgressMonitor monitor)
		{
			Merge (path, LibSvnClient.Rev.Head, (LibSvnClient.Rev) revision);
		}

		public override void RevertRevision (FilePath path, Revision revision, IProgressMonitor monitor)
		{
			SvnRevision srev = (SvnRevision) revision;
			Merge (path, (LibSvnClient.Rev) srev, LibSvnClient.Rev.Number (srev.Rev - 1));
		}
		
		private void Merge (string path, LibSvnClient.Rev revision1, LibSvnClient.Rev revision2)
		{
			TryStartOperation ();

			IntPtr localpool = newpool (pool);
			try {
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
				apr.pool_destroy (localpool);
				TryEndOperation ();
			}
		}
		
		IntPtr svn_client_get_commit_log_impl (ref IntPtr log_msg, ref IntPtr tmp_file,
		                                       IntPtr commit_items, IntPtr baton, IntPtr pool)
		{
			log_msg = apr.pstrdup (pool, commitmessage);
			tmp_file = IntPtr.Zero;
			return IntPtr.Zero;
		}

		void TryStartOperation ()
		{
			lock (sync) {
				Console.WriteLine ("*************************************");
				Console.WriteLine (Environment.StackTrace);
				Console.WriteLine ("*************************************");
				if (inProgress)
					throw new SubversionException ("Another Subversion operation is already in progress.");
				inProgress = true;
			}
		}

		void TryEndOperation ()
		{
			lock (sync) {
				if (!inProgress)
					throw new SubversionException ("No Subversion operation is in progress.");
				inProgress = false;
			}
		}

		private VersionInfo CreateNode (LibSvnClient.StatusEnt ent, Repository repo) 
		{
			VersionStatus rs = VersionStatus.Unversioned;
			Revision rr = null;
			
			if (ent.RemoteTextStatus != LibSvnClient.svn_wc_status_kind.EMPTY) {
				rs = ConvertStatus (LibSvnClient.NodeSchedule.Normal, ent.RemoteTextStatus);
				rr = new SvnRevision (repo, ent.LastCommitRevision, ent.LastCommitDate,
				                      ent.LastCommitAuthor, "(unavailable)", null);
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
		
		private VersionStatus ConvertStatus (LibSvnClient.NodeSchedule schedule, LibSvnClient.svn_wc_status_kind status) {
			switch (schedule) {
				case LibSvnClient.NodeSchedule.Add: return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case LibSvnClient.NodeSchedule.Delete: return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case LibSvnClient.NodeSchedule.Replace: return VersionStatus.Versioned | VersionStatus.ScheduledReplace;
			}
			
			switch (status) {
			case LibSvnClient.svn_wc_status_kind.None: return VersionStatus.Versioned;
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
			return !(mimeType.StartsWith ("text/") || 
			         mimeType == "image/x-xbitmap" || 
			         mimeType == "image/x-xpixmap");
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
///*				actiondesc = data.content_state == LibSvnClient.NotifyState.Conflicted ? "C" : "E";
//				if (data.prop_state == LibSvnClient.NotifyState.Conflicted) {
//					actiondesc += "C";
//				} else if (data.prop_state == LibSvnClient.NotifyState.Merged) {
//					actiondesc += "G";
//				}
//				actiondesc += " {0}";
//				actiondesc = string.Format (actiondesc, file); */
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
				System.Console.WriteLine("untranslated action:" + data.action);
				actiondesc = data.action.ToString () + " " + file;
				break;
				/*
				 StatusCompleted,
				 StatusExternal,
				 BlameRevision*/
			}
			
			if (updatemonitor != null && !string.IsNullOrEmpty (actiondesc)) {
				if (skipEol) {
					updatemonitor.Log.Write (actiondesc);
				} else {
					updatemonitor.Log.WriteLine (actiondesc);
				}
			}
			if (updateFileList != null && notifyChange && File.Exists (file))
				updateFileList.Add (file);
			
			if (lockFileList != null && data.lock_state == requiredLockState)
				lockFileList.Add (file);
		}
		
		public class StatusCollector {
			ArrayList statuses;

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
		
		private class LogCollector
		{
			static readonly DateTime Epoch = new DateTime (1970, 1, 1);
			
			List<SvnRevision> logs;
			SubversionRepository repo;

			public LibSvnClient.svn_log_message_receiver_t Func {
				get; private set;
			}

			public LogCollector (SubversionRepository repo, List<SvnRevision> logs) {
				this.repo = repo;
				this.logs = logs;
				Func = CollectorFunc;
			}

			IntPtr CollectorFunc (IntPtr baton, IntPtr apr_hash_changed_paths, svn_revnum_t revision, IntPtr author, IntPtr date, IntPtr message, IntPtr pool)
			{
				long time;
				svn.time_from_cstring (out time, Marshal.PtrToStringAnsi (date), pool);
				string smessage = "";
				
				if (message != IntPtr.Zero)
					smessage = Marshal.PtrToStringAnsi (message);
				
				if (smessage != null)
					smessage = smessage.Trim ();
				
				List<RevisionPath> items = new List<RevisionPath>();
				
				IntPtr item = apr.hash_first (pool, apr_hash_changed_paths);
				while (item != IntPtr.Zero) {
					IntPtr nameptr, val;
					int namelen;
					apr.hash_this (item, out nameptr, out namelen, out val);
					
					string name = Marshal.PtrToStringAnsi (nameptr);
					LibSvnClient.svn_log_changed_path_t ch = (LibSvnClient.svn_log_changed_path_t) Marshal.PtrToStructure (val, typeof (LibSvnClient.svn_log_changed_path_t));
					item = apr.hash_next (item);
					
					RevisionAction ac;
					switch (ch.action) {
						case 'A': ac = RevisionAction.Add; break;
						case 'D': ac = RevisionAction.Delete; break;
						case 'R': ac = RevisionAction.Replace; break;
						default: ac = RevisionAction.Modify; break; // should be an 'M'
					}
					
					IntPtr result = IntPtr.Zero;
					SvnClient.CheckError (svn.client_root_url_from_path (ref result, repo.RootPath, IntPtr.Zero, pool));
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
			Stream buf;

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
			Annotation[] annotations;
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
					annotations[(int)line_no] = new Annotation (revision.ToString (), author, tdate);
				}
				
				return IntPtr.Zero;
			}
			
			public AnnotationCollector (Annotation[] annotations)
			{
				this.annotations = annotations;
				Func = CollectorFunc;
			}
		}
		
	}
}
