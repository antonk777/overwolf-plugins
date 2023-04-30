using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Diagnostics;
using System.Security.Policy;
using System.Linq;

namespace com.overwolf.wss {
    public class WebSocketServer : IDisposable {
        private readonly List<WebSocket> connections = new List<WebSocket> { };
        private HttpListener listener = null;

        #region Events
        public event Action<object> onMessageReceived;
        #endregion Events

        public async Task startServer(int port, string path = "/", Action<object> callback = null) {
            if (listener != null) {
                await stopServer();
            }

            try {
                var url = $"http://[::1]:{port}{path}";

                listener = new HttpListener();

                listener.Prefixes.Add(url);
                listener.Start();

                Console.WriteLine($"Started: {url} {listener.IsListening}");
                Debug.WriteLine($"Started: {url} {listener.IsListening}");

                callback?.Invoke(new { success = true });
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
                callback?.Invoke(new { success = false, error = ex.ToString() });
            }

            while (listener.IsListening) {
                HttpListenerContext listenerContext = await listener.GetContextAsync();

                Console.WriteLine($"WS request {listenerContext.Request.IsWebSocketRequest}");
                Debug.WriteLine($"WS request {listenerContext.Request.IsWebSocketRequest}");

                if (listenerContext.Request.IsWebSocketRequest) {
                    processRequest(listenerContext);
                } else {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }
        public async Task stopServer(Action<object> callback = null) {
            try {
                foreach (WebSocket connection in connections) {
                    await connection.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "",
                        CancellationToken.None
                    );
                }

                connections.Clear();
                listener?.Close();

                listener = null;

                callback?.Invoke(new { success = true });
            } catch (Exception ex) {
                callback?.Invoke(new { success = false, error = ex.ToString() });
            }
        }
        public void stopServerSync() {
            foreach (WebSocket connection in connections) {
                connection.Abort();
            }

            connections.Clear();
            listener?.Close();
            listener = null;
        }
        public async void send(string message, Action<object> callback = null) {
            try {
                byte[] buffer = Encoding.UTF8.GetBytes(message);

                var tasks = new List<Task>();

                foreach (WebSocket connection in connections) {
                    tasks.Add(sendMessageToClient(connection, buffer));
                }
                
                await Task.WhenAll(tasks);

                callback?.Invoke(new { success = true });
            } catch (Exception ex) {
                callback?.Invoke(new { success = false, error = ex.ToString() });
            }
        }

        #region privates Func
        private Task sendMessageToClient(WebSocket connection, byte[] buffer) {
            return connection.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                false,
                CancellationToken.None
            );
        }
        private async void processRequest(HttpListenerContext listenerContext) {
            WebSocketContext webSocketContext;

            try {
                webSocketContext = await listenerContext.AcceptWebSocketAsync("ws");
            } catch (Exception ex) {
                Console.WriteLine($"processRequest(): exception: {ex}");
                Debug.WriteLine($"processRequest(): exception: {ex}");
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            connections.Add(webSocket);

            try {
                byte[] receiveBuffer = new byte[1024];

                while (webSocket.State == WebSocketState.Open) {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer),
                        CancellationToken.None
                    );

                    if (receiveResult.MessageType == WebSocketMessageType.Close) {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "",
                            CancellationToken.None
                        );
                    } else if (receiveResult.EndOfMessage) {
                        var bytes = new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count).ToArray();

                        onMessageReceived(Encoding.UTF8.GetString(bytes));
                        //await webSocket.SendAsync(
                        //    new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count),
                        //    receiveResult.MessageType,
                        //    receiveResult.EndOfMessage,
                        //    CancellationToken.None
                        //);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine("Exception: {0}", e);
            } finally {
                if (webSocket != null) {
                    connections.Remove(webSocket);
                    webSocket.Dispose();
                }
            }
        }
        #endregion privates Func

        #region IDisposable
        public void Dispose() {
            // clear and kill all process
            try {
                lock (this) {
                    stopServerSync();
                }
            } catch {

            }
        }
        #endregion IDisposable
    }
}
