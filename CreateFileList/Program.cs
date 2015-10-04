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
            var fiList = Directory.EnumerateFiles(targetDir, "*.*", SearchOption.AllDirectories)
                .Select(_ => new FileInformation(_.Substring(targetDir.Length + 1), _))
                .ToArray();

            // ファイルのリストを標準出力に書き込む
            for (int i = 0; i < fiList.Length; i++)
            {
                if (i == 0)
                {
                    Console.WriteLine(fiList[i].ToStringHeader());
                }
                Console.WriteLine(fiList[i].ToString());
            }
            return 0;
        }
    }
}
