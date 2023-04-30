using System;
using System.Diagnostics;
using System.Threading.Tasks;
using com.overwolf.wss;
using System.Security.Principal;


namespace overwolf.plugins.unittest {
    class WebSocketServerTests {
        public void Run() {
            Console.WriteLine("Starting");
            Console.WriteLine("Admin: {0}", IsUserAdministrator());

            var wss = new WebSocketServer();

            wss.onMessageReceived += (result) => {
                Console.WriteLine("Message: {0}", result);
                Debug.WriteLine("Message: {0}", result);
            };

            Task srv = wss.startServer(32110, "/erp-server/");

            string line = Console.ReadLine();

            while (line != "q") {
                line = Console.ReadLine();
            }

            wss.stopServerSync();
            Console.WriteLine("Exit");
        }
        public bool IsUserAdministrator() {
            //bool value to hold our return value
            bool isAdmin;
            WindowsIdentity user = null;
            try {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            } catch (UnauthorizedAccessException ex) {
                isAdmin = false;
                Console.WriteLine("UnauthorizedAccessException: {0}", ex);
            } catch (Exception ex) {
                isAdmin = false;
                Console.WriteLine("Exception: {0}", ex);
            } finally {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }
    }
}