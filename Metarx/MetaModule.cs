using System;
using System.Linq;
using System.Text;
using System.Threading;

using Metarx.Core;

using Nancy;

namespace Metarx
{
    public class MetaModule : NancyModule
    {
        const int MaxRequestBodyLength = 1000000;

        static MetaModule()
        {
            Engine.RegisterProgram(inputs => new Rosie().Execute(inputs));
            Engine.RegisterProgram(inputs => new SampleProgram().Execute(inputs));
            Engine.RegisterProgram(inputs => new NihilEntryPoint().Execute(inputs));
            Engine.RegisterProgram(inputs => new DevCombineLatest().Execute(inputs));
            Engine.RegisterProgram(inputs => new DroneSample().Execute(inputs));
        }

        public MetaModule()
        {
            Func<string, string, Response> postHandler = (pid, name) =>
                {
                    int id = Int32.Parse(pid);
                    var len = (int)Request.Body.Length;

                    if (Request.Body.Length > MaxRequestBodyLength)
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

            Get["/programs/{id}"] = ps =>
                {
                    int id = Int32.Parse(ps.id);
                    var q = Engine.GetResultQueue(id);
                    if (q == null)
                    {
                        return new Response { StatusCode = HttpStatusCode.NotFound };
                    }

                    while (q.Count == 0)
                    {
                        Thread.Sleep(1000);
                    }

                    var it = q.Dequeue();
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

            Post["/programs/{id}/{name}"] = ps => postHandler(ps.id, ps.name);

            Post["/programs/{id}"] = ps => postHandler(ps.id, "<default>");

            Post["/clear"] = ps =>
                {
                    Engine.Clear();
                    return new Response { StatusCode = HttpStatusCode.OK };
                };
        }

        private static bool IsExecutable(object it)
        {
            return (it as string) == null && it.GetType().GetMethods().Count(m => m.Name == "Execute") > 0;
        }
    }
}