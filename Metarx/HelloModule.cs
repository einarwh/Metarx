using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using Nancy;
using Nancy.ModelBinding;

namespace Metarx
{
    /*
    public class HelloModule : NancyModule
    {
        const int MAX_REQUEST_BODY_LENGTH = 1000000;

        private static int _counter = 0;
        private static Dictionary<int, dynamic> _programs = new Dictionary<int, dynamic>();

        public HelloModule()
        {
            Post["/programs"] = ps =>
            {
                int len = (int)Request.Body.Length;
                if (Request.Body.Length > MAX_REQUEST_BODY_LENGTH)
                {
                    return new Response { StatusCode = HttpStatusCode.BadRequest };
                }

                var buf = new byte[Request.Body.Length];

                Request.Body.Read(buf, 0, len);

                var code = Encoding.UTF8.GetString(buf);
                var program = Rose.CreateProgram(code);

                int id = ++_counter;
                _programs[id] = program;

                var res = new Response { 
                    StatusCode = HttpStatusCode.Created
                };

                var url = string.Format("{0}/{1}", Request.Url.ToString(), id);
                res.Headers["Location"] = url;

                return res;
            };

            Get["/programs/{id}"] = ps =>
            {
                string id = ps.id;

                return "Running program" + id;
            };

            Post["/programs/{id}/data"] = ps =>
                {
                    int id = Int32.Parse(ps.id);

                    int len = (int)Request.Body.Length;
                    if (Request.Body.Length > MAX_REQUEST_BODY_LENGTH)
                    {
                        return new Response { StatusCode = HttpStatusCode.BadRequest };
                    }

                    var buf = new byte[Request.Body.Length];

                    Request.Body.Read(buf, 0, len);

                    var data = Encoding.UTF8.GetString(buf);

                    var program = _programs[id];

                    var result = (string) program.Run(data);

                    var res = new Response
                    {
                        StatusCode = HttpStatusCode.OK
                    };

                    var u = Request.Url;
                    var url = string.Format("{0}/{1}", Request.Url.BasePath, id);

                    var text = string.Format("Program {0} responded to '{1}' with '{2}'", id, data, result);

                    res.Contents = s =>
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(text);
                        s.Write(bytes, 0, bytes.Length);
                    };

                    return res;
                };
        }
    }
     * */
}