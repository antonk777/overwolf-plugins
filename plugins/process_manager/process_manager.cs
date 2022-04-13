using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.overwolf.procmgr {
  public class ProcessManager : IDisposable {
    private class ProcessTrackingInfo {
      public Process process { get; set; }
      public bool track { get; set; }
    }

    private readonly string _dllLocation;
    private Dictionary<int, ProcessTrackingInfo> _runningProcess =
      new Dictionary<int, ProcessTrackingInfo>();
    private bool _disposing = false;

    #region Events
    public event Action<object> onProcessExited;

    public event Action<object> onDataReceivedEvent;
    #endregion Events

    //--------------------------------------------------------------------------
    public ProcessManager() {
      _dllLocation =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    //--------------------------------------------------------------------------
    public bool isProcessRunning(string processName) {
      var processes = Process.GetProcessesByName(processName);
      if (processes.Length > 0) {
        return true;
      } else {
        return false;
      }
    }

    //--------------------------------------------------------------------------
    // path can be relative to the dll location or absolute
    public void launchProcess(
      string executableFilename,
      string arguments,
      object environmentVariables,
      bool hidden,
      // kill process if our process ends
      bool killOnClose,
      Action<object> callback
    ) {
      Task.Run(() => {
        try {
          Process process = new Process();

          process.StartInfo.UseShellExecute = false;
          process.StartInfo.FileName = executableFilename;
          process.StartInfo.Arguments = arguments;
          process.StartInfo.CreateNoWindow = hidden;
          process.StartInfo.WindowStyle = hidden ? ProcessWindowStyle.Hidden :
                                                   ProcessWindowStyle.Normal;
          process.StartInfo.RedirectStandardOutput = true;
          process.StartInfo.RedirectStandardError = true;
          process.StartInfo.RedirectStandardInput = true;
          process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
          process.StartInfo.WorkingDirectory =
            new FileInfo(executableFilename).Directory.FullName;
          onDataReceivedEvent(new { data = process.StartInfo.WorkingDirectory });

          if (environmentVariables != null) {
            try {
             var jsonObject = JObject.Parse(environmentVariables.ToString());
             foreach (var item in jsonObject) {
               process.StartInfo.EnvironmentVariables[item.Key] = (string)item.Value;
             }
            } catch {
              callback(new { error = "can't set environment variables" });
              return;
            }
          }

          process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
            if (!String.IsNullOrEmpty(e.Data))
            {
              if (onDataReceivedEvent != null) {
                onDataReceivedEvent(new { error = e.Data });
              }
            }
          };
          process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
          {
            if (!String.IsNullOrEmpty(e.Data))
            {
              if (onDataReceivedEvent != null) {
                onDataReceivedEvent(new { data = e.Data });
              }
            }
          };

          process.Start();

          process.BeginOutputReadLine();
          process.BeginErrorReadLine();

          if (killOnClose) {
            ChildProcessTracker.AddProcess(process);
          }

          lock (this) {
            _runningProcess.Add(process.Id, new ProcessTrackingInfo() {
              process = process,
              track = killOnClose
            });
          }

          process.EnableRaisingEvents = true;
          process.Exited += ProcessExited;
          callback(new { data = process.Id });
        } catch (Exception ex) {
          callback(new { error = "unknown exception: " + ex.ToString() });
        }
      });
    }

    //--------------------------------------------------------------------------
    // path can be relative to the dll location or absolute
    public void launchProcessAsAdmin(
      string executableFilename,
      string arguments,
      object environmentVariables,
      bool hidden,
      // kill process if our process ends
      bool killOnClose,
      Action<object> callback
    ) {
      Task.Run(() => {
        try {
          Process process = new Process();

          process.StartInfo.Verb = "runas";
          process.StartInfo.UseShellExecute = true;
          process.StartInfo.FileName = executableFilename;
          process.StartInfo.Arguments = arguments;
          process.StartInfo.CreateNoWindow = hidden;
          process.StartInfo.WindowStyle = hidden ? ProcessWindowStyle.Hidden :
                                                   ProcessWindowStyle.Normal;
          process.StartInfo.WorkingDirectory =
            new FileInfo(executableFilename).Directory.FullName;
          onDataReceivedEvent(new { data = process.StartInfo.WorkingDirectory });

          if (environmentVariables != null) {
            try {
             var jsonObject = JObject.Parse(environmentVariables.ToString());
             foreach (var item in jsonObject) {
               process.StartInfo.EnvironmentVariables[item.Key] = (string)item.Value;
             }
            } catch {
              callback(new { error = "can't set environment variables" });
              return;
            }
          }

          process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
            if (!String.IsNullOrEmpty(e.Data))
            {
              if (onDataReceivedEvent != null) {
                onDataReceivedEvent(new { error = e.Data });
              }
            }
          };
          process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
          {
            if (!String.IsNullOrEmpty(e.Data))
            {
              if (onDataReceivedEvent != null) {
                onDataReceivedEvent(new { data = e.Data });
              }
            }
          };

          process.Start();

          if (killOnClose) {
            ChildProcessTracker.AddProcess(process);
          }

          lock (this) {
            _runningProcess.Add(process.Id, new ProcessTrackingInfo() {
              process = process,
              track = killOnClose
            });
          }

          process.EnableRaisingEvents = true;
          process.Exited += ProcessExited;
          callback(new { data = process.Id });
        } catch (Exception ex) {
          callback(new { error = "unknown exception: " + ex.ToString() });
        }
      });
    }

    //--------------------------------------------------------------------------
    public void terminateProcess(int processId) {
      Task.Run(() => {
        try {
          ProcessTrackingInfo info;
          lock (this) {
            if (!_runningProcess.TryGetValue(processId, out info))
              return;
          }
          Console.WriteLine("Terminate {0}", processId);
          info.process.Kill();
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.ToString());
        }
      });
    }

    #region privates Func
    //--------------------------------------------------------------------------
    private void ProcessExited(object sender, EventArgs e) {
      if (_disposing)
        return;

      try {
        var process = sender as Process;
        if (process == null)
          return;

        int exitCode = 0;
        try { exitCode = process.ExitCode; } catch { }

        lock (this) {
          _runningProcess.Remove(process.Id);
        }

        if (onProcessExited == null)
          return;

        onProcessExited(new { processId = process.Id, exitCode = exitCode });

      } catch {

      }
    }

    #endregion Private func

    #region IDisposable
    //--------------------------------------------------------------------------
    public void Dispose() {
      _disposing = true;

      // clear and kill all process
      try {
        lock (this) {
          foreach (var entry in _runningProcess) {
            try {
              entry.Value.process.Exited -= ProcessExited;

              if (entry.Value.track) {
                entry.Value.process.Kill();
              }
            } catch { }
          }

          _runningProcess.Clear();
        }
      } catch {

      }
    }
    #endregion IDisposable
  }
}
