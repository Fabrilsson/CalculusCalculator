using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaximaSharp
{
    public static class Maxima
    {
        private static List<string> _functions = new List<string> { "pow(x, y) := x^y" };

        private static StringBuilder _output { get; set; }

        public static string Simplify(this string expr)
        {
            return EvalToExpression("factor(fullratsimp(trigsimp({0})))", expr);
        }

        public static string Differentiate(this string expr, string wrt = "x")
        {
            return EvalToExpression("diff({0}, {1})", expr, wrt);
        }

        public static string Integrate(this string expr, string wrt = "x")
        {
            return EvalToExpression("integrate({0}, {1})", expr, wrt);
        }

        public static string Integrate(this string expr, int from, int to, string wrt = "x")
        {
            return EvalToExpression("integrate({0}, {1}, {2}, {3})", expr, wrt, from, to);
        }

        public static LambdaExpression Plus(this LambdaExpression f, LambdaExpression g)
        {
            return Expression.Lambda(Expression.Add(f.Body, g.Body), f.Parameters.Union(g.Parameters, new ParameterEqualityComparer()));
        }

        public static LambdaExpression Minus(this LambdaExpression f, LambdaExpression g)
        {
            return Expression.Lambda(Expression.Subtract(f.Body, g.Body), f.Parameters.Union(g.Parameters, new ParameterEqualityComparer()));
        }

        public static LambdaExpression Times(this LambdaExpression f, LambdaExpression g)
        {
            return Expression.Lambda(Expression.Multiply(f.Body, g.Body), f.Parameters.Union(g.Parameters, new ParameterEqualityComparer()));
        }

        public static LambdaExpression Over(this LambdaExpression f, LambdaExpression g)
        {
            return Expression.Lambda(Expression.Divide(f.Body, g.Body), f.Parameters.Union(g.Parameters, new ParameterEqualityComparer()));
        }

        public static object At(this LambdaExpression f, params object[] args)
        {
            return f.Compile().DynamicInvoke(args);
        }

        public static void GnuPlot(string s)
        {
            var gnuplot = Process.Start(new ProcessStartInfo(Path.Combine(Config.FunctionDirectory, @"..\..\..\..\lib\Maxima-5.30.0\gnuplot\gnuplot.exe"))
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
            gnuplot.StandardInput.WriteLine(s);
        }

        private static string EvalToExpression(string format, params object[] args)
        {
            return Eval(string.Format(format, args));
        }

        private static string Eval(string expr)
        {
            var process = NewMaxima();

            _output = new StringBuilder();

            process.StandardInput.WriteLine(string.Format("{0}$ grind({1});", string.Join("$", _functions), expr.ToLower()));
            var result = process.StandardOutput.ReadLine();

            while (!result.EndsWith("$"))
                result += process.StandardOutput.ReadLine();

            process.StandardOutput.ReadLine();

            if (result.EndsWith("$")) return result.TrimEnd('$');

            throw new Exception(string.Format("Unexpected result: {0}", result));
        }

        private static Process NewMaxima()
        {
            var process = new Process();

            var startInfo = new ProcessStartInfo(Path.GetFullPath(Path.Combine(Config.FunctionDirectory, @"..\..\..\..\lib\Maxima-5.30.0\lib\maxima\" +
                @"5.30.0\binary-gcl\maxima.exe")), @"-eval ""(cl-user::run)"" -f -- -very-quiet")
            {
                WorkingDirectory = Path.GetFullPath(Path.Combine(Config.FunctionDirectory, @"..\..\..\..\lib\Maxima-5.30.0\bin\")),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.EnvironmentVariables.Add("maxima_prefix", Path.GetFullPath(Path.Combine(Config.FunctionDirectory, @"..\..\..\..\lib\Maxima-5.30.0")));

            process.StartInfo = startInfo;

            process.Start();

            return process;
        }

        private class ParameterEqualityComparer : IEqualityComparer<ParameterExpression>
        {
            public bool Equals(ParameterExpression x, ParameterExpression y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(ParameterExpression obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
