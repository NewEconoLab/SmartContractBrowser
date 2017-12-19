using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

public class Contract : SmartContract
{
    public static event System.Action<int> aaa;
    public static void Main()
    {
        Storage.Put(Storage.CurrentContext, "Hello", "World");
    }
}