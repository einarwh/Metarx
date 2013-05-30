using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Metarx.Core.Test
{
    public class NihilTestBase
    {
        private static IEnvironment Setup(string program)
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

        protected IEnumerable<object> Execute(string program, IEnumerable<Tuple<string, string>> input)
        {
            var stream = input.ToObservable();
            var env = Setup(program);
            var nihil = new NihilProgramWrapper("execute", env);
            var os = nihil.Execute(stream);
            var result = os.ToEnumerable();
            return result;
        }
    }
}
