using System;
using System.Linq;
using System.Threading;

namespace OneSecond.CatchValidation
{
    interface IExample
    {
        void Run();
    }

    interface IResult
    {
        void ShowResult();
    }

    class Context : IResult
    {
        public long Counter { get; set; } = 0;
        public string Name { get; set; }
        public void ShowResult()
        {
            Console.WriteLine($"{Name} : {Counter}");
        }
    }

    class OneSecondRunnerService<T>
        where T:class,IResult
    {
        public void Run(Action<T> task, T ctx)
        {
            using (var reset = new ManualResetEvent(false))
            {
                var thread = new Thread((c) => { task(ctx); });
                thread.IsBackground = true;
                thread.Start(ctx);
                reset.WaitOne(TimeSpan.FromMilliseconds(1000));
                thread.Abort();
            }
            ctx.ShowResult();
        }
    }

    class MultiTierExecutor : IExample
    {
        public class MultiTierExecutorException : Exception
        {
            public MultiTierExecutorException(string message, Exception innerException)
                :base(message,innerException)
            {
            }
        }

        interface IExecutor<T>
        {
            T Execute();
        }
        class SnailExecutor<T> : IExecutor<T>
        {
            private readonly Func<T> _func;

            public SnailExecutor(Func<T> func)
            {
                if (func == null)
                    throw new ArgumentNullException("func");
                _func = func;
            }
            public T Execute()
            {
                try
                {
                    return _func();
                }
                catch(Exception ex)
                {
                    throw new MultiTierExecutorException("repacked", ex);
                }
            }
        }
        interface ICheetahExecutor<T>
        {
            Tuple<string,T> Execute();
        }
        class CheetahExecutor<T> : ICheetahExecutor<T>
        {
            private readonly Func<T> _func;
            public CheetahExecutor(Func<T> func)
            {
                _func = func;
            }

            public Tuple<string, T> Execute()
            {
                try
                {
                    if(_func == null)
                        return new Tuple<string, T>("Null func", default(T));
                    return new Tuple<string,T>("OK",_func());
                }
                catch (Exception ex)
                {
                    return new Tuple<string, T>(ex.Message, default(T));
                }
            }
        }

        public void Run()
        {
            var runner = new OneSecondRunnerService<Context>();
            var ctx01 = new Context { Name = "SnailExecutor null argument" };
            var ctx02 = new Context { Name = "SnailExecutor execute exception" };
            var ctx03 = new Context { Name = "CheetahExecutor null argument" };
            var ctx04 = new Context { Name = "CheetahExecutor execute exception" };
            var ctx05 = new Context { Name = "CheetahExecutor execute" };
            var ctx06 = new Context { Name = "2 tier SnailExecutor execute exception" };
            var ctx07 = new Context { Name = "2 tier CheetahExecutor execute exception" };
            runner.Run(RunNullSnail, ctx01);
            runner.Run(RunExecuteExceptionSnail, ctx02);
            runner.Run(RunNullCheetah, ctx03);
            runner.Run(RunExecuteExceptionCheetah, ctx04);
            runner.Run(RunExecuteCheetah, ctx05);
            runner.Run(RunExecuteExceptionSnail2Tier, ctx06);
            runner.Run(RunExecuteExceptionCheetah2Tier, ctx07);
        }
        private string FuncToExecute()
        {
            return "executed";
        }
        private string FuncToExecuteWithException()
        {
            throw new Exception("sample");
        }
        private string CallProxy(IExecutor<string> proxy)
        {
            try
            {
                return proxy.Execute();
            }
            catch(Exception ex)
            {
                throw new MultiTierExecutorException("proxy repacked", ex);
            }
        }
        private Tuple<string, string> CallProxy(ICheetahExecutor<string> proxy)
        {
            try
            {
                return proxy.Execute();
            }
            catch(Exception ex)
            {
                return new Tuple<string, string>(ex.Message, default(string));
            }
        }
        private void RunNullSnail(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                try
                {
                    var executor = new SnailExecutor<string>(null);
                    var result = executor.Execute();
                }
                catch { ctx.Counter++; }
            }
        }
        private void RunExecuteExceptionSnail(Context ctx)
        {
            for (long i = 0; ; i++)
            {
                try
                {
                    var executor = new SnailExecutor<string>(FuncToExecuteWithException);
                    var result = executor.Execute();
                }
                catch { ctx.Counter++; }
            }
        }
        private void RunNullCheetah(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                try
                {
                    var executor = new CheetahExecutor<string>(null);
                    var result = executor.Execute();
                    ctx.Counter++;
                }
                catch {  }
            }
        }
        private void RunExecuteExceptionCheetah(Context ctx)
        {
            for (long i = 0; ; i++)
            {
                try
                {
                    var executor = new CheetahExecutor<string>(FuncToExecuteWithException);
                    var result = executor.Execute();
                    ctx.Counter++;
                }
                catch {  }
            }
        }
        private void RunExecuteCheetah(Context ctx)
        {
            for (long i = 0; ; i++)
            {
                try
                {
                    var executor = new CheetahExecutor<string>(FuncToExecute);
                    var result = executor.Execute();
                    ctx.Counter++;
                }
                catch {  }
            }
        }
        private void RunExecuteExceptionSnail2Tier(Context ctx)
        {
            for (long i = 0; ; i++)
            {
                try
                {
                    var proxy = new SnailExecutor<string>(FuncToExecuteWithException);
                    var executor = new SnailExecutor<string>(()=>CallProxy(proxy));
                    var result = executor.Execute();
                }
                catch { ctx.Counter++; }
            }
        }
        private void RunExecuteExceptionCheetah2Tier(Context ctx)
        {
            for (long i = 0; ; i++)
            {
                try
                {
                    var proxy = new CheetahExecutor<string>(FuncToExecuteWithException);
                    var executor = new CheetahExecutor<string>(() => {
                        var res = CallProxy(proxy);
                        return res.Item1 == "OK" ? res.Item2 : res.Item1;
                    });
                    var result = executor.Execute();
                    ctx.Counter++;
                }
                catch {  }
            }
        }
    }

    class DateTimeParsersExample : IExample
    {
        public void Run()
        {
            var runner = new OneSecondRunnerService<Context>();
            var ctx01 = new Context { Name = "Parse        dates" };
            var ctx02 = new Context { Name = "Parse catch  dates" };
            var ctx03 = new Context { Name = "TryParse     dates" };
            var ctx04 = new Context { Name = "Parse    bad dates" };
            var ctx05 = new Context { Name = "TryParse bad dates" };
            runner.Run(ParseDates, ctx01);
            runner.Run(ParseCatchDates, ctx02);
            runner.Run(TryParseDates, ctx03);
            runner.Run(ParseBadDates, ctx04);
            runner.Run(TryParseBadDates, ctx05);
        }
        private void ParseDates(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "2018-03-23";
                var result = DateTime.Parse(str);
                ctx.Counter++;
            }
        }
        private void ParseCatchDates(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "2018-03-23";
                try
                {
                    var result = DateTime.Parse(str);
                    ctx.Counter++;
                }
                catch { }
            }
        }
        private void TryParseDates(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "2018-03-23";
                DateTime result;
                if(DateTime.TryParse(str, out result))
                    ctx.Counter++;
            }
        }
        private void ParseBadDates(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "2018-03-33";// 33 of march
                try
                {
                    var result = DateTime.Parse(str);
                }
                catch { ctx.Counter++; }
            }
        }
        private void TryParseBadDates(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "2018-03-33";// 33 of march
                DateTime result;
                if(!DateTime.TryParse(str, out result))
                    ctx.Counter++;
            }
        }
    }

    class LongParsersExample : IExample
    {
        public void Run()
        {
            var runner = new OneSecondRunnerService<Context>();
            var ctx01 = new Context { Name = "Parse        numbers" };
            var ctx02 = new Context { Name = "Parse catch  numbers" };
            var ctx03 = new Context { Name = "TryParse     numbers" };
            var ctx04 = new Context { Name = "Parse    bad numbers" };
            var ctx05 = new Context { Name = "TryParse bad numbers" };
            runner.Run(ParseNumbers, ctx01);
            runner.Run(ParseCatchNumbers, ctx02);
            runner.Run(TryParseNumbers, ctx03);
            runner.Run(ParseBadNumbers, ctx04);
            runner.Run(TryParseBadNumbers, ctx05);
        }
        private void ParseNumbers(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "" + i;
                var result = long.Parse(str);
                ctx.Counter++;
            }
        }
        private void ParseCatchNumbers(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "" + i;
                try
                {
                    var result = long.Parse(str);
                    ctx.Counter++;
                }
                catch { }
            }
        }
        private void TryParseNumbers(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "" + i;
                long result;
                if(long.TryParse(str, out result))
                    ctx.Counter++;
            }
        }
        private void ParseBadNumbers(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "x" + i;
                try
                {
                    var result = long.Parse(str);
                }
                catch
                {
                    ctx.Counter++;
                }
            }
        }
        private void TryParseBadNumbers(Context ctx)
        {
            for(long i = 0; ; i++)
            {
                var str = "x" + i;
                long result;
                if(!long.TryParse(str, out result))
                    ctx.Counter++;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("start");

            //var examples = new IExample[] { new LongParsersExample(), new DateTimeParsersExample() };
            var examples = new IExample[] { new MultiTierExecutor() };
            examples.ToList().ForEach(example=>example.Run());

            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}
