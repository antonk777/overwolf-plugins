using com.overwolf.dwnldr;
using System;

namespace overwolf.plugins.unittest {
  public class DownloaderTests {
    private Downloader _downloader = new Downloader();

    // CHANGE THESE!!!
    private const string kRemoteFile =
      @"http://localhost:8080/arrive.zip";
    private const string kLocalFile = @"S:\temp\setup\any";

    public void Run() {
      _downloader.downloadFile(
        kRemoteFile,
        kLocalFile,
        OnDownloadComplete,
        OnDownloadProgress
      );
    }

    private void OnDownloadComplete(object obj) {
      // obj is a json object
      Console.WriteLine("Completed: " + obj);
    }

    private void OnDownloadProgress(int progress) {
      // obj is a json object
      Console.WriteLine("Progress: " + progress);
    }
  }
}
