using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography;

namespace overwolf.plugins.simpleio {
  public class SimpleIOPlugin : IDisposable {
    IntPtr _window = IntPtr.Zero;

    public SimpleIOPlugin(int window) {
      _window = new IntPtr(window); ;
    }

    public SimpleIOPlugin() {
    }

    #region Events
    public event Action<object, object, object> onFileListenerChanged;
    public event Action<object, object, object, object> onFileListenerChanged2;
    #endregion Events

    #region IDisposable
    public void Dispose() {
      FileListenerManager.Dispose();
    }
    #endregion

    #region Properties
    public string PROGRAMFILES {
      get { return Constants.kProgramFiles; }
    }

    public string PROGRAMFILESX86 {
      get { return Constants.kProgramFilesX86; }
    }

    public string COMMONFILES {
      get { return Constants.kCommonFiles; }
    }

    public string COMMONFILESX86 {
      get { return Constants.kCommonFilesX86; }
    }

    public string COMMONAPPDATA {
      get { return Constants.kCommonAppDataFiles; }
    }

    public string DESKTOP {
      get { return Constants.kDesktop; }
    }

    public string WINDIR {
      get { return Constants.kWinDir; }
    }

    public string SYSDIR {
      get { return Constants.kSysDir; }
    }

    public string SYSDIRX86 {
      get { return Constants.kSysDirX86; }
    }

    public string MYDOCUMENTS {
      get { return Constants.kMyDocuments; }
    }

    public string MYVIDEOS {
      get { return Constants.kMyVideos; }
    }

    public string MYPICTURES {
      get { return Constants.kMyPictures; }
    }

    public string MYMUSIC {
      get { return Constants.kMyMusic; }
    }

    public string COMMONDOCUMENTS {
      get { return Constants.kCommonDocuments; }
    }

    public string FAVORITES {
      get { return Constants.kFavorites; }
    }

    public string FONTS {
      get { return Constants.kFonts; }
    }

    public string STARTMENU {
      get { return Constants.kStartMenu; }
    }

    public string LOCALAPPDATA {
      get { return Constants.kLocalApplicationData; }
    }

    public string PLUGINPATH {
      get { return Constants.kPluginPath; }
    }

    #endregion

    #region Functions
    public void fileExists(string path, Action<object> callback) {

      if (callback == null)
        return;

      if (string.IsNullOrEmpty(path)) {
        callback(false);
        return;
      }

      Task.Run(() => {
        try {
          path = path.Replace('/', '\\');
          callback(File.Exists(path));
        } catch (Exception ex) {
          callback(string.Format("error: ", ex.ToString()));
        }
      });
    }

    public void isDirectory(string path, Action<object> callback) {
      if (callback == null)
        return;

      if (path == null) {
        callback(false);
        return;
      }

      try {
        Task.Run(() => {
          try {
            path = path.Replace('/', '\\');
            callback(Directory.Exists(path));
          } catch (Exception) {

            callback(false);
          }
        });
      } catch (Exception ex) {
        callback(string.Format("error: ", ex.ToString()));
      }
    }

    public void writeFile(string path, string content, Action<object, object> callback) {
      if (callback == null)
        return;

      try {
        Task.Run(() => {
          try {
            path = path.Replace('/', '\\');
            if (path.StartsWith("\\")) {
              path = path.Remove(0, 1);
            }

            // make sure the folder exists, prior to writing the file
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists) {
              fileInfo.Directory.Create();
            }

            using (FileStream filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)) {
              byte[] info = new UTF8Encoding(false).GetBytes(content);
              filestream.Write(info, 0, info.Length);
            }
            callback(true, "");
          } catch (Exception ex) {
            callback(false, string.Format("unexpected error when trying to write to '{0}' : {1}",
              path, ex.ToString()));
          }
        });
      } catch (Exception ex) {
        callback(false, string.Format("error: ", ex.ToString()));
      }
    }

    public void deleteFile(string path, Action<object, object> callback) {
      if (callback == null)
        return;

      try {
        if (Directory.Exists(path)) {
          Directory.Delete(path, true);
          callback(true, "");
          return;
        }

        if (File.Exists(path)) {
          File.Delete(path);
          callback(true, "");
          return;
        }

        callback(true, "");
      } catch (Exception ex) {
        callback(false, string.Format("error: ", ex.ToString()));
      }
    }

    public void getLatestFileInDirectory(string path, Action<object, object> callback) {
      if (callback == null)
        return;

      try {
        Task.Run(() => {
          try {
            var filePattern = "*";
            var folder = path;

            if (!NormalizePathWithFilePattern(path, out filePattern, out  folder)) {
              callback(false, "folder not found");
              return;
            }

            var lastFile = Directory.GetFiles(folder, filePattern)
                       .OrderByDescending(x => new FileInfo(x).LastWriteTime)
                       .FirstOrDefault();

            if (lastFile == null) {
              callback(false, "no file in directory");
            } else {
              callback(true, new FileInfo(lastFile).Name);
            }

          } catch (Exception ex) {
            callback(false, string.Format("unknown error: ", ex.ToString()));
          }
        });

      } catch (Exception ex) {
        callback(false, string.Format("unknown error: ", ex.ToString()));
      }
    }

    public void getTextFile(string filePath, bool widechars, Action<object, object> callback) {
      if (callback == null)
        return;

      try {
        Task.Run(() => {
          string output = string.Empty;
          try {
            if (!File.Exists(filePath)) {
              callback(false, string.Format("File doesn't exists", filePath));
              return;
            }

            try {
              output = File.ReadAllText(
                filePath, widechars ? Encoding.Default : Encoding.UTF8);
            } catch {
              try {
                string tempFile = Path.GetTempFileName();
                File.Copy(filePath, tempFile, true);

                output = File.ReadAllText(tempFile,
                  widechars ? Encoding.Default : Encoding.UTF8);

                try { File.Delete(tempFile); } catch { }

              } catch (Exception ex) {
                callback(false, "Fail to create temp file " + ex.ToString());
                return;
              }
            }

            callback(true, output);

          } catch (Exception ex) {
            callback(false, string.Format("Exception GetTextFile : {0}", ex.ToString()));
          }
        });
      } catch (Exception ex) {
        callback(false, string.Format("Exception GetTextFile: {0}", ex.ToString()));
      }
    }

    public void getBinaryFile(string filePath, int limit, Action<object, object> callback) {
      if (callback == null)
        return;

      try {
        Task.Run(() => {
          string output = string.Empty;
          try {
            if (!File.Exists(filePath)) {
              callback(false, string.Format("File doesn't exists", filePath));
              return;
            }

            try {
              using (FileStream filestream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                int readSize = limit <= 0 ?
                  (int)filestream.Length : (Math.Min(limit, (int)filestream.Length));

                if (readSize == 0) {
                  callback(false, "file has no content");
                  return;
                }

                byte[] buffer = new byte[readSize];
                filestream.Read(buffer, 0, readSize);

                StringBuilder result = new StringBuilder(readSize * 2);
                result.Append(buffer[0]);

                for (int i = 1; i < buffer.Length; i++) {
                  result = result.Append(",");
                  result = result.Append(buffer[i]);
                }

                callback(true, result.ToString());
              }
            } catch (Exception ex) {
              callback(false, "Fail to read file " + ex.ToString());
              return;
            }

          } catch (Exception ex) {
            callback(false, string.Format("Exception GetTextFile : {0}", ex.ToString()));
          }
        });
      } catch (Exception ex) {
        callback(false, string.Format("Exception GetTextFile: {0}", ex.ToString()));
      }
    }

    public void listDirectory(string path, Action<object, object> callback) {
      if (callback == null)
        return;

      if (path == null) {
        callback(false, "empty path");
        return;
      }

      try {
        Task.Run(() => {
          try {

            var filePattern = "*";
            var folder = path;
            if (!NormalizePathWithFilePattern(path, out filePattern, out  folder)) {
              callback(false, "folder not found");
              return;
            }

            var fileList = Directory.GetFiles(folder, filePattern)
              .OrderByDescending(x => new FileInfo(x).LastWriteTime);

            StringBuilder resoltJson = new StringBuilder();
            resoltJson.Append("[");
            foreach (string file in fileList) {
              resoltJson.Append(string.Format("{{ \"name\" : \"{0}\" , \"type\": \"file\" }},", new FileInfo(file).Name));
              //resoltJson.Append(string.Format("{ {0} }", file));
            }

            var folderList = Directory.GetDirectories(folder);
            foreach (string subFolder in folderList) {
              resoltJson.Append(string.Format("{{ \"name\" : \"{0}\" , \"type\": \"dir\" }},", new DirectoryInfo(subFolder).Name));
              //resoltJson.Append(string.Format("{ {0} }", file));
            }
            resoltJson.Remove(resoltJson.Length - 1, 1);

            resoltJson.Append("]");
            callback(true, resoltJson.ToString());

          } catch (Exception ex) {
            callback(false, string.Format("listDirectory exception: {0}", ex.ToString()));
          }
        });
      } catch (Exception ex) {
        callback(false, string.Format("listDirectory exception: {0}", ex.ToString()));
      }
    }

    public void listenOnFile(string id, string filename, bool skipToEnd, Action<object, object, object> callback) {
      FileListenerManager.ListenOnFile(id, filename, skipToEnd, callback, OnFileChanged);
    }

    public void stopFileListen(string id) {
      FileListenerManager.stopFileListen(id);
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
    /// Unzip a local file
    /// </summary>
    /// <param name="zipFile">Path to .zip file to unpack</param>
    /// <param name="extractPath">Destination path on local disk</param>
    /// <param name="callback">Callback that will receive results</param>
    public void unzipFile(string zipFile, string extractPath, Action<object> callback) {
      Task.Run(() => {
        try {
          if (Directory.Exists(extractPath)) {
            Directory.Delete(extractPath, true);
          }

          System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, extractPath);

          callback(new {
            status = true
          });
        } catch (Exception e) {
          callback(new {
            status = false,
            error = e.Message.ToString()
          });
        }
      });
    }

    #endregion Functions

    #region Private Funcs

    /// <summary>
    /// Assure the file doesn't exist and the full folder path is created
    /// </summary>
    /// <param name="localFile"></param>
    private static void PrepareLocalFileForDownload(string localFile) {
      try {
        // Make sure the file doesn't already exist - otherwise we'll fail
        // downloading
        if (File.Exists(localFile)) {
          File.Delete(localFile);
        }

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
    private static string CalculateMD5(string filename) {
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
    private void OnFileChanged(object id, object status, object data, bool isNew) {
      if (onFileListenerChanged != null) {
        onFileListenerChanged(id, status, data);
      }

      // new event: |isNew| the line was written after start listen on file
      if (onFileListenerChanged2 != null) {
         onFileListenerChanged2(id, status, data, isNew);
      }
    }
    private bool NormalizePathWithFilePattern(string path, out string pattern, out string folder) {
      path = path.Replace('/', '\\');
      folder = path;
      pattern = "*";
      if (!Directory.Exists(folder)) {
        folder = Path.GetDirectoryName(path);
        pattern = Path.GetFileName(path);
      }

      if (!Directory.Exists(folder)) {
        return false;
      }

      return true;
    }
    #endregion Private Funcs
  }
}
