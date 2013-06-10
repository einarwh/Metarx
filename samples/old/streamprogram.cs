using System;
using System.Reactive.Linq;

namespace HelloWorld 
{
  public class Foo 
  {
    public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
    { 
       return stream
	  .Where(t => t.Item1 == "<default>")
	  .Select(t => t.Item2)
	  .Select(v => v + " is " + v.Length + " chars long.");
    }
  }
}
