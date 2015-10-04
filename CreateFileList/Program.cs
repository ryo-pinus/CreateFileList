using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CreateFileList
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                return 1;
            }

            // ファイルのリストを作成
            var targetDir = args[0];
            var list = Directory.EnumerateFiles(targetDir, "*.*", SearchOption.AllDirectories)
                .OrderBy(_ => _)
                .Select((_, i) => new {i, fi = new FileInformation(_.Substring(targetDir.Length + 1), _)});

            // ファイルのリストを標準出力に書き込む
            foreach (var item in list)
            {
                if (item.i == 0)
                {
                    Console.WriteLine(item.fi.ToStringHeader());
                }
                Console.WriteLine(item.fi.ToString());
            }
            return 0;
        }
    }
}
