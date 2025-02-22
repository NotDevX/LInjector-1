﻿using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LInjector.Classes
{
    public class WebComs
    {
        private static readonly object lockObject = new object();
        private static WebComs instance;
        private WebSocket webSocket;

        public WebComs() { }

        public static WebComs GetInstance()
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new WebComs();
                    }
                }
            }
            return instance;
        }

        public async Task Start()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5343/");
            listener.Start();

            try
            {
                while (true)
                {
                    var context = await listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        await ProcessWebSocketRequest(context);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebSocket Error: " + ex.Message, "LInjector | Error", MessageBoxButton.OK);
            }
            finally
            {
                listener.Close();
            }
        }

        public async Task SendMessage(string message)
        {
            WebSocket socket;

            lock (lockObject)
            {
                socket = webSocket;
            }

            try
            {
                if (socket != null && socket.State == WebSocketState.Open)
                {
                    byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    MessageBox.Show("WebSocket Error: WebSocket not initialized or closed.", "LInjector | Error", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebSocket Error: " + ex.Message, "LInjector | Error", MessageBoxButton.OK);
            }
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            var wsContext = await context.AcceptWebSocketAsync(null);

            using (WebSocket socket = wsContext.WebSocket)
            {
                this.webSocket = socket;

                try
                {
                    byte[] buffer = new byte[1024];
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            CustomCw.rconsoleprint(message, "lgray");

                            string responseMessage = "Received: " + message;
                            await SendMessage(responseMessage);
                        }
                    }
                    while (!result.EndOfMessage);
                }
                catch (WebSocketException ex)
                {
                    MessageBox.Show("WebSocket Error: " + ex.Message, "LInjector | Error", MessageBoxButton.OK);
                }
                finally
                {
                    socket.Dispose(); // Ensure WebSocket resources are released
                }
            }
        }
    }

}
