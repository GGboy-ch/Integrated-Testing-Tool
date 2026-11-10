using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SetupTool
{
    internal class Program
    {
        private const string FtpUri = "ftp://120.26.41.210:21/";

        private const string FtpUserName = "PC";

        private const string FtpPassword = "cdh1531079";

        private const string FtpRemoteDirectory = "PC/程序";

        private const bool FtpEnableSsl = false;

        static int Main(string[] args)
        {
            try
            {
                string rootpath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                    ? Path.GetFullPath(args[0])
                    : Environment.CurrentDirectory;
                Console.WriteLine($"需要打包项目所在的路径：{rootpath}");
                string projectName = Path.GetFileNameWithoutExtension(new DirectoryInfo(rootpath).GetFiles().Where(file => file.Name.EndsWith(".csproj")).FirstOrDefault()!.FullName);
                string applicationIconPath = Path.Combine(rootpath, $"{projectName}.ico");
                if (!File.Exists(applicationIconPath))
                {
                    throw new FileNotFoundException("未找到程序图标文件。", applicationIconPath);
                }

                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PackTool.iss");
                Console.WriteLine($"开始打包。。。");
                List<string> specialFiles = new List<string>();
                List<string> iss = File.ReadAllLines(templatePath).ToList();
                XDocument xmlDoc = XDocument.Load(rootpath + $"\\{projectName}.csproj");
                XNamespace ns = xmlDoc.Root!.GetDefaultNamespace();

                bool isSDK = xmlDoc.Root.ToString().Contains("Sdk");
                string projectGuid = xmlDoc.Descendants(ns + "ProjectGuid").FirstOrDefault()?.Value!;
                string packpath = rootpath + "\\bin\\Release\\net8.0";

                if (isSDK)
                {
                    if (!Directory.Exists(packpath))
                    {
                        specialFiles = GetSpecialFiles(projectName, rootpath + "\\bin\\Release\\net8.0-windows");
                    }
                    else
                    {
                        specialFiles = GetSpecialFiles(projectName, packpath);
                    }
                }
                else
                {
                    specialFiles = GetSpecialFiles(projectName, rootpath + "\\bin\\Release");
                }
                // 直接读取编译产物，保证安装包与程序使用相同版本。
                string assemblyName = xmlDoc.Descendants(ns + "AssemblyName").FirstOrDefault()?.Value ?? projectName;
                string buildOutput = isSDK
                    ? (Directory.Exists(packpath) ? packpath : rootpath + "\\bin\\Release\\net8.0-windows")
                    : rootpath + "\\bin\\Release";
                string assemblyPath = Path.Combine(buildOutput, assemblyName + (isSDK ? ".dll" : ".exe"));
                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException("未找到编译产物，无法读取安装包版本。", assemblyPath);
                }

                string version = AssemblyName.GetAssemblyName(assemblyPath).Version?.ToString()
                    ?? throw new InvalidOperationException("编译产物缺少程序集版本。");
                IReadOnlyDictionary<string, string> updateHistory = ReadUpdateHistory(assemblyPath, version);
                string updateSummary = updateHistory[version].Split('\n', 2)[0].TrimEnd();
                Console.WriteLine($"安装包版本：{version}，更新简介：{updateSummary}");
                string installerBaseName = $"{projectName}-{version}";
                int i = 0;

                //编辑[Files]段
                iss.RemoveAll(str => str.StartsWith("Source:"));//先清除[Files]段中的数据
                int filesIndex = iss.IndexOf(iss.Where(str => str.StartsWith("[Files]")).FirstOrDefault()!) + 1;
                string configfilemessage = "Source: \"{#MyOutputDir}\\bin\\Release\\net8.0\\*\"; DestDir: \"{app}\"; Flags: ignoreversion recursesubdirs createallsubdirs";
                if (isSDK)
                {
                    if (!Directory.Exists(packpath))
                    {
                        configfilemessage = "Source: \"{#MyOutputDir}\\bin\\Release\\net8.0-windows\\*\"; DestDir: \"{app}\"; Flags: ignoreversion recursesubdirs createallsubdirs";
                    }
                }
                else
                {
                    configfilemessage = "Source: \"{#MyOutputDir}\\bin\\Release\\*\"; DestDir: \"{app}\"; Flags: ignoreversion recursesubdirs createallsubdirs";
                }
                iss.Insert(filesIndex, configfilemessage);

                foreach (string file in specialFiles)
                {
                    if (isSDK)
                    {
                        configfilemessage = "Source: \"" + "{#MyOutputDir}\\" + $"bin\\Release\\{file}\"; " + "DestDir: \"{app}\"; Flags: ignoreversion onlyifdoesntexist";
                    }
                    else
                    {
                        configfilemessage = "Source: \"" + "{#MyOutputDir}\\" + $"bin\\{file}\"; " + "DestDir: \"{app}\"; Flags: ignoreversion onlyifdoesntexist";
                    }
                    if (!iss.Contains(file))
                    {
                        iss.Insert(filesIndex, configfilemessage);
                    }
                }

                //编辑常量定义部分
                while (i < iss.Count)
                {
                    string str = string.Empty;
                    if (!string.IsNullOrEmpty(iss[i]) && iss[i].StartsWith("#define"))
                    {
                        str = iss[i].Substring(iss[i].IndexOf(' ') + 1, iss[i].LastIndexOf(' ') - iss[i].IndexOf(' ') - 1);
                        switch (str)
                        {
                            case "MyAppName":
                                iss[i] = $"#define MyAppName \"{projectName}\""; break;
                            case "MyAppId":
                                iss[i] = $"#define MyAppId \"{projectGuid}\""; break;
                            case "MyAppVersion":
                                iss[i] = $"#define MyAppVersion \"{version}\""; break;
                            case "MyOutputDir":
                                iss[i] = $"#define MyOutputDir \"{rootpath}\""; break;
                            case "MyUpdateMessage":
                                iss[i] = $"#define MyUpdateMessage \"{updateSummary.Replace("\"", "\"\"")}\""; break;
                            default:
                                break;
                        }
                    }
                    if (iss[i].StartsWith("OutputBaseFilename="))
                    {
                        iss[i] = $"OutputBaseFilename={installerBaseName}";
                    }
                    if (iss[i].StartsWith("SetupIconFile"))
                    {
                        iss[i] = $"SetupIconFile=\"{applicationIconPath}\"";//使用项目根目录下的程序图标作为安装包图标
                    }
                    i++;
                }

                //将配置文件和分拣计划提取出来便于打包
                foreach (string file in specialFiles)
                {
                    if (isSDK)
                    {
                        string outputDir = rootpath + $"\\bin\\Release\\net8.0\\{file}";
                        if (!Directory.Exists(packpath))
                        {
                            outputDir = rootpath + $"\\bin\\Release\\net8.0-windows\\{file}"; // 如果没有net8.0目录，则使用net8.0-windows目录
                        }
                        File.Copy(outputDir, rootpath + $"\\bin\\Release\\{file}", true);
                        File.Delete(outputDir);
                    }
                    else
                    {
                        File.Copy(rootpath + $"\\bin\\Release\\{file}", rootpath + $"\\bin\\{file}", true);
                        File.Delete(rootpath + $"\\bin\\Release\\{file}");
                    }
                }

                // 每次打包使用独立的脚本，避免并行构建覆盖模板和其他项目的脚本。
                string scriptPath = Path.Combine(Path.GetTempPath(), $"SetupTool-{Guid.NewGuid():N}.iss");
                try
                {
                    File.WriteAllText(scriptPath, string.Join("\r\n", iss), Encoding.UTF8);
                    UseInnoSetup(scriptPath, @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe");
                }
                finally
                {
                    File.Delete(scriptPath);
                }

                //打包完成之后将配置文件和分拣计划放回到原来位置
                foreach (string file in specialFiles)
                {
                    if (isSDK)
                    {
                        string outputDir = rootpath + $"\\bin\\Release\\net8.0\\{file}";
                        if (!Directory.Exists(packpath))
                        {
                            outputDir = rootpath + $"\\bin\\Release\\net8.0-windows\\{file}"; // 如果没有net8.0目录，则使用net8.0-windows目录
                        }
                        File.Copy(rootpath + $"\\bin\\Release\\{file}", outputDir, true);
                        File.Delete(rootpath + $"\\bin\\Release\\{file}");
                    }
                    else
                    {
                        File.Copy(rootpath + $"\\bin\\{file}", rootpath + $"\\bin\\Release\\{file}", true);
                        File.Delete(rootpath + $"\\bin\\{file}");
                    }
                }

                string installerPath = Path.Combine(rootpath, "bin", $"{installerBaseName}.exe");
                if (!File.Exists(installerPath))
                {
                    throw new FileNotFoundException("打包完成后未找到安装包。", installerPath);
                }

                string historyPath = Path.Combine(rootpath, "bin", $"{projectName}.changelog.json");
                WriteUpdateHistory(historyPath, updateHistory);
                Console.WriteLine($"版本历史：{historyPath}");
                UploadInstallerToFtp(installerPath, applicationIconPath, historyPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[打包失败，失败原因]：{ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 读取 AssemblyInfo.cs 编译后的 ChangeLog，不加载或执行目标程序及其依赖。
        /// 每条记录以日期和版本号开头，后续行组成该版本的完整更新信息。
        /// </summary>
        internal static IReadOnlyDictionary<string, string> ReadUpdateHistory(string assemblyPath, string version)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using PEReader pe = new PEReader(stream);
            MetadataReader reader = pe.GetMetadataReader();
            foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
            {
                CustomAttribute attribute = reader.GetCustomAttribute(handle);
                if (attribute.Constructor.Kind != HandleKind.MemberReference)
                {
                    continue;
                }

                MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                if (constructor.Parent.Kind != HandleKind.TypeReference)
                {
                    continue;
                }

                TypeReference type = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                if (reader.GetString(type.Namespace) != "System.Reflection" ||
                    reader.GetString(type.Name) != "AssemblyMetadataAttribute")
                {
                    continue;
                }

                BlobReader value = reader.GetBlobReader(attribute.Value);
                if (value.ReadUInt16() != 1 || value.ReadSerializedString() != "ChangeLog")
                {
                    continue;
                }

                string changeLog = (value.ReadSerializedString() ?? string.Empty).Trim().ReplaceLineEndings("\n");
                MatchCollection headers = Regex.Matches(changeLog, @"(?m)^[^\S\n]*\d{4}-?\d{2}-?\d{2}[^\S\n]+(?<version>\d+\.\d+\.\d+\.\d+)[^\S\n]*$");
                if (headers.Count == 0 || headers[0].Index != 0 || headers[0].Groups["version"].Value != version)
                {
                    throw new InvalidOperationException("AssemblyInfo.cs 的最新 ChangeLog 版本必须与程序集版本一致。");
                }

                var history = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int index = 0; index < headers.Count; index++)
                {
                    Match header = headers[index];
                    string entryVersion = header.Groups["version"].Value;
                    int detailsStart = header.Index + header.Length;
                    int detailsEnd = index + 1 < headers.Count ? headers[index + 1].Index : changeLog.Length;
                    string details = changeLog[detailsStart..detailsEnd].Trim();
                    if (details.Length == 0 || !history.TryAdd(entryVersion, details))
                    {
                        throw new InvalidOperationException($"AssemblyInfo.cs 的 ChangeLog 版本 {entryVersion} 重复或缺少更新信息。");
                    }
                }

                return history;
            }

            throw new InvalidOperationException("AssemblyInfo.cs 未定义 ChangeLog 更新日志。");
        }

        internal static void WriteUpdateHistory(string historyPath, IReadOnlyDictionary<string, string> history)
        {
            string json = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });
            string temporaryPath = $"{historyPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, historyPath, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        /// <summary>
        /// 获取需要特殊处理的文件
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        private static List<string> GetSpecialFiles(string projectName, string releasepath)
        {
            List<string> fileList = new List<string>();
            var files = new DirectoryInfo(releasepath).GetFiles().ToList();

            //使用忽略文件来筛选需要特殊处理的文件
            string[] ignoreFiles = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\ignoreFiles.txt").Split('/');
            foreach (var ignoreFile in ignoreFiles)
            {
                fileList.AddRange(files.Where(file => file.Name.EndsWith(ignoreFile)).ToList().Select(p => p.Name));
            }
            return fileList;
        }

        /// <summary>
        /// 调用Inno Setup进行打包
        /// </summary>
        /// <param name="scriptPath">Inno Setup 脚本文件路径</param>
        /// <param name="innoSetupPath">Inno Setup 可执行文件路径</param>
        private static void UseInnoSetup(string scriptPath, string innoSetupPath)
        {
            // 创建进程信息
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = innoSetupPath,
                Arguments = $"\"{scriptPath}\"", // 引号用于处理路径中的空格
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // 启动进程
            using (Process process = new Process { StartInfo = processInfo })
            {
                process.Start();

                // 读取输出信息
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                // 显示结果
                Console.WriteLine("Inno Setup Output:");
                Console.WriteLine(output);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine("Inno Setup Errors:");
                    Console.WriteLine(error);
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Inno Setup 打包失败，退出码：{process.ExitCode}。");
                }

                Console.WriteLine("Build completed successfully.");
            }
        }

        /// <summary>
        /// 将程序名图标、版本历史和安装包上传到 FTP 服务器。
        /// </summary>
        /// <param name="installerPath">本地安装包路径</param>
        /// <param name="iconPath">项目中的程序图标路径</param>
        /// <param name="historyPath">项目版本历史路径</param>
        private static void UploadInstallerToFtp(string installerPath, string iconPath, string historyPath)
        {
            if (string.IsNullOrWhiteSpace(FtpUri))
            {
                Console.WriteLine("未配置FTP服务器地址，跳过FTP上传。");
                return;
            }

            if (!Uri.TryCreate(FtpUri, UriKind.Absolute, out Uri? ftpUri) || ftpUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new InvalidOperationException("FTP服务器地址必须是有效的 ftp:// 地址。");
            }

            if (string.IsNullOrWhiteSpace(FtpUserName) || string.IsNullOrWhiteSpace(FtpPassword))
            {
                throw new InvalidOperationException("FTP用户名和密码不能为空。");
            }

            NetworkCredential credential = new NetworkCredential(FtpUserName, FtpPassword);
            Uri remoteInstallerUri = BuildRemoteFileUri(ftpUri, FtpRemoteDirectory, Path.GetFileName(installerPath));
            Uri remoteIconUri = BuildRemoteFileUri(ftpUri, FtpRemoteDirectory, Path.GetFileName(iconPath));
            string remoteHistoryName = Path.ChangeExtension(Path.GetFileName(installerPath), ".changelog.json");
            Uri remoteHistoryUri = BuildRemoteFileUri(ftpUri, FtpRemoteDirectory, remoteHistoryName);

            EnsureFtpDirectoryExists(remoteInstallerUri, credential, FtpEnableSsl);
            // 先上传图标和该安装包对应的历史快照，最后上传安装包作为完成标记。
            UploadFileToFtp(remoteIconUri, iconPath, credential);
            UploadFileToFtp(remoteHistoryUri, historyPath, credential);
            UploadFileToFtp(remoteInstallerUri, installerPath, credential);
        }

        /// <summary>
        /// 上传单个文件到FTP服务器。
        /// </summary>
        private static void UploadFileToFtp(Uri remoteFileUri, string localPath, NetworkCredential credential)
        {
            Console.WriteLine($"开始上传文件：{remoteFileUri}");

#pragma warning disable SYSLIB0014
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteFileUri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = credential;
            request.EnableSsl = FtpEnableSsl;
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = 60000;
            request.ReadWriteTimeout = 60000;

            using FileStream input = File.OpenRead(localPath);
            request.ContentLength = input.Length;
            using (Stream output = request.GetRequestStream())
            {
                input.CopyTo(output);
            }

            using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Console.WriteLine($"FTP上传成功：{Path.GetFileName(localPath)}，{response.StatusDescription?.Trim()}");
        }

        /// <summary>
        /// 创建FTP远程目录。
        /// </summary>
        private static void EnsureFtpDirectoryExists(Uri remoteFileUri, NetworkCredential credential, bool enableSsl)
        {
            string[] segments = remoteFileUri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .SkipLast(1)
                .ToArray();
            Uri serverRoot = new UriBuilder(remoteFileUri.Scheme, remoteFileUri.Host, remoteFileUri.Port).Uri;
            string currentPath = string.Empty;

            foreach (string segment in segments)
            {
                currentPath += $"/{segment}";
                Uri directoryUri = new Uri(serverRoot, currentPath);

                try
                {
#pragma warning disable SYSLIB0014
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(directoryUri);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = credential;
                    request.EnableSsl = enableSsl;
                    request.UsePassive = true;
                    request.KeepAlive = false;
                    request.Timeout = 60000;

                    using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                }
                catch (WebException ex) when (ex.Response is FtpWebResponse response && response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    response.Dispose();
                }
            }
        }

        /// <summary>
        /// 组装FTP远程文件地址。
        /// </summary>
        private static Uri BuildRemoteFileUri(Uri ftpUri, string remoteDirectory, string fileName)
        {
            string basePath = Uri.UnescapeDataString(ftpUri.AbsolutePath).Trim('/');
            string directory = remoteDirectory.Trim().Trim('/').Replace('\\', '/');
            string[] pathSegments = new[] { basePath, directory, fileName }
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .SelectMany(segment => segment.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Select(Uri.EscapeDataString)
                .ToArray();
            UriBuilder builder = new UriBuilder(ftpUri)
            {
                Path = string.Join('/', pathSegments)
            };
            return builder.Uri;
        }

        public static void AddToPath(string newPath, bool prepend = false)
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            var paths = new HashSet<string>(currentPath.Split(';'));

            if (!paths.Contains(newPath))
            {
                if (prepend)
                    currentPath = newPath + ";" + currentPath;
                else
                    currentPath += ";" + newPath;

                Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.User);
                Console.WriteLine("PATH更新成功！");
            }
            else
            {
                Console.WriteLine("路径已存在，无需重复添加。");
            }
        }
    }
}
