using System;
using System.Reactive.Linq;

namespace Metarx
{
    public class SampleProgram
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var foo = stream.Where(t => t.Item1 == "foo").Select(t => t.Item2);
            var bar = stream.Where(t => t.Item1 == "bar").Select(t => t.Item2);
            var zipped = foo.Zip(bar, (f, b) => f + ":" + b);
            return zipped;
        }
    }
}