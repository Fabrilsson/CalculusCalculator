using Microsoft.VisualBasic;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace MaximaSharp
{
    public static class Maxima
    {
        public static List<string> Functions = new List<string> { "pow(x, y) := x^y" };
        private static Process Process = NewMaxima();

        private static Process NewMaxima()
        {
            if (Process != null) Process.Dispose();
            var startInfo = new ProcessStartInfo(Path.GetFullPath(Path.Combine(Config.FunctionDirectory, "..\\..\\..\\..\\lib\\Maxima-5.30.0\\lib\\maxima\\" +
                "5.30.0\\binary-gcl\\maxima.exe")), @"-eval ""(cl-user::run)"" -f -- -very-quiet")
            {
                WorkingDirectory = Path.GetFullPath(Path.Combine(Config.FunctionDirectory, "..\\..\\..\\..\\lib\\Maxima-5.30.0\\bin\\")),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.EnvironmentVariables.Add("maxima_prefix", Path.GetFullPath(Path.Combine(Config.FunctionDirectory, "..\\..\\..\\..\\lib\\Maxima-5.30.0")));
            return Process.Start(startInfo);
        }

        public static string Eval(string expr)
        {

            Process.StandardInput.WriteLine(string.Format("{0}$ grind({1});", string.Join("$", Functions), expr.ToLower()));
            var result = Process.StandardOutput.ReadLine();
            while (!result.EndsWith("$"))
                result += Process.StandardOutput.ReadLine();
            Process.StandardOutput.ReadLine();
            if (result.EndsWith("$")) return result.TrimEnd('$');
            Process = NewMaxima();
            throw new Exception(string.Format("Unexpected result: {0}", result));
        }

        public static LambdaExpression ToExpression(string types, string parameters, string code) //System.CodeDom Fails
        {
            try
            {
                return VBCodeProvider.CreateProvider("VB", new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } })
                    .CompileAssemblyFromSource(new CompilerParameters(new[] { "System.Core.dll" }), string.Format(
               @"   Imports System
                    Imports System.Linq.Expressions
                    Public Class Program 
                        Public Shared Lambda As Expression(Of Func(Of {0})) = Function({1}) {2}
                    End Class
                ", types, parameters, code.Replace("log", "Math.Log").Replace("sin", "Math.Sin").Replace("cos", "Math.Cos")))
                    .CompiledAssembly.GetType("Program").GetField("Lambda").GetValue(null) as LambdaExpression;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to convert to expression: {0}", code), ex);
            }
        }

        private static string EvalToExpression(string format, params object[] args)
        {
            return Eval(string.Format(format, args));
        }

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
            var gnuplot = Process.Start(new ProcessStartInfo(@"C:\Users\fabricio.sonego\Documents\CalculusCalculator\bin\Maxima-5.30.0\gnuplot\gnuplot.exe")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
            gnuplot.StandardInput.WriteLine(s);
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
