using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testrosyln
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((e) =>
            {
                testc();
                while (true)
                {
                    System.Threading.Thread.Sleep(10);
                }
            });
            while (true)
            {
                Console.ReadLine();
            }
        }
        static async void testc()
        {
            var srcfile = System.IO.Path.Combine("D:\\git\\nel\\SmartContractBrowser\\SmartContractBrowser\\sample_contract", "sample_contract" + ".csproj");
            try
            {
                DateTime t01 = DateTime.Now;
                Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();

                DateTime t02 = DateTime.Now;
                var proj = await workspace.OpenProjectAsync(srcfile);
                DateTime t03 = DateTime.Now;

                var com = await proj.GetCompilationAsync();
                DateTime t04 = DateTime.Now;

                var ms = new System.IO.MemoryStream();
                var mspdb = new System.IO.MemoryStream();
                com.Emit(ms, mspdb);
                DateTime t05 = DateTime.Now;
                Console.WriteLine("init rosyln time=" + (t02 - t01).TotalMilliseconds);
                Console.WriteLine("open project time=" + (t03 - t02).TotalMilliseconds);
                Console.WriteLine("build time=" + (t04 - t03).TotalMilliseconds);
                Console.WriteLine("gen dll & pdb time=" + (t05 - t04).TotalMilliseconds);
                List<string> errinfo = new List<string>();
                var bytes = ms.ToArray();
                var bytespdb = mspdb.ToArray();
                var b = BuildNeon(new System.IO.MemoryStream(bytes), new System.IO.MemoryStream(bytespdb), errinfo);
            }
            catch (Exception err)
            {

            }

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
        public static bool BuildNeon(System.IO.Stream ms, System.IO.Stream mspdb, List<string> errinfo)
        {
            string OutPath = "";
            //loaddll
            Neo.Compiler.MSIL.ILModule module = null;
            //try
            {
                module = new Neo.Compiler.MSIL.ILModule();
                module.LoadModule(ms, mspdb);
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
                //System.IO.File.Copy(System.IO.Path.Combine(BuildPath, "Contract" + ".cs"),
                //    System.IO.Path.Combine(OutPath, hashstr + ".cs"));
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
