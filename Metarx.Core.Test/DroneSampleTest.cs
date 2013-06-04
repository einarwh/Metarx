using System;
using System.Linq;
using System.Reactive.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class DroneSampleTest
    {
        [TestMethod]
        public void Works()
        {
            var values = new[]
                {
                    new Tuple<string, string>("faces", "[]"),
                    new Tuple<string, string>("faces", "[{ \"foo\": \"fools\", \"confidence\": -1.503985\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 0.0\"}]"),
                    new Tuple<string, string>("faces", "[{ \"foo\": \"fools\", \"confidence\": 0.937\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 0.2\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 0.4\"}]"),
                    new Tuple<string, string>("faces", "[{ \"foo\": \"fools\", \"confidence\": 1.01\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 0.6\"}]"),
                    new Tuple<string, string>("faces", "[{ \"foo\": \"fools\", \"confidence\": 1.20\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 0.8\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 1.0\"}]"),
                    new Tuple<string, string>("faces", "[{ \"foo\": \"fools\", \"confidence\": 1.25\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 1.2\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 1.4\"}]"),
                    new Tuple<string, string>("faces", "[{ \"foo\": \"fools\", \"confidence\": 1.30\"}]"),
                    new Tuple<string, string>("navdata", "[{ \"foo\": \"fools\", \"altitude\": 1.6\"}]"),
                };

            var drone = new DroneSample();
            var results = drone.Execute(values.ToObservable()).ToEnumerable();
            var list = results.ToList();
            Assert.IsTrue(list.Any());
        }
    }
}
