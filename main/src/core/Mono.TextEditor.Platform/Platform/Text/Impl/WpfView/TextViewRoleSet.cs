// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    internal class TextViewRoleSet : ITextViewRoleSet
    {
        private List<string> roles;

        public TextViewRoleSet(IEnumerable<string> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException("roles");
            }
            this.roles = new List<String>();
            foreach (string role in roles)
            {
                if (role == null)
                {
                    throw new ArgumentNullException("roles");
                }
                else
                {
                    this.roles.Add(role.ToUpperInvariant());
                }
            }
        }

        public bool Contains(string textViewRole)
        {
            if (textViewRole == null)
            {
                throw new ArgumentNullException("textViewRole");
            }
            string upperTextViewRole = textViewRole.ToUpperInvariant();
            foreach (string role in this.roles)
            {
                if (role == upperTextViewRole)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsAny(IEnumerable<string> textViewRoles)
        {
            if (textViewRoles == null)
            {
                throw new ArgumentNullException("textViewRoles");
            }
            foreach (string textViewRole in textViewRoles)
            {
                if (textViewRole != null)
                {
                    string upperTextViewRole = textViewRole.ToUpperInvariant();
                    foreach (string role in this.roles)
                    {
                        if (role == upperTextViewRole)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool ContainsAll(IEnumerable<string> textViewRoles)
        {
            if (textViewRoles == null)
            {
                throw new ArgumentNullException("textViewRoles");
            }
            foreach (string textViewRole in textViewRoles)
            {
                if (textViewRole != null)
                {
                    bool found = false;
                    string upperTextViewRole = textViewRole.ToUpperInvariant();
                    foreach (string role in this.roles)
                    {
                        if (role == upperTextViewRole)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public ITextViewRoleSet UnionWith(ITextViewRoleSet roleSet)
        {
            if (roleSet == null)
            {
                throw new ArgumentNullException("roleSet");
            }
            var resultRoles = new HashSet<string>(this.roles);
            foreach (string role in roleSet)
            {
                if (!resultRoles.Contains(role))
                {
                    resultRoles.Add(role);
                }
            }
            return new TextViewRoleSet(resultRoles);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            foreach (string role in this.roles)
            {
                yield return role;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (string role in this.roles)
            {
                yield return role;
            }
        }

        public override string ToString()
        {
            // NOTE: Don't change this! There is code in VsCodeWindowAdapter that
            // relies on the format returned by this method.
            return string.Join(",", this.roles);
        }
    }

}