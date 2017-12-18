using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compileService
{
    public class testc
    {

        static string[] CallDotnet(string Arguments)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "dotnet";
            p.StartInfo.WorkingDirectory = testc.BuildPath;
            p.StartInfo.Arguments = Arguments;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            List<string> allline = new List<string>();
            while (p.StandardOutput.EndOfStream == false)
            {
                var l = p.StandardOutput.ReadLine();
                allline.Add(l);
            }
            p.WaitForExit();
            return allline.ToArray();
        }
        public static readonly string BuildPath;
        public static readonly string OutPath;
        static testc()
        {
            var dllpath = typeof(testc).Assembly.Location;
            BuildPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dllpath)
                , "sample_contract"
                );
            OutPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dllpath)
                , "script_out"
                );
        }
        public static async void Init()
        {
            string[] outstr = CallDotnet("restore");
            bool succ = false;
            foreach (var ss in outstr)
            {
                if (ss.Contains("Restore completed"))
                {
                    Console.WriteLine(ss);
                    succ = true;
                    break;
                }
            }
            if (succ)
                Console.WriteLine("restore proj cool.");
            else
                Console.WriteLine("restore proj fail.");

            List<string> errinfo = new List<string>();
            succ =  Build(errinfo);
            foreach (var e in errinfo)
                Console.WriteLine(e);
            if (succ)
            {
                Console.WriteLine("build proj cool.");
            }
            else
            {
                Console.WriteLine("build proj fail.");
            }
        }

        public static void SetCode(string src)
        {
            var srcfile = System.IO.Path.Combine(BuildPath, "Contract" + ".cs");
            System.IO.File.Delete(srcfile);
            System.IO.File.WriteAllText(srcfile, src, System.Text.Encoding.UTF8);
        }
        //public static bool Build(List<string> errorinfo)
        //{
        //    System.CodeDom.sh
        //    var cstr = CallDotnet("clean");
        //    //操作目录
        //    var outstr = CallDotnet("build -c debug");
        //    foreach (var ss in outstr)
        //    {
        //        if (ss.Contains("error"))
        //        {
        //            errorinfo.Add(ss);
        //        }
        //        if (ss.Contains("warning"))
        //        {
        //            errorinfo.Add(ss);
        //        }
        //        if (ss.Contains("sample_contract.dll"))
        //        {

        //            Console.WriteLine(ss);
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        public static  bool Build(List<string> errorinfo)
        {
            Build2(errorinfo);
            return true;
        }
        public static async Task<bool> Build2(List<string> errorinfo)
        {
            var srcfile = System.IO.Path.Combine(BuildPath, "sample_contract" + ".csproj");
            try
            {
                Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
                var proj = await workspace.OpenProjectAsync(srcfile);
                var com = await proj.GetCompilationAsync();
            }
            catch(Exception err)
            {

            }

            return true;
        }
        public class Logger : Neo.Compiler.ILogger
        {
            List<string> list;
            public Logger(List<string> list)
            {
                this.list = list;
            }
            public void Log(string log)
            {
                list.Add(log);
            }
        }
        public static string CalcScriptHashString(byte[] script)
        {
            var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hash256 = sha256.ComputeHash(script);
            var ripemd160 = new Neo.Cryptography.RIPEMD160Managed();
            var hash = ripemd160.ComputeHash(hash256);
            StringBuilder sb = new StringBuilder();
            sb.Append("0x");
            foreach (var b in hash.Reverse().ToArray())
            {
                sb.Append(b.ToString("x02"));
            }
            return sb.ToString();
        }
        public static bool BuildNeon(List<string> errinfo)
        {
            //loaddll
            Neo.Compiler.MSIL.ILModule module = null;
            //try
            {
                var name = System.IO.Path.Combine(BuildPath, "bin\\debug\\net40\\sample_contract.dll");
                var namepdb = System.IO.Path.Combine(BuildPath, "bin\\debug\\net40\\sample_contract.pdb");
                if (System.IO.File.Exists(name) == false || System.IO.File.Exists(namepdb) == false)
                {
                    throw new Exception("必须同时拥有dll 和 pdb 文件");
                }
                var stream = System.IO.File.OpenRead(name);
                var streampdb = System.IO.File.OpenRead(namepdb);

                module = new Neo.Compiler.MSIL.ILModule();
                module.LoadModule(stream, streampdb);
            }
            //catch (Exception err)
            //{
            //    errinfo.Add("LoadDll error:" + err.Message);
            //    return false;
            //}
            //convert
            Neo.Compiler.MSIL.ModuleConverter converter = null;
            try
            {
                converter = new Neo.Compiler.MSIL.ModuleConverter(new Logger(errinfo));
                converter.Convert(module);
            }
            catch (Exception err)
            {
                errinfo.Add("Convert error:" + err.Message);
                return false;
            }
            //gen debug info
            string debuginfo = null;
            try
            {
                Neo.Compiler.MyJson.JsonNode_Array arr = new Neo.Compiler.MyJson.JsonNode_Array();
                foreach (var m in converter.outModule.mapMethods)
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
                                //if (srcfile == null)
                                //    srcfile = debugcode;

                                infos.AddArrayValue(c.Value.addr.ToString("X04") + "-" + c.Value.debugline.ToString());
                            }
                        }
                    }
                }
                debuginfo = arr.ToString();
            }
            catch (Exception err)
            {
                errinfo.Add("GenDebugInfo error:" + err.Message);
                return false;
            }
            //save
            try
            {
                var bytes = converter.outModule.Build();//avm
                var hashstr = CalcScriptHashString(bytes);//hash

                System.IO.File.WriteAllBytes(System.IO.Path.Combine(OutPath, hashstr + ".avm"), bytes);
                System.IO.File.WriteAllText(System.IO.Path.Combine(OutPath, hashstr + ".debug.json"), debuginfo, Encoding.UTF8);
                System.IO.File.Copy(System.IO.Path.Combine(BuildPath, "Contract" + ".cs"),
                    System.IO.Path.Combine(OutPath, hashstr + ".cs"));
                return true;
            }
            catch (Exception err)
            {
                errinfo.Add("Save error:" + err.Message);
                return false;
            }
        }
    }
}
