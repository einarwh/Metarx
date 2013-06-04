using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    [TestClass]
    public class BasicNihilTest : NihilTestBase
    {
        [TestMethod]
        public void TestMethod1()
        {
            const string program = "(define (execute stream) ((lambda (x) (and #t)) 1))";
            var results = Execute(program, new ArraySegment<Tuple<string, string>>());
        }

        [TestMethod]
        public void TestMethod2()
        {
            const string program = "(define (execute stream) ((lambda (x) (if #t #t)) 1))";
            var results = Execute(program, new ArraySegment<Tuple<string, string>>());
        }

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
