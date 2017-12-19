using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace remotebuilderCore
{
    class CodeDomBuilder
    {
        SHA1 sha1 = SHA1.Create();
        public static string ToHexString(IEnumerable<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
        public class Log
        {
            public string id;
            public string msg;
            public int line = -1;
            public int col = -1;
        }
        public class buildResult
        {
            public byte[] dll;
            public byte[] pdb;
            public List<Log> errors = new List<Log>();
            public List<Log> warnings = new List<Log>();
        }
        public buildResult buildSrc(string src, string temppath)
        {
            var path = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            path = System.IO.Path.Combine(path, temppath);
            var bts = System.Text.Encoding.UTF8.GetBytes(src);
            var hashname = ToHexString(sha1.ComputeHash(bts));
            var outpath = System.IO.Path.Combine(path, hashname + ".exe");
            var outpathpdb = System.IO.Path.Combine(path, hashname + ".pdb");

            CompilerParameters option = new CompilerParameters();
            option.GenerateExecutable = true;
            option.GenerateInMemory = false;
            option.IncludeDebugInformation = true;
            option.ReferencedAssemblies.Add("Neo.SmartContract.Framework.dll");
            option.OutputAssembly = System.IO.Path.Combine(path, outpath);
            var provider = CodeDomProvider.CreateProvider("c#");
            var br = provider.CompileAssemblyFromSource(option, src);
            buildResult buildresult = new buildResult();

            try
            {
                if (br.CompiledAssembly != null)
                {
                    buildresult.dll = System.IO.File.ReadAllBytes(outpath);
                    buildresult.pdb = System.IO.File.ReadAllBytes(outpathpdb);
                    return buildresult;

                }
            }
            catch (Exception err)
            {

            }
            foreach (CompilerError err in br.Errors)
            {
                var item = new Log();
                item.id = err.ErrorNumber;
                item.msg = err.ErrorText;
                item.line = err.Line;
                item.col = err.Column;
                if (err.IsWarning)
                {
                    buildresult.warnings.Add(item);
                }
                else
                {
                    buildresult.errors.Add(item);
                }
            }
            return buildresult;

        }
    }
}
