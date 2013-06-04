using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class BasicNihilTest : NihilTestBase
    {
        [TestMethod]
        public void TestMethod3()
        {
            var values = new[] { new Tuple<string, string>("foo", "bar") };
            const string program = "(define (execute stream) (rx-select (lambda (x) (and (= 1 1))) stream))";
            var results = Execute(program, values);
        }
        
        [TestMethod]
        public void TestMethod4()
        {
            var values = new[] { new Tuple<string, string>("foo", "bar") };
            const string program = "(define (execute stream) (rx-select (lambda (x) #t) stream))";
            var results = Execute(program, values);
        }

        [TestMethod]
        public void TestMethod5()
        {
            var evaluator = new Evaluator();
            var reader = new Reader();
            var program = "((lambda () #t))";
            var sexp = reader.Read(program, evaluator.Environment);
            var rexp = evaluator.Evaluate(sexp);
        }
    }
}
