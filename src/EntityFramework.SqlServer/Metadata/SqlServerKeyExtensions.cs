﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.SqlServer.Metadata
{
    public class SqlServerKeyExtensions : ReadOnlySqlServerKeyExtensions
    {
        public SqlServerKeyExtensions([NotNull] Key key)
            : base(key)
        {
        }

        [CanBeNull]
        public new virtual string Name
        {
            get { return base.Name; }
            [param: CanBeNull]
            set
            {
                Check.NullButNotEmpty(value, nameof(value));

                ((Key)Key)[SqlServerNameAnnotation] = value;
            }
        }

        [CanBeNull]
        public new virtual bool? IsClustered
        {
            get { return base.IsClustered; }
            [param: CanBeNull]
            set
            {
                // TODO: Issue #777: Non-string annotations
                // TODO: Issue #700: Annotate associated index object instead
                ((Key)Key)[SqlServerClusteredAnnotation] = value == null ? null : value.ToString();
            }
        }
    }
}
