using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace GameServerTest.Servers
{
    public class Server
    {
        private readonly string _uriPrefix;
        private WebSocket? _serverWebSocket;
        private List<Client> _clientList = new List<Client>();

        public Server(string ipStr, int port)
        {
            _uriPrefix = string.Format("http://{0}:{1}/", ipStr, port);
        }

        /// <summary>
        /// 启动服务器连接
        /// </summary>
        public async void StartServerASync()
        {
            //创建Listerer
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(_uriPrefix);
            listener.Start();

            Console.WriteLine("WebSocket服务器启动");

            while (true)
            {
                // 接收请求
                HttpListenerContext context = await listener.GetContextAsync();

                // 检查是否为WebSocket请求
                if (context.Request.IsWebSocketRequest)
                {
                    // 接受WebSocket连接
                    HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);

                    //接受链接后创建客户端对象并存储
                    WebSocket clientSocket = wsContext.WebSocket;
                    Client client = new Client(clientSocket, this);
                    this._clientList.Add(client);
                    Console.WriteLine("客户端连接成功，开启新的线程，与客户端对话...");

                    // 开启新的线程处理通信
                    Thread t = new Thread(async () => await HandleSocket(client));
                    t.Start();
                }
            }
        }

        public void HandleRequest(byte[] data, Client client)
        {
            //this._controllerManager.HandleRequest(requestCode, actionCode, data, client);
            var str = Encoding.UTF8.GetString(data);
            Console.WriteLine(string.Format("data: {0}", str));
        }

        public bool ReomoveClient(Client client)
        {
            lock (this._clientList)
            {
                var result = this._clientList.Remove(client);
                return result;
            }
        }

        private async Task HandleSocket(Client client)
        {
            await client.HandleReceive();
        }
    }
}
