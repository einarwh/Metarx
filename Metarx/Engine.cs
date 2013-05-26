using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Metarx
{
    public class Engine
    {
        private static int _counter = 0;

        private static readonly Engine _ = new Engine();

        private static readonly Subject<Tuple<int, string, string>> _input = new Subject<Tuple<int, string, string>>();

        private static readonly Dictionary<int, Queue<object>> ResultMap = new Dictionary<int, Queue<object>>(); 

        public static Engine Instance
        {
            get
            {
                return _;
            }
        }

        public static Subject<Tuple<int, string, string>> MainInputStream
        {
            get
            {
                return _input;
            }
        } 

        public static IObservable<Tuple<string, string>> GetProgramInputStream(int id)
        {
            return _input.Where(t => t.Item1 == id).Select(t => new Tuple<string, string>(t.Item2, t.Item3));
        }

        public static IObservable<string> GetNamedProgramDataInputStream(int id, string name)
        {
            return GetProgramInputStream(id).Where(t => t.Item1 == name).Select(t => t.Item2);
        }

        private Engine()
        {
            
        }

        public static Queue<object> GetResultQueue(int id)
        {
            return ResultMap.ContainsKey(id) ? ResultMap[id] : null;
        }

        public static int RegisterProgram(Func<IObservable<Tuple<string, string>>, IObservable<object>> execute)
        {
            int id = _counter++;
            var q = new Queue<object>();
            execute(GetProgramInputStream(id)).Subscribe(obj =>
                {
                    var x = obj;
                    q.Enqueue(obj);
                });
            ResultMap[id] = q;
            return id;
        }
    }

    public class ProgramWrapper
    {
        private dynamic _program;

        public ProgramWrapper(dynamic program)
        {
            _program = program;
        }

        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            return stream.Where(t => t.Item1 == "<default>").Select(t => _program.Execute(t.Item2));
        }
    }
}