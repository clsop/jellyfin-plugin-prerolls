using System.Linq;
using System.Reflection;

using Moq;

public class TestHarness<T> where T: class
{
    public TestHarness()
    {
        // TODO: reflect find contructor injected interfaces
        var type = GetType();
        
        foreach (var ctor in type.GetConstructors(BindingFlags.Public))
        {
            var interfaces = ctor.GetGenericArguments().Where(x => x.IsInterface);
            //var interfaceMock = Mock.
        }
    }
}