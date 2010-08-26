/*
 * Copyright (C) 2008, Google Inc
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSharp.Core.Transport;
using GitSharp.Core;

namespace GitSharp.Commands
{
    public class ConfigCommand : AbstractCommand
    {

        public ConfigCommand() {
        }

    	#region Arguments
    	public string Arg1 { get; set; }
    	
    	public string Arg2 { get; set; }

    	public string Arg3 { get; set; }
    	#endregion
    	
    	#region Properties
    	public bool Bool { get; set; } 
 		
 		public bool Int { get; set; }

 		public bool BoolOrInt { get; set; }
 		
 		public bool Null { get; set; }
 		
  		public bool Global { get; set; }
 		
 		public bool System { get; set; }
 		
 		public string File { get; set; }
		#endregion
		
 		#region SubCommands
        /// <summary>
        /// Not implemented.
        /// 
        /// For multi-line options only.
        /// </summary>
        public bool Add { get; set; }
 		
        public bool ReplaceAll { get; set; }
        
 		public bool Get { get; set; }
 		
 		public bool GetAll { get; set; }
 		
 		public bool GetRegExp { get; set; }
 		
 		public bool RemoveSection { get; set; }
 		
 		public bool RenameSection { get; set; }
 		
 		public bool UnSet { get; set; }
 		
 		public bool UnSetAll { get; set; }
 		
 		public bool List { get; set; }
  		
 		public bool GetColorBool { get; set; }
 		
 		public bool GetColor { get; set; }
 		
 		public bool Edit { get; set; }
 		
 		#endregion
 		
        public override void Execute()
        {
        	if (Add)
        		doAdd(Arg1, Arg2);
        	else if (ReplaceAll)
        		doReplaceAll(Arg1, Arg2, Arg3);
        	else if (Get)
        		doGet(Arg1, Arg2);
        	else if (GetAll)
        		doGetAll(Arg1, Arg2);
        	else if (GetRegExp)
        		doGetRegExp(Arg1, Arg2);
 			else if (RemoveSection)
 				doRemoveSection(Arg1);
 			else if (RenameSection)
        		doRenameSection(Arg1, Arg2);
	 		else if (UnSet)
        		doUnSet(Arg1, Arg2);
 			else if (UnSetAll)
        		doUnSetAll(Arg1, Arg2);
 			else if (List)
        		doList();
 			else if (GetColorBool)
        		doGetColorBool(Arg1, Arg2);
 			else if (GetColor)
        		doGetColor(Arg1, Arg2);
	 		else if (Edit)
        		doEdit();
			else
			{
				doDefault(Arg1, Arg2, Arg3);
			}
        }

        #region Methods
        
        private void doAdd(string name, string value)
        {
        	throw new NotImplementedException();
        }
        
       	private void doReplaceAll(string name, string value, string regexValue)
        {
        	throw new NotImplementedException();
        }
        
       	private void doGet(string name, string regexValue)
        {
        	throw new NotImplementedException();
        }
        
       	private void doGetAll(string name, string regexValue)
        {
        	throw new NotImplementedException();
        }
        
       	private void doGetRegExp(string nameRegex, string valueRegex)
        {
        	throw new NotImplementedException();
        }
        
		private void doRemoveSection(string name)
        {
        	throw new NotImplementedException();
        }
        
       	private void doRenameSection(string oldName, string newName)
        {
        	throw new NotImplementedException();
        }
        
       	private void doUnSet(string name, string valueRegex)
        {
        	throw new NotImplementedException();
        }
        
       	private void doUnSetAll(string name, string valueRegex)
        {
        	throw new NotImplementedException();
        }

        /// <summary>
        /// Displays list of all the variables set in the config file
        /// </summary>
       	private void doList()
        {
            GitSharp.Config cfg = new GitSharp.Config(Repository);
            foreach (KeyValuePair<string, string> pair in cfg)
            {
                OutputStream.WriteLine(pair.Key + "=" + pair.Value);
            }
            OutputStream.Flush();
        }
        
		private void doGetColorBool(string color, string ouputToTerminal)
        {
        	throw new NotImplementedException();
        }
        
       	private void doGetColor(string color, string defaultColor)
        {
        	throw new NotImplementedException();
        }
        
       	private void doEdit()
        {
        	throw new NotImplementedException();
        }
        
		private void doDefault(string name, string value, string valueRegex)
        {
        	throw new NotImplementedException();
        }
        
        #endregion
    }
}