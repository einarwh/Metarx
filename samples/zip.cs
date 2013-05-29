using System;
using System.Reactive.Linq;

namespace CombinerDemo
{
  public class Combiner 
  {
    public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
    {
       var foo = stream.Where(t => t.Item1 == "foo").Select(t => t.Item2);
       var bar = stream.Where(t => t.Item1 == "bar").Select(t => t.Item2);

       return foo.Zip(bar, (f, b) => f + ":" + b)
	  .Select(v => v + " is " + v.Length + " chars long.");
    }
  }
}
