using System;
using System.Reactive.Linq;

namespace CombineLatestDemo
{
    public class CombineLatest
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var faces = stream
                .Where(t => t.Item1 == "faces")
                .Select(t => t.Item2)
                .Where(s => !s.Equals("[]"));

            var navdata = stream
                .Where(t => t.Item1 == "navdata")
                .Select(t => t.Item2);

            return faces.CombineLatest(navdata, (f, n) => "Use " + f + " with " + n);
        }
    }
}
