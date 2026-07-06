// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using ColorCode.Common;

namespace ColorCode.Parsing
{
    public class Scope
    {
        public Scope(string name,
                     int index,
                     int length)
        {
            Guard.ArgNotNullAndNotEmpty(name, "name");

            Name = name;
            Index = index;
            Length = length;
        }

        // Children is allocated lazily on the first AddChild: most scopes are leaves that never gain a child, so
        // the common case avoids a per-Scope empty List allocation. Reads return a shared empty list.
        public IList<Scope> Children
        {
            get => _children ?? (IList<Scope>)Array.Empty<Scope>();
            set => _children = value as List<Scope> ?? (value == null ? null : new List<Scope>(value));
        }
        public int Index { get; set; }
        public int Length { get; set; }
        public Scope Parent { get; set; }
        public string Name { get; set; }

        public void AddChild(Scope childScope)
        {
            if (childScope.Parent != null)
                throw new InvalidOperationException("The child scope already has a parent.");

            childScope.Parent = this;

            (_children ??= new List<Scope>()).Add(childScope);
        }

        private List<Scope> _children;
    }
}