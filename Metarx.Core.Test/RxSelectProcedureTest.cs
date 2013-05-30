using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class RxSelectProcedureTest : NihilTestBase
    {
        [TestMethod]
        public void WorksWithMacro()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("quux", "steele")
                };

            var program = "(define (execute stream) (rx-select (method get_Item2) stream))";
            var results = Execute(program, values);
            Assert.AreEqual(3, results.Count());
        }

        [TestMethod]
        public void Works()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("quux", "steele")
                };

            var program = "(define (execute stream) (rx-select (lambda (t) (invoke-instance t \"get_Item2\")) stream))";
            var results = Execute(program, values);
            Assert.AreEqual(3, results.Count());
        }

    }
}
