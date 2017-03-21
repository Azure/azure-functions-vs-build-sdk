using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MakeFunctionJson;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            FunctionJsonConvert.Convert(args[0], args[1]);
        }
    }
}
