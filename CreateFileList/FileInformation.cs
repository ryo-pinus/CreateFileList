using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CreateFileList
{
    /// <summary>
    /// ファイルの情報を格納するクラス。
    /// </summary>
    class FileInformation
    {
        private const int OFF_SET_OF_E_LFANEW = 60;
        private const int OFF_SET_OF_TIME_DATE_STAMP = 8;
        private const int OFF_SET_OF_LINKER_VERSION = 26;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fileFullPath">ファイルのフルパス。</param>
        public FileInformation(string name, string fileFullPath)
        {
            Name = name;
            FilePath = fileFullPath;

            // ファイル名
            var fi = new FileInfo(fileFullPath);

            // SHA256ハッシュの作成
            HashString = CreateHashString(fileFullPath);

            // ファイルサイズ・更新日時
            try
            {
                FileSize = fi.Length;
                LastWriteTime = fi.LastWriteTime;
            }
            catch
            {
                FileSize = 0;
                LastWriteTime = null;
            }

            // ファイルバージョン・プロダクトバージョン
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(fileFullPath);
                FileVersion = fvi.FileVersion;
                ProductVersion = fvi.ProductVersion;
            }
            catch
            {
                FileVersion = string.Empty;
                ProductVersion = string.Empty;
            }

            // ビルド日時・リンカバージョン
            DateTime? buildDateTime;
            string linkerVersion;
            ReadPeInfo(fileFullPath, out buildDateTime, out linkerVersion);
            BuildDateTime = buildDateTime;
            LinkerVersion = linkerVersion;
        }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// ファイルパス。
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// ファイルサイズ。
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// 最終更新日時。
        /// </summary>
        public DateTime? LastWriteTime { get; private set; }

        /// <summary>
        /// ファイルバージョン。
        /// </summary>
        public string FileVersion { get; private set; }

        /// <summary>
        /// 製品バージョン。
        /// </summary>
        public string ProductVersion { get; private set; }

        /// <summary>
        /// ビルド日時。
        /// </summary>
        public DateTime? BuildDateTime { get; private set; }

        /// <summary>
        /// リンカバージョン。
        /// </summary>
        public string LinkerVersion { get; private set; }

        /// <summary>
        /// ハッシュ文字列。
        /// </summary>
        public string HashString { get; private set; }

        public string ToStringHeader()
        {
            return ToStringHeader("\t");
        }

        public string ToStringHeader(string sep)
        {
            return string.Join(sep, new string[] { "名称", "ファイルサイズ", "最終更新日時", "ハッシュ値", "ファイルバージョン", 
                "製品バージョン", "ビルド日時", "リンカバージョン" });
        }

        public override string ToString()
        {
            return ToString("\t");
        }

        public string ToString(string sep)
        {
            return string.Join(sep, new string[] { Name, FileSize.ToString(), LastWriteTime.ToString(), HashString, FileVersion, 
                ProductVersion, BuildDateTime.ToString(), LinkerVersion });
        }

        /// <summary>
        /// time_tをDateTime構造体に変換する。
        /// </summary>
        /// <param name="time">time_t。</param>
        /// <returns>変換後の値。</returns>
        private DateTime ToDateTime(int time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time).ToLocalTime();
        }

        /// <summary>
        /// PEヘッダを保持しているか確認する。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <returns>PEヘッダを保持しているファイルの場合はtrueが返る。</returns>
        private bool HasPeHeader(string filePath)
        {
            var targetExt = new string[] { ".exe", ".dll" };
            var ext = Path.GetExtension(filePath);
            return targetExt.Any(_ => string.Equals(_, ext, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// PEヘッダから必要な情報を読みだす。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="buildDateTime">読みだしたビルド日時。</param>
        /// <param name="linkerVersion">読みだしたリンカバージョン。</param>
        private void ReadPeInfo(string filePath, out DateTime? buildDateTime, out string linkerVersion)
        {
            buildDateTime = null;
            linkerVersion = string.Empty;
            if (HasPeHeader(filePath))
            {
                try
                {
                    using (var fs = File.OpenRead(filePath))
                    {
                        using (var reader = new BinaryReader(fs, Encoding.ASCII))
                        {
                            reader.BaseStream.Seek(OFF_SET_OF_E_LFANEW, SeekOrigin.Begin);
                            var e_lfanew = reader.ReadInt32();

                            reader.BaseStream.Seek(e_lfanew + OFF_SET_OF_TIME_DATE_STAMP, SeekOrigin.Begin);
                            buildDateTime = ToDateTime(reader.ReadInt32());

                            reader.BaseStream.Seek(e_lfanew + OFF_SET_OF_LINKER_VERSION, SeekOrigin.Begin);
                            var majorLinkerVersion = reader.ReadSByte();
                            var minorLinkerVersion = reader.ReadSByte();
                            linkerVersion = string.Format("{0}.{1}", majorLinkerVersion, minorLinkerVersion);
                        }
                    }
                }
                catch
                {
                    buildDateTime = null;
                    linkerVersion = string.Empty;
                }
            }
        }

        /// <summary>
        /// ハッシュ文字列を作成する。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <returns>ハッシュ文字列。</returns>
        private string CreateHashString(string filePath)
        {
            try
            {
                var crypto = new SHA256CryptoServiceProvider();
                using (var fs = File.OpenRead(filePath))
                {
                    var hash = crypto.ComputeHash(fs);
                    return string.Join("", hash.Select(_ => _.ToString("X2")));
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
