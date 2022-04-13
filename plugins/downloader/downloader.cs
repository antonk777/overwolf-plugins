using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace com.overwolf.dwnldr {
  public class Downloader {
    /// Public Methods
    public Downloader() {
    }

    /// <summary>
    /// Download a file from the web
    /// </summary>
    /// <param name="url">URL to download from</param>
    /// <param name="localFile">Destination file on local disk</param>
    /// <param name="callback">Callback that will receive results</param>
    /// <param name="onProgress">Callback receive download progress</param>
    public void downloadFile(
      string url,
      string localFile,
      Action<object> callback,
      Action<int> onProgress = null
    ) {
      PrepareLocalFileForDownload(localFile);
      SetServicePointManagerGlobalParams();

      try {
        using (WebClient wc = new WebClient()) {
          if (onProgress != null) {
            wc.DownloadProgressChanged += (_, e) => {
              onProgress(e.ProgressPercentage);
            };
          }

          wc.DownloadFileCompleted += (_, e) => {
            callback(new {
              status = true,
              md5 = CalculateMD5(localFile)
            });
          };

          wc.DownloadFileAsync(new System.Uri(url), localFile);
        }
      } catch (Exception e) {
        callback(new {
          status = false,
          error = e.Message.ToString()
        });
      }
    }

    /// <summary>
    /// Assure the file doesn't exist and the full folder path is created
    /// </summary>
    /// <param name="localFile"></param>
    private void PrepareLocalFileForDownload(string localFile) {
      try {
        // Make sure the file doesn't already exist - otherwise we'll fail
        // downloading
        File.Delete(localFile);
      } catch (Exception) {
        // File probably doesn't exist
      }

      try {
        var localFileInfo = new FileInfo(localFile);
        Directory.CreateDirectory(localFileInfo.DirectoryName);
      } catch (Exception) {

      }
    }

    /// <summary>
    /// https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    static string CalculateMD5(string filename) {
      try {
        using (var md5 = MD5.Create()) {
          using (var stream = File.OpenRead(filename)) {
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash)
                               .Replace("-", "")
                               .ToLowerInvariant();
          }
        }
      } catch(Exception) {
        return String.Empty;
      }
    }

    /// <summary>
    /// Sets the global |ServicePointManager| to support new TLS protocols
    /// </summary>
    private static void SetServicePointManagerGlobalParams() {
      // Wrapping this with a try...catch... since we've seen that
      // |ServicePointManager| might not always exist in memory
      try {
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                               SecurityProtocolType.Tls11 |
                                               SecurityProtocolType.Tls12 |
                                               SecurityProtocolType.Ssl3;
      } catch (Exception) {
        // NOTE(twolf): Suppressing errors - because if we really need to
        // support these protocols for the file being downloaded, the error
        // will return to the caller of |download|
      }
    }

  }
}
