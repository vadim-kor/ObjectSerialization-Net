﻿using System;
using System.Collections.Generic;
using ObjectSerialization.Performance.Serializers;
using ObjectSerialization.Performance.TestCases;

namespace ObjectSerialization.Performance
{
    internal class PerformanceMonitor
    {
        private readonly List<PerformanceResult> _results = new List<PerformanceResult>();
        private readonly ISerializerAdapter[] _serializers;

        public PerformanceMonitor(params ISerializerAdapter[] serializers)
        {
            _serializers = serializers;
        }

        public IEnumerable<PerformanceResult> GetResults()
        {
            return _results;
        }

        public void MeasureFor(TestCase testCase)
        {
            foreach (ISerializerAdapter serializer in _serializers)
                Store(testCase.Measure(serializer));
        }

        private void Store(PerformanceResult measure)
        {
            Console.WriteLine(measure);
            _results.Add(measure);
        }
    }
}