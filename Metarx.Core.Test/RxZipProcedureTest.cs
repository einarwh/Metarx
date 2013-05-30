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
            var match = results.First();
            Assert.AreEqual(match, "guy:shaving");
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
            var match = results.First();
            Assert.AreEqual(match, "guy:shaving");
        }
    }
}
