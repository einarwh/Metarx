using System;
using System.Reactive.Linq;

namespace EchoValueDemo 
{
  public class Echo 
  {
    public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
    {
       return stream.Select(t => t.Item2);
    }
  }
}
