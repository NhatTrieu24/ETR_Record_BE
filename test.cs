using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\nguye\.nuget\packages\microsoft.openapi\2.7.5\lib\net10.0\Microsoft.OpenApi.dll");
        var t = asm.GetTypes().FirstOrDefault(x => x.Name == "OpenApiSecurityScheme");
        if(t != null) {
            foreach(var p in t.GetProperties()) Console.WriteLine(p.Name);
        }
    }
}
