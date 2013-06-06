using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class RxWhereProcedureTest : NihilTestBase
    {
        [TestMethod]
        public void Works()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("quux", "steele")
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-select (lambda (s) (invoke-instance s 'ToUpper')) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (u) (= 'quux' (invoke-instance u 'get_Item1'))) stream))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values);
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public void Works1()
        {
            var values = new[]
                {
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 0.573957 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 4.384927 }]"),
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-where (lambda (d) (and (not (invoke-static 'System.Double' 'IsNaN' d)) (> d 0.2))) (rx-select (lambda (s) (jdv-parse 'altitudeMeters' (invoke-instance s 'Substring' 2 (- ((method get_Length) s) 4)))) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'navdata' ((method get_Item1) t))) stream)))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values);
            Assert.AreEqual(3, results.Count());
        }

        [TestMethod]
        public void Works2()
        {
            var values = new[]
                {
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 0.573957 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 4.384927 }]"),
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-where (lambda (d) (> d 0.2)) (rx-select (lambda (s) (jdv-parse 'altitudeMeters' (invoke-instance s 'Substring' 2 (- ((method get_Length) s) 4)))) (rx-select (lambda (t) (cdr t)) (rx-where (lambda (t) (= 'navdata' (car t))) stream)))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values);
            Assert.AreEqual(3, results.Count());
        }

    }
}
