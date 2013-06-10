using System;
using System.Reactive.Linq;

namespace Metarx.Core
{
    public class SampleProgram
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var first = stream.Where(t => t.Item1 == "firstname").Select(t => t.Item2);
            var last = stream.Where(t => t.Item1 == "lastname").Select(t => t.Item2);
            
            var zipped = first.Zip(last, (f, b) => f + " " + b);
            return zipped;
        }
    }
}