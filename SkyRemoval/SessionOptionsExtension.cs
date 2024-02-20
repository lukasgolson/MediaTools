using System.Reflection;
using Microsoft.ML.OnnxRuntime;

namespace SkyRemoval;

public static class SessionOptionsExtension
{
    private static void InvokePrivate<T>(string methodName) where T : new()
    {
        var method = typeof(T).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        
        if (method == null)
        {
            throw new MissingMethodException($"Method {methodName} not found in {typeof(T).FullName}");
        }
        method.Invoke(new T(), null);
    }
    
    public static void CheckTensorrtExecutionProviderDLLs(this SessionOptions sessionOptions)
    {
        InvokePrivate<SessionOptions>("CheckTensorrtExecutionProviderDLLs");
    }
    
    public static void CheckCudaExecutionProviderDLLs(this SessionOptions sessionOptions)
    {
        InvokePrivate<SessionOptions>("CheckCudaExecutionProviderDLLs");
    }
}