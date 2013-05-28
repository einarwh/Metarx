using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reactive.Linq;
using System;

namespace Metarx
{
    public class NihilProgramWrapper
    {
        private readonly string _procedureName;

        private readonly ChainedEnvironment _env;

        public NihilProgramWrapper(string procedureName, IEnvironment baseEnv)
        {
            _procedureName = procedureName;
            _env = new ChainedEnvironment(baseEnv);
        }

        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var reader = new Reader();
            _env.Add("globalrxstream", new LiteralExpression(stream));
            var callText = string.Format("({0} globalrxstream)", _procedureName);
            var callExp = reader.Read(callText, _env);
            var evaluator = new Evaluator { Environment = _env };
            var evalResult = (AtomExpression)evaluator.Evaluate(callExp);
            var atom = evalResult.Atom;
            var obs = atom as IObservable<object>;
            return obs;
        } 
    }

    public class EntryPoint
    {
        public IObservable<object> Execute(IObservable<Tuple<string, string>> stream)
        {
            var s = stream.Select(t => t.Item2);
            var result = s.Select(ExecuteExpression);
            return result;
        }

        public object ExecuteExpression(string s)
        {
            var evaluator = new Evaluator();
            var reader = new Reader();
            var sexp = reader.Read(s, evaluator.Environment);
            var result = evaluator.Evaluate(sexp);
            var env = evaluator.Environment;
            if (result is Symbol)
            {
                var sym = (Symbol)result;
                var varRef = env.Lookup(sym);
                var evaluee = varRef.Evaluate((IScope)env, false);
                if (evaluee is Procedure)
                {
                    var procName = sym.Print();
                    return new NihilProgramWrapper(procName, env);
                }
            }

            var value = ((LiteralExpression)result).Atom;
            return value;
        }

        public override string ToString()
        {
            return "I am Nihil.";
        }
    }

    public class AddProcedure : NativeProcedure
    {
        public AddProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.Args;
        }

        public override SExpression Evaluate(IScope env)
        {
            var argsExp = (ConsCell)env.Get(0);
            var list = new List<object>();
            while (!argsExp.IsNil())
            {
                var it = argsExp.Car;
                list.Add(((AtomExpression)it).Atom);
                argsExp = argsExp.Next;
            }

            var sum = Add(list.ToArray());

            return new LiteralExpression(sum);
        }

        private static object Add(params object[] values)
        {
            if (values.Length == 0)
            {
                return 0;
            }

            var val = values[0];

            if (val is int)
            {
                int sum = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    sum += (int)values[i];
                }
                return sum;
            }

            if (val is double)
            {
                double sum = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    sum += (double)values[i];
                }
                return sum;
            }

            if (val is string)
            {
                var sum = string.Join("", values.OfType<string>());
                return sum;
            }
            throw new Exception("I don't know how to add this stuff together. First value has type " + val.GetType() + ".");
        }
    }

    public class AndForm : CompiledForm
    {
        private readonly CompiledForm[] _forms;
        private readonly Symbol _false;
        private readonly Symbol _true;

        public AndForm(ConsCell forms, IEnvironment env)
        {
            _forms = forms.CompileAll(env);
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            foreach (CompiledForm f in _forms)
            {
                if (_false == f.Evaluate(env, false)) return _false;
            }
            return _true;
        }
    }

    class ArithmeticShiftProcedure : NativeProcedure
    {
        public ArithmeticShiftProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            int value = (int)GetAtom(env, 0);
            int shift = (int)GetAtom(env, 1);
            int result = value;

            if (shift >= 0)
            {
                result <<= shift;
            }
            else
            {
                result >>= -shift;
            }
            return new LiteralExpression(result);
        }

        private static object GetAtom(IScope env, int i)
        {
            return ((AtomExpression)env.Get(i)).Atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XYArgs;
        }
    }

    public class AtomCheckProcedure : NativeProcedure
    {
        private readonly Symbol _false;
        private readonly Symbol _true;

        public AtomCheckProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            var sexp = env.Get(0);
            return (sexp is ConsCell) ? _false : _true;
        }
    }

    public abstract class AtomExpression : SExpression
    {
        private readonly object _atom;

        protected AtomExpression(object atom)
        {
            _atom = atom;
        }

        public override CompiledForm Compile(IEnvironment env)
        {
            return new ValueForm(this);
        }

        public object Atom
        {
            get { return _atom; }
        }
    }

    class BeginForm : CompiledForm
    {
        private readonly CompiledForm[] _forms;

        public BeginForm(IEnvironment env, ConsCell body)
        {
            _forms = body.CompileAll(env);
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            for (int i = 0; i < _forms.Length - 1; i++)
            {
                _forms[i].Evaluate(env, false);
            }
            return _forms[_forms.Length - 1].Evaluate(env, false);
        }
    }

    class BitAndProcedure : BitwiseProcedure
    {
        public BitAndProcedure(IEnvironment env)
            : base(env)
        {
        }

        protected override int Op(int x, int y)
        {
            return x & y;
        }
    }

    class BitOrProcedure : BitwiseProcedure
    {
        public BitOrProcedure(IEnvironment env)
            : base(env)
        {
        }

        protected override int Op(int x, int y)
        {
            return x | y;
        }
    }

    abstract class BitwiseProcedure : NativeProcedure
    {
        protected BitwiseProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            int x = (int)GetAtom(env, 0);
            int y = (int)GetAtom(env, 1);
            int result = Op(x, y);
            return new LiteralExpression(result);
        }

        protected abstract int Op(int x, int y);

        private object GetAtom(IScope env, int i)
        {
            return ((AtomExpression)env.Get(i)).Atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XYArgs;
        }
    }

    class BitXorProcedure : BitwiseProcedure
    {
        public BitXorProcedure(IEnvironment env)
            : base(env)
        {
        }

        protected override int Op(int x, int y)
        {
            return x ^ y;
        }
    }

    public class CarProcedure : NativeProcedure
    {
        public CarProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            var cell = (ConsCell)env.Get(0);
            return cell.Car;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }
    }

    public class CdrProcedure : NativeProcedure
    {
        public CdrProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            var cell = (ConsCell)env.Get(0);
            return cell.Cdr;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }
    }

    public class ChainedEnvironment : IEnvironment, IScope
    {
        public Symbols Symbols
        {
            get { return OuterEnvironment.Symbols; }
        }

        private readonly IEnvironment _outer;

        public IEnvironment OuterEnv
        {
            get { return _outer; }
        }

        public IScope OuterScope
        {
            get { return (IScope)_outer; }
        }

        public SExpression Get(int i)
        {
            return _values[i];
        }

        public void Set(int i, SExpression value)
        {
            _values[i] = value;
        }

        private readonly IDictionary<Symbol, int> _symbolTable = new Dictionary<Symbol, int>();
        private readonly IList<SExpression> _values = new List<SExpression>();

        public ChainedEnvironment(IEnvironment outer)
        {
            _outer = outer;
        }

        public int GetIndex(Symbol symbol)
        {
            if (_symbolTable.ContainsKey(symbol))
            {
                return _symbolTable[symbol];
            }
            return -1;
        }

        public VariableReference Lookup(Symbol symbol)
        {
            int level = 0;
            IEnvironment env = this;
            while (true)
            {
                int index = env.GetIndex(symbol);
                if (index >= 0) return new VariableReference(level, index);
                env = env.OuterEnv;
                level++;
            }
        }

        public VariableReference LookupOrNull(Symbol symbol)
        {
            int level = 0;
            IEnvironment env = this;
            while (true)
            {
                int index = env.GetIndex(symbol);
                if (index >= 0) return new VariableReference(level, index);
                env = env.OuterEnv;
                level++;
                if (env is NullEnvironment) return null;
            }
        }

        public VariableReference Lookup(string symbol)
        {
            return Lookup(Symbols.GetSymbol(symbol));
        }

        public void Add(Symbol symbol)
        {
            _values.Add(Nil.Instance);
            _symbolTable[symbol] = _symbolTable.Count;
        }

        public void Add(string symbol)
        {
            Add(Symbols.GetSymbol(symbol));
        }

        public int Depth
        {
            get { return _outer.Depth + 1; }
        }

        public IEnvironment OuterEnvironment
        {
            get { return _outer; }
        }

        public int ScopeSize()
        {
            return _symbolTable.Count;
        }

        public void AddArguments(ConsCell args, SExpression formals)
        {
            int i = 0;
            ConsCell a = args;
            SExpression f = formals;
            while (f is ConsCellImpl)
            {
                Set(i, a.Car);
                i++;
                a = a.Next;
                f = ((ConsCell)f).Next;
            }
            if (f is Symbol) Set(i, a);
        }

        public void Add(string name, SExpression value)
        {
            Add(name);
            Lookup(name).Set(value, this);
        }

    }

    abstract class CompareNumbersProcedure : NativeProcedure
    {
        private readonly string _procedureName;
        private readonly Symbol _false;
        private readonly Symbol _true;

        protected CompareNumbersProcedure(IEnvironment env, string procedureName)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
            _procedureName = procedureName;
        }

        public override SExpression Evaluate(IScope env)
        {
            object o1 = ((AtomExpression)env.Get(0)).Atom;
            object o2 = ((AtomExpression)env.Get(1)).Atom;
            return Compare(o1, o2) ? _true : _false;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XYArgs;
        }

        private bool Compare(object o1, object o2)
        {
            if (o1 is int && o2 is int)
            {
                return Compare((int)o1, (int)o2);
            }
            if ((o1 is double || o1 is int) && (o2 is double || o2 is int))
            {
                return Compare((double)o1, (double)o2);
            }
            throw new ArgumentException(_procedureName + ": Invalid arguments of types: " + o1.GetType() + " and " + o2.GetType());
        }

        protected abstract bool Compare(int x, int y);

        protected abstract bool Compare(double x, double y);
    }

    public abstract class CompiledForm
    {
        public abstract SExpression Evaluate(IScope env, bool isTail);
    }

    internal class CondForm : CompiledForm
    {
        private readonly CompiledForm[][] _clauses;
        private readonly Symbol _false;
        private readonly Symbol _true;

        public CondForm(ConsCell clauses, IEnvironment env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
            _clauses = new CompiledForm[clauses.Size()][];
            ConsCell c = clauses;
            int i = 0;
            while (!(c is Nil))
            {
                var clauseCell = (ConsCell)c.Car;
                if (clauseCell.Car == env.Symbols.Else) _clauses[i] = new CompiledForm[] { clauseCell.Next.Car.Compile(env) };
                else _clauses[i] = new CompiledForm[] { clauseCell.Car.Compile(env), clauseCell.Next.Car.Compile(env) };
                c = c.Next;
                i++;
            }
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            foreach (CompiledForm[] f in _clauses)
            {
                if (f.Length == 1) return f[0].Evaluate(env, isTail);
                CompiledForm predicate = f[0];
                if (_false != predicate.Evaluate(env, false))
                {
                    return f[1].Evaluate(env, isTail);
                }
            }
            return _false;
        }
    }

    public abstract class ConditionalSequenceFormBase : CompiledForm
    {
        private readonly CompiledForm _predicate;
        private readonly CompiledForm[] _consequents;
        private readonly Symbol _false;
        private readonly Symbol _true;

        protected ConditionalSequenceFormBase(string formName, SExpression predicate, ConsCell consequents, IEnvironment env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (consequents == null)
            {
                throw new ArgumentNullException("consequents");
            }
            _predicate = predicate.Compile(env);
            _consequents = consequents.CompileAll(env);
        }

        protected abstract bool ShouldEvaluateConsequents(IScope env);

        protected bool IsPredicateFalse(IScope env)
        {
            return _predicate.Evaluate(env, false) == _false;
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            if (!ShouldEvaluateConsequents(env))
            {
                return _false;
            }

            SExpression res = _false;

            foreach (CompiledForm f in _consequents)
            {
                res = f.Evaluate(env, false);
            }
            return res;
        }
    }

    public abstract class ConsCell : SExpression, IEnumerable<SExpression>
    {
        public abstract SExpression Car { get; set; }
        public abstract SExpression Cdr { get; set; }

        public ConsCell Next
        {
            get { return (ConsCell)Cdr; }
        }

        public int Size()
        {
            int i = 0;
            ConsCell c = this;
            while (!(c is Nil))
            {
                i++;
                c = c.Next;
            }
            return i;
        }

        public CompiledForm[] CompileAll(IEnvironment env)
        {
            var res = new ArrayList();
            ConsCell c = this;
            while (!(c is Nil))
            {
                var item = c.Car;
                var itemType = item.GetType();
                CompiledForm form = item.Compile(env);
                if (!(form is NoopForm)) res.Add(form);
                c = c.Next;
            }

            return (CompiledForm[])res.ToArray(typeof(CompiledForm));
        }

        public static ConsCell List(params SExpression[] args)
        {
            ConsCell res = Nil.Instance;
            for (int i = args.Length - 1; i >= 0; i--) res = args[i].Cons(res);
            return res;
        }

        public static ConsCell ImproperList(params SExpression[] args)
        {
            int len = args.Length;
            ConsCell res = args[len - 2].Cons(args[len - 1]);
            for (int i = len - 3; i >= 0; i--) res = args[i].Cons(res);
            return res;
        }

        public IEnumerator<SExpression> GetEnumerator()
        {
            return new ConsCellEnumerator(this);
        }

        public abstract bool IsNil();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConsCellEnumerator : IEnumerator<SExpression>
    {
        private readonly ConsCell _start;
        private ConsCell _current;

        public ConsCellEnumerator(ConsCell cell)
        {
            _start = cell;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _current = _current == null ? _start : _current.Next;
            return !_current.IsNil();
        }

        public void Reset()
        {
            _current = _start;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public SExpression Current
        {
            get { return _current; }
        }
    }

    public class ConsCellImpl : ConsCell
    {
        private SExpression _car;
        private SExpression _cdr;

        public ConsCellImpl(ConsCell cell)
        {
            _car = cell.Car;
            _cdr = cell.Cdr;
        }

        public ConsCellImpl(SExpression car, SExpression cdr)
        {
            _car = car;
            _cdr = cdr;
        }

        public override SExpression Car
        {
            get { return _car; }
            set { _car = value; }
        }

        public override SExpression Cdr
        {
            get { return _cdr; }
            set { _cdr = value; }
        }

        public override bool IsNil()
        {
            return false;
        }

        public override string Print()
        {
            // TODO: Depends on whether or not the cdr is a cons cell, right? What happens if it ends in something else?
            // TODO: There are three cases: cdr is nil, cdr is a non-nil cons cell, cdr is something else.
            string s = "";
            ConsCell cdrCell = this;
            bool isFirst = true;
            do
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    s += " ";
                }
                s += cdrCell.Car.Print();
                var tempCdr = cdrCell.Cdr;
                if (tempCdr is ConsCell)
                {
                    cdrCell = (ConsCell)tempCdr;
                }
                else
                {
                    s += " . " + tempCdr.Print();
                    break;
                }
            } while (!cdrCell.IsNil());

            return "(" + s + ")";
        }

        public override CompiledForm Compile(IEnvironment env)
        {
            if (Car is Symbol)
            {
                CompiledForm res = CreateSuitableForm(env);
                if (res != null) return res;
            }

            return CreateProcedureCallForm(env);
        }

        private CompiledForm CreateSuitableForm(IEnvironment env)
        {
            Symbols symbols = env.Symbols;
            if (Car == symbols.Define)
            {
                return CreateDefineForm(env);
            }
            if (Car == symbols.Set)
            {
                return CreateSetForm(env);
            }
            if (Car == symbols.Lambda)
            {
                return CreateLambdaForm(env);
            }
            if (Car == symbols.Begin)
            {
                return CreateBeginForm(env);
            }
            if (Car == symbols.Quote)
            {
                return CreateQuoteForm();
            }
            if (Car == symbols.Quasiquote)
            {
                return CreateQuasiquoteForm(env);
            }
            if (Car == symbols.If)
            {
                return CreateIfForm(env);
            }
            if (Car == symbols.Cond)
            {
                return CreateCondForm(env);
            }
            if (Car == symbols.When)
            {
                return CreateWhenForm(env);
            }
            if (Car == symbols.Unless)
            {
                return CreateUnlessForm(env);
            }
            if (Car == symbols.And)
            {
                return CreateAndForm(env);
            }
            if (Car == symbols.Or)
            {
                return CreateOrForm(env);
            }
            if (Car == symbols.Apply)
            {
                return CreateApplyForm(env);
            }
            if (Car == symbols.DefineMacro)
            {
                return DefineMacro(env);
            }
            var vRef = env.LookupOrNull((Symbol)Car);
            if (vRef != null)
            {
                var v = vRef.Evaluate((IScope)env, false);
                if (v is Macro) return MacroExpand(env, (Macro)v);
            }

            return null;
        }

        private CompiledForm CreateDefineForm(IEnvironment env)
        {
            var cdrCell = Next;
            if (cdrCell.Car is ConsCell) return CreateDefineProcedureForm(env);
            var name = (Symbol)cdrCell.Car;
            cdrCell = (ConsCell)cdrCell.Cdr;
            var x = cdrCell.Car;
            return new DefineForm(name, x, env);
        }

        private CompiledForm CreateSetForm(IEnvironment env)
        {
            var cdrCell = Next;
            var name = (Symbol)cdrCell.Car;
            cdrCell = (ConsCell)cdrCell.Cdr;
            var x = cdrCell.Car;
            return new SetForm(name, x, env);
        }

        private CompiledForm CreateDefineProcedureForm(IEnvironment env)
        {
            var symbols = env.Symbols;
            var signature = (ConsCell)Next.Car;
            var parameters = signature.Cdr;
            var body = Next.Next;
            var lambdaForm = symbols.Lambda.Cons(parameters.Cons(body));

            var name = signature.Car;
            var defineForm = symbols.Define.Cons(name.Cons(lambdaForm.Cons(Nil.Instance)));
            return defineForm.CreateDefineForm(env);

        }

        private CompiledForm CreateLambdaForm(IEnvironment env)
        {
            var cell = (ConsCell)Cdr;
            var forms = cell.Next;
            return new LambdaForm(cell.Car, forms, env);
        }

        private CompiledForm CreateQuoteForm()
        {
            return new ValueForm(Next.Car);
        }

        private CompiledForm CreateQuasiquoteForm(IEnvironment env)
        {
            return QuasiquoteForm.Quasiquote(env, Next.Car);
        }

        private CompiledForm CreateIfForm(IEnvironment env)
        {
            var cdrCell = (ConsCell)Cdr;
            var predicate = cdrCell.Car;
            cdrCell = cdrCell.Next;
            var consequence = cdrCell.Car;
            cdrCell = cdrCell.Next;
            if (cdrCell.IsNil())
            {
                return new IfForm(predicate, consequence, env);
            }
            var alternative = cdrCell.Car;
            return new IfForm(predicate, consequence, alternative, env);
        }

        private CompiledForm CreateWhenForm(IEnvironment env)
        {
            var cdrCell = (ConsCell)Cdr;
            return new WhenForm(cdrCell.Car, cdrCell.Next, env);
        }

        private CompiledForm CreateUnlessForm(IEnvironment env)
        {
            var cdrCell = (ConsCell)Cdr;
            return new UnlessForm(cdrCell.Car, cdrCell.Next, env);
        }

        private CompiledForm CreateCondForm(IEnvironment env)
        {
            var cdrCell = (ConsCell)Cdr;
            return new CondForm(cdrCell, env);
        }

        private CompiledForm CreateAndForm(IEnvironment env)
        {
            return new AndForm((ConsCell)Cdr, env);
        }

        private CompiledForm CreateOrForm(IEnvironment env)
        {
            return new OrForm((ConsCell)Cdr, env);
        }

        private CompiledForm CreateBeginForm(IEnvironment env)
        {
            return new BeginForm(env, Next);
        }

        private bool IsName(string val)
        {
            return true;
        }

        private ProcedureCallForm CreateProcedureCallForm(IEnvironment env)
        {
            return new ProcedureCallForm(env, Car, Next);
        }

        private ProcedureCallForm CreateApplyForm(IEnvironment env)
        {
            return ProcedureCallForm.ApplyForm(env, Next.Car, Next.Next);
        }

        private CompiledForm DefineMacro(IEnvironment env)
        {
            var symbols = env.Symbols;
            var signature = (ConsCell)Next.Car;
            var parameters = signature.Cdr;
            var body = Next.Next;
            var lambdaForm = symbols.Lambda.Cons(parameters.Cons(body));
            var res = (LambdaForm)lambdaForm.Compile(env);
            var name = (Symbol)signature.Car;
            env.Add(name);
            env.Lookup(name).Set(res.MakeMacro((IScope)env), (IScope)env);
            return new NoopForm(signature.Car);
        }

        private CompiledForm MacroExpand(IEnvironment callEnv, Macro macro)
        {
            var env = new Scope(macro.Scope, macro.ScopeSize);
            env.AddArguments(Next, macro.Formals);
            return macro.Evaluate(env).Compile(callEnv);
        }
    }

    public class ConsProcedure : NativeProcedure
    {
        public ConsProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            var cell1 = env.Get(0);
            var cell2 = env.Get(1);
            return new ConsCellImpl(cell1, cell2);
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XYArgs;
        }
    }

    class CreateMatrixProcedure : NativeProcedure
    {
        public CreateMatrixProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            int columns = (int)((AtomExpression)env.Get(0)).Atom;
            int rows = (int)((AtomExpression)env.Get(1)).Atom;
            var matrix = new Matrix(columns, rows);
            return new LiteralExpression(matrix);
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XYArgs;
        }
    }

    public class DecrementProcedure : NativeProcedure
    {
        public DecrementProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            var sexp = env.Get(0);
            object o = ((AtomExpression)sexp).Atom;
            return new LiteralExpression(Decrement(o));
        }

        private static object Decrement(object o)
        {
            if (o is int)
            {
                return Decrement((int)o);
            }
            if (o is double)
            {
                return Decrement((double)o);
            }
            throw new ArgumentException("Decrement: Got an argument of type: " + o.GetType());
        }

        private static double Decrement(double val)
        {
            return --val;
        }

        private static int Decrement(int val)
        {
            return --val;
        }
    }

    public class DefineForm : CompiledForm
    {
        private readonly Symbol _name;
        private readonly VariableReference _var;
        private readonly CompiledForm _form;

        public DefineForm(Symbol variableName, SExpression exp, IEnvironment env)
        {
            _name = variableName;
            env.Add(variableName);
            _var = (VariableReference)variableName.Compile(env);
            _form = exp.Compile(env);
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            _var.Set(_form.Evaluate(env, false), env);
            return _name;
        }

    }

    public class DivideProcedure : NativeProcedure
    {
        public DivideProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            var valExp = (AtomExpression)env.Get(0);
            var val = valExp.Atom;

            var sexp = (ConsCell)env.Get(1);
            var list = new List<object>();
            while (!sexp.IsNil())
            {
                var it = sexp.Car;
                list.Add(((AtomExpression)it).Atom);
                sexp = sexp.Next;
            }

            var sum = Divide(val, list.ToArray());

            return new LiteralExpression(sum);
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            Symbols symbols = env.Symbols;
            return ConsCell.ImproperList(symbols.GetSymbol("x"), symbols.Args);
        }

        private static object Divide(object val, params object[] values)
        {
            if (val is int)
            {
                int result = (int)val;
                if (values.Length == 0)
                {
                    return 1 / result;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    result /= (int)values[i];
                }

                return result;
            }

            if (val is double)
            {
                double result = (double)val;
                if (values.Length == 0)
                {
                    return 1 / result;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    result /= (double)values[i];
                }

                return result;
            }

            throw new Exception("I don't know how to divide this stuff. Type: " + val.GetType());
        }
    }

    public class EqualsProcedure : NativeProcedure
    {
        private readonly Symbol _false;
        private readonly Symbol _true;

        public EqualsProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression Evaluate(IScope env)
        {
            object o1 = ((AtomExpression)env.Get(0)).Atom;
            object o2 = ((AtomExpression)env.Get(1)).Atom;
            var result = Compare(o1, o2) ? _true : _false;
            return result;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XYArgs;
        }

        private static bool Compare(object o1, object o2)
        {
            if (o1 is int && o2 is int)
            {
                return Compare((int)o1, (int)o2);
            }

            if ((o1 is double || o1 is int) && (o2 is double || o2 is int))
            {
                return Compare((double)o1, (double)o2);
            }

            if (o1 is string && o2 is string)
            {
                return o1.Equals(o2);
            }

            throw new ArgumentException("=: Invalid arguments of types: " + o1.GetType() + " and " + o2.GetType());
        }

        private static bool Compare(int x, int y)
        {
            return x == y;
        }

        private static bool Compare(double x, double y)
        {
            return x == y;
        }
    }

    public class Evaluator
    {
        private IEnvironment _env;

        public IEnvironment Environment
        {
            get { return _env; }
            set
            {
                _env = value;
            }
        }

        public Evaluator()
        {
            _env = CreateBasicEnvironment();
        }

        public Evaluator(IDictionary<string, ProcedureFactory> table)
        {
            _env = new ChainedEnvironment(CreateBasicEnvironment());
            foreach (string k in table.Keys)
            {
                var factory = table[k];
                _env.Add(k, factory(_env));
            }
        }

        private static IEnvironment CreateBasicEnvironment()
        {
            var env = new ChainedEnvironment(new NullEnvironment());
            Symbols symbols = env.Symbols;
            env.Add("cons", new ConsProcedure(env));
            env.Add("car", new CarProcedure(env));
            env.Add("cdr", new CdrProcedure(env));
            env.Add("list", new ListProcedure(env));
            var add = new AddProcedure(env);
            var sub = new SubProcedure(env);
            env.Add("add", add);
            env.Add("sub", sub);
            env.Add("+", add);
            env.Add("-", sub);
            env.Add("*", new MultiplyProcedure(env));
            env.Add("/", new DivideProcedure(env));
            env.Add("ash", new ArithmeticShiftProcedure(env));
            env.Add("bit-and", new BitAndProcedure(env));
            env.Add("bit-or", new BitOrProcedure(env));
            env.Add("bit-xor", new BitXorProcedure(env));
            env.Add("inc", new IncrementProcedure(env));
            env.Add("dec", new DecrementProcedure(env));
            env.Add("<", new LessThanProcedure(env));
            env.Add("<=", new LessThanEqualsProcedure(env));
            env.Add(">", new GreaterThanProcedure(env));
            env.Add(">=", new GreaterThanEqualsProcedure(env));
            env.Add("=", new EqualsProcedure(env));
            env.Add("println", new PrintlnProcedure(env));
            env.Add("exit", new ExitProcedure(env));
            env.Add("null", Nil.Instance);
            env.Add("null?", new NullCheckProcedure(env));
            env.Add("#t", symbols.True);
            env.Add("#f", symbols.False);
            env.Add("pair?", new PairCheckProcedure(env));
            env.Add("symbol?", new SymbolCheckProcedure(env));
            env.Add("new", new InvokeCtorProcedure(env));
            env.Add("invoke-instance", new InvokeInstanceMethodProcedure(env));
            env.Add("invoke-static", new InvokeStaticMethodProcedure(env));
            env.Add("create-matrix", new CreateMatrixProcedure(env));
            env.Add("init-matrix", new InitMatrixProcedure(env));
            env.Add("get-cell", new GetMatrixCellProcedure(env));
            env.Add("set-cell", new SetMatrixCellProcedure(env));
            env.Add("is-cell?", new IsMatrixCellProcedure(env));
            env.Add("gensym", new GensymProcedure(env));
            env.Add("str", new StringProcedure(env));
            env.Add("rx-select", new RxSelectProcedure(env));
            env.Add("rx-where", new RxWhereProcedure(env));
            env.Add("rx-zip", new RxZipProcedure(env));
            return env;
        }

        public SExpression Evaluate(SExpression f)
        {
            return Evaluate(f, false);
        }

        public SExpression Evaluate(SExpression f, bool isTail)
        {
            return f.Compile(_env).Evaluate((IScope)_env, isTail);
        }
    }

    public class EvaluatorException : MihilException
    {
        public EvaluatorException(string msg)
            : base(msg)
        {
        }
    }

    class ExitProcedure : NativeProcedure
    {
        public ExitProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            // TODO: Find a reasonable implementation???
            return Nil.Instance;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return Nil.Instance;
        }

    }

    public static class FormalParameters
    {
        public static ConsCell Create(params string[] names)
        {
            var stack = new Stack<string>();
            foreach (string n in names)
            {
                stack.Push(n);
            }
            ConsCell cell = Nil.Instance;
            while (stack.Count > 0)
            {
                cell = new LiteralExpression(stack.Pop()).Cons(cell);
            }
            return cell;
        }

    }

    class GensymProcedure : NativeProcedure
    {
        public GensymProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            var name = (string)((AtomExpression)env.Get(0)).Atom;
            return new Symbol("gen:" + name);
        }
    }

    class RxSelectProcedure : NativeProcedure
    {
        public RxSelectProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope evalScope)
        {
            var proc = (Procedure)evalScope.Get(0);
            var stream = GetAtom(evalScope, 1);
            Type elemType = stream.GetType().BaseType.GetGenericArguments().First();

            var paramExp = Expression.Parameter(elemType, "it");
            var bodyExp = GetRealBody(proc, paramExp);

            // Func<object, Tuple<string, string>>
            Type funcType = typeof(Func<,>).MakeGenericType(elemType, typeof(object));
            var selectLambdaExp = Expression.Lambda(funcType, bodyExp, paramExp);
            var selectLambda = selectLambdaExp.Compile();
            var selectors = typeof(Observable).GetMethods().Where(m => m.Name == "Select").ToArray();
            var firstSel = selectors.First();

            var selectorMethod = firstSel.MakeGenericMethod(elemType, typeof(object));
            var resultStream = selectorMethod.Invoke(null, new [] { stream, selectLambda } );

            return new LiteralExpression(resultStream);
        }

        private Expression GetRealBody(Procedure proc, ParameterExpression paramExp)
        {
            var currentScopeExp = Expression.Constant(proc.Scope);
            var scopeSizeExp = Expression.Constant(proc.ScopeSize);

            var scopeCtor = typeof(Scope).GetConstructors().First();
            // Create new scope for proc eval: new Scope(currentScope, scopeSize);
            var scopeCtorExp = Expression.New(scopeCtor, currentScopeExp, scopeSizeExp);

            // Assign to variable: Scope procScope = new Scope(currentScope, scopeSize);
            var procScopeVarExp = Expression.Variable(typeof(Scope), "procScope");
            var assignProcScopeExp = Expression.Assign(procScopeVarExp, scopeCtorExp);
 
            // Wrap paramExp in Literal? Yes?
            var litExpCtor = typeof(LiteralExpression).GetConstructors().First();
            var litExpCtorExp = Expression.New(litExpCtor, paramExp);
            // Add parameter to scope.

            var setMethod = typeof(Scope).GetMethods().First(m => m.Name == "Set");
            var zeroExp = Expression.Constant(0);
            var setCallExp = Expression.Call(procScopeVarExp, setMethod, zeroExp, litExpCtorExp);

            // Call procedure: proc.Evaluate(procScope);
            var procExp = Expression.Constant(proc);
            var evaluateMethod = typeof(Procedure).GetMethods().First(m => m.Name == "Evaluate");
            var evalProcExp = Expression.Call(procExp, evaluateMethod, procScopeVarExp);

            var convertExp = Expression.Convert(evalProcExp, typeof(TailCall));

            var tailEvalMethod =
                typeof(TailCall).GetMethods().First(m => m.Name == "Evaluate" && !m.GetParameters().Any());

            var evalTailCallExp = Expression.Call(convertExp, tailEvalMethod);

            // Unwrap literal.
            var convertedResultExp = Expression.Convert(evalTailCallExp, typeof(LiteralExpression));
            var getAtomMethod = typeof(LiteralExpression).GetMethods().First(m => m.Name == "get_Atom");
            
            var atomResultExp = Expression.Call(convertedResultExp, getAtomMethod);

            var bodyExp = Expression.Block(new [] { procScopeVarExp }, 
                assignProcScopeExp, litExpCtorExp, setCallExp, atomResultExp);
            return bodyExp;
        }

        private object GetAtom(IScope env, int i)
        {
            var atom = ((AtomExpression)env.Get(i)).Atom;
            return atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("f"), s.GetSymbol("x"));
        }
        
    }

    class RxWhereProcedure : NativeProcedure
    {
        private readonly Symbol _falseSymbol;

        public RxWhereProcedure(IEnvironment env)
            : base(env)
        {
            _falseSymbol = env.Symbols.False;
        }

        public override SExpression Evaluate(IScope evalScope)
        {
            var proc = (Procedure)evalScope.Get(0);
            var stream = GetAtom(evalScope, 1);
            Type elemType = stream.GetType().BaseType.GetGenericArguments().First();

            var paramExp = Expression.Parameter(elemType, "it");
            var bodyExp = GetRealBody(proc, paramExp);

            // Func<bool, Tuple<string, string>>
            Type funcType = typeof(Func<,>).MakeGenericType(elemType, typeof(bool));
            var lambdaExp = Expression.Lambda(funcType, bodyExp, paramExp);
            var lambda = lambdaExp.Compile();
            var methods = typeof(Observable).GetMethods().Where(m => m.Name == "Where").ToArray();
            var methodTemplate = methods.First();

            var method = methodTemplate.MakeGenericMethod(elemType);
            var resultStream = method.Invoke(null, new[] { stream, lambda });

            return new LiteralExpression(resultStream);
        }

        private Expression GetRealBody(Procedure proc, ParameterExpression paramExp)
        {
            var currentScopeExp = Expression.Constant(proc.Scope);
            var scopeSizeExp = Expression.Constant(proc.ScopeSize);

            var scopeCtor = typeof(Scope).GetConstructors().First();
            // Create new scope for proc eval: new Scope(currentScope, scopeSize);
            var scopeCtorExp = Expression.New(scopeCtor, currentScopeExp, scopeSizeExp);

            // Assign to variable: Scope procScope = new Scope(currentScope, scopeSize);
            var procScopeVarExp = Expression.Variable(typeof(Scope), "procScope");
            var assignProcScopeExp = Expression.Assign(procScopeVarExp, scopeCtorExp);

            // Wrap paramExp in Literal? Yes?
            var litExpCtor = typeof(LiteralExpression).GetConstructors().First();
            var litExpCtorExp = Expression.New(litExpCtor, paramExp);
            // Add parameter to scope.

            var setMethod = typeof(Scope).GetMethods().First(m => m.Name == "Set");
            var zeroExp = Expression.Constant(0);
            var setCallExp = Expression.Call(procScopeVarExp, setMethod, zeroExp, litExpCtorExp);

            // Call procedure: proc.Evaluate(procScope);
            var procExp = Expression.Constant(proc);
            var evaluateMethod = typeof(Procedure).GetMethods().First(m => m.Name == "Evaluate");
            var evalProcExp = Expression.Call(procExp, evaluateMethod, procScopeVarExp);

            var convertExp = Expression.Convert(evalProcExp, typeof(TailCall));

            var tailEvalMethod =
                typeof(TailCall).GetMethods().First(m => m.Name == "Evaluate" && !m.GetParameters().Any());

            var evalTailCallExp = Expression.Call(convertExp, tailEvalMethod);

            // Compare to Nihil false.
            var equalExp = Expression.NotEqual(Expression.Constant(_falseSymbol), evalTailCallExp);
        
            var bodyExp = Expression.Block(new[] { procScopeVarExp },
                assignProcScopeExp, litExpCtorExp, setCallExp, equalExp);

            return bodyExp;
        }

        private object GetAtom(IScope env, int i)
        {
            var atom = ((AtomExpression)env.Get(i)).Atom;
            return atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("f"), s.GetSymbol("x"));
        }
    }

    class RxZipProcedure : NativeProcedure
    {
        public RxZipProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope evalScope)
        {
            var proc = (Procedure)evalScope.Get(0);
            var stream1 = GetAtom(evalScope, 1);
            var stream2 = GetAtom(evalScope, 2);
            Type elemType1 = stream1.GetType().BaseType.GetGenericArguments().First();
            Type elemType2 = stream2.GetType().BaseType.GetGenericArguments().First();

            var paramExp1 = Expression.Parameter(elemType1, "x");
            var paramExp2 = Expression.Parameter(elemType2, "y");
            var bodyExp = GetRealBody(proc, paramExp1, paramExp2);

            // Func<Foo, Bar, object>
            Type funcType = typeof(Func<,,>).MakeGenericType(elemType1, elemType2, typeof(object));
            var lambdaExp = Expression.Lambda(funcType, bodyExp, paramExp1, paramExp2);
            var lambda = lambdaExp.Compile();

            var zipMethod = GetZipMethodTemplate().MakeGenericMethod(elemType1, elemType2, typeof(object));
            
            var resultStream = zipMethod.Invoke(null, new[] { stream1, stream2, lambda });

            return new LiteralExpression(resultStream);
        }

        private static MethodInfo GetZipMethodTemplate()
        {
            Predicate<ParameterInfo> paramCheck =
                p => p.ParameterType.GetGenericTypeDefinition() == typeof(IObservable<>);

            var zippers = typeof(Observable)
                .GetMethods()
                .Where(m => m.Name == "Zip" && m.GetParameters().Count() == 3)
                .Where(m =>
                {
                    var ps = m.GetParameters();
                    var result = paramCheck(ps[0]) && paramCheck(ps[1]);
                    return result;
                });

            var zipMethod = zippers.First();

            return zipMethod;
        }

        private Expression GetRealBody(Procedure proc, ParameterExpression paramExp0, ParameterExpression paramExp1)
        {
            var currentScopeExp = Expression.Constant(proc.Scope);
            var scopeSizeExp = Expression.Constant(proc.ScopeSize);

            // Create variable for new scope.
            var procScopeVarExp = Expression.Variable(typeof(Scope), "procScope");

            // Scope procScope = new Scope(currentScope, scopeSize);
            var assignProcScopeExp = Expression.Assign(procScopeVarExp, 
                Expression.New(typeof(Scope).GetConstructors().First(), 
                    currentScopeExp, 
                    scopeSizeExp));

            var setMethod = typeof(Scope).GetMethods().First(m => m.Name == "Set");

            // procScope.Set(0, paramExp0);
            var setZeroCallExp = Expression.Call(
                procScopeVarExp, 
                setMethod, 
                Expression.Constant(0), 
                Expression.New(typeof(LiteralExpression).GetConstructors().First(), paramExp0));

            // procScope.Set(1, paramExp1);
            var setOneCallExp = Expression.Call(
                procScopeVarExp,
                setMethod,
                Expression.Constant(1),
                Expression.New(typeof(LiteralExpression).GetConstructors().First(), paramExp1));

            // Call procedure (which involves evaluating tail call as well): 
            // proc.Evaluate(procScope);
            var evalTailCallExp = Expression.Call(
                Expression.Convert(Expression.Call(
                    Expression.Constant(proc), 
                    typeof(Procedure).GetMethods().First(m => m.Name == "Evaluate"), 
                    procScopeVarExp), typeof(TailCall)), 
                typeof(TailCall).GetMethods().First(m => m.Name == "Evaluate" && !m.GetParameters().Any()));

            // Unwrap atom from literalexpression:
            var atomResultExp = Expression.Call(
                Expression.Convert(evalTailCallExp, typeof(LiteralExpression)), 
                typeof(LiteralExpression).GetMethods().First(m => m.Name == "get_Atom"));

            var bodyExp = Expression.Block(
                new[] { procScopeVarExp },
                assignProcScopeExp, 
                setZeroCallExp, 
                setOneCallExp, 
                atomResultExp);
            
            return bodyExp;
        }

        private object GetAtom(IScope env, int i)
        {
            var atom = ((AtomExpression)env.Get(i)).Atom;
            return atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("f"), s.GetSymbol("x"), s.GetSymbol("y"));
        }
    }

    class GetMatrixCellProcedure : NativeProcedure
    {
        public GetMatrixCellProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            var matrix = (Matrix)GetAtom(env, 0);
            var x = (int)GetAtom(env, 1);
            var y = (int)GetAtom(env, 2);
            var cell = matrix.GetCell(x, y);
            return new LiteralExpression(cell);
        }

        private object GetAtom(IScope env, int i)
        {
            var atom = ((AtomExpression)env.Get(i)).Atom;
            return atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("m"), s.X, s.Y);
        }
    }

    internal class GreaterThanEqualsProcedure : CompareNumbersProcedure
    {
        public GreaterThanEqualsProcedure(IEnvironment env) : base(env, ">=") { }

        protected override bool Compare(int x, int y)
        {
            return x >= y;
        }

        protected override bool Compare(double x, double y)
        {
            return x >= y;
        }
    }

    internal class GreaterThanProcedure : CompareNumbersProcedure
    {
        public GreaterThanProcedure(IEnvironment env) : base(env, ">") { }

        protected override bool Compare(int x, int y)
        {
            return x > y;
        }

        protected override bool Compare(double x, double y)
        {
            return x > y;
        }
    }

    public interface IEnvironment
    {
        Symbols Symbols { get; }
        IEnvironment OuterEnv { get; }

        VariableReference Lookup(Symbol symbol);
        int GetIndex(Symbol symbol);
        VariableReference LookupOrNull(Symbol symbol);
        VariableReference Lookup(string symbol);

        void Add(Symbol symbol);
        void Add(string symbol);
        void Add(string symbol, SExpression sexp);

        int Depth { get; }
    }

    public class IfForm : CompiledForm
    {
        private readonly CompiledForm _alternative;
        private readonly CompiledForm _consequent;
        private readonly CompiledForm _predicate;
        private readonly Symbol _false;

        public IfForm(SExpression predicate, SExpression consequent, IEnvironment env) : this(predicate, consequent, null, env) { }

        public IfForm(SExpression predicate, SExpression consequent, SExpression alternative, IEnvironment env)
        {
            _false = env.Symbols.False;
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (consequent == null)
            {
                throw new ArgumentNullException("consequent");
            }
            _predicate = predicate.Compile(env);
            _consequent = consequent.Compile(env);
            if (alternative != null)
            {
                _alternative = alternative.Compile(env);
            }
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            CompiledForm x = _predicate.Evaluate(env, false) == _false ? _alternative : _consequent;
            if (x == null)
            {
                return _false;
            }
            return x.Evaluate(env, isTail);
        }

    }

    public class IncompleteInputException : Exception
    {
        private readonly int _indentation;
        public int Indentation { get { return _indentation; } }

        public IncompleteInputException(int indentation)
        {
            _indentation = indentation;
        }
    }

    public class IncrementProcedure : NativeProcedure
    {
        public IncrementProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            var sexp = env.Get(0);
            object o = ((AtomExpression)sexp).Atom;
            return new LiteralExpression(Increment(o));
        }

        private static object Increment(object o)
        {
            if (o is int)
            {
                return Increment((int)o);
            }
            if (o is double)
            {
                return Increment((double)o);
            }
            throw new ArgumentException("Increment: Got an argument of type: " + o.GetType());
        }

        private static double Increment(double val)
        {
            return ++val;
        }

        private static int Increment(int val)
        {
            return ++val;
        }

    }

    class InitMatrixProcedure : NativeProcedure
    {
        public InitMatrixProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            var matrixExp = (AtomExpression)env.Get(0);
            var matrix = (Matrix)matrixExp.Atom;
            var value = ((AtomExpression)env.Get(1)).Atom;
            matrix.InitCells(value);
            return matrixExp;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("m"), s.GetSymbol("v"));
        }
    }

    public class InvokeCtorProcedure : NativeProcedure
    {
        public InvokeCtorProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            var type = GetTypeToCreate(env);
            var args = GetArgs(env);
            var argTypes = GetArgTypes(args);
            var ctor = GetConstructor(type, argTypes);
            var result = ctor.Invoke(args);
            return new LiteralExpression(result);
        }

        private static Type GetTypeToCreate(IScope env)
        {
            var it = env.Get(0);
            object o = ((AtomExpression)it).Atom;
            string typeName = (string)o;
            var type = Type.GetType(typeName);
            if (type == null)
            {
                throw new MihilException("Unable to resolve type " + typeName);
            }
            return type;
        }

        private static object[] GetArgs(IScope env)
        {
            var args = (ConsCell)env.Get(1);
            var cell = args;
            var vals = new List<object>();
            while (!cell.IsNil())
            {
                var exp = (AtomExpression)cell.Car;
                vals.Add(exp.Atom);
                cell = cell.Next;
            }

            return vals.ToArray();
        }

        private static Type[] GetArgTypes(object[] args)
        {
            var argTypes = new Type[args.Length];
            for (int i = 0; i < argTypes.Length; i++)
            {
                argTypes[i] = args[i].GetType();
            }

            return argTypes;
        }

        private static ConstructorInfo GetConstructor(Type type, Type[] argTypes)
        {
            var ctor = type.GetConstructor(argTypes);
            if (ctor == null)
            {
                throw new MihilException("No suitable constructor was found for type " + type);
            }

            return ctor;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            Symbols symbols = env.Symbols;
            return ConsCell.ImproperList(symbols.GetSymbol("type"), symbols.Args);
        }
    }

    public class InvokeInstanceMethodProcedure : InvokeMethodProcedure
    {
        public InvokeInstanceMethodProcedure(IEnvironment env)
            : base(env)
        {
        }

        protected override Type GetType(object atom)
        {
            return atom.GetType();
        }

        protected override object GetInstance(object atom)
        {
            return atom;
        }
    }

    public abstract class InvokeMethodProcedure : NativeProcedure
    {

        private readonly Symbol _false;
        private readonly Symbol _true;

        protected InvokeMethodProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression Evaluate(IScope env)
        {
            var atom = ((AtomExpression)env.Get(0)).Atom;
            var type = GetType(atom);
            var instance = GetInstance(atom);
            var name = GetMethodName(env);
            var args = GetArgs(env);
            var argTypes = GetArgTypes(args);
            var method = GetMethod(type, name, argTypes);
            var result = method.Invoke(instance, args);
            if (result == null) return Nil.Instance;
            if (result is bool) return (bool)result ? _true : _false;

            return new LiteralExpression(result);
        }

        protected abstract Type GetType(object atom);

        protected abstract object GetInstance(object atom);

        protected string GetMethodName(IScope env)
        {
            var exp = (LiteralExpression)env.Get(1);
            return (string)exp.Atom;
        }

        protected MethodInfo GetMethod(Type type, string name, Type[] argTypes)
        {
            var method = type.GetMethod(name, argTypes);
            if (method == null)
            {
                throw new MihilException("No suitable method " + name + " was found for type " + type + ".");
            }

            return method;
        }

        protected object[] GetArgs(IScope env)
        {
            var args = (ConsCell)env.Get(2);
            var cell = args;
            var vals = new List<object>();
            while (!cell.IsNil())
            {
                var exp = cell.Car;
                if (exp is Nil) vals.Add(null);
                else if (exp == _false) vals.Add(false);
                else if (exp == _true) vals.Add(true);
                else
                {
                    var atom = (AtomExpression)cell.Car;
                    var v = atom.Atom;
                    vals.Add(v);
                }
                cell = cell.Next;
            }

            return vals.ToArray();
        }

        protected Type[] GetArgTypes(object[] args)
        {
            var argTypes = new Type[args.Length];
            for (int i = 0; i < argTypes.Length; i++)
            {
                argTypes[i] = args[i].GetType();
            }

            return argTypes;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            Symbols symbols = env.Symbols;
            return ConsCell.ImproperList(symbols.X, symbols.GetSymbol("method"), symbols.Args);
        }
    }

    public class InvokeStaticMethodProcedure : InvokeMethodProcedure
    {
        public InvokeStaticMethodProcedure(IEnvironment env)
            : base(env)
        {
        }

        protected override Type GetType(object atom)
        {
            return Type.GetType((string)atom);
        }

        protected override object GetInstance(object atom)
        {
            return null;
        }
    }

    public interface IProcedure
    {
        IScope Scope { get; }

        SExpression Evaluate(IScope env);

        SExpression Formals { get; }

        int ScopeSize { get; }
    }

    public interface IScope
    {
        IScope OuterScope { get; }
        SExpression Get(int i);
        void Set(int i, SExpression value);
        void AddArguments(ConsCell args, SExpression formals);
    }

    class IsMatrixCellProcedure : NativeProcedure
    {

        private readonly Symbol _false;
        private readonly Symbol _true;
        public IsMatrixCellProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression Evaluate(IScope env)
        {
            var matrix = (Matrix)GetAtom(env, 0);
            var x = (int)GetAtom(env, 1);
            var y = (int)GetAtom(env, 2);
            bool result = matrix.IsCell(x, y);
            return result ? _true : _false;
        }

        private static object GetAtom(IScope env, int index)
        {
            var atom = ((AtomExpression)env.Get(index)).Atom;
            return atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("m"), s.X, s.Y);
        }
    }

    public class LambdaForm : CompiledForm
    {
        private readonly SExpression _formals; // Needs to be all expressions with variable names.
        private readonly CompiledForm[] _forms;
        private readonly int _scopeSize;

        public LambdaForm(SExpression formals, ConsCell forms, IEnvironment env)
        {
            _formals = formals;
            var newEnv = new ChainedEnvironment(env);
            AddParameters(newEnv);
            _forms = forms.CompileAll(newEnv);
            _scopeSize = newEnv.ScopeSize();
        }

        private void AddParameters(IEnvironment env)
        {
            SExpression exp = _formals;
            while (exp is ConsCellImpl)
            {
                env.Add((Symbol)((ConsCell)exp).Car);
                exp = ((ConsCell)exp).Cdr;
            }
            if (exp is Symbol) env.Add((Symbol)exp);
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            return new Procedure(env, _formals, _forms, _scopeSize);
        }

        public SExpression MakeMacro(IScope env)
        {
            return new Macro(env, _formals, _forms, _scopeSize);
        }
    }

    internal class LessThanEqualsProcedure : CompareNumbersProcedure
    {
        public LessThanEqualsProcedure(IEnvironment env) : base(env, "<=") { }

        protected override bool Compare(int x, int y)
        {
            return x <= y;
        }

        protected override bool Compare(double x, double y)
        {
            return x <= y;
        }
    }

    internal class LessThanProcedure : CompareNumbersProcedure
    {
        public LessThanProcedure(IEnvironment env) : base(env, "<") { }

        protected override bool Compare(int x, int y)
        {
            return x < y;
        }

        protected override bool Compare(double x, double y)
        {
            return x < y;
        }
    }

    public class ListProcedure : NativeProcedure
    {
        public ListProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.Args;
        }

        public override SExpression Evaluate(IScope env)
        {
            return env.Get(0);
        }
    }

    public class LiteralExpression : AtomExpression
    {
        public LiteralExpression(object literal)
            : base(literal)
        {
            if (literal is LiteralExpression)
            {
                throw new Exception("Nesting literals, man! " + literal);
            }
        }

        public override string Print()
        {
            return Atom.ToString();
        }
    }

    class Macro : Procedure
    {
        public Macro(IScope env, SExpression formals, CompiledForm[] forms, int scopeSize) : base(env, formals, forms, scopeSize) { }

        public override SExpression Evaluate(IScope env)
        {
            var res = base.Evaluate(env);
            return (res is TailCall) ? ((TailCall)res).Evaluate() : res;
        }
    }

    public class Matrix
    {
        private readonly int _columns;
        private readonly int _rows;

        private readonly object[][] _values;

        public Matrix(int columns, int rows)
        {
            _columns = columns;
            _rows = rows;
            var rowArray = new object[rows][];
            for (int i = 0; i < rows; i++)
            {
                rowArray[i] = new object[columns];
            }
            _values = rowArray;
        }

        public bool IsCell(int column, int row)
        {
            return column >= 0 && column < _columns && row >= 0 && row < _rows;
        }

        public object GetCell(int column, int row)
        {
            return _values[row][column];
        }

        public void SetCell(int column, int row, object value)
        {
            _values[row][column] = value;
        }

        public void InitCells(object value)
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    _values[row][col] = value;
                }
            }
        }

        public override string ToString()
        {
            string s = "";
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    s += " " + _values[row][col];
                }
                s += "\n";
            }
            return s;
        }
    }

    public class MihilException : Exception
    {
        public MihilException(string message) : base(message) { }

        public MihilException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class MultiplyProcedure : NativeProcedure
    {
        public MultiplyProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.Args;
        }

        public override SExpression Evaluate(IScope env)
        {
            var argsExp = (ConsCell)env.Get(0);
            var list = new List<object>();
            while (!argsExp.IsNil())
            {
                var it = argsExp.Car;
                list.Add(((AtomExpression)it).Atom);
                argsExp = argsExp.Next;
            }

            var sum = Multiply(list.ToArray());

            return new LiteralExpression(sum);
        }

        private static object Multiply(params object[] values)
        {
            if (values.Length == 0)
            {
                return 1;
            }

            var val = values[0];

            if (val is int)
            {
                int sum = 1;
                for (int i = 0; i < values.Length; i++)
                {
                    sum *= (int)values[i];
                }
                return sum;
            }

            if (val is double)
            {
                double sum = 1;
                for (int i = 0; i < values.Length; i++)
                {
                    sum *= (double)values[i];
                }
                return sum;
            }

            throw new Exception("I don't know how to multiply this stuff together. First value has type " + val.GetType() + ".");
        }
    }

    public abstract class NativeProcedure : SExpression, IProcedure
    {
        private readonly IScope _scope;

        private readonly IEnvironment _staticEnv;
        
        private readonly SExpression _formals;

        private int _scopeSize;

        public IScope Scope { get { return _scope; } }
        
        public SExpression Formals { get { return _formals; } }
        
        public abstract SExpression Evaluate(IScope env);
        
        public abstract SExpression CreateFormals(IEnvironment env);

        public int ScopeSize
        {
            get { return _scopeSize; }
        }

        public IEnvironment StaticEnvironment
        {
            get
            {
                return _staticEnv;
            }
        }

        protected NativeProcedure(IEnvironment env)
        {
            _formals = CreateFormals(env);
            _scopeSize = CalculateScopeSize();
            _scope = (IScope)env;
            _staticEnv = env;
        }

        private int CalculateScopeSize()
        {
            SExpression exp = _formals;
            int i = 0;
            while (exp is ConsCellImpl)
            {
                exp = ((ConsCell)exp).Cdr;
                i++;
            }
            return (exp is Nil) ? i : i + 1;
        }

        public override string Print()
        {
            return "nativeproc";
        }
    }

    public class Nil : ConsCell
    {

        private Nil() { }

        private static readonly Nil _nil = new Nil();
        public static Nil Instance
        {
            get { return _nil; }
        }

        public override string Print()
        {
            return "()";
        }

        public override SExpression Car
        {
            get { throw new Exception("Car is undefined on Nil"); }
            set { throw new Exception("Car is undefined on Nil"); }
        }

        public override SExpression Cdr
        {
            get { throw new Exception("Cdr is undefined on Nil."); }
            set { throw new Exception("Cdr is undefined on Nil."); }
        }

        public override bool IsNil()
        {
            return true;
        }
    }

    public class NoopForm : CompiledForm
    {

        private readonly SExpression _value;

        public NoopForm(SExpression value = null)
        {
            _value = value;
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            return (_value == null) ? Nil.Instance : _value;
        }
    }


    public class NullCheckProcedure : NativeProcedure
    {

        private readonly Symbol _false;
        private readonly Symbol _true;

        public NullCheckProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            var sexp = env.Get(0);
            return (sexp is Nil) ? _true : _false;
        }
    }


    public class NullEnvironment : IEnvironment
    {
        private readonly Symbols _symbols = new Symbols();

        public Symbols Symbols
        {
            get { return _symbols; }
        }

        public VariableReference LookupOrNull(Symbol symbol)
        {
            return null;
        }

        public int GetIndex(Symbol symbol)
        {
            throw new Exception("The symbol " + symbol.Print() + " is undefined.");
        }

        public VariableReference Lookup(Symbol symbol)
        {
            throw new Exception("Derp. The symbol " + symbol.Print() + " is undefined.");
        }

        public VariableReference Lookup(string symbol)
        {
            throw new Exception("Derp. The symbol " + symbol + " is undefined.");
        }

        public void Add(Symbol symbol)
        {
            throw new Exception("Cannot add symbols to this environment. Symbol: " + symbol.Print());
        }

        public void Add(string symbol)
        {
            throw new Exception("Cannot add symbols to this environment. Symbol: " + symbol);
        }

        public void Add(string symbol, SExpression sexp)
        {
            throw new Exception("Cannot add symbols to this environment. Symbol: " + symbol);
        }

        public int Depth
        {
            get { return 0; }

        }

        public void AddArguments(ConsCell exp)
        {
            throw new NotSupportedException();
        }

        public void AddArguments(ConsCell exp, int rest)
        {
            throw new NotSupportedException();
        }

        public IEnvironment OuterEnv
        {
            get { throw new NotSupportedException(); }
        }
    }

    public class OrForm : CompiledForm
    {
        private readonly CompiledForm[] _forms;
        private readonly Symbol _false;
        private readonly Symbol _true;

        public OrForm(ConsCell forms, IEnvironment env)
        {
            _forms = forms.CompileAll(env);
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {

            foreach (CompiledForm f in _forms)
            {
                if (_false != f.Evaluate(env, false)) return _true;
            }
            return _false;
        }
    }

    class PairCheckProcedure : NativeProcedure
    {

        private readonly Symbol _false;
        private readonly Symbol _true;

        public PairCheckProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            var val = env.Get(0);
            return val is ConsCellImpl ? _true : _false;
        }
    }

    public class PrintlnProcedure : NativeProcedure
    {
        public PrintlnProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            SExpression sexp = env.Get(0);
            Console.WriteLine(sexp.Print());
            return sexp;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.Args;
        }
    }

    public class Procedure : SExpression, IProcedure
    {
        private readonly SExpression _formals;
        private readonly CompiledForm[] _forms;
        private readonly IScope _scope;
        public IScope Scope
        {
            get { return _scope; }
        }

        public int ScopeSize { get; private set; }

        public Procedure(IScope scope, SExpression formals, CompiledForm[] forms, int scopeSize)
        {
            // TODO: For debugging, it might be nice if named procedures included a name.
            // TODO: Anonymous procedures could be named, well, anonymous.
            _scope = scope;
            _formals = formals;
            _forms = forms;
            ScopeSize = scopeSize;
        }

        public SExpression Formals
        {
            get { return _formals; }
        }

        public override string Print()
        {
            return "proc";
        }

        public virtual SExpression Evaluate(IScope env)
        {
            return EvaluateCall(env);
        }

        private SExpression EvaluateCall(IScope env)
        {
            for (int i = 0; i < _forms.Length - 1; i++)
            {
                _forms[i].Evaluate(env, false);
            }
            var result = _forms[_forms.Length - 1].Evaluate(env, true);
            return result;
        }
    }

    public class ProcedureCallForm : CompiledForm
    {
        private readonly CompiledForm _proc;
        private readonly CompiledForm[] _operands;
        private readonly CompiledForm _rest;

        public CompiledForm[] Operands
        {
            get { return _operands; }
        }

        public ProcedureCallForm(IEnvironment env, SExpression proc, ConsCell operands, SExpression rest = null)
        {
            // SExpression -> LambdaForm (or Symbol???)
            _proc = proc.Compile(env);

            var beforeType = proc.GetType();
            var compiledType = _proc.GetType();

            if (operands == null)
            {
                throw new ArgumentNullException("operands");
            }
            _operands = operands.CompileAll(env);
            _rest = (rest == null) ? null : rest.Compile(env);
        }

        private static ConsCell NotLast(ConsCell list)
        {
            if (list.Next is Nil) return Nil.Instance;
            return list.Car.Cons(NotLast(list.Next));
        }

        private static SExpression Last(ConsCell list)
        {
            if (list.Next is Nil) return list.Car;
            return Last(list.Next);
        }

        public static ProcedureCallForm ApplyForm(IEnvironment env, SExpression proc, ConsCell operands)
        {
            return new ProcedureCallForm(env, proc, NotLast(operands), Last(operands));
        }

        public override SExpression Evaluate(IScope callEnv, bool isTail)
        {
            if (isTail) return TailCall(callEnv);

            var pcf = this;

            var proc = (IProcedure)pcf._proc.Evaluate(callEnv, false);
            var scope = new Scope(proc.Scope, proc.ScopeSize);
            ConsCell args = EvaluateAll(callEnv, pcf._operands, pcf._rest);
            scope.AddArguments(args, proc.Formals);
            var result = proc.Evaluate(scope);

            return (result is TailCall) ? ((TailCall)result).Evaluate() : result;
        }

        public TailCall TailCall(IScope env)
        {
            return new TailCall((IProcedure)_proc.Evaluate(env, false), EvaluateAll(env, _operands, _rest));
        }

        private static ConsCell EvaluateAll(IScope env, CompiledForm[] forms, CompiledForm rest, int index = 0)
        {
            if (index == forms.Length) return (rest == null) ? Nil.Instance : (ConsCell)rest.Evaluate(env, false);
            return new ConsCellImpl(forms[index].Evaluate(env, false), EvaluateAll(env, forms, rest, index + 1));
        }
    }

    public delegate NativeProcedure ProcedureFactory(IEnvironment env);

    public class QuasiquoteForm : CompiledForm
    {

        private readonly CompiledForm[] _forms;
        private QuasiquoteForm(CompiledForm[] forms)
        {
            _forms = forms;
        }

        public static CompiledForm Quasiquote(IEnvironment env, SExpression exp, int depth = 0)
        {
            Symbols symbols = env.Symbols;
            if (!(exp is ConsCellImpl))
            {
                return new ValueForm(exp);
            }
            var cell = (ConsCell)exp;
            if (cell.Car == symbols.Quasiquote) depth++;
            else if (cell.Car == symbols.Unquote)
            {
                if (depth == 0) return cell.Next.Car.Compile(env);
                depth--;
            }
            else if (cell.Car == symbols.UnquoteSplicing)
            {
                //TODO splicing
                if (depth == 0) return new UnquoteSplicing(env, cell.Next.Car);
                depth--;
            }
            var forms = new List<CompiledForm>();
            while (!(cell is Nil))
            {
                forms.Add(Quasiquote(env, cell.Car, depth));
                cell = cell.Next;
            }
            return new QuasiquoteForm(forms.ToArray());
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            return MakeCons(env);
        }

        private SExpression MakeCons(IScope env, int index = 0)
        {
            if (index == _forms.Length) return Nil.Instance;
            var f = _forms[index];
            if (!(f is UnquoteSplicing))
                return _forms[index].Evaluate(env, false).Cons(MakeCons(env, index + 1));
            var uq = (UnquoteSplicing)f;
            var cell = (ConsCell)uq.QuasiquoteEvaluate(env);
            return Splice(env, cell, index);
        }

        private SExpression Splice(IScope env, ConsCell cell, int index)
        {
            if (cell is Nil) return MakeCons(env, index + 1);
            else return cell.Car.Cons(Splice(env, cell.Next, index));
        }

    }

    public class Reader
    {
        private string _s;
        private int _pos;
        private int _indentation;
        private int _lastNewline;

        public SExpression Read(String s, IEnvironment env)
        {
            _s = s;
            _pos = 0;
            _indentation = 0;
            _lastNewline = 0;
            return ReadExpression(env);
        }

        private void ReadWhitespace()
        {
            while (true)
            {
                if (_pos == _s.Length) throw new IncompleteInputException(_indentation);
                var c = _s[_pos];
                if (!IsWhiteSpace(c)) return;
                if (c == '\n') _lastNewline = _pos + 1;
                _pos++;
            }
        }

        private bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\n';
        }

        private ConsCell ReadExpressionList(IEnvironment env)
        {
            var indentation = _pos - _lastNewline;
            var bodyIndentation = _pos + 1 - _lastNewline;
            SExpression rest = null;
            var stack = new Stack<SExpression>();
            bool hasBody = false;
            while (true)
            {
                _indentation = indentation;
                ReadWhitespace();
                char c = _s[_pos];
                if (c == ')')
                {
                    break;
                }

                var newIndentation = _pos - _lastNewline;
                var res = ReadExpression(env);
                if (stack.Count == 0 && (res == env.Symbols.Define || res == env.Symbols.Lambda))
                {
                    hasBody = true;
                    indentation = bodyIndentation;
                }
                else if (!hasBody) indentation = newIndentation;

                if (res == env.Symbols.GetSymbol("."))
                {
                    _indentation = indentation;
                    rest = ReadExpression(env);
                    break;
                }
                stack.Push(res);
            }
            if (stack.Count == 0)
            {
                if (rest != null) throw new Exception("dotted pair with no car");
                return Nil.Instance;
            }

            ConsCell cell = new ConsCellImpl(stack.Pop(), rest ?? Nil.Instance);
            while (stack.Count > 0)
            {
                cell = (stack.Pop()).Cons(cell);
            }
            return cell;
        }

        private SExpression ReadExpression(IEnvironment env)
        {
            if (_s.Length == _pos) throw new IncompleteInputException(_indentation);
            char c = _s[_pos];
            ReadWhitespace();
            if (c == '\'')
            {
                _pos++;
                return new ConsCellImpl(env.Symbols.Quote, new ConsCellImpl(ReadExpression(env), Nil.Instance));
            }
            if (c == '`')
            {
                _pos++;
                return new ConsCellImpl(env.Symbols.Quasiquote, new ConsCellImpl(ReadExpression(env), Nil.Instance));
            }
            if (c == ',')
            {
                _pos++;
                if (_s[_pos] == '@')
                {
                    _pos++;
                    return new ConsCellImpl(env.Symbols.UnquoteSplicing, new ConsCellImpl(ReadExpression(env), Nil.Instance));
                }
                return new ConsCellImpl(env.Symbols.Unquote, new ConsCellImpl(ReadExpression(env), Nil.Instance));
            }

            return c == '(' ? ReadListExpression(env) : ReadAtomExpression(env);
        }

        private SExpression ReadAtomExpression(IEnvironment env)
        {
            int startPos = _pos;
            bool isReadingString = false;
            if (_s[_pos] == '"')
            {
                isReadingString = true;
                _pos++;
            }
            bool isDoneReadingString = false;
            // TODO: bool isEscaping = false;
            while (_pos < _s.Length)
            {
                char c = _s[_pos];
                if (c == ')' || (IsWhiteSpace(c) && (!isReadingString || isDoneReadingString)))
                {
                    return CreateSuitableAtomExpression(startPos, isReadingString, env);
                }
                if (isDoneReadingString)
                {
                    throw new Exception("Strange string.");
                }
                if (isReadingString && c == '"')
                {
                    isDoneReadingString = true;
                }
                ++_pos;
            }
            if (_pos == startPos)
            {
                throw new Exception("Logical error in reading atom expression.");
            }
            return CreateSuitableAtomExpression(startPos, isReadingString, env);
        }

        private SExpression CreateSuitableAtomExpression(int startPos, bool isString, IEnvironment env)
        {
            string atomText = _s.Substring(startPos, _pos - startPos);

            if (isString)
            {
                return new LiteralExpression(atomText.Substring(1, atomText.Length - 2));
            }

            char c = atomText[0];
            if (c <= '9' && (c >= '0' || ((c == '+' || c == '-') && atomText.Length > 1)))
            {
                try
                {
                    return new LiteralExpression(int.Parse(atomText));
                }
                catch (Exception)
                {
                }
                try
                {
                    return new LiteralExpression(double.Parse(atomText));
                }
                catch (Exception)
                {
                }
            }

            if (IsIdentifier(atomText))
            {
                return env.Symbols.GetSymbol(atomText);
            }

            return new LiteralExpression(atomText);
        }

        private static bool IsIdentifier(string atomText)
        {
            // TODO: Yikes.
            return atomText[0] != '"';
        }

        private SExpression ReadListExpression(IEnvironment env)
        {
            ReadOpeningParens();
            var sexps = ReadExpressionList(env);
            ReadClosingParens();
            return sexps;
        }

        private void ReadClosingParens()
        {
            char c = _s[_pos++];
            if (c != ')')
            {
                throw new ReaderException("Expected ')', got '" + c + "'");
            }
        }

        private void ReadOpeningParens()
        {
            char c = _s[_pos++];
            if (c != '(')
            {
                throw new ReaderException("Expected '(', got '" + c + "'");
            }
        }
    }

    internal class ReaderException : Exception
    {
        public ReaderException(string msg)
            : base(msg)
        {
        }
    }

    public class Scope : IScope
    {
        private readonly IScope _outer;
        private readonly SExpression[] _values;

        public IScope OuterScope
        {
            get { return _outer; }
        }

        public Scope(IScope outer, int size)
        {
            _outer = outer;
            _values = new SExpression[size];
        }

        public SExpression Get(int i)
        {
            return _values[i];
        }

        public void Set(int i, SExpression value)
        {
            _values[i] = value;
        }

        public void AddArguments(ConsCell args, SExpression formals)
        {
            int i = 0;
            ConsCell a = args;
            SExpression f = formals;
            while (f is ConsCellImpl)
            {
                Set(i, a.Car);
                i++;
                a = a.Next;
                f = ((ConsCell)f).Cdr;
            }
            if (f is Symbol) Set(i, a);
        }
    }

    class SetForm : CompiledForm
    {
        private readonly Symbol _name;
        private readonly VariableReference _var;
        private readonly CompiledForm _form;

        public SetForm(Symbol variableName, SExpression exp, IEnvironment env)
        {
            _name = variableName;
            _var = (VariableReference)variableName.Compile(env);
            _form = exp.Compile(env);
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            _var.Set(_form.Evaluate(env, false), env);
            return _name;
        }

    }

    class SetMatrixCellProcedure : NativeProcedure
    {
        public SetMatrixCellProcedure(IEnvironment env)
            : base(env)
        {
        }

        public override SExpression Evaluate(IScope env)
        {
            var matrix = (Matrix)GetAtom(env, 0);
            var x = (int)GetAtom(env, 1);
            var y = (int)GetAtom(env, 2);
            var valueExp = (AtomExpression)env.Get(3);
            var value = valueExp.Atom;
            matrix.SetCell(x, y, value);
            return valueExp;
        }

        private object GetAtom(IScope env, int index)
        {
            var atom = ((AtomExpression)env.Get(index)).Atom;
            return atom;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            var s = env.Symbols;
            return ConsCell.List(s.GetSymbol("m"), s.X, s.Y, s.GetSymbol("v"));
        }
    }

    public abstract class SExpression
    {
        public abstract string Print();

        public virtual CompiledForm Compile(IEnvironment env)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return GetType() + ": " + Print();
        }

        public ConsCellImpl Cons(SExpression cdr)
        {
            return new ConsCellImpl(this, cdr);
        }
    }

    class StringProcedure : NativeProcedure
    {

        public StringProcedure(IEnvironment env) : base(env) { }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }
        public override SExpression Evaluate(IScope env)
        {
            return new LiteralExpression(env.Get(0).Print());
        }
    }

    public class SubProcedure : NativeProcedure
    {
        public SubProcedure(IEnvironment env) : base(env) { }

        public override SExpression Evaluate(IScope env)
        {
            var valExp = (AtomExpression)env.Get(0);
            var val = valExp.Atom;

            var sexp = (ConsCell)env.Get(1);
            var list = new List<object>();
            while (!sexp.IsNil())
            {
                var it = sexp.Car;
                list.Add(((AtomExpression)it).Atom);
                sexp = sexp.Next;
            }

            var sum = Sub(val, list.ToArray());

            return new LiteralExpression(sum);
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            Symbols symbols = env.Symbols;
            return ConsCell.ImproperList(symbols.GetSymbol("x"), symbols.Args);
        }

        private static object Sub(object val, params object[] values)
        {
            if (val is int)
            {
                int result = (int)val;
                if (values.Length == 0)
                {
                    return -result;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    result -= (int)values[i];
                }

                return result;
            }

            if (val is double)
            {
                double result = (double)val;
                if (values.Length == 0)
                {
                    return -result;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    result -= (double)values[i];
                }

                return result;
            }

            throw new Exception("I don't know how to subtract this stuff. Type: " + val.GetType());
        }
    }

    public class Symbol : SExpression
    {

        private readonly string _name;

        public Symbol(string name)
        {
            _name = name;
        }

        public override string Print()
        {
            return _name;
        }

        public override CompiledForm Compile(IEnvironment env)
        {
            return env.Lookup(this);
        }

    }

    class SymbolCheckProcedure : NativeProcedure
    {

        private readonly Symbol _false;
        private readonly Symbol _true;
        public SymbolCheckProcedure(IEnvironment env)
            : base(env)
        {
            _false = env.Symbols.False;
            _true = env.Symbols.True;
        }

        public override SExpression CreateFormals(IEnvironment env)
        {
            return env.Symbols.XArg;
        }

        public override SExpression Evaluate(IScope env)
        {
            return env.Get(0) is Symbol ? _true : _false;
        }
    }

    public class Symbols
    {
        private readonly IDictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();
        private readonly Symbol _quote;
        private readonly Symbol _true;
        private readonly Symbol _false;
        private readonly Symbol _cons;
        private readonly Symbol _cond;
        private readonly Symbol _else;
        private readonly Symbol _if;
        private readonly Symbol _when;
        private readonly Symbol _unless;
        private readonly Symbol _isNull;
        private readonly Symbol _and;
        private readonly Symbol _or;
        private readonly Symbol _define;
        private readonly Symbol _set;
        private readonly Symbol _lambda;
        private readonly Symbol _x;
        private readonly Symbol _y;
        private readonly Symbol _a;
        private readonly Symbol _b;
        private readonly Symbol _c;
        private readonly Symbol _arg;
        private readonly Symbol _args;
        private readonly Symbol _apply;
        private readonly Symbol _defineMacro;
        private readonly Symbol _quasiquote;
        private readonly Symbol _unquote;
        private readonly Symbol _unquoteSplicing;
        private readonly Symbol _begin;


        public Symbol Quote { get { return _quote; } }
        public Symbol True { get { return _true; } }
        public Symbol False { get { return _false; } }
        public Symbol Cons { get { return _cons; } }
        public Symbol Cond { get { return _cond; } }
        public Symbol Else { get { return _else; } }
        public Symbol If { get { return _if; } }
        public Symbol Unless { get { return _unless; } }
        public Symbol When { get { return _when; } }
        public Symbol IsNull { get { return _isNull; } }
        public Symbol Or { get { return _or; } }
        public Symbol And { get { return _and; } }
        public Symbol Define { get { return _define; } }
        public Symbol Set { get { return _set; } }
        public Symbol Lambda { get { return _lambda; } }
        public Symbol X { get { return _x; } }
        public Symbol Y { get { return _y; } }
        public Symbol A { get { return _a; } }
        public Symbol B { get { return _b; } }
        public Symbol C { get { return _c; } }
        public Symbol Arg { get { return _arg; } }
        public Symbol Args { get { return _args; } }
        public Symbol Apply { get { return _apply; } }
        public Symbol DefineMacro { get { return _defineMacro; } }
        public Symbol Quasiquote { get { return _quasiquote; } }
        public Symbol Unquote { get { return _unquote; } }
        public Symbol UnquoteSplicing { get { return _unquoteSplicing; } }
        public Symbol Begin { get { return _begin; } }

        public ConsCell XArg { get { return X.Cons(Nil.Instance); } }
        public ConsCell XYArgs { get { return X.Cons(Y.Cons(Nil.Instance)); } }

        public Symbols()
        {
            _quote = GetSymbol("quote");
            _true = GetSymbol("#t");
            _false = GetSymbol("#f");
            _cons = GetSymbol("cons");
            _cond = GetSymbol("cond");
            _else = GetSymbol("else");
            _if = GetSymbol("if");
            _when = GetSymbol("when");
            _unless = GetSymbol("unless");
            _isNull = GetSymbol("null?");
            _and = GetSymbol("and");
            _or = GetSymbol("or");
            _define = GetSymbol("define");
            _lambda = GetSymbol("lambda");
            _x = GetSymbol("x");
            _y = GetSymbol("y");
            _a = GetSymbol("a");
            _b = GetSymbol("b");
            _c = GetSymbol("c");
            _arg = GetSymbol("arg");
            _args = GetSymbol("args");
            _apply = GetSymbol("apply");
            _defineMacro = GetSymbol("define-macro");
            _quasiquote = GetSymbol("quasiquote");
            _unquote = GetSymbol("unquote");
            _unquoteSplicing = GetSymbol("unquote-splicing");
            _set = GetSymbol("set!");
            _begin = GetSymbol("begin");
        }

        public Symbol GetSymbol(string name)
        {
            if (!_symbols.ContainsKey(name)) _symbols.Add(name, new Symbol(name));
            return _symbols[name];
        }
    }

    public class TailCall : SExpression
    {
        private readonly IProcedure _proc;
        private readonly ConsCell _args;
        public TailCall(IProcedure proc, ConsCell args)
        {
            _proc = proc;
            _args = args;
        }

        public override string Print()
        {
            throw new NotSupportedException();
        }

        public SExpression Evaluate()
        {

            SExpression res = this;
            while (res is TailCall)
            {
                var tailCall = (TailCall)res;
                var proc = tailCall._proc;
                var args = tailCall._args;
                var env = new Scope(proc.Scope, proc.ScopeSize);
                env.AddArguments(args, proc.Formals);
                res = proc.Evaluate(env);
            }
            return res;
        }
    }

    public class UnlessForm : ConditionalSequenceFormBase
    {
        public UnlessForm(SExpression predicate, ConsCell consequents, IEnvironment env)
            : base("unless", predicate, consequents, env)
        {
        }

        protected override bool ShouldEvaluateConsequents(IScope env)
        {
            return IsPredicateFalse(env);
        }
    }

    public class UnquoteSplicing : CompiledForm
    {
        private readonly CompiledForm _form;
        public UnquoteSplicing(IEnvironment env, SExpression exp)
        {
            _form = exp.Compile(env);
        }
        public override SExpression Evaluate(IScope env, bool isTail)
        {
            throw new NotSupportedException();
        }

        public SExpression QuasiquoteEvaluate(IScope env)
        {
            return _form.Evaluate(env, false);
        }
    }

    class ValueForm : CompiledForm
    {
        private readonly SExpression _value;

        public ValueForm(SExpression value)
        {
            _value = value;
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            return _value;
        }
    }

    public class VariableReference : CompiledForm
    {
        private readonly int _level;
        private readonly int _index;

        public VariableReference(int level, int index)
        {
            _level = level;
            _index = index;
        }
        public void Set(SExpression value, IScope env)
        {
            int i = _level;
            while (i > 0)
            {
                env = env.OuterScope;
                i--;
            }
            env.Set(_index, value);
        }

        public override SExpression Evaluate(IScope env, bool isTail)
        {
            int i = _level;
            while (i > 0)
            {
                env = env.OuterScope;
                i--;
            }
            return env.Get(_index);
        }
    }

    public class WhenForm : ConditionalSequenceFormBase
    {
        public WhenForm(SExpression predicate, ConsCell consequents, IEnvironment env)
            : base("when", predicate, consequents, env)
        {
        }

        protected override bool ShouldEvaluateConsequents(IScope env)
        {
            return !IsPredicateFalse(env);
        }
    }
}
