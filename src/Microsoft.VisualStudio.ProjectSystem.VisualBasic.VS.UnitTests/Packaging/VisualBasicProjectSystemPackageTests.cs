﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.Packaging
{
    [ProjectSystemTrait]
    public class VisualBasicProjectSystemPackageTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new VisualBasicProjectSystemPackage();
        }
    }
}
