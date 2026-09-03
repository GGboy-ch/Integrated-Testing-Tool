using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
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
                string scriptPath = AppDomain.CurrentDomain.BaseDirectory + "PackTool.iss";
                Console.WriteLine($"开始打包。。。");
                List<string> specialFiles = new List<string>();
                List<string> assemblyInfos = File.ReadAllLines(rootpath + "\\AssemblyInfo.cs").ToList();
                List<string> iss = File.ReadAllLines(scriptPath).ToList();
                XDocument xmlDoc = XDocument.Load(rootpath + $"\\{projectName}.csproj");
                XNamespace ns = xmlDoc.Root!.GetDefaultNamespace();

                bool isSDK = xmlDoc.Root.ToString().Contains("Sdk");
                string projectGuid = xmlDoc.Descendants(ns + "ProjectGuid").FirstOrDefault()?.Value!;
                string version = string.Empty;
                string uploadMessage = xmlDoc.Descendants(ns + "UploadMessage").FirstOrDefault()?.Value!;
                string packpath = rootpath + "\\bin\\Release\\net8.0";
                Console.WriteLine($"UploadMessage : {uploadMessage}");

                if (isSDK)
                {
                    if (!Directory.Exists(packpath))
                    {
                        specialFiles = GetSpecialFiles(projectName, rootpath + "\\bin\\Release\\net8.0-windows");
                        version = xmlDoc.Descendants(ns + "AssemblyVersion").FirstOrDefault()?.Value!;
                    }
                    else
                    {
                        specialFiles = GetSpecialFiles(projectName, packpath);
                        version = xmlDoc.Descendants(ns + "AssemblyVersion").FirstOrDefault()?.Value!;
                    }
                }
                else
                {
                    specialFiles = GetSpecialFiles(projectName, rootpath + "\\bin\\Release");
                    version = ExtractStr(assemblyInfos.Where(str => str.StartsWith("[assembly: AssemblyVersion")).FirstOrDefault()!);
                }
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
                                iss[i] = $"#define MyUpdateMessage \"{uploadMessage}\""; break;
                            default:
                                break;
                        }
                    }
                    if (iss[i].StartsWith("SetupIconFile"))
                    {
                        iss[i] = $"SetupIconFile={rootpath}\\{projectName}.ico";//使用项目根目录下的icon.ico作为安装包图标
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

                //向iss脚本文件中写入信息
                File.WriteAllText(scriptPath, string.Join("\r\n", iss), Encoding.UTF8);
                UseInnoSetup(scriptPath, $"C:\\Program Files (x86)\\Inno Setup 6\\ISCC.exe");

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

                string installerPath = Path.Combine(rootpath, "bin", $"{projectName}.exe");
                if (!File.Exists(installerPath))
                {
                    throw new FileNotFoundException("打包完成后未找到安装包。", installerPath);
                }

                UploadInstallerToFtp(installerPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[打包失败，失败原因]：{ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 提取AssemblyInfo.cs文件中的内容
        /// </summary>
        /// <param name="str">需要处理的字符串</param>
        /// <returns></returns>
        private static string ExtractStr(string str)
        {
            string str2 = str.Substring(str.IndexOf('"') + 1, str.LastIndexOf('"') - str.IndexOf('"') - 1);
            return str2;
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
        /// 将安装包上传到FTP服务器。
        /// </summary>
        /// <param name="installerPath">本地安装包路径</param>
        private static void UploadInstallerToFtp(string installerPath)
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
            Uri remoteFileUri = BuildRemoteFileUri(ftpUri, FtpRemoteDirectory, Path.GetFileName(installerPath));

            Console.WriteLine($"开始上传安装包：{remoteFileUri}");
            EnsureFtpDirectoryExists(remoteFileUri, credential, FtpEnableSsl);

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

            using FileStream input = File.OpenRead(installerPath);
            request.ContentLength = input.Length;
            using (Stream output = request.GetRequestStream())
            {
                input.CopyTo(output);
            }

            using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Console.WriteLine($"FTP上传成功：{response.StatusDescription?.Trim()}");
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
