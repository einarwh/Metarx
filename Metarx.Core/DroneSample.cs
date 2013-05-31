using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Metarx.Core
{
    public class AltitudeParser
    {
        public static double Parse(string s)
        {
            var regex = new Regex(@"altitudeMeters\S:\s?((-?)\d+\.\d+)");
            var match = regex.Match(s);
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
            var faces =
                stream.Where(t => t.Item1 == "faces")
                      .Select(t => t.Item2)
                      .Where(s => s.Length > 4)
                      .Select(s => s.Substring(2, s.Length - 4))
                      .Select(
                          s =>
                              {
                                  var parts = s.Split(',');
                                  var confPart = parts[parts.Length - 1];
                                  var confParts = confPart.Split(':');
                                  var confVal = confParts[1];
                                  var result = double.Parse(confVal, CultureInfo.InvariantCulture);
                                  return result;
                              });
            int magic = 0;
            var height = stream
                .Where(t => t.Item1 == "navdata")
                .Select(t => t.Item2)
                .Select(AltitudeParser.Parse)
                .Where(d => !double.IsNaN(d) && d > 0.2);

            return faces
                .Where(c => c > 1.0)
                .CombineLatest(height, (f, h) => "Conf: " + f + " && " + h);
        }
    }
}
