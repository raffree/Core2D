﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using Core2D.Interfaces;
using Xunit;

namespace Core2D.UnitTests
{
    public class QuadraticBezierSegmentTests
    {
        private readonly IFactory _factory = new Factory();

        [Fact]
        [Trait("Core2D.Path", "Segments")]
        public void GetPoints_Should_Return_All_Segment_Points()
        {
            var segment = _factory.CreateQuadraticBezierSegment(_factory.CreatePointShape(), _factory.CreatePointShape(), true, true);

            var target = segment.GetPoints();

            Assert.Equal(2, target.Count());

            Assert.Contains(segment.Point1, target);
            Assert.Contains(segment.Point2, target);
        }

        [Fact]
        [Trait("Core2D.Path", "Segments")]
        public void ToString_Should_Return_Path_Markup()
        {
            var target = _factory.CreateQuadraticBezierSegment(_factory.CreatePointShape(), _factory.CreatePointShape(), true, true);

            var actual = target.ToString();

            Assert.Equal("Q0,0 0,0", actual);
        }
    }
}
