using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks; //.net6.0不需要引入就可以使用Task

namespace LearnServer
{
    public class WebServerAsync
    {
        private MessageOld message = new MessageOld();
        public async void StartServerASync()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:6688/");
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
                    WebSocket socket = wsContext.WebSocket;

                    Console.WriteLine("客户端连接成功，开启新的线程，与客户端对话...");

                    // 开启新的线程处理通信
                    Thread t = new Thread(async () => await HandleSocket(socket));
                    t.Start();
                }
            }
        }

        // 处理客户端连接
        private async Task HandleSocket(WebSocket socket)
        {
            // 给客户端发送问候消息
            string msg = "Hello WebSocket!";
            ArraySegment<byte> byteBuffer = new(Encoding.UTF8.GetBytes(msg));
            await socket.SendAsync(byteBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

            // 初始化缓冲区
            byte[] buffer = new byte[1024];
            ArraySegment<byte> seg = new ArraySegment<byte>(buffer);

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(seg, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(result.CloseStatus.Value, null, CancellationToken.None);
                        Console.WriteLine("客户端主动关闭会话~");
                        break;
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(seg.Array, 0, result.Count);
                        Console.WriteLine("收到:" + message);

                        // await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("echo: " + message)),
                        //                         WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("连接出错:" + e.Message);
            }
        }
    }
}