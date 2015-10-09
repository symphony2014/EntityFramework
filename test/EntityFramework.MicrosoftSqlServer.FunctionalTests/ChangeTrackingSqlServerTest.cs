// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.FunctionalTests;

namespace Microsoft.Data.Entity.SqlServer.FunctionalTests
{
    public class ChangeTrackingSqlServerTest : ChangeTrackingTestBase<NorthwindQuerySqlServerFixture>
    {
        public ChangeTrackingSqlServerTest(NorthwindQuerySqlServerFixture fixture)
            : base(fixture)
        {
        }

        public override void Precendence_of_tracking_modifiers5()
        {
            base.Precendence_of_tracking_modifiers5();
        }
    }
}
