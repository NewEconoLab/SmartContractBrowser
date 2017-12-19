//using Neo.Compiler;
//using Neo.Compiler.JVM;
//using Neo.Compiler.MSIL;
//using Microsoft.Owin;
//using remotegen;
//using System;
//using System.CodeDom.Compiler;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LitServer
//{
//    public class AntGateway : CustomServer.IParser
//    {
//        public const string ver = "0.070😂";

//        public async Task HandleRequest(IOwinContext context, string rootpath, string relativePath)
//        {
//            var api = relativePath.ToLower();
//            var formdata = await FormData.FromRequest(context.Request);
//            if (formdata == null)
//            {
//                context.Response.StatusCode = 500;
//                context.Response.ContentType = "text/plain";
//                MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                json["msg"] = new MyJson.JsonNode_ValueString("formdata format error.");
//                json["tag"] = new MyJson.JsonNode_ValueNumber(-1);
//                await context.Response.WriteAsync(json.ToString());
//                return;
//            }

//            if (relativePath == "parse")
//            {
//                await parse(context, formdata);
//                return;
//            }

//            else
//            {
//                await help(context, formdata);
//                return;
//            }


//        }
//        private static async Task help(IOwinContext context, FormData formdata)
//        {
//            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//            json["tag"] = new MyJson.JsonNode_ValueNumber(0);
//            MyJson.JsonNode_Array maps = new MyJson.JsonNode_Array();
//            json.SetDictValue("msg", "AntShares Http Gate By lights " + AntGateway.ver);
//            await context.Response.WriteAsync(json.ToString());
//            return;
//        }

//        public class Log2Json : ILogger
//        {
//            public MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();

//            public void Log(string log)
//            {
//                array.Add(new MyJson.JsonNode_ValueString(log));
//            }
//        }
//        private static async Task parse(IOwinContext context, FormData formdata)
//        {

//            if (formdata.mapParams.ContainsKey("language") && formdata.mapFiles.ContainsKey("file"))
//            {
//                var l = formdata.mapParams["language"].ToLower();
//                if (l == "csharp")
//                    await parseCSharp(context, formdata);
//                else if (l == "java")
//                    await parseJAVA(context, formdata);
//                else
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-5);
//                    json["msg"] = new MyJson.JsonNode_ValueString("fail language.");
//                    await context.Response.WriteAsync(json.ToString());
//                }
//            }
//            else
//            {
//                MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                json["tag"] = new MyJson.JsonNode_ValueNumber(-1);
//                json["msg"] = new MyJson.JsonNode_ValueString("need param: language & file.");
//                await context.Response.WriteAsync(json.ToString());
//                return;
//            }

//        }
//        static string ConvCSharpType(string csharpType)
//        {
//            switch (csharpType)
//            {
//                case "System.Void":
//                    return "void";
//                case "System.Object[]":
//                    return "any[]";
//                case "System.Object":
//                    return "any";
//                case "System.String":
//                    return "string";
//                case "System.Byte":
//                case "System.SByte":
//                case "System.Int16":
//                case "System.UInt16":
//                case "System.Int32":
//                case "System.UInt32":
//                case "System.Int64":
//                case "System.UInt64":
//                case "System.Numerics.BigInteger":
//                    return "number";
//                case "System.Boolean":
//                    return "bool";
//                case "System.Byte[]":
//                    return "bytearray";
//            }
//            if (csharpType == "System.Object")
//                return "any";
//            if (csharpType == "System.Int32")
//                return "number";
//            return "Unknown:" + csharpType;
//        }
//        private static async Task parseCSharp(IOwinContext context, FormData formdata)
//        {
//            try
//            {
//                var file = formdata.mapFiles["file"];
//                var code = System.Text.Encoding.UTF8.GetString(file);

//                //编译
//                List<string> codes = new List<string>();
//                codes.Add(code);
//                CompilerResults r = null;

//                try
//                {
//                    r = gencode.GenCode(codes, true);
//                    if (r.Errors.Count > 0)
//                    {
//                        MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                        json["tag"] = new MyJson.JsonNode_ValueNumber(-3);
//                        json["msg"] = new MyJson.JsonNode_ValueString("compile fail.");
//                        MyJson.JsonNode_Array errs = new MyJson.JsonNode_Array();
//                        json["errors"] = errs;
//                        for (var i = 0; i < r.Errors.Count; i++)
//                        {
//                            MyJson.JsonNode_Object errtag = new MyJson.JsonNode_Object();
//                            errs.Add(errtag);
//                            errtag.SetDictValue("msg", r.Errors[i].ErrorText);
//                            errtag.SetDictValue("line", r.Errors[i].Line);
//                            errtag.SetDictValue("col", r.Errors[i].Column);
//                            errtag.SetDictValue("tag", r.Errors[i].IsWarning ? "警告" : "错误");
//                        }
//                        await context.Response.WriteAsync(json.ToString());
//                        return;
//                    }
//                }
//                catch (Exception err)
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
//                    json["msg"] = new MyJson.JsonNode_ValueString("unknown fail on comp.");
//                    json["err"] = new MyJson.JsonNode_ValueString(err.ToString());
//                    await context.Response.WriteAsync(json.ToString());
//                    return;
//                }
//                //conv
//                try
//                {
//                    var st = System.IO.File.OpenRead(r.PathToAssembly);
//                    using (st)
//                    {
//                        var logjson = new Log2Json();

//                        var module = new ILModule();
//                        module.LoadModule(st, null);

//                        var conv = new Neo.Compiler.MSIL.ModuleConverter(logjson);
//                        Dictionary<AntsMethod, ILMethod> revert = new Dictionary<AntsMethod, ILMethod>();
//                        foreach (var item in conv.methodLink)
//                        {
//                            revert[item.Value] = item.Key;
//                        }
//                        var neomd = conv.Convert(module);
//                        //var mm = neomd.mapMethods[neomd.mainMethod];


//                        var bs = neomd.Build();
//                        if (bs != null)
//                        {

//                            {
//                                MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                                json["tag"] = new MyJson.JsonNode_ValueNumber(0);
//                                StringBuilder sb = new StringBuilder();
//                                StringBuilder sb2 = new StringBuilder();
//                                var hash = System.Security.Cryptography.SHA256.Create();
//                                var hashbs = hash.ComputeHash(bs);
//                                foreach (var b in bs)
//                                {
//                                    sb.Append(b.ToString("X02"));
//                                }
//                                foreach (var b in hashbs)
//                                {
//                                    sb2.Append(b.ToString("X02"));
//                                }
//                                json["hex"] = new MyJson.JsonNode_ValueString(sb.ToString());
//                                json["hash"] = new MyJson.JsonNode_ValueString(sb2.ToString());

//                                var funcsigns = new MyJson.JsonNode_Object();
//                                json["funcsigns"] = funcsigns;
//                                foreach (var function in neomd.mapMethods)
//                                {
//                                    var mm = function.Value;
//                                    if (mm.isPublic == false)
//                                        continue;
//                                    var ps = mm.name.Split(new char[] { ' ', '(' }, StringSplitOptions.RemoveEmptyEntries);
//                                    var funcsign = new MyJson.JsonNode_Object();
//                                    funcsigns[ps[1]] = funcsign;
//                                    funcsign.SetDictValue("name", ps[1]);
//                                    var rtype = ConvCSharpType(mm.returntype);
//                                    funcsign.SetDictValue("returntype", rtype);
//                                    MyJson.JsonNode_Array funcparams = new MyJson.JsonNode_Array();
//                                    funcsign["params"] = funcparams;
//                                    if (mm.paramtypes != null)
//                                    {
//                                        foreach (var v in mm.paramtypes)
//                                        {
//                                            var ptype = ConvCSharpType(v.type);
//                                            var item = new MyJson.JsonNode_Object();
//                                            funcparams.Add(item);
//                                            item.SetDictValue("name", v.name);
//                                            item.SetDictValue("type", ptype);
//                                        }
//                                    }
//                                }
//                                await context.Response.WriteAsync(json.ToString());
//                                return;
//                            }
//                        }
//                        else
//                        {

//                            {
//                                MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                                json["tag"] = new MyJson.JsonNode_ValueNumber(-4);
//                                json["msg"] = new MyJson.JsonNode_ValueString("compile fail.");
//                                json["info"] = logjson.array;
//                                await context.Response.WriteAsync(json.ToString());
//                                return;
//                            }
//                        }
//                    }
//                }
//                catch (Exception err)
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
//                    json["msg"] = new MyJson.JsonNode_ValueString("unknown fail on conv.");
//                    json["err"] = new MyJson.JsonNode_ValueString(err.ToString());
//                    await context.Response.WriteAsync(json.ToString());
//                    return;
//                }

//            }
//            catch
//            {
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
//                    json["msg"] = new MyJson.JsonNode_ValueString("parse fail.");
//                    await context.Response.WriteAsync(json.ToString());
//                    return;
//                }
//            }
//        }


//        private static async Task parseJAVA(IOwinContext context, FormData formdata)
//        {
//            try
//            {
//                string dictname = null;
//                string classname = null;
//                var file = formdata.mapFiles["file"];
//                var code = System.Text.Encoding.UTF8.GetString(file);

//                //准备临时目录
//                {
//                    Random i = new Random();
//                    var num = i.Next();

//                    while (System.IO.Directory.Exists("tmp\\tmp_" + num.ToString("X08")))
//                    {
//                        num++;
//                    }
//                    dictname = "tmp\\tmp_" + num.ToString("X08");

//                    var fc = code.IndexOf("class");
//                    int ibegin = -1;

//                    for (int ib = fc + 6; ib < fc + 100; ib++)
//                    {
//                        if (ibegin < 0)
//                        {
//                            if (code[ib] == ' ') continue;
//                            else
//                            {
//                                ibegin = ib;
//                            }
//                        }
//                        else
//                        {
//                            if (code[ib] == ' ' || code[ib] == '}')
//                            {
//                                classname = code.Substring(ibegin, ib - ibegin);
//                                break;
//                            }
//                        }
//                    }
//                }
//                System.IO.Directory.CreateDirectory(dictname);
//                string filename = System.IO.Path.Combine(dictname, classname + ".java");
//                System.IO.File.WriteAllText(filename, code);
//                string jarfile = "Neo.SmartContract.Framework.jar";
//                System.IO.File.Copy(jarfile, System.IO.Path.Combine(dictname, jarfile));

//                //编译
//                try
//                {
//                    System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
//                    info.FileName = "cmd.exe";
//                    info.UseShellExecute = false;
//                    info.RedirectStandardOutput = true;
//                    info.RedirectStandardInput = true;
//                    info.RedirectStandardError = true;
//                    info.WorkingDirectory = dictname;
//                    var proc = System.Diagnostics.Process.Start(info);
//                    proc.StandardInput.WriteLine("javac -cp " + jarfile + " " + classname + ".java");
//                    proc.StandardInput.WriteLine("exit");


//                    string back = proc.StandardError.ReadToEnd();
//                    string inerror = "";
//                    int line = -1;
//                    string tag = "";
//                    List<string> outline = new List<string>();
//                    List<int> errorline = new List<int>();
//                    List<string> errorTag = new List<string>();
//                    string[] lines = back.Split(new string[] { "\r\n" }, StringSplitOptions.None);
//                    for (var i = 0; i < lines.Length; i++)
//                    {
//                        if (inerror == "")
//                        {
//                            var mm = lines[i].Split(':');
//                            if (mm.Length > 3)
//                            {
//                                line = int.Parse(mm[1]);
//                                inerror += mm[3];
//                                tag = mm[2];
//                            }

//                        }
//                        else
//                        {
//                            if (lines[i].IndexOf("^") >= 0)
//                            {
//                                outline.Add(inerror);
//                                errorline.Add(line);
//                                errorTag.Add(tag);
//                                inerror = "";
//                            }
//                            else
//                            {
//                                inerror += "\n" + lines[i];
//                            }
//                        }
//                    }

//                    if (outline.Count == 0)
//                    {
//                        //succ
//                    }
//                    else
//                    {
//                        MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                        json["tag"] = new MyJson.JsonNode_ValueNumber(-3);
//                        json["msg"] = new MyJson.JsonNode_ValueString("compile fail.");

//                        MyJson.JsonNode_Array errs = new MyJson.JsonNode_Array();
//                        json["errors"] = errs;
//                        for (var i = 0; i < outline.Count; i++)
//                        {
//                            MyJson.JsonNode_Object errtag = new MyJson.JsonNode_Object();
//                            errs.Add(errtag);
//                            errtag.SetDictValue("msg", outline[i]);
//                            errtag.SetDictValue("line", errorline[i]);
//                            errtag.SetDictValue("tag", errorTag[i]);
//                            //errtag.SetDictValue("col", r.Errors[i].Column);
//                        }
//                        await context.Response.WriteAsync(json.ToString());
//                        return;
//                    }
//                }
//                catch (Exception err)
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
//                    json["msg"] = new MyJson.JsonNode_ValueString("unknown fail on comp.");
//                    json["err"] = new MyJson.JsonNode_ValueString(err.ToString());
//                    await context.Response.WriteAsync(json.ToString());
//                    return;
//                }

//                //conv
//                try
//                {
//                    JavaModule module = new JavaModule();
//                    module.LoadJar(jarfile);
//                    module.LoadClass(System.IO.Path.Combine(dictname, classname + ".class"));
//                    var logjson = new Log2Json();
//                    var conv = new Neo.Compiler.JVM.ModuleConverter(logjson);
//                    var neomd = conv.Convert(module);
//                    var mm = neomd.mapMethods[neomd.mainMethod];

//                    var bs = neomd.Build();
//                    if (bs != null)
//                    {
//                        MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                        json["tag"] = new MyJson.JsonNode_ValueNumber(0);
//                        StringBuilder sb = new StringBuilder();
//                        StringBuilder sb2 = new StringBuilder();
//                        var hash = System.Security.Cryptography.SHA256.Create();
//                        var hashbs = hash.ComputeHash(bs);
//                        foreach (var b in bs)
//                        {
//                            sb.Append(b.ToString("X02"));
//                        }
//                        foreach (var b in hashbs)
//                        {
//                            sb2.Append(b.ToString("X02"));
//                        }
//                        json["hex"] = new MyJson.JsonNode_ValueString(sb.ToString());
//                        json["hash"] = new MyJson.JsonNode_ValueString(sb2.ToString());

//                        json["returntype"] = new MyJson.JsonNode_ValueString(mm.returntype);
//                        MyJson.JsonNode_Array funcparams = new MyJson.JsonNode_Array();
//                        json["params"] = funcparams;
//                        if (mm.paramtypes != null)
//                        {
//                            foreach (var v in mm.paramtypes)
//                            {
//                                funcparams.Add(new MyJson.JsonNode_ValueString(v.type));
//                            }
//                        }

//                        await context.Response.WriteAsync(json.ToString());
//                        return;
//                    }


//                    else
//                    {

//                        {
//                            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                            json["tag"] = new MyJson.JsonNode_ValueNumber(-4);
//                            json["msg"] = new MyJson.JsonNode_ValueString("compile fail.");
//                            json["info"] = logjson.array;
//                            await context.Response.WriteAsync(json.ToString());
//                            return;
//                        }
//                    }

//                }
//                catch (Exception err)
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
//                    json["msg"] = new MyJson.JsonNode_ValueString("unknown fail on conv.");
//                    json["err"] = new MyJson.JsonNode_ValueString(err.ToString());
//                    await context.Response.WriteAsync(json.ToString());
//                    return;
//                }

//            }
//            catch
//            {
//                {
//                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
//                    json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
//                    json["msg"] = new MyJson.JsonNode_ValueString("parse fail.");
//                    await context.Response.WriteAsync(json.ToString());
//                    return;
//                }
//            }
//        }
//    }

//}
