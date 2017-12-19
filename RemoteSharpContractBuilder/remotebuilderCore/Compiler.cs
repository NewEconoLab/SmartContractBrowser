using httplib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace remotebuilderCore
{
    class Compiler
    {
        public const string verstr = "1.0";
        public async Task<MyJson.IJsonNode> onHelp(FormData input)
        {
            MyJson.JsonNode_Object help = new MyJson.JsonNode_Object();
            help.SetDictValue("tag", 0);
            help.SetDictValue("msg", "Neo C# Http Compiler by lights 😂 " + verstr);
            return help;
        }

        roslynBuilder.RsolynBuilder builder = new roslynBuilder.RsolynBuilder();

        public class NeonResult : Neo.Compiler.ILogger
        {
            public string hash;
            public List<string> logs = new List<string>();
            public void Log(string log)
            {
                logs.Add(log);
            }
            public byte[] avm;
            public string abi;
            public string map;
        }
        NeonResult BuildNeon(byte[] dll, byte[] pdb)
        {
            NeonResult nr = new NeonResult();
            Neo.Compiler.MSIL.ModuleConverter convert = new Neo.Compiler.MSIL.ModuleConverter(nr);
            Neo.Compiler.MSIL.ILModule module = new Neo.Compiler.MSIL.ILModule();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(dll);
            System.IO.MemoryStream mspdb = new System.IO.MemoryStream(pdb);
            module.LoadModule(ms, mspdb);
            var mod = convert.Convert(module);
            nr.avm = mod.Build();
            var abijson = vmtool.FuncExport.Export(mod, nr.avm);
            nr.hash = abijson["hash"].AsString();
            nr.abi = abijson.ToString();
            {//gen debug info
                Neo.Compiler.MyJson.JsonNode_Array arr = new Neo.Compiler.MyJson.JsonNode_Array();
                foreach (var m in convert.outModule.mapMethods)
                {
                    Neo.Compiler.MyJson.JsonNode_Object item = new Neo.Compiler.MyJson.JsonNode_Object();
                    arr.Add(item);
                    item.SetDictValue("name", m.Value.displayName);
                    item.SetDictValue("addr", m.Value.funcaddr.ToString("X04"));
                    Neo.Compiler.MyJson.JsonNode_Array infos = new Neo.Compiler.MyJson.JsonNode_Array();
                    item.SetDictValue("map", infos);
                    foreach (var c in m.Value.body_Codes)
                    {
                        if (c.Value.debugcode != null)
                        {
                            var debugcode = c.Value.debugcode.ToLower();
                            if (debugcode.Contains(".cs"))
                            {
                                infos.AddArrayValue(c.Value.addr.ToString("X04") + "-" + c.Value.debugline.ToString());
                            }
                        }
                    }
                }
                nr.map = arr.ToString();
            }
            return nr;
        }
        //public async Task<MyJson.IJsonNode> onJsonRPC(FormData input)
        //{
        //    string method = null;
        //    double jsonrpc = 0;
        //    MyJson.JsonNode_Array _params = null;
        //    int id = 0;
        //    try
        //    {
        //        method = input.mapParams["method"];
        //        jsonrpc = double.Parse(input.mapParams["jsonrpc"]);
        //        _params = MyJson.Parse(input.mapParams["params"]).AsList() as MyJson.JsonNode_Array;
        //        id = int.Parse(input.mapParams["id"]);
        //    }
        //    catch (Exception err)
        //    {
        //        MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();
        //        item["jsonrpc"] = new MyJson.JsonNode_ValueNumber(2.0);
        //        item["error"] = new MyJson.JsonNode_ValueString("rpc input format error.");
        //        return item;
        //    }
        //    if (method == "compile")
        //    {
        //        MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();
        //        item["jsonrpc"] = new MyJson.JsonNode_ValueNumber(2.0);
        //        item["id"] = new MyJson.JsonNode_ValueNumber(id);
        //        string srccode = null;
        //        MyJson.IJsonNode result = null;
        //        string error = null;
        //        try
        //        {
        //            srccode = _params[0].AsString();
        //        }
        //        catch (Exception err)
        //        {
        //            error = err.Message;
        //        }
        //        if (srccode != null)
        //        {
        //            try
        //            {
        //                result = await compile(srccode);
        //            }
        //            catch (Exception err)
        //            {
        //                error = err.Message;
        //            }
        //        }
        //        if (result != null)
        //        {
        //            item["result"] = result;
        //        }
        //        else
        //        {
        //            item["error"] = new MyJson.JsonNode_ValueString("err in compile." + error);
        //        }
        //        return item;
        //    }
        //    else if (method == "getfile")
        //    {
        //        MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();
        //        item["jsonrpc"] = new MyJson.JsonNode_ValueNumber(2.0);
        //        item["id"] = new MyJson.JsonNode_ValueNumber(id);
        //        string hashid = null;
        //        MyJson.IJsonNode result = null;
        //        string error = null;
        //        try
        //        {
        //            hashid = _params[0].AsString();
        //        }
        //        catch (Exception err)
        //        {
        //            error = err.Message;
        //        }
        //        if (hashid != null)
        //        {
        //            try
        //            {
        //                result = await getfile(hashid);
        //            }
        //            catch (Exception err)
        //            {
        //                error = err.Message;
        //            }
        //        }
        //        if (result != null)
        //        {
        //            item["result"] = result;
        //        }
        //        else
        //        {
        //            item["error"] = new MyJson.JsonNode_ValueString("err in getfile." + error);
        //        }
        //        return item;
        //    }
        //    else
        //    {
        //        MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();
        //        item["jsonrpc"] = new MyJson.JsonNode_ValueNumber(2.0);
        //        item["id"] = new MyJson.JsonNode_ValueNumber(id);
        //        item["error"] = new MyJson.JsonNode_ValueString("rpc method not exist.");
        //        return item;
        //    }
        //}
        public async Task<MyJson.IJsonNode> onCompile(FormData input)
        {
            MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();

            var src = System.Text.Encoding.UTF8.GetString(input.mapFiles["file"]);
            roslynBuilder.buildResult result = await builder.buildSrc(src, "temp");

            //编译成功
            if (result.dll != null && result.dll.Length > 0)
            {
                var neonr = BuildNeon(result.dll, result.pdb);

            }
            else
            {
                item.SetDictValue("tag", -3);
                item.SetDictValue("msg", "compile fail.");
                MyJson.JsonNode_Array errors = new MyJson.JsonNode_Array();
                item.SetDictValue("errors", errors);
                foreach (var err in result.errors)
                {
                    var jsonitem = new MyJson.JsonNode_Object();
                    jsonitem.SetDictValue("tag", "error");
                    jsonitem.SetDictValue("msg", err.msg);
                    jsonitem.SetDictValue("id", err.id);
                    jsonitem.SetDictValue("line", err.line);
                    jsonitem.SetDictValue("col", err.col);
                    errors.Add(jsonitem);
                }
                foreach (var err in result.warnings)
                {
                    var jsonitem = new MyJson.JsonNode_Object();
                    jsonitem.SetDictValue("tag", "warn");
                    jsonitem.SetDictValue("msg", err.msg);
                    jsonitem.SetDictValue("id", err.id);
                    jsonitem.SetDictValue("line", err.line);
                    jsonitem.SetDictValue("col", err.col);
                    errors.Add(jsonitem);
                }
            }
            return item;
        }
        //{"tag":0,"hex":"00C56B51616C7566","hash":"8CC00DB1FC4D513520D313DF1A600E4CE6CCA661A37833B5264726A8A449CD09","funcsigns":{"A::Main":{"name":"A::Main","returntype":"number","params":[]
        //{"tag":-3,"msg":"compile fail.","errors":[{"msg":"未能找到类型或命名空间名称“in3t”(是否缺少 using 指令或程序集引用?)","line":7,"col":19,"tag":"错误"}]}
    }
}
