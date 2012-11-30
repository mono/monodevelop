//
// LibSvnClient0.cs
//
// Author:
//       Jeffrey Stedfast <jeff@xamarin.com>
//       Alan McGovern <alan@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;

using MonoDevelop.Core;
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Subversion.Gui;

using svn_revnum_t = System.Int32;

namespace MonoDevelop.VersionControl.Subversion.Unix {
	
	public class LibSvnClient1 : LibSvnClient {
		private const string svnclientlib = "libsvn_client-1.so.1";
		
		public override IntPtr client_root_url_from_path (ref IntPtr url, string path_or_url, IntPtr ctx, IntPtr pool)
		{
			return svn_client_root_url_from_path (ref url, path_or_url, ctx, pool);
		}
		
		public override void config_ensure (string config_dir, IntPtr pool)
		{
			svn_config_ensure (config_dir, pool);
		}
		
		public override void config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool)
		{
			svn_config_get_config (ref cfg_hash, config_dir, pool);
		}
		
		public override void auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool)	
		{
			svn_auth_open (out auth_baton, providers, pool);
		}
		
		public override void auth_set_parameter (IntPtr auth_baton, string name, IntPtr value)
		{
			svn_auth_set_parameter (auth_baton, name, value);
		}
		
		public override IntPtr auth_get_parameter (IntPtr auth_baton, string name)
		{
			return svn_auth_get_parameter (auth_baton, name);
		}
		
		public override void client_get_simple_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_simple_provider (item, pool);
		}
		
		public override void client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_simple_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_username_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_username_provider (item, pool);
		}
		
		public override void client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_username_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_server_trust_file_provider (item, pool);
		}
		
		public override void client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_file_provider (item, pool);
		}
		
		public override void client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_pw_file_provider (item, pool);
		}
		
		public override void client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool)
		{
			svn_client_get_ssl_server_trust_prompt_provider (item, prompt_func, prompt_batton, pool);
		}
		
		public override void client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override void client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool)
		{
			svn_client_get_ssl_client_cert_pw_prompt_provider (item, prompt_func, prompt_batton, retry_limit, pool);
		}
		
		public override IntPtr client_version ()
		{
			return svn_client_version ();
		}
		
		public override IntPtr client_create_context (out IntPtr ctx, IntPtr pool)
		{
			return svn_client_create_context (out ctx, pool);
		}
		
		public override IntPtr client_ls (out IntPtr dirents, string path_or_url,
		                                  ref Rev revision, int recurse, IntPtr ctx,
		                                  IntPtr pool)
		{
			return svn_client_ls (out dirents, path_or_url, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_status (IntPtr result_rev, string path, ref Rev revision,
		                                      svn_wc_status_func2_t status_func, IntPtr status_baton,
		                                      bool recurse, bool get_all, bool update, bool no_ignore,
		                                      bool ignore_externals, IntPtr ctx, IntPtr pool)
		{
			return svn_client_status2 (result_rev, path, ref revision, status_func,
			                           status_baton, recurse, get_all, update,
			                           no_ignore, ignore_externals, ctx, pool);
		}
		
		public override IntPtr client_log (IntPtr apr_array_header_t_targets,
		                                   ref Rev rev_start, ref Rev rev_end,
		                                   int discover_changed_paths,
		                                   int strict_node_history,
		                                   svn_log_message_receiver_t receiver,
		                                   IntPtr receiver_baton,
		                                   IntPtr ctx, IntPtr pool)
		{
			return svn_client_log (apr_array_header_t_targets, ref rev_start, ref rev_end,
			                       discover_changed_paths, strict_node_history, receiver,
			                       receiver_baton,ctx, pool);
		}
		
		public override IntPtr time_from_cstring (out long aprtime, string time, IntPtr pool)
		{
			return svn_time_from_cstring (out aprtime, time, pool);
		}
		
		public override IntPtr client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool)
		{
			return svn_client_url_from_path (ref url, path_or_url, pool);
		}
		
		public override IntPtr client_cat2 (IntPtr stream, string path_or_url,
		                                    ref Rev peg_revision,
		                                    ref Rev revision,
		                                    IntPtr ctx, IntPtr pool)
		{
			return svn_client_cat2 (stream, path_or_url, ref peg_revision, ref revision, ctx, pool);
		}
		
		public override IntPtr stream_create (IntPtr baton, IntPtr pool)
		{
			return svn_stream_create (baton, pool);
		}
		
		//public override IntPtr stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		public override IntPtr stream_set_write (IntPtr stream, svn_readwrite_fn_t writer)
		{
			return svn_stream_set_write (stream, writer);
		}
		
		public override IntPtr client_update (IntPtr result_rev, string path, ref Rev revision,
		                                      bool recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_update (result_rev, path, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                      int force, IntPtr ctx, IntPtr pool)
		{
			return svn_client_delete (ref commit_info_p, apr_array_header_t_targets, force, ctx, pool);
		}
		
		public override IntPtr client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool)
		{
			return svn_client_add3 (path, recurse, force, no_ignore, ctx, pool);
		}
		
		public override IntPtr client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                      IntPtr apr_array_header_t_targets, int nonrecursive,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_commit (ref svn_client_commit_info_t_commit_info, apr_array_header_t_targets,
			                          nonrecursive, ctx, pool);
		}
		
		public override IntPtr client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                      IntPtr ctx, IntPtr pool)
		{
			return svn_client_revert (apr_array_header_t_targets, recursive, ctx, pool);
		}
		
		public override IntPtr client_resolved (string path, int recursive, IntPtr ctx, IntPtr pool)
		{
			return svn_client_resolved (path, recursive, ctx, pool);
		}
		
		public override IntPtr client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                    string destPath, int force, IntPtr ctx, IntPtr pool)
		{
			return svn_client_move (ref commit_info_p, srcPath, ref rev, destPath, force, ctx, pool);
		}
		
		public override IntPtr client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                        bool recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_checkout (result_rev, url, path, ref rev, recurse, ctx, pool);
		}
		
		public override IntPtr client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool)
		{
			return svn_client_mkdir2 (ref commit_info, apr_array_paths, ctx, pool);
		}
		
		public override IntPtr client_diff (IntPtr diff_options, string path1, ref Rev revision1,
		                                    string path2, ref Rev revision2, int recurse,
		                                    int ignore_ancestry, int no_diff_deleted,
		                                    IntPtr outfile, IntPtr errfile,
		                                    IntPtr ctx, IntPtr pool)
		{
			return svn_client_diff (diff_options, path1, ref revision1, path2, ref revision2, recurse, ignore_ancestry,
			                        no_diff_deleted, outfile, errfile, ctx, pool);
		}

		public override IntPtr client_merge_peg2 (
		                          string source,
		                          ref Rev revision1,
		                          ref Rev revision2,
		                          ref Rev peg_revision,
		                          string target_wcpath,
		                          bool recurse,
		                          bool ignore_ancestry,
		                          bool force,
		                          bool dry_run,
		                          IntPtr merge_options,
		                          IntPtr ctx,
		                          IntPtr pool)
		{		
			// svn_boolean_t == int
			return svn_client_merge_peg2 (source, ref revision1, ref revision2, ref peg_revision, target_wcpath, 
			                              recurse ? 1: 0, ignore_ancestry ? 1 : 0, force ? 1 : 0, dry_run ? 1 : 0,
			                              merge_options, ctx, pool);
		}
		
		public override IntPtr client_lock (IntPtr apr_array_header_t_targets, string comment, int steal_lock, IntPtr ctx, IntPtr pool)
		{
			return svn_client_lock (apr_array_header_t_targets, comment, steal_lock, ctx, pool);
		}
		
		public override IntPtr client_unlock (IntPtr apr_array_header_t_targets, int break_lock, IntPtr ctx, IntPtr pool)
		{
			return svn_client_unlock (apr_array_header_t_targets, break_lock, ctx, pool);
		}
		
		public override IntPtr client_prop_get (out IntPtr value, string name, string target, ref Rev revision, int recurse, IntPtr ctx, IntPtr pool)
		{
			return svn_client_prop_get (out value, name, target, ref revision, recurse, ctx, pool);
		}
		
		public override IntPtr client_blame (string path, ref Rev rev_start, ref Rev rev_end, svn_client_blame_receiver_t receiver, System.IntPtr baton, System.IntPtr ctx, System.IntPtr pool)
		{
			return svn_client_blame (path, ref rev_start, ref rev_end, receiver, baton, ctx, pool);
		}
		
		public override void strerror (int statcode, byte[] buf, int bufsize)
		{
			svn_strerror (statcode, buf, bufsize);
		}
		
		public override IntPtr path_internal_style (string path, IntPtr pool)
		{
			return svn_path_internal_style (path, pool);
		}
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_root_url_from_path (ref IntPtr url, string path_or_url, IntPtr ctx, IntPtr pool);
		[DllImport(svnclientlib)] static extern IntPtr svn_config_ensure (string config_dir, IntPtr pool);
		[DllImport(svnclientlib)] static extern IntPtr svn_config_get_config (ref IntPtr cfg_hash, string config_dir, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_auth_open (out IntPtr auth_baton, IntPtr providers, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_auth_set_parameter (IntPtr auth_baton, string name, IntPtr value);
		[DllImport(svnclientlib)] static extern IntPtr svn_auth_get_parameter (IntPtr auth_baton, string name);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_simple_prompt_provider (IntPtr item, svn_auth_simple_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_username_prompt_provider (IntPtr item, svn_auth_username_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_file_provider (IntPtr item, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_server_trust_prompt_provider (IntPtr item, svn_auth_ssl_server_trust_prompt_func_t prompt_func, IntPtr prompt_batton, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		[DllImport(svnclientlib)] static extern void svn_client_get_ssl_client_cert_pw_prompt_provider (IntPtr item, svn_auth_ssl_client_cert_pw_prompt_func_t prompt_func, IntPtr prompt_batton, int retry_limit, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_version();
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_create_context(out IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_ls (out IntPtr dirents, string path_or_url,
		                                                              ref Rev revision, int recurse, IntPtr ctx,
		                                                              IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_status2 (IntPtr svn_revnum_t, string path, ref Rev revision,
		                                                                   svn_wc_status_func2_t status_func, IntPtr status_baton,
		                                                                   [MarshalAs (UnmanagedType.Bool)] bool recurse,
		                                                                   [MarshalAs (UnmanagedType.Bool)] bool get_all,
		                                                                   [MarshalAs (UnmanagedType.Bool)] bool update,
		                                                                   [MarshalAs (UnmanagedType.Bool)] bool no_ignore,
		                                                                   [MarshalAs (UnmanagedType.Bool)] bool ignore_externals,
		                                                                   IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_log (IntPtr apr_array_header_t_targets,
		                                                               ref Rev rev_start, ref Rev rev_end,
		                                                               int discover_changed_paths,
		                                                               int strict_node_history,
		                                                               svn_log_message_receiver_t receiver,
		                                                               IntPtr receiver_baton,
		                                                               IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_time_from_cstring (out long aprtime, string time, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_url_from_path (ref IntPtr url, string path_or_url, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_cat2 (IntPtr stream, string path_or_url,
		                                                                ref Rev peg_revision,
		                                                                ref Rev revision,
		                                                                IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_create (IntPtr baton, IntPtr pool);
		
		//[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_read (IntPtr stream, svn_readwrite_fn_t reader);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_stream_set_write (IntPtr stream, svn_readwrite_fn_t writer);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_update (IntPtr result_rev, string path, ref Rev revision,
		                                                                  [MarshalAs (UnmanagedType.Bool)] bool recurse,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_delete (ref IntPtr commit_info_p, IntPtr apr_array_header_t_targets, 
		                                                                  int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_add3 (string path, int recurse, int force, int no_ignore, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_commit (ref IntPtr svn_client_commit_info_t_commit_info,
		                                                                  IntPtr apr_array_header_t_targets, int nonrecursive,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_revert (IntPtr apr_array_header_t_targets, int recursive,
		                                                                  IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_resolved (string path, int recursive, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_move (ref IntPtr commit_info_p, string srcPath, ref Rev rev,
		                                                                string destPath, int force, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_checkout (IntPtr result_rev, string url, string path, ref Rev rev, 
		                                                                    [MarshalAs (UnmanagedType.Bool)] bool recurse,
		                                                                    IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_mkdir2 (ref IntPtr commit_info, IntPtr apr_array_paths, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_diff (IntPtr diff_options, string path1,
		                                                                ref Rev revision1, string path2,
		                                                                ref Rev revision2, int recurse,
		                                                                int ignore_ancestry,
		                                                                int no_diff_deleted,
		                                                                IntPtr outfile,
		                                                                IntPtr errfile,
		                                                                IntPtr ctx,
		                                                                IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_merge_peg2 (string source,
		                                                                      ref Rev revision1,
		                                                                      ref Rev revision2,
		                                                                      ref Rev peg_revision,
		                                                                      string target_wcpath,
		                                                                      int recurse,
		                                                                      int ignore_ancestry,
		                                                                      int force,
		                                                                      int dry_run,
		                                                                      IntPtr merge_options,
		                                                                      IntPtr ctx,
		                                                                      IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_lock (IntPtr apr_array_header_t_targets, string comment, int steal_lock, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_unlock (IntPtr apr_array_header_t_targets, int break_lock, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_prop_get (out IntPtr value, string name, string target, ref Rev revision, int recurse, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_client_blame (string path, ref Rev rev_start, ref Rev rev_end, svn_client_blame_receiver_t receiver, IntPtr baton, IntPtr ctx, IntPtr pool);
		
		[DllImport(svnclientlib)] static extern void svn_strerror (int statcode, byte[] buf, int bufsize);
		
		[DllImport(svnclientlib)] static extern IntPtr svn_path_internal_style (string path, IntPtr pool);
	}
}