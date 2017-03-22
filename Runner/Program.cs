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
