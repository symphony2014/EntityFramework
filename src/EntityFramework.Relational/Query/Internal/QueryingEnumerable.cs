// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Storage;

namespace Microsoft.Data.Entity.Query.Internal
{
    public class QueryingEnumerable : IEnumerable<ValueBuffer>
    {
        private readonly RelationalQueryContext _relationalQueryContext;
        private readonly CommandBuilder _commandBuilder;
        private readonly ISensitiveDataLogger _logger;
#pragma warning disable 0618
        private readonly TelemetrySource _telemetrySource;
        private readonly int? _queryIndex;

        public QueryingEnumerable(
            [NotNull] RelationalQueryContext relationalQueryContext,
            [NotNull] CommandBuilder commandBuilder,
            [NotNull] ISensitiveDataLogger logger,
            [NotNull] TelemetrySource telemetrySource,
            int? queryIndex)
        {
            _relationalQueryContext = relationalQueryContext;
            _commandBuilder = commandBuilder;
            _logger = logger;
            _telemetrySource = telemetrySource;
            _queryIndex = queryIndex;
        }
#pragma warning restore 0618

        public virtual IEnumerator<ValueBuffer> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<ValueBuffer>, IValueBufferCursor
        {
            private readonly QueryingEnumerable _queryingEnumerable;

            private DbDataReader _dataReader;
            private Queue<ValueBuffer> _buffer;

            private bool _disposed;

            public Enumerator(QueryingEnumerable queryingEnumerable)
            {
                _queryingEnumerable = queryingEnumerable;
            }

            public bool MoveNext()
            {
                if (_buffer == null)
                {
                    if (_dataReader == null)
                    {
                        _queryingEnumerable._relationalQueryContext.Connection.Open();

                        using (var command
                            = _queryingEnumerable._commandBuilder
                                .Build(
                                    _queryingEnumerable._relationalQueryContext.Connection,
                                    _queryingEnumerable._relationalQueryContext.ParameterValues))
                        {
                            _queryingEnumerable._relationalQueryContext
                                .RegisterValueBufferCursor(this, _queryingEnumerable._queryIndex);

                            _queryingEnumerable._logger.LogCommand(command);

                            WriteTelemetry(RelationalTelemetry.BeforeExecuteCommand, command);

                            try
                            {
                                _dataReader = command.ExecuteReader();
                            }
                            catch (Exception exception)
                            {
                                _queryingEnumerable._telemetrySource
                                    .WriteCommandError(
                                        command,
                                        RelationalTelemetry.ExecuteMethod.ExecuteReader,
                                        async: false,
                                        exception: exception);

                                throw;
                            }

                            WriteTelemetry(RelationalTelemetry.AfterExecuteCommand, command);

                            _queryingEnumerable._commandBuilder.NotifyReaderCreated(_dataReader);
                        }
                    }

                    var hasNext = _dataReader.Read();

                    Current
                        = hasNext
                            ? _queryingEnumerable._commandBuilder.ValueBufferFactory
                                .Create(_dataReader)
                            : default(ValueBuffer);

                    return hasNext;
                }

                if (_buffer.Count > 0)
                {
                    Current = _buffer.Dequeue();

                    return true;
                }

                return false;
            }

            private void WriteTelemetry(string name, DbCommand command)
                => _queryingEnumerable._telemetrySource
                    .WriteCommand(
                        name,
                        command,
                        RelationalTelemetry.ExecuteMethod.ExecuteReader,
                        async: false);

            public ValueBuffer Current { get; private set; }

            public void BufferAll()
            {
                if (_buffer == null)
                {
                    _buffer = new Queue<ValueBuffer>();

                    using (_dataReader)
                    {
                        while (_dataReader.Read())
                        {
                            _buffer.Enqueue(
                                _queryingEnumerable._commandBuilder.ValueBufferFactory
                                    .Create(_dataReader));
                        }
                    }

                    _dataReader = null;
                }
            }

            public Task BufferAllAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _dataReader?.Dispose();
                    _queryingEnumerable._relationalQueryContext.DeregisterValueBufferCursor(this);
                    _queryingEnumerable._relationalQueryContext.Connection?.Close();

                    _disposed = true;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
