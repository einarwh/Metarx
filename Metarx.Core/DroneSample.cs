using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Metarx.Core
{
    public class JsonDoubleValueParser
    {
        private readonly Regex _regex;

        public JsonDoubleValueParser(string name)
        {
            var rex = name + @"\S:\s?((-?)\d+\.\d+)";
            _regex = new Regex(rex);
        }

        public double Parse(string s)
        {
            var match = _regex.Match(s);
            if (match.Success)
            {
                double d;
                var result = double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out d) ? d : 0;
                return result;
            }
            return double.NaN;
        }
    }

    public class DroneSample
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var confidenceParser = new JsonDoubleValueParser("confidence");
            var faces =
                stream.Where(t => t.Item1 == "faces")
                      .Select(t => t.Item2)
                      .Select(confidenceParser.Parse);

            var altitudeParser = new JsonDoubleValueParser("altitudeMeters");
            var height = stream
                .Where(t => t.Item1 == "navdata")
                .Select(t => t.Item2)
                .Select(altitudeParser.Parse)
                .Where(d => d > 0.2);

            return faces
                .Where(c => c > 1.0)
                .CombineLatest(height, (f, h) => "Conf: " + f + " && " + h);
        }
    }
}
