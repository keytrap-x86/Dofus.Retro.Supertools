
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dofus.Retro.Supertools.Core.Object;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class LoginHelper
    {

        public class FindResult
        {
            public string Error { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            public FindResult(int x, int y)
            {
                X = x;
                Y = y;
            }

            public FindResult(string error)
            {
                Error = error;
            }
        }

        public enum Mode
        {
            Login,
            Server,
            Play,
            ServerList
        }

        public static async Task<FindResult> FindByImage(Mode mode, CancellationTokenSource cancellationTokenSource, string imageName = null, int timeOut = 20, string imgPath = null)
        {
            Console.WriteLine(@"[FIND_BY_IMAGE]");

            return await Task.Run(() =>
            {
                FindResult result = null;

                for (var i = 0; i < timeOut; i++)
                {
                    if (cancellationTokenSource != null)
                        if (cancellationTokenSource.IsCancellationRequested)
                            break;

                    var currentPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                    string filePath;
                    switch (mode)
                    {
                        case Mode.Login:
                            filePath = $"{currentPath}\\Resources\\login\\login.bmp";
                            break;
                        case Mode.Server:
                            filePath = $"{currentPath}\\Resources\\servers\\{imageName}.bmp";
                            break;
                        case Mode.Play:
                            filePath = $"{currentPath}\\Resources\\play\\play.bmp";
                            //filePath = imgPath;
                            break;
                        case Mode.ServerList:
                            filePath = $"{currentPath}\\Resources\\server_list.bmp";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                    }

                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine(@"Error : file not found : " + filePath);
                        break;
                    }
                    result = ImageHelper.SearchImage(filePath, 6);
                    Console.WriteLine($@"Looking for {mode} - {imageName} [{i}]");
                    if (result != null)
                        break;

                    Thread.Sleep(1000);
                }

                return result;
            }, cancellationTokenSource.Token);

        }


        /// <summary>
        /// Fonction qui va permettre de trouver la position d'un élément sur l'écran.
        /// </summary>
        /// <param name="textToFind"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static async Task<FindResult> FindByOcr(OcrString ocrString, CancellationTokenSource cancellationTokenSource, int timeOut = 20)
        {
            Console.WriteLine(@"[FIND_BY_OCR]");

            return await Task.Run(() =>
             {
                 FindResult result = null;

                 for (var i = 0; i < timeOut; i++)
                 {
                     if (cancellationTokenSource != null)
                         if (cancellationTokenSource.IsCancellationRequested)
                             break;


                     var p = new Process();
                     var findByOcrExe = Path.Combine(Environment.CurrentDirectory, @"Resources\find_by_ocr.exe");
                     if (!File.Exists(findByOcrExe))
                     {
                         result = new FindResult($"[ERREUR] : Impossible de trouver {findByOcrExe}");
                         break;
                     }

                     var pi = new ProcessStartInfo(findByOcrExe, ocrString.Data);
                     p.StartInfo = pi;
                     pi.UseShellExecute = false;
                     p.OutputDataReceived += (w, o) =>
                     {
                         if (string.IsNullOrEmpty(o.Data) || o.Data.Trim().Length <= 2 || !o.Data.Contains(";"))
                             return;
                         try
                         {
                             var point = o.Data.Trim().Split(';');
                             var x = Convert.ToInt32(point[0].Substring(0, point[0].IndexOf('.') > 0 ? point[0].IndexOf('.') : point[0].Length));
                             var y = Convert.ToInt32(point[1].Substring(0, point[1].IndexOf('.') > 0 ? point[1].IndexOf('.') : point[1].Length));
                             Console.WriteLine($@"[FIND_BY_OCR] : {x}:{y}");
                             result = new FindResult(x, y);
                         }
                         catch (Exception)
                         {
                             // ignored
                         }
                     };
                     p.EnableRaisingEvents = true;
                     pi.RedirectStandardOutput = true;
                     pi.StandardOutputEncoding = Encoding.UTF8;


                     Console.WriteLine($@"Looking for {ocrString.Name} [{i}]");


                     p.Start();
                     p.BeginOutputReadLine();

                     p.WaitForExit();



                     if (result != null && string.IsNullOrEmpty(result.Error))
                         break;

                     Thread.Sleep(1000);
                 }


                 return result;

             });
        }

        public static async Task<string> FindCharacter(string characterName, CancellationTokenSource cancellationTokenSource)
        {
            return await Task.Run(() =>
            {

                string result = null;
                var p = new Process();
                var pi = new ProcessStartInfo(@"Resources\find_character.exe", characterName);
                p.StartInfo = pi;
                pi.UseShellExecute = false;
                p.OutputDataReceived += (w, o) => result = o.Data;
                p.EnableRaisingEvents = true;
                pi.RedirectStandardOutput = true;
                pi.StandardOutputEncoding = Encoding.UTF8;
                //p.Exited += (sender, args) => ;

                p.Start();
                result = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                return result;

            });
        }
    }
}
