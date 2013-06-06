using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class RxCombineLatestTest : NihilTestBase
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

            const string SingleQuoteProgram = "(define (execute stream) (rx-combine-latest (lambda (x y) (+ x ':' y)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (u) (= 'quux' (invoke-instance u 'get_Item1'))) stream)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (u) (= 'yak' (invoke-instance u 'get_Item1'))) stream))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("guy:shaving", results.First());
        }

        [TestMethod]
        public void WorksWithFacesAndHeights()
        {
            var values = new[]
                {
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": -0.239784 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 0.573957 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.42347 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.94728 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 2.5493784 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 1.8381293 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 3.5634756 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 4.384927 }]"),
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-combine-latest (lambda (c h) 'Found a face at the right height') (rx-where (lambda (c) (> c 1.0)) (rx-select (lambda (s) (jdv-parse 'confidence' (invoke-instance s 'Substring' 2 (- ((method get_Length) s) 4)))) (rx-where (lambda (t) (> ((method get_Length) t) 4)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'faces' ((method get_Item1) t))) stream))))) (rx-where (lambda (d) (and (not (invoke-static 'System.Double' 'IsNaN' d)) (> d 0.2))) (rx-select (lambda (s) (jdv-parse 'altitudeMeters' (invoke-instance s 'Substring' 2 (- ((method get_Length) s) 4)))) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'navdata' ((method get_Item1) t))) stream))))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(3, results.Count());
            Assert.AreEqual("Found a face at the right height", results.First());
        }

        [TestMethod]
        public void WorksWithFacesAndHeightsThreadingMacro()
        {
            var values = new[]
                {
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": -0.239784 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 0.573957 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.42347 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.94728 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 2.5493784 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 1.8381293 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 3.5634756 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 4.384927 }]"),
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-combine-latest (lambda (c h) 'Found a face at the right height') (->> stream (rx-where (lambda (t) (= 'faces' ((method get_Item1) t)))) (rx-select (method get_Item2)) (rx-where (lambda (t) (> ((method get_Length) t) 4))) (rx-select (lambda (s) (jdv-parse 'confidence' (invoke-instance s 'Substring' 2 (- ((method get_Length) s) 4))))) (rx-where (lambda (c) (> c 1.0)))) (->> stream (rx-where (lambda (t) (= 'navdata' ((method get_Item1) t)))) (rx-select (method get_Item2)) (rx-select (lambda (s) (jdv-parse 'altitudeMeters' (invoke-instance s 'Substring' 2 (- ((method get_Length) s) 4))))) (rx-where (lambda (d) (and (not (invoke-static 'System.Double' 'IsNaN' d)) (> d 0.2)))))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(3, results.Count());
            Assert.AreEqual("Found a face at the right height", results.First());
        }

        [TestMethod]
        public void WorksWithSimplifiedFacesAndHeightsThreadingMacro()
        {
            var values = new[]
                {
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": -0.239784 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 0.573957 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.42347 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.94728 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 2.5493784 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 1.8381293 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 3.5634756 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 4.384927 }]"),
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-combine-latest (lambda (c h) 'Found a face at the right height') (->> stream (rx-where (lambda (t) (= 'faces' ((method get_Item1) t)))) (rx-select (method get_Item2)) (rx-where (lambda (t) (> ((method get_Length) t) 4))) (rx-select (lambda (s) (jdv-parse 'confidence' s))) (rx-where (lambda (c) (> c 1.0)))) (->> stream (rx-where (lambda (t) (= 'navdata' ((method get_Item1) t)))) (rx-select (method get_Item2)) (rx-select (lambda (s) (jdv-parse 'altitudeMeters' s))) (rx-where (lambda (d) (and (not (invoke-static 'System.Double' 'IsNaN' d)) (> d 0.2)))))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(3, results.Count());
            Assert.AreEqual("Found a face at the right height", results.First());
        }

        [TestMethod]
        public void WorksWithSimplifiedFacesAndHeightsThreadingMacro1()
        {
            var values = new[]
                {
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": -0.239784 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 0.573957 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.42347 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 0.94728 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"superfoo\", \"altitudeMeters\": 1.687834 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"moarfoo\", \"altitudeMeters\": 2.5493784 }]"),
                    new Tuple<string, string>("faces", "[{ \"barity\": \"bar\", \"confidence\": 1.8381293 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 3.5634756 }]"),
                    new Tuple<string, string>("navdata", "[{ \"fooness\": \"fooest\", \"altitudeMeters\": 4.384927 }]"),
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-combine-latest (lambda (c h) 'Found a face at the right height') (->> stream (rx-where (lambda (t) (= 'faces' (car t)))) (rx-select (lambda (t) (cdr t))) (rx-where (lambda (t) (> ((method get_Length) t) 4))) (rx-select (lambda (s) (jdv-parse 'confidence' s))) (rx-where (lambda (c) (> c 1.0)))) (->> stream (rx-where (lambda (t) (= 'navdata' (car t)))) (rx-select (lambda (t) (cdr t))) (rx-select (lambda (s) (jdv-parse 'altitudeMeters' s))) (rx-where (lambda (d) (> d 0.2))))))";                
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(3, results.Count());
            Assert.AreEqual("Found a face at the right height", results.First());
        }

    }
}
