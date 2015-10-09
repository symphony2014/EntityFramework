// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Storage
{
    public abstract class RelationalTypeMapper : IRelationalTypeMapper
    {
        private readonly ThreadSafeDictionaryCache<int, RelationalTypeMapping> _boundedStringMappings
            = new ThreadSafeDictionaryCache<int, RelationalTypeMapping>();

        private readonly ThreadSafeDictionaryCache<int, RelationalTypeMapping> _boundedBinaryMappings
            = new ThreadSafeDictionaryCache<int, RelationalTypeMapping>();

        protected abstract IReadOnlyDictionary<Type, RelationalTypeMapping> SimpleMappings { get; }

        protected abstract IReadOnlyDictionary<string, RelationalTypeMapping> SimpleNameMappings { get; }

        // Not using IRelationalAnnotationProvider here because type mappers are Singletons
        protected abstract string GetColumnType([NotNull] IProperty property);

        public virtual RelationalTypeMapping GetMapping(IProperty property)
        {
            Check.NotNull(property, nameof(property));

            RelationalTypeMapping mapping = null;

            var typeName = GetColumnType(property);
            if (typeName != null)
            {
                var paren = typeName.IndexOf("(", StringComparison.Ordinal);
                SimpleNameMappings.TryGetValue((paren >= 0 ? typeName.Substring(0, paren) : typeName).ToLowerInvariant(), out mapping);
            }

            return mapping
                   ?? (SimpleMappings.TryGetValue(property.ClrType.UnwrapEnumType().UnwrapNullableType(), out mapping)
                       ? mapping
                       : GetCustomMapping(property));
        }

        public virtual RelationalTypeMapping GetMapping(string typeName)
        {
            Check.NotEmpty(typeName, nameof(typeName));

            RelationalTypeMapping mapping;
            if (SimpleNameMappings.TryGetValue(typeName, out mapping))
            {
                return mapping;
            }

            throw new NotSupportedException(RelationalStrings.UnsupportedType(typeName));
        }

        public virtual RelationalTypeMapping GetMapping(Type clrType)
        {
            Check.NotNull(clrType, nameof(clrType));

            RelationalTypeMapping mapping;
            if (SimpleMappings.TryGetValue(clrType.UnwrapEnumType(), out mapping))
            {
                return mapping;
            }

            throw new NotSupportedException(RelationalStrings.UnsupportedType(clrType.Name));
        }

        protected virtual RelationalTypeMapping GetCustomMapping([NotNull] IProperty property)
        {
            Check.NotNull(property, nameof(property));

            throw new NotSupportedException(RelationalStrings.UnsupportedType(property.ClrType.Name));
        }

        protected virtual RelationalTypeMapping GetStringMapping(
            [NotNull] IProperty property,
            int maxBoundedLength,
            [NotNull] Func<int, RelationalTypeMapping> boundedMapping,
            [NotNull] RelationalTypeMapping unboundedMapping,
            [NotNull] RelationalTypeMapping defaultMapping,
            [CanBeNull] RelationalTypeMapping keyMapping = null)
        {
            Check.NotNull(property, nameof(property));
            Check.NotNull(defaultMapping, nameof(defaultMapping));

            var maxLength = property.GetMaxLength();

            return maxLength.HasValue
                ? maxLength <= maxBoundedLength
                    ? _boundedStringMappings.GetOrAdd(maxLength.Value, boundedMapping)
                    : unboundedMapping
                : (keyMapping != null
                   && (property.IsKey() || property.FindContainingEntityTypes().Any(property.IsForeignKey))
                    ? keyMapping
                    : defaultMapping);
        }

        protected virtual RelationalTypeMapping GetByteArrayMapping(
            [NotNull] IProperty property,
            int maxBoundedLength,
            [NotNull] Func<int, RelationalTypeMapping> boundedMapping,
            [NotNull] RelationalTypeMapping unboundedMapping,
            [NotNull] RelationalTypeMapping defaultMapping,
            [CanBeNull] RelationalTypeMapping keyMapping = null,
            [CanBeNull] RelationalTypeMapping rowVersionMapping = null)
        {
            Check.NotNull(property, nameof(property));
            Check.NotNull(defaultMapping, nameof(defaultMapping));

            if (property.IsConcurrencyToken
                && property.ValueGenerated == ValueGenerated.OnAddOrUpdate
                && rowVersionMapping != null)
            {
                return rowVersionMapping;
            }

            var maxLength = property.GetMaxLength();

            return maxLength.HasValue
                ? maxLength <= maxBoundedLength
                    ? _boundedBinaryMappings.GetOrAdd(maxLength.Value, boundedMapping)
                    : unboundedMapping
                : (keyMapping != null
                   && (property.IsKey() || property.FindContainingEntityTypes().Any(property.IsForeignKey))
                    ? keyMapping
                    : defaultMapping);
        }
    }
}
