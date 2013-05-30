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
    }
}
