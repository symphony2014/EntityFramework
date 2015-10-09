// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Storage
{
    public class RelationalTypeMapping
    {
        public RelationalTypeMapping([NotNull] string defaultTypeName, [CanBeNull] Type clrType, DbType? storeType = null)
        {
            Check.NotEmpty(defaultTypeName, nameof(defaultTypeName));

            DefaultTypeName = defaultTypeName;
            StoreType = storeType;
            ClrType = clrType;
        }

        public virtual string DefaultTypeName { get; }

        public virtual DbType? StoreType { get; }

        [CanBeNull]
        public Type ClrType { get; }

        public virtual DbParameter CreateParameter(
            [NotNull] DbCommand command,
            [NotNull] string name,
            [CanBeNull] object value,
            bool? nullable = null)
        {
            Check.NotNull(command, nameof(command));

            var parameter = command.CreateParameter();
            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;

            if (nullable.HasValue)
            {
                parameter.IsNullable = nullable.Value;
            }

            if (StoreType.HasValue)
            {
                parameter.DbType = StoreType.Value;
            }

            ConfigureParameter(parameter);

            return parameter;
        }

        protected virtual void ConfigureParameter([NotNull] DbParameter parameter)
        {
        }
    }
}
