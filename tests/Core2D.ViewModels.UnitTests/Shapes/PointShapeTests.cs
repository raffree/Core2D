﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Core2D.Interfaces;
using Xunit;

namespace Core2D.Shapes.UnitTests
{
    public class PointShapeTests
    {
        private readonly IFactory _factory = new Factory();

        [Fact]
        [Trait("Core2D.Shapes", "Shapes")]
        public void Inherits_From_BaseShape()
        {
            var target = _factory.CreatePointShape();
            Assert.True(target is BaseShape);
        }
    }
}
