using System;
using System.Linq;
using System.Reactive.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Metarx.Core.Test
{
    public static class NihilTest
    {
        public static IEnvironment Setup(string program)
        {
            var evaluator = new Evaluator();
            var reader = new Reader();
            foreach (string lispThing in EntryPoint.GetBasicLispThings())
            {
                evaluator.Evaluate(reader.Read(lispThing, evaluator.Environment));
            }

            evaluator.Evaluate(reader.Read(program, evaluator.Environment));

            return evaluator.Environment;
        }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("quux", "steele")
                };

            var stream = values.ToObservable();

            var program = "(define (execute stream) (rx-select (method get_Item2) stream))";
            var env = NihilTest.Setup(program);
            var nihil = new NihilProgramWrapper("execute", env);

            var os = nihil.Execute(stream);

            var results = os.ToEnumerable();

            Assert.AreEqual(3, results.Count());
        }

        [TestMethod]
        public void TestMethod2()
        {
            var values = new[]
                {
                    new Tuple<string, string>("quux", "guy"),
                    new Tuple<string, string>("yak", "shaving"),
                    new Tuple<string, string>("quux", "steele")
                };

            var stream = values.ToObservable();

            var program = "(define (execute stream) (rx-select (lambda (t) (invoke-instance t \"get_Item2\")) stream))";
            var env = NihilTest.Setup(program);
            var nihil = new NihilProgramWrapper("execute", env);

            var os = nihil.Execute(stream);

            var results = os.ToEnumerable();

            Assert.AreEqual(3, results.Count());
        }

    }
}
