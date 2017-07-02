using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using CacheManager.Core.Internal;

namespace CacheManager.Benchmarks
{
    public class BackplaneMessageBenchmarkMultiple
    {
        private static byte[] _ownderBytes = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
        private static BackplaneMessage[] _multiple;
        private byte[] _multipleSerialized = BackplaneMessage.Serialize(_multiple);

        static BackplaneMessageBenchmarkMultiple()
        {
            var messages = new List<BackplaneMessage>();
            for (var i = 0; i < 10; i++)
            {
                messages.Add(BackplaneMessage.ForChanged(_ownderBytes, "somerandomkey" + i, CacheItemChangedEventAction.Update));
                messages.Add(BackplaneMessage.ForChanged(_ownderBytes, "somerandomkey" + i, "withregion", CacheItemChangedEventAction.Add));
            }
            for (var i = 0; i < 10; i++)
            {
                messages.Add(BackplaneMessage.ForClear(_ownderBytes));
            }
            for (var i = 0; i < 10; i++)
            {
                messages.Add(BackplaneMessage.ForClearRegion(_ownderBytes, "somerandomregion" + i));
            }
            for (var i = 0; i < 10; i++)
            {
                messages.Add(BackplaneMessage.ForRemoved(_ownderBytes, "somerandomkey" + i, "withregion"));
            }
            _multiple = messages.ToArray();
        }

        [Benchmark]
        public void SerializeMany()
        {
            var bytes = BackplaneMessage.Serialize(_multiple);
        }

        [Benchmark()]
        public void DeserializeMany()
        {
            var messages = BackplaneMessage.Deserialize(_multipleSerialized).ToArray();
        }
    }

    public class BackplaneMessageBenchmark
    {
        private static byte[] _ownderBytes = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());

        private static BackplaneMessage _dataSingleChange = BackplaneMessage.ForChanged(_ownderBytes, "somerandomkey", CacheItemChangedEventAction.Update);
        private byte[] _rawSingleChange = BackplaneMessage.Serialize(_dataSingleChange);

        private static BackplaneMessage _dataSingleChangeRegion = BackplaneMessage.ForChanged(_ownderBytes, "somerandomkey", "withregion", CacheItemChangedEventAction.Add);
        private byte[] _rawSingleChangeRegion = BackplaneMessage.Serialize(_dataSingleChangeRegion);

        private static BackplaneMessage _dataSingleClear = BackplaneMessage.ForClear(_ownderBytes);
        private byte[] _rawSingleClear = BackplaneMessage.Serialize(_dataSingleClear);

        private static BackplaneMessage _dataSingleClearRegion = BackplaneMessage.ForClearRegion(_ownderBytes, "somerandomregion");
        private byte[] _rawSingleClearRegion = BackplaneMessage.Serialize(_dataSingleClearRegion);

        private static BackplaneMessage _dataSingleRemove = BackplaneMessage.ForRemoved(_ownderBytes, "somerandomkey", "withregion");
        private byte[] _rawSingleRemove = BackplaneMessage.Serialize(_dataSingleRemove);

        [Benchmark(Baseline = true)]
        public void SerializeChange()
        {
            var fullMessage = BackplaneMessage.Serialize(_dataSingleChange);
        }

        [Benchmark()]
        public void DeserializeChange()
        {
            var msg = BackplaneMessage.Deserialize(_rawSingleChange).ToArray();
        }

        [Benchmark]
        public void SerializeChangeRegion()
        {
            var fullMessage = BackplaneMessage.Serialize(_dataSingleChangeRegion);
        }

        [Benchmark()]
        public void DeserializeChangeRegion()
        {
            var msg = BackplaneMessage.Deserialize(_rawSingleChangeRegion).ToArray();
        }

        [Benchmark]
        public void SerializeClear()
        {
            var fullMessage = BackplaneMessage.Serialize(_dataSingleClear);
        }

        [Benchmark()]
        public void DeserializeClear()
        {
            var msg = BackplaneMessage.Deserialize(_rawSingleClear).ToArray();
        }

        [Benchmark]
        public void SerializeClearRegion()
        {
            var fullMessage = BackplaneMessage.Serialize(_dataSingleClearRegion);
        }

        [Benchmark()]
        public void DeserializeClearRegion()
        {
            var msg = BackplaneMessage.Deserialize(_rawSingleClearRegion).ToArray();
        }

        [Benchmark]
        public void SerializeRemove()
        {
            var fullMessage = BackplaneMessage.Serialize(_dataSingleRemove);
        }

        [Benchmark()]
        public void DeserializeRemove()
        {
            var msg = BackplaneMessage.Deserialize(_rawSingleRemove).ToArray();
        }
    }
}