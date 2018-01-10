using httplib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

namespace remotebuilderCore
{
    class Compiler
    {
        public const string verstr = "1.0";
        public const string outputpath = "output";
        public async Task<string> onHelp(FormData input)
        {
            MyJson.JsonNode_Object help = new MyJson.JsonNode_Object();
            help.SetDictValue("tag", 0);
            help.SetDictValue("msg", "Neo C# Http Compiler by lights 😂 " + verstr);
            return help.ToString();
        }

        CodeDomBuilder builder = new CodeDomBuilder();

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
            class LockObj
            {

            }
            LockObj lockobj = new LockObj();
            public void Save(string outpath, string srccode)
            {
                lock (lockobj)
                {
                    string path = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
                    path = System.IO.Path.Combine(path, outpath);
                    if (System.IO.Directory.Exists(path) == false)
                        System.IO.Directory.CreateDirectory(path);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(path, hash + ".avm"), this.avm);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(path, hash + ".cs"), srccode);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(path, hash + ".map.json"), this.map);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(path, hash + ".abi.json"), this.abi);
                }
            }
        }
        public static string ToHexString(IEnumerable<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
        void BuildNeon(byte[] dll, byte[] pdb, NeonResult result)
        {
            NeonResult nr = result;
            Neo.Compiler.MSIL.ModuleConverter convert = new Neo.Compiler.MSIL.ModuleConverter(nr);
            Neo.Compiler.MSIL.ILModule module = new Neo.Compiler.MSIL.ILModule();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(dll);
            System.IO.MemoryStream mspdb = new System.IO.MemoryStream(pdb);
            module.LoadModule(ms, mspdb);
            var mod = convert.Convert(module);
            var avm = mod.Build();
            nr.avm = avm;
            var abijson = vmtool.FuncExport.Export(mod, avm);
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
        public async Task<string> onCompile(FormData input)
        {
            Console.WriteLine("docomplile:" + DateTime.Now.ToLongTimeString());
            MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();

            var src = System.Text.Encoding.UTF8.GetString(input.mapFiles["file"]);
            CodeDomBuilder.buildResult result = null;
            try
            {
                result = builder.buildSrc(src, "temp");
            }
            catch (Exception err)
            {
                item.SetDictValue("tag", -3);
                item.SetDictValue("msg", "compile unknown error.");
                MyJson.JsonNode_Array errors = new MyJson.JsonNode_Array();
                item.SetDictValue("errors", errors);
                var jsonitem = new MyJson.JsonNode_Object();
                jsonitem.SetDictValue("tag", "error");
                jsonitem.SetDictValue("msg", err.Message);
                jsonitem.SetDictValue("id", err.HResult.ToString("X02"));
                errors.Add(jsonitem);
                Console.WriteLine("docomplile -3:");
                return item.ToString();
            }
            //编译成功
            if (result.dll != null && result.dll.Length > 0)
            {
                NeonResult neonr = new NeonResult();
                try
                {
                    BuildNeon(result.dll, result.pdb, neonr);
                }
                catch (Exception err)
                {
                    item.SetDictValue("tag", -2);
                    item.SetDictValue("msg", "neon compile error.");
                    MyJson.JsonNode_Array errors = new MyJson.JsonNode_Array();
                    item.SetDictValue("errors", errors);
                    var _jsonitem = new MyJson.JsonNode_Object();
                    _jsonitem.SetDictValue("tag", "error");
                    _jsonitem.SetDictValue("msg", err.Message);
                    _jsonitem.SetDictValue("id", err.HResult.ToString("X02"));
                    errors.Add(_jsonitem);
                    foreach (var log in neonr.logs)
                    {
                        var jsonitem = new MyJson.JsonNode_Object();
                        jsonitem.SetDictValue("tag", "log");
                        jsonitem.SetDictValue("msg", log);
                        jsonitem.SetDictValue("id", "0");
                        errors.Add(jsonitem);
                    }
                    Console.WriteLine("docomplile -2:");
                    return item.ToString();
                }
                try
                {
                    neonr.Save(outputpath, src);
                    item.SetDictValue("tag", 0);
                    item.SetDictValue("hash", neonr.hash);
                    item.SetDictValue("hex", ToHexString(neonr.avm));
                    item.SetDictValue("funcsigns", neonr.abi);
                }
                catch (Exception err)
                {
                    item.SetDictValue("tag", -1);
                    item.SetDictValue("msg", "save error.");
                    MyJson.JsonNode_Array errors = new MyJson.JsonNode_Array();
                    item.SetDictValue("errors", errors);
                    var jsonitem = new MyJson.JsonNode_Object();
                    jsonitem.SetDictValue("tag", "error");
                    jsonitem.SetDictValue("msg", err.Message);
                    jsonitem.SetDictValue("id", err.HResult.ToString("X02"));
                    errors.Add(jsonitem);
                    Console.WriteLine("docomplile -1:");
                    return item.ToString();
                }
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
                Console.WriteLine("docomplile -3:");

                return item.ToString();
            }
            Console.WriteLine("docomplile succ:");

            try
            {
                //编译结果入库mongo
                mongoHelper mh = new mongoHelper();
                string hash = item["hash"].ToString();
                JObject J = JObject.Parse(getFile2Json(hash));
                J.Add("hash", hash);
                string requestIP = string.Empty;
                if (input.mapParams["requestIP"] != null)
                {
                    requestIP = input.mapParams["requestIP"];
                }
                J.Add("requestIP", requestIP);
                J["cs"] = HttpUtility.UrlDecode((string)J["cs"]);
                J["map"] = JArray.Parse(HttpUtility.UrlDecode((string)J["map"]));
                J["abi"] = JObject.Parse(HttpUtility.UrlDecode((string)J["abi"]));
                mh.insertJson2MongoOnlyonce(JsonConvert.SerializeObject(J),hash);
            }
            catch (Exception e)
            {
                var a = e;
            }

            return item.ToString();
        }
        //{"tag":0,"hex":"00C56B51616C7566","hash":"8CC00DB1FC4D513520D313DF1A600E4CE6CCA661A37833B5264726A8A449CD09","funcsigns":{"A::Main":{"name":"A::Main","returntype":"number","params":[]
        //{"tag":-3,"msg":"compile fail.","errors":[{"msg":"未能找到类型或命名空间名称“in3t”(是否缺少 using 指令或程序集引用?)","line":7,"col":19,"tag":"错误"}]}
        public async Task<string> onGetfile(FormData input)
        {
            var hash = input.mapParams["hash"];
            return getFile2Json(hash);

            //if (hash.IndexOf("0x") == -1)
            //{
            //    hash = "0x" + hash;
            //}
            //MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            //string path = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            //path = System.IO.Path.Combine(path, outputpath);
            //if (System.IO.Directory.Exists(path) == false)
            //    System.IO.Directory.CreateDirectory(path);

            //string pathAvm = System.IO.Path.Combine(path, hash + ".avm");
            //string pathCs = System.IO.Path.Combine(path, hash + ".cs");
            //string pathMap = System.IO.Path.Combine(path, hash + ".map.json");
            //string pathAbi = System.IO.Path.Combine(path, hash + ".abi.json");

            //if (System.IO.File.Exists(pathAvm))
            //{
            //    var bts = System.IO.File.ReadAllBytes(pathAvm);
            //    json.SetDictValue("avm", ToHexString(bts));
            //}
            //if (System.IO.File.Exists(pathCs))
            //{
            //    var txt = System.IO.File.ReadAllText(pathCs);
            //    txt = Uri.EscapeDataString(txt);
            //    json.SetDictValue("cs", txt);
            //}
            //if (System.IO.File.Exists(pathMap))
            //{
            //    var txt = System.IO.File.ReadAllText(pathMap);
            //    txt = Uri.EscapeDataString(txt);
            //    json.SetDictValue("map", txt);
            //}
            //if (System.IO.File.Exists(pathCs))
            //{
            //    var txt = System.IO.File.ReadAllText(pathAbi);
            //    txt = Uri.EscapeDataString(txt);
            //    json.SetDictValue("abi", txt);
            //}

            //return json.ToString();
        }

        private string getFile2Json(string hash)
        {
            if (hash.IndexOf("0x") == -1)
            {
                hash = "0x" + hash;
            }
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            string path = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            path = System.IO.Path.Combine(path, outputpath);
            if (System.IO.Directory.Exists(path) == false)
                System.IO.Directory.CreateDirectory(path);

            string pathAvm = System.IO.Path.Combine(path, hash + ".avm");
            string pathCs = System.IO.Path.Combine(path, hash + ".cs");
            string pathMap = System.IO.Path.Combine(path, hash + ".map.json");
            string pathAbi = System.IO.Path.Combine(path, hash + ".abi.json");

            if (System.IO.File.Exists(pathAvm))
            {
                var bts = System.IO.File.ReadAllBytes(pathAvm);
                json.SetDictValue("avm", ToHexString(bts));
            }
            if (System.IO.File.Exists(pathCs))
            {
                var txt = System.IO.File.ReadAllText(pathCs);
                txt = Uri.EscapeDataString(txt);
                json.SetDictValue("cs", txt);
            }
            if (System.IO.File.Exists(pathMap))
            {
                var txt = System.IO.File.ReadAllText(pathMap);
                txt = Uri.EscapeDataString(txt);
                json.SetDictValue("map", txt);
            }
            if (System.IO.File.Exists(pathCs))
            {
                var txt = System.IO.File.ReadAllText(pathAbi);
                txt = Uri.EscapeDataString(txt);
                json.SetDictValue("abi", txt);
            }

            return json.ToString();
        }
    }
}
