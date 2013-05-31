using System;
using System.Reactive.Linq;

namespace CombinerDemo
{
  public class Combiner 
  {
    public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
    {
       var faces = stream
	  .Where(t => t.Item1 == "faces")
	  .Select(t => t.Item2)
	  .Where(s => s.Length > 2);
       
       var height = stream.Where(t => t.Item1 == "height").Select(t => double.Parse(t.Item2));

       return faces.Zip(height, (f, h) => f + "..." + h);
    }
  }
}
