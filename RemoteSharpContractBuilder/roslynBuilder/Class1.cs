using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace roslynBuilder
{
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

    public class RsolynBuilder
    {
        SHA1 sha1 = SHA1.Create();
        public static string ToHexString(IEnumerable<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
        class LockObj
        {

        }
        LockObj lockobj = new LockObj();

        public async Task<buildResult> buildSrc(string src, string temppath)
        {


            Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
            var path = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            //build proj
            var bts = System.Text.Encoding.UTF8.GetBytes(src);
            var hashname = ToHexString(sha1.ComputeHash(bts));
            var outpath = System.IO.Path.Combine(path, temppath, hashname);

            lock (lockobj)//不要同时搞目录
            {
                if (System.IO.Directory.Exists(outpath))
                    System.IO.Directory.Delete(outpath, true);
                if (System.IO.Directory.Exists(outpath) == false)
                    System.IO.Directory.CreateDirectory(outpath);
                System.IO.File.Copy(System.IO.Path.Combine(path, "sample_contract\\mscorlib.dll"), System.IO.Path.Combine(outpath, "mscorlib.dll"), true);
                System.IO.File.Copy(System.IO.Path.Combine(path, "sample_contract\\Neo.SmartContract.Framework.dll"), System.IO.Path.Combine(outpath, "Neo.SmartContract.Framework.dll"), true);
                System.IO.File.Copy(System.IO.Path.Combine(path, "sample_contract\\sample_contract.csproj"), System.IO.Path.Combine(outpath, "sample_contract.csproj"), true);
                System.IO.File.WriteAllText(System.IO.Path.Combine(outpath, "Contract.cs"), src);
            }
            //srcfile
            var projfile = System.IO.Path.Combine(outpath, "sample_contract.csproj");
            Project proj = null;
            try
            {
                proj = await workspace.OpenProjectAsync(projfile);
                var com = await proj.GetCompilationAsync();
                var diag = com.GetDiagnostics();
                buildResult br = new buildResult();

                foreach (var d in diag)
                {
                    if (d.Severity == DiagnosticSeverity.Warning)
                    {
                        Log item = new Log();
                        item.msg = d.GetMessage();
                        item.id = d.Id;
                        if (d.Location.IsInSource)
                        {
                            var span = d.Location.GetLineSpan();
                            item.line = span.Span.Start.Line;
                            item.col = span.Span.Start.Character;
                        }
                        //item.line =d.Location
                        br.warnings.Add(item);
                    }
                    else if (d.Severity == DiagnosticSeverity.Error)
                    {
                        Log item = new Log();
                        item.msg = d.GetMessage();
                        item.id = d.Id;
                        if (d.Location.IsInSource)
                        {
                            var span = d.Location.GetLineSpan();
                            item.line = span.Span.Start.Line;
                            item.col = span.Span.Start.Character;
                        }
                        br.errors.Add(item);
                    }
                }
                var ms = new System.IO.MemoryStream();
                var mspdb = new System.IO.MemoryStream();
                com.Emit(ms, mspdb);
                br.dll = ms.ToArray();
                br.pdb = mspdb.ToArray();
                ms.Close();
                mspdb.Close();
                return br;
            }
            catch (Exception err)
            {
                return null;
            }


        }
    }
}
