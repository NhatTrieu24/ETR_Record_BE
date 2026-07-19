using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\nguye\.nuget\packages\swashbuckle.aspnetcore.swaggergen\10.2.3\lib\net10.0\Swashbuckle.AspNetCore.SwaggerGen.dll");
        var t = asm.GetTypes().FirstOrDefault(x => x.Name == "SwaggerGenOptions");
        if(t != null) {
            foreach(var m in t.GetMethods().Where(m => m.Name == "AddSecurityRequirement")) {
                Console.WriteLine(m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
            }
        }
    }
}
