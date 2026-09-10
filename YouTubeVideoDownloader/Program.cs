using YoutubeExplode; // dotnet add package YoutubeExplode
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;

namespace YoutubeVideoDownloader;

public static class Program
{
    private static readonly YoutubeClient Youtube = new();

    private static readonly string ProgramFfmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
    
    public static async Task Main(string[] args)
    {
        if (!File.Exists(ProgramFfmpegPath))
        {
            Console.WriteLine("FFMPEG NÃO ENCONTRADO!");
            return;
        }
        
        await Menu();
    }

    private static async Task Menu()
    {
        while (true)
        {
            Console.WriteLine("O QUE VOCÊ DESEJA?\n[ 1 ]BAIXAR MP3 (APENAS ÁUDIO)\n[ 2 ]BAIXAR MP4 (ÁUDIO E VÍDEO)\n[ 0 ]SAIR");
            
            var opcao = ReadInt("SUA ESCOLHA: ");
            
            if (opcao == 0)
            {
                break;
            }
            
            switch (opcao)
            {
                case 1:
                {
                    try
                    {
                        var link = GetLink();
                        if (link == "SAIR")
                        {
                            break;
                        }

                        if (IsPlaylist(link))
                        {
                            var videos = await GetPlaylistAsync(link);

                            await DownloadPlaylistMp3(videos);
                        }

                        else
                        {
                            var video = await GetVideoAsync(link);

                            await DownloadMp3(video);
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERRO: {ex.Message}");
                        break;
                    }
                }
                
                case 2:
                {
                    try
                    {
                        var link = GetLink();
                        if (link == "SAIR")
                        {
                            break;
                        }

                        if (IsPlaylist(link))
                        {
                            var videos = await GetPlaylistAsync(link);

                            if (videos.Count == 0)
                            {
                                Console.WriteLine("NENHUM VÍDEO VÁLIDO FOI ENCONTRADO NA PLAYLIST.");
                                break;
                            }
                            
                            var firstVideo = await GetVideoAsync(videos[0].Url);
                            
                            var resolution = ChooseResolution(firstVideo);

                            await DownloadPlaylistMp4(videos, resolution);
                        }

                        else
                        {
                            var video = await GetVideoAsync(link);

                            var resolution = ChooseResolution(video);

                            await DownloadMp4(video, resolution);
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERRO: {ex.Message}");
                        break;
                    }
                }
            }
        }
    }


    private static string GetLink()
    {
        Console.WriteLine("DIGITE \"SAIR\" PARA PARAR O PROGRAMA");
        var link = ReadString("DIGITE O LINK DO VÍDEO, SHORTS OU PLAYLIST QUE VOCÊ QUER BAIXAR: ");

        return link.Equals("sair", StringComparison.OrdinalIgnoreCase) ? "SAIR" : link;
    }


    private static async Task<Video> GetVideoAsync(string link)
    {
        var ytVideo = await Youtube.Videos.GetAsync(link);
        
        var manifest = await Youtube.Videos.Streams.GetManifestAsync(ytVideo.Id);

        return new Video
        {
            Title = ytVideo.Title,

            Url = link,

            Resolutions = manifest
                .GetVideoStreams()
                .Select(v => v.VideoQuality.Label)
                .Distinct()
                .OrderByDescending(r =>
                {
                    var numbers = new string(r.TakeWhile(char.IsDigit).ToArray());
                    return int.TryParse(numbers, out var value) ? value : 0;
                })
                .ToList()
        };
    }


    private static async Task<List<Video>> GetPlaylistAsync(string link)
    {
        List<Video> videos = [];

        await foreach (var ytVideo in Youtube.Playlists.GetVideosAsync(link))
        {
            try
            {
                videos.Add(new Video
                {
                    Title = ytVideo.Title,
                    Url = $"https://youtube.com/watch?v={ytVideo.Id}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERRO: Não foi possível obter o vídeo '{ytVideo.Title}'");
                Console.WriteLine(ex.Message);
            }
        }

        return videos;
    }
    
    
    private static async Task DownloadMp3(Video video)
    {
        var ytVideo = await Youtube.Videos.GetAsync(video.Url);
        
        var manifest = await Youtube.Videos.Streams.GetManifestAsync(ytVideo.Id);

        var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        var title = RemoveInvalidCharacters(video.Title);

        var extension = audioStream.Container.Name;
        
        var tempPath = GetUniqueFilePath(Path.Combine(downloads, $"{title}_audio.{extension}"));

        var finalPath = GetUniqueFilePath(Path.Combine(downloads, $"{title}.mp3"));
        
        var progress = new Progress<double>(p =>
        {
            Console.Write($"\rPROGRESSO: {p:P0}");
        });
        
        await Youtube.Videos.Streams.DownloadAsync(audioStream, tempPath, progress);

        Console.WriteLine("\nCONVERTENDO PARA MP3...");

        try
        {
            await ConvertToMp3(tempPath, finalPath);
        }

        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        Console.WriteLine("DOWNLOAD COMPLETO.");
    }


    private static async Task DownloadPlaylistMp3(List<Video> videos)
    {
        foreach (var video in videos)
        {
            try
            {
                Console.WriteLine($"\nBAIXANDO: {video.Title}");
                
                await DownloadMp3(video);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERRO AO BAIXAR: {video.Title}");
                Console.WriteLine(ex.Message);
            }
        }
        
        Console.WriteLine("DOWNLOAD DA PLAYLIST COMPLETO.");
    }


    private static async Task ConvertToMp3(string inputPath, string outputPath)
    {
        var convert = Process.Start(new ProcessStartInfo
        {
            FileName = ProgramFfmpegPath,
            Arguments = $"-y -i \"{inputPath}\" -vn -q:a 0 \"{outputPath}\"",
            
            CreateNoWindow = true,

            UseShellExecute = false,

            RedirectStandardError = true,
        });
        
        await convert?.WaitForExitAsync()!;
    }
    
    
    private static async Task DownloadMp4(Video video, string resolution)
    {
        var ytVideo = await Youtube.Videos.GetAsync(video.Url);
        
        var manifest = await Youtube.Videos.Streams.GetManifestAsync(ytVideo.Id);
        
        var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        
        var videoStreams = manifest.GetVideoOnlyStreams();

        var videoStream = videoStreams
            .Where(v => v.VideoQuality.Label == resolution)
            .OrderByDescending(v => v.Bitrate)
            .FirstOrDefault();

        if (videoStream == null)
        {
            videoStream = videoStreams
                .OrderByDescending(v => v.VideoQuality.MaxHeight)
                .First();
        }
        
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        var title = RemoveInvalidCharacters(video.Title);

        var videoExtension = videoStream.Container.Name;
        
        var audioExtension = audioStream.Container.Name;
        
        var videoTempPath = GetUniqueFilePath(Path.Combine(downloads, $"{title}_video.{videoExtension}"));
        
        var audioTempPath = GetUniqueFilePath(Path.Combine(downloads, $"{title}_audio.{audioExtension}"));
        
        var finalPath = GetUniqueFilePath(Path.Combine(downloads, $"{title}.mp4"));
        
        var videoProgress = new Progress<double>(p =>
        {
            Console.Write($"\rVIDEO: {p:P0}");
        });
        
        var audioProgress = new Progress<double>(p =>
        {
            Console.Write($"\rAUDIO: {p:P0}");
        });
        
        await Youtube.Videos.Streams.DownloadAsync(videoStream, videoTempPath, videoProgress);

        await Youtube.Videos.Streams.DownloadAsync(audioStream, audioTempPath, audioProgress);
        
        Console.WriteLine("\nCONVERTENDO PARA MP4...");

        try
        {
            await ConvertToMp4(videoTempPath, audioTempPath, finalPath);
        }

        finally
        {
            if (File.Exists(videoTempPath))
            {
                File.Delete(videoTempPath);
            }

            if (File.Exists(audioTempPath))
            {
                File.Delete(audioTempPath);
            }
        }
    }


    private static async Task DownloadPlaylistMp4(List<Video> videos, string resolution)
    {
        foreach (var video in videos)
        {
            try
            {
                Console.WriteLine($"\nBAIXANDO: {video.Title}");
                
                await DownloadMp4(video, resolution);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERRO AO BAIXAR: {video.Title}");
                Console.WriteLine(ex.Message);
            }
        }
        Console.WriteLine("DOWNLOAD DA PLAYLIST COMPLETO.");
    }


    private static async Task ConvertToMp4(string videoPath, string audioPath, string outputPath)
    {
        var convert = Process.Start(new ProcessStartInfo
        {
            FileName = ProgramFfmpegPath,
            Arguments = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac \"{outputPath}\"",
            
            CreateNoWindow = true,

            UseShellExecute = false,

            RedirectStandardError = true
        });
        
        var error = await convert?.StandardError.ReadToEndAsync()!;
        
        await convert.WaitForExitAsync();
        
        if (convert.ExitCode != 0)
        {
            Console.WriteLine(error);

            throw new Exception("ERRO AO CONVERTER MP4.");
        }
    }


    private static string ChooseResolution(Video video)
    {
        var cont = 1;
        
        var resolutions = video.Resolutions;
        
        foreach (var resolution in resolutions)
        {
            Console.WriteLine($"[ {cont++} ]{resolution}");
        }

        int opcao;

        do
        {
            Console.WriteLine("QUAL DAS RESOLUÇÕES ACIMA VOCÊ QUER BAIXAR?");
            opcao = ReadInt("Sua escolha: ");

        } while (opcao < 1 || opcao > resolutions.Count);

        var chosenResolution = resolutions[opcao - 1];

        return chosenResolution;
    }


    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }
        
        var directory = Path.GetDirectoryName(path)!;
        
        var fileName = Path.GetFileNameWithoutExtension(path);
        
        var extension = Path.GetExtension(path);

        var counter = 1;

        string newPath;

        do
        {
            newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");

            counter++;

        } while (File.Exists(newPath));

        return newPath;
    }
    
    
    private static string RemoveInvalidCharacters(string nome)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            nome = nome.Replace(c.ToString(), "");
        }

        return nome;
    }
    
    
    private static bool IsPlaylist(string link)
    {
        if (!link.Contains("list="))
        {
            return false;
        }

        return !link.Contains("list=RD");
    }
    
    
    private static int ReadInt(string msg)
    {
        try
        {
            while (true)
            {
                Console.Write(msg);
                if (int.TryParse(Console.ReadLine()?.Trim(), out var integer))
                {
                    return integer;
                }
            }
        }
        catch
        {
            return -1;
        }
    }
    

    private static string ReadString(string msg)
    {
        try
        {
            while (true)
            {
                Console.Write(msg);
                var str = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return str;
                }
            }
        }
        catch
        {
            return "";
        }
    }
}