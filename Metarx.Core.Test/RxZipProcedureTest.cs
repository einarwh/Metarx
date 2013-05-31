using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class RxZipProcedureTest : NihilTestBase
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

            const string SingleQuoteProgram = "(define (execute stream) (rx-zip (lambda (x y) (+ x ':' y)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (u) (= 'quux' (invoke-instance u 'get_Item1'))) stream)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (u) (= 'yak' (invoke-instance u 'get_Item1'))) stream))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("guy:shaving", results.First());
        }

        [TestMethod]
        public void WorksWithMacro()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("quux", "steele")
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-zip (lambda (x y) (+ x ':' y)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'quux' ((method get_Item1) t))) stream)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'yak' ((method get_Item1) t))) stream))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("guy:shaving", results.First());
        }

        [TestMethod]
        public void WorksWithLotsOfStreams()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("lemming", "ferocious"),
                    new Tuple<string, string>("llama", "knitting"),
                    new Tuple<string, string>("otter", "water"),
                    new Tuple<string, string>("llama", "spit"),
                    new Tuple<string, string>("quux", "steele"),
                    new Tuple<string, string>("otter", "pensive"),
                    new Tuple<string, string>("yak", "hair"),
                    new Tuple<string, string>("lemming", "leader")
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-zip (lambda (a b c d e) (+ a ':' b ':' c ':' d ':' e)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (t) (= 'quux' ((method get_Item1) t))) stream)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (t) (= 'yak' ((method get_Item1) t))) stream)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (t) (= 'lemming' ((method get_Item1) t))) stream)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (t) (= 'llama' ((method get_Item1) t))) stream)) (rx-select (lambda (t) (invoke-instance t 'get_Item2')) (rx-where (lambda (t) (= 'otter' ((method get_Item1) t))) stream))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("guy:shaving:ferocious:knitting:water", results[0]);
            Assert.AreEqual("steele:hair:leader:spit:pensive", results[1]);
        }

        [TestMethod]
        public void WorksWithMacroWithLotsOfStreams()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("lemming", "ferocious"),
                    new Tuple<string, string>("llama", "knitting"),
                    new Tuple<string, string>("otter", "water"),
                    new Tuple<string, string>("llama", "spit"),
                    new Tuple<string, string>("quux", "steele"),
                    new Tuple<string, string>("otter", "pensive"),
                    new Tuple<string, string>("yak", "hair"),
                    new Tuple<string, string>("lemming", "leader")
                };

            const string SingleQuoteProgram = "(define (execute stream) (rx-zip (lambda (a b c d e) (+ a ':' b ':' c ':' d ':' e)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'quux' ((method get_Item1) t))) stream)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'yak' ((method get_Item1) t))) stream)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'lemming' ((method get_Item1) t))) stream)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'llama' ((method get_Item1) t))) stream)) (rx-select (method get_Item2) (rx-where (lambda (t) (= 'otter' ((method get_Item1) t))) stream))))";
            var program = SingleQuoteProgram.Replace('\'', '"');
            var results = Execute(program, values).ToList();
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("guy:shaving:ferocious:knitting:water", results[0]);
            Assert.AreEqual("steele:hair:leader:spit:pensive", results[1]);
        }
    }
}
