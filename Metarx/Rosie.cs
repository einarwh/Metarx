using System;
using System.Reactive.Linq;

namespace Metarx
{
    public class Rosie
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var programs = stream.Where(t => t.Item1 == "<default>").Select(t => t.Item2);
            var result = programs.Select(code => Rose.CreateProgram(code));
            return result;
        }
    }
}