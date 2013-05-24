using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using Nancy;

namespace Metarx
{
    public class MetaModule : NancyModule
    {
        const int MAX_REQUEST_BODY_LENGTH = 1000000;

        static MetaModule()
        {
            Engine.RegisterProgram(inputs => new Rosie().Execute(inputs));
            Engine.RegisterProgram(inputs => new SampleProgram().Execute(inputs));
        }

        public async Task<object> GetValueFromStream(IObservable<object> stream)
        {
            return await stream.FirstOrDefaultAsync();
        }

        public MetaModule()
        {
            Get["/programs/{id}"] = ps =>
                {
                    int id = Int32.Parse(ps.id);
                    var q = Engine.GetResultQueue(id);
                    if (q == null || q.Count == 0)
                    {
                        return new Response { StatusCode = HttpStatusCode.NotFound };
                    }

                    var it = Engine.GetResultQueue(id).Dequeue();
                    if (IsExecutable(it))
                    {
                        dynamic program = it;
                        var newId = Engine.RegisterProgram(inputs => program.Execute(inputs));
                        var url = string.Format("{0}/programs/{1}", Request.Url.SiteBase.ToString(), newId);
                        var er = new Response { StatusCode = HttpStatusCode.SeeOther };
                        er.Headers["Location"] = url;
                        return er;

                    }
                    var r = new Response
                        {
                            StatusCode = HttpStatusCode.OK,
                            Contents = s =>
                                {
                                    var bytes = Encoding.UTF8.GetBytes(it.ToString());
                                    s.Write(bytes, 0, bytes.Length);
                                }
                        };

                    return r;
                };

            Post["/programs/{id}/{name}"] = ps =>
            {
                int id = Int32.Parse(ps.id);
                string name = ps.name;

                int len = (int)Request.Body.Length;

                if (Request.Body.Length > MAX_REQUEST_BODY_LENGTH)
                {
                    return new Response { StatusCode = HttpStatusCode.BadRequest };
                }

                var buf = new byte[Request.Body.Length];

                Request.Body.Read(buf, 0, len);

                var data = Encoding.UTF8.GetString(buf);

                var tuple = new Tuple<int, string, string>(id, name, data);

                var mis = Engine.MainInputStream;
                mis.OnNext(tuple);

                var res = new Response
                {
                    StatusCode = HttpStatusCode.OK
                };

                return res;
            };

            Post["/programs/{id}"] = ps =>
            {
                int id = Int32.Parse(ps.id);
                string name = "<default>";

                int len = (int)Request.Body.Length;

                if (Request.Body.Length > MAX_REQUEST_BODY_LENGTH)
                {
                    return new Response { StatusCode = HttpStatusCode.BadRequest };
                }

                var buf = new byte[Request.Body.Length];

                Request.Body.Read(buf, 0, len);

                var data = Encoding.UTF8.GetString(buf);

                var tuple = new Tuple<int, string, string>(id, name, data);

                var mis = Engine.MainInputStream;
                mis.OnNext(tuple);

                var res = new Response
                {
                    StatusCode = HttpStatusCode.OK
                };

                return res;
            };
        }

        private static bool IsExecutable(object it)
        {
            return (it as string) == null && it.GetType().GetMethods().Count(m => m.Name == "Execute") > 0;
        }
    }
}