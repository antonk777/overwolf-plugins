using overwolf.plugins.simpleio;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace overwolf.plugins.unittest {
  class SimpleIOPluginTest {
    private SimpleIOPlugin _plugn = new SimpleIOPlugin();

    private const string kRemoteFile =
      @"http://localhost:8080/arrive.zip";
    private const string kLocalFile = @"S:\temp\setup\any";

    public void Run() {
      int maxthread = 0;
      int io;
      ThreadPool.GetMaxThreads(out maxthread, out io);
      ThreadPool.SetMaxThreads(1, 1);

      //SimpleIOPlugin plugn = new SimpleIOPlugin();

      _plugn.onFileListenerChanged += plugn_OnFileListenerChanged;
      _plugn.onFileListenerChanged2 += plugn_onFileListenerChanged2;

      _plugn.downloadFile(
        kRemoteFile,
        kLocalFile,
        OnDownloadComplete,
        OnDownloadProgress
      );

      var folder = _plugn.PROGRAMFILES + "/overwolf";

      _plugn.getLatestFileInDirectory(folder, new Action<object, object>((x, y) => {

      }));


      _plugn.getLatestFileInDirectory(folder + "/*.msi", new Action<object, object>((x, y) => {

      }));


      _plugn.getTextFile(@"c:\Users\elad.bahar\AppData\Local\Overwolf\Log\OverwolfCEF_13096.log", true, new Action<object, object>((x, y) => {

      }));

      _plugn.getBinaryFile(_plugn.PROGRAMFILES + "/overwolf/Overwolf.exe.config", -1, new Action<object, object>((x, y) => {

      }));

      _plugn.listDirectory(@"c:\Users\elad.bahar\AppData\Local\Overwolf", new Action<object, object>((x, y) => {

      }));


      _plugn.listenOnFile("test", @"e:\temp\python.log", false, new Action<object, object, object>((id, status, line) => {
        // Trace.WriteLine(line);
      }));
      Task.Run(() => {
        try {
          Thread.Sleep(5000);
          _plugn.stopFileListen("test");
          //plugn.listenOnFile("test", @"c:\Temp\test.txt", true, new Action<object, object, object>((id, status, line) =>
          //{
          //  Trace.WriteLine(line);
          //}));
        } catch (Exception) {
          //callback(string.Format("error: ", ex.ToString()));
        }
      });
    }

    void plugn_onFileListenerChanged2(object arg1, object arg2, object arg3, object arg4) {

    }

    void plugn_OnFileListenerChanged(object id, object status, object data) {
      //Console.WriteLine(string.Format("file updated: id:{0} status:{1} data:{2}", id, status, data));
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
