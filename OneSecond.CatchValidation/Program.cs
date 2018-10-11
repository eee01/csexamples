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

            var examples = new IExample[] { new LongParsersExample() };
            examples.ToList().ForEach(example=>example.Run());

            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}
