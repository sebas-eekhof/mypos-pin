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
using System.Configuration;
using System.Collections.Specialized;

// https://github.com/developermypos/myPOS-SDK-dotNET/tree/master?tab=readme-ov-file#make-transactions

namespace Pin {
    internal class Program {
        private static HttpListener httpListener;
        private static myPOSTerminal terminal;
        private static string callbackURL;
        private static string COM;
        private static bool isReady = false;

        static void Main(string[] args) {
            COM = ConfigurationManager.AppSettings.Get("com_port");
            callbackURL = ConfigurationManager.AppSettings.Get("callback_url");
            Run(args);
            while(true)
                Console.Read();
        }

        private static async void Run(string[] args) {
            if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
                Console.WriteLine("\n\nPlease run the application as Administrator.\n\nAutomatically closing this window in 10 seconds...");
                Thread.Sleep(10000);
                return;
            }

            Logger.App.LogWithSpinner("Starting webserver");

            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://*:8080/");
            httpListener.Start();

            Logger.StopSpinner();
            Logger.App.Success("Webserver started succesfully");

            ReceiveHttpRequest();

            Logger.App.LogWithSpinner("Initializing MyPOS terminal SDK");

            terminal = new myPOSTerminal();

            terminal.ProcessingFinished += TerminalResult;

            terminal.SetLanguage(Language.English);

            Logger.StopSpinner();
            Logger.App.Success("MyPOS terminal SDK initialized");

            _ = connectToTerminal();
        }

        private static async Task checkConnection() {
            while(isReady) {
                RequestResult statusResult = terminal.GetStatus();
                if(statusResult == RequestResult.NotInitialized) {
                    isReady = false;
                    Logger.App.Error("Lost connection to MyPOS terminal");
                    _ = connectToTerminal();
                }
                await Task.Delay(5000);
            }
        }

        private static async Task connectToTerminal() {
            Logger.App.LogWithSpinner($"Connecting to MyPOS terminal via port \"{COM}\"");

            RequestResult initializeResult = terminal.Initialize(COM);

            if(initializeResult == RequestResult.NotInitialized) {
                Logger.StopSpinner();
                Logger.App.Warn("Terminal not connected, retrying in 5 seconds");
                await Task.Delay(5000);
                await connectToTerminal();
                return;
            }

            bool result = await PosHelper.waitFor(() => isReady, 10);
            Logger.StopSpinner();
            if(!result) {
                Logger.App.Warn("Terminal not initialized within 10 seconds, retrying in 5 seconds");
                await Task.Delay(5000);
                await connectToTerminal();
                return;
            }
            Logger.App.Success("Connected to MyPOS terminal");
            _ = checkConnection();
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
                    StartPinTransaction(double.Parse(rawAmount), rawDescription);
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                } catch(Exception ex) {
                    Logger.App.Error(ex.Message);
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            } else if(url.AbsolutePath.StartsWith("/cancel")) {
                context.Response.Headers.Clear();
                context.Response.SendChunked = false;
                context.Response.Headers.Add("Server", string.Empty);
                context.Response.Headers.Add("Date", string.Empty);
                context.Response.StatusCode = 200;
                context.Response.Close();
                Logger.App.Info("Canceling transaction based on request");
                CheckRequestResult(terminal.VendingStop());
            } else if(url.AbsolutePath.StartsWith("/callback")) {
                context.Response.Headers.Clear();
                context.Response.SendChunked = false;
                context.Response.Headers.Add("Server", string.Empty);
                context.Response.Headers.Add("Date", string.Empty);
                context.Response.StatusCode = 200;
                context.Response.Close();
                Logger.App.Warn("Please change the callback url setting in the .config file");
            } else {
                context.Response.Headers.Clear();
                context.Response.SendChunked = false;
                context.Response.StatusCode = 404;
                context.Response.Headers.Add("Server", string.Empty);
                context.Response.Headers.Add("Date", string.Empty);
                context.Response.Close();
            }
        }

        private static void StartPinTransaction(double amount, string description) {
            Logger.App.Info($"Starting MyPOS transaction: EUR {amount.ToString()} ({description})");
            CheckRequestResult(terminal.Purchase(amount, Currencies.EUR, description));
        }

        protected static void TerminalResult(ProcessingResult res) {
            isReady = true;
            Logger.StopSpinner();
            if(res.Method == Method.PURCHASE)
                if(res.Status == TransactionStatus.Success || res.Status == TransactionStatus.SuccessWithInfo)
                    TransactionSucceeded();
                else
                    TransactionFailed(res.Status);
        }

        private static void CheckRequestResult(RequestResult result) {
            if(result == RequestResult.NotInitialized) {
                isReady = false;
                _ = connectToTerminal();
            }
        }

        private static void TransactionFailed(TransactionStatus reason) {
            Logger.App.Error($"Transaction failed ({reason})");
            new WebClient().DownloadString($"{callbackURL}?success=false&reason={reason.ToString()}");
        }

        private static void TransactionSucceeded() {
            Logger.App.Success($"Transaction succeeded");
            new WebClient().DownloadString($"{callbackURL}?success=true");
        }
    }
}
