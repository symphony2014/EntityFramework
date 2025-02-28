// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Update.Internal
{
    public class SqlServerModificationCommandBatchFactory : IModificationCommandBatchFactory
    {
        private readonly IRelationalCommandBuilderFactory _commandBuilderFactory;
        private readonly ISqlGenerator _sqlGenerator;
        private readonly ISqlServerUpdateSqlGenerator _updateSqlGenerator;
        private readonly IRelationalValueBufferFactoryFactory _valueBufferFactoryFactory;
        private readonly IDbContextOptions _options;
        private readonly ISensitiveDataLogger<SqlServerModificationCommandBatchFactory> _logger;
#pragma warning disable 0618
        private readonly TelemetrySource _telemetrySource;

        public SqlServerModificationCommandBatchFactory(
            [NotNull] IRelationalCommandBuilderFactory commandBuilderFactory,
            [NotNull] ISqlGenerator sqlGenerator,
            [NotNull] ISqlServerUpdateSqlGenerator updateSqlGenerator,
            [NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
            [NotNull] IDbContextOptions options,
            [NotNull] ISensitiveDataLogger<SqlServerModificationCommandBatchFactory> logger,
            [NotNull] TelemetrySource telemetrySource)
        {
            Check.NotNull(commandBuilderFactory, nameof(commandBuilderFactory));
            Check.NotNull(sqlGenerator, nameof(sqlGenerator));
            Check.NotNull(updateSqlGenerator, nameof(updateSqlGenerator));
            Check.NotNull(valueBufferFactoryFactory, nameof(valueBufferFactoryFactory));
            Check.NotNull(options, nameof(options));
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(telemetrySource, nameof(telemetrySource));

            _commandBuilderFactory = commandBuilderFactory;
            _sqlGenerator = sqlGenerator;
            _updateSqlGenerator = updateSqlGenerator;
            _valueBufferFactoryFactory = valueBufferFactoryFactory;
            _options = options;
            _logger = logger;
            _telemetrySource = telemetrySource;
        }
#pragma warning restore 0618

        public virtual ModificationCommandBatch Create()
        {
            var optionsExtension = _options.Extensions.OfType<SqlServerOptionsExtension>().FirstOrDefault();

            return new SqlServerModificationCommandBatch(
                _commandBuilderFactory,
                _sqlGenerator,
                _updateSqlGenerator,
                _valueBufferFactoryFactory,
                _logger,
                _telemetrySource,
                optionsExtension?.MaxBatchSize);
        }
    }
}
