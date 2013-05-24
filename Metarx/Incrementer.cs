using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Metarx
{
    public class Incrementer : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int n = 0;
            while (true)
            {
                yield return ++n;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}