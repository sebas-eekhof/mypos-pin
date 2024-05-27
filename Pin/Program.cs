using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using myPOS;
using System.Web;

// https://github.com/developermypos/myPOS-SDK-dotNET/tree/master?tab=readme-ov-file#make-transactions

namespace Pin {
    internal class Program {
        private static HttpListener httpListener;
        private static myPOSTerminal terminal;
        private static string COM = "COM3";

        static void Main(string[] args) {
            if(args.Length >= 1)
                COM = args[0];

            if(!IsAdministrator()) {
                Console.WriteLine("\n\nPlease run the application as Administrator.\n\nAutomatically closing this window in 10 seconds...");
                Thread.Sleep(10000);
                return;
            }

            Console.WriteLine("[APP] Initializing myPOS terminal");

            terminal = new myPOSTerminal();
            RequestResult initializeResult = terminal.Initialize(COM);
            terminal.SetLanguage(Language.English);

            if(initializeResult.ToString() == "NotInitialized") {
                Console.WriteLine($"\n\nCannot initialize myPOS on {COM}, please check the connection.\n\nAutomatically closing this window in 10 seconds...");
                Thread.Sleep(10000);
                return;
            }

            terminal.ProcessingFinished += TerminalResult;

            Console.WriteLine("[APP] Terminal connected succesfully");
            Console.WriteLine("[APP] Starting webserver");

            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://*:8080/");
            httpListener.Start();

            Console.WriteLine("[APP] Webserver started succesfully");

            ReceiveHttpRequest();
            while(true)
                Console.Read();
        }

        private static void ReceiveHttpRequest() {
            httpListener.BeginGetContext(new AsyncCallback(HttpCallback), httpListener);
        }

        private static void HttpCallback(IAsyncResult result) {
            if(!httpListener.IsListening)
                return;

            var context = httpListener.EndGetContext(result);
            var request = context.Request;
            Uri url = request.Url;
            var query = HttpUtility.ParseQueryString(url.Query);

            Console.WriteLine($"[APP] {url.AbsolutePath}{url.Query}");

            ReceiveHttpRequest();

            if(url.AbsolutePath.StartsWith("/start")) {
                string rawAmount = query.Get("amount");
                string rawDescription = query.Get("description");
                context.Response.Headers.Clear();
                context.Response.SendChunked = false;
                context.Response.Headers.Add("Server", string.Empty);
                context.Response.Headers.Add("Date", string.Empty);
                if(rawAmount.Trim().Length == 0 || rawDescription.Trim().Length == 0) {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
                try {
                    startPinTransaction(double.Parse(rawAmount), rawDescription);
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                } catch(Exception ex) {
                    Console.WriteLine($"[APP] Error: {ex.Message}");
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            } else {
                context.Response.Headers.Clear();
                context.Response.SendChunked = false;
                context.Response.StatusCode = 404;
                context.Response.Headers.Add("Server", string.Empty);
                context.Response.Headers.Add("Date", string.Empty);
                context.Response.Close();
            }
        }

        private static void startPinTransaction(double amount, string description) {
            Console.WriteLine($"[APP] Starting myPOS transaction: EUR {amount.ToString()} ({description})");
            RequestResult transactionResult = terminal.Purchase(3.00, Currencies.EUR, "Description");
            Console.WriteLine($"[myPOS] {transactionResult}");
        }

        private static bool IsAdministrator() {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);

        }

        protected static void TerminalResult(ProcessingResult res) {
            if(res.TranData == null)
                return;
            // Do something with result
        }
    }
}
