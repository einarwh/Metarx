using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Metarx
{
    public class TheMightyQuux
    {
        public IObservable<string> Process(string input, Dictionary<string, IObservable<string>> streamMap)
        {
            var faces = streamMap["faces"];
            var heights = streamMap["heights"].Select(double.Parse);
            var foo = faces.CombineLatest(heights, (f, h) => h > 2.00).Select(b => b.ToString());
            return foo;
        }
    }
}
