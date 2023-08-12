using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Net.WebSockets;
using System.Text;
using System.Net.Sockets;

namespace GameServerTest.Servers
{
    public class Client
    {
        private WebSocket _clientSocket;
        private Server _server;
        private MySqlConnection? _connection;

        public Client(WebSocket socket, Server server)
        {
            this._clientSocket = socket;
            this._server = server;
        }

        #region Public
        public void Start()
        {
            //HandleReceive();
        }

        public void Close()
        {
            //关闭socket连接
            CloseClientConnection(WebSocketCloseStatus.NormalClosure, "主动关闭", CancellationToken.None);
        }

        /// <summary>
        /// 接收客户端持续发送的数据
        /// </summary>
        /// <returns>接收数据的异步任务</returns>
        public async Task HandleReceive()
        {
            try
            {
                byte[] buffer = new byte[1024];
                ArraySegment<byte> seg = new ArraySegment<byte>(buffer);

                while (true)
                {
                    var result = await this._clientSocket.ReceiveAsync(seg, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        OnProcessMessage(seg.Array, result.Count);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        CloseClientConnection(result.CloseStatus.Value, "接收到关闭信号", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                // 关闭后的异常,不需要处理
                Console.WriteLine(string.Format("关闭客户端连接。{0}", ex));
                CloseClientConnection(WebSocketCloseStatus.NormalClosure, "OperationCanceled", CancellationToken.None);
            }
            catch (Exception ex)
            {
                // 其他意外异常,需要记录日志或处理
                Console.WriteLine(string.Format("Error: 接收客户端消息异常： {0}", ex));
                CloseClientConnection(WebSocketCloseStatus.NormalClosure, "Exception", CancellationToken.None);
            }

        }

        /// <summary>
        /// 发送给客户端数据
        /// </summary>
        /// <param name="data">发送的数据</param>
        public async void Send(byte[] data)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(data);
            await SendAsync(buffer);
        }

        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        public async void CloseClientConnection(WebSocketCloseStatus status, string? statusDescription, CancellationToken cancellationToken)
        {
            var isClose = await CloseConnection(status, statusDescription, cancellationToken);

            //从server中删除客户端
            if (isClose)
            {
                Console.WriteLine(string.Format("关闭客户端与服务端连接，Des:{0}.", statusDescription));
                this._server.ReomoveClient(this);
            }
        }
        #endregion

        #region Privates
        /// <summary>
        /// 异步发送给客户端数据的方法
        /// </summary>
        /// <param name="buffer">数据数组</param>
        /// <returns>异步任务</returns>
        private async Task SendAsync(ArraySegment<byte> buffer)
        {
            await this._clientSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        /// <summary>
        /// 关闭客户端Socket连接的方法
        /// </summary>
        /// <returns>任务</returns>
        public async Task<bool> CloseConnection(WebSocketCloseStatus status, string? statusDescription, CancellationToken cancellationToken)
        {
            if (CanDisConnect(this._clientSocket.State))
            {
                await this._clientSocket.CloseOutputAsync(status, statusDescription, cancellationToken);
                await this._clientSocket.CloseAsync(status, statusDescription, cancellationToken);
                return true;
            }
            return false;

            static bool CanDisConnect(WebSocketState state)
            {
                return state == WebSocketState.Open || state == WebSocketState.CloseSent || state == WebSocketState.CloseReceived;
            }
        }
        #endregion

        #region Callback
        private void OnProcessMessage(byte[] data, int validLength)
        {
            var value = GetVaildDatas(data, validLength);
            _server.HandleRequest(value, this);
            Send(value);

            byte[] GetVaildDatas(byte[] data, int validLength)
            {
                var result = new byte[validLength];
                for (int i = 0; i < validLength; i++)
                {
                    result[i] = data[i];
                }
                return result;
            }
        }
        #endregion

    }
}
