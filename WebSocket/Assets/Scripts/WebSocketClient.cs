using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonData;
using UnityEngine;

public class WebSocketClient
{
    //必须在主线程调用修改Unity组件，比如在awake/start注册
    public Action<RequestCode, string> OnMessageReceived;
    public static AutoResetEvent WaitHandle = new(false);

    //建立连接和发送数据
    private readonly Message _message;
    private ClientWebSocket _clientWebSocket;
    private readonly ConcurrentQueue<string> _messageQueue;

    // 发送和接收任务
    private Task _sender;
    private Task _receiver;
    private CancellationTokenSource _cancelTokenSource;

    public WebSocketClient()
    {
        // 发送和接收任务
        this._sender = null;
        this._receiver = null;
        this._cancelTokenSource = null;

        //建立连接和发送数据
        this._clientWebSocket = null;
        this._message = new Message();
        this._messageQueue = new();
    }

    /// <summary>
    /// 外部调用开始连接服务器的方法
    /// </summary>
    /// <param name="onReceive"></param>
    /// <returns></returns>
    public async void ConnectToServer(string url)
    {
        //已经连接就不能再连接了
        if (CannotStartConnect(this._clientWebSocket)) return;

        this._clientWebSocket = new();

        //创建取消任务的对象
        _cancelTokenSource = new CancellationTokenSource();

        //创建URI并连接服务器
        Uri serverUrl = new(url);//"ws://127.0.0.1:6688"

        await ConnectAsync(serverUrl);

        static bool CannotStartConnect(ClientWebSocket socket)
        {
            var state = socket?.State;
            var isConnect = state == WebSocketState.Open || state == WebSocketState.Connecting;
            return socket != null && isConnect;
        }
    }

    /// <summary>
    /// 发送信息（后面会改成发送一个对象）
    /// </summary>
    /// <param name="message">信息内容</param>
    public void SendMessage(string message)
    {
        if (_clientWebSocket?.State != WebSocketState.Open)
            return;

        // 放入消息队列
        _messageQueue.Enqueue(message);
    }

    /// <summary>
    /// 测试用接口
    /// </summary>
    public async Task ConnectClose()
    {
        await StopConnect();
    }

    /// <summary>
    /// 开始连接服务器的任务
    /// </summary>
    /// <param name="serverUrl">连接服务器的serverUri</param>
    /// <returns>连接服务器的任务</returns>
    private async Task ConnectAsync(Uri serverUrl)
    {
        try
        {
            await _clientWebSocket.ConnectAsync(serverUrl, _cancelTokenSource.Token);
            Debug.Log("连接服务器成功!");

            // 开启发送和接收任务
            await ReceiveAndSendAsync();
        }
        catch (Exception e)
        {
            await StopConnect(WebSocketCloseStatus.NormalClosure, "连接到远程服务器错误。");
            Debug.LogError("Error: 连接到远程服务器错误。   " + e);
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    /// <param name="status">以何种状态关闭</param>
    /// <param name="statusDescription">描述</param>
    /// <returns>关闭连接的任务</returns>
    private async Task StopConnect(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string statusDescription = null)
    {
        try
        {
            if (_clientWebSocket != null && CanDisConnect(_clientWebSocket.State))
            {
                await _clientWebSocket.CloseAsync(status, statusDescription, CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            string warningData = string.Format("Error: 无法关闭跟服务器的连接！！ 错误信息：{0}", e);
            Debug.LogWarning(warningData);
        }
        finally
        {
            DisposeResources();
        }

        static bool CanDisConnect(WebSocketState state)
        {
            return state == WebSocketState.Open || state == WebSocketState.CloseSent || state == WebSocketState.CloseReceived;
        }
    }

    // 接收和发送任务
    private async Task ReceiveAndSendAsync()
    {
        // 接收消息
        this._receiver = Task.Run(() => ReceiveMessageAsync());

        // 发送消息
        this._sender = Task.Run(() => SendMessageFromQueue());

        // 开启发送和接收任务
        // 使用Task.Run在后台线程上运行异步方法
        await Task.WhenAll(_receiver, _sender);
    }


    /// <summary>
    /// 接收服务端数据的任务
    /// </summary>
    /// <returns></returns>
    private async Task ReceiveMessageAsync()
    {
        while (_clientWebSocket.State == WebSocketState.Open)
        {
            try
            {
                ArraySegment<byte> seg = new(new byte[1024]);
                WebSocketReceiveResult result = await _clientWebSocket.ReceiveAsync(seg, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await StopConnect(result.CloseStatus.Value);
                }
                this._message.AddDatas(seg.Array, result.Count);
                this._message.ReadMessage(OnProcessMessage);

            }
            catch (Exception e)
            {
                await StopConnect();
                Debug.LogError("Error: 连接服务器，接收消息出错:" + e.Message);
            }
        }
    }

    /// <summary>
    /// 从队列发送消息
    /// </summary>
    /// <returns></returns>
    private async Task SendMessageFromQueue()
    {
        while (_clientWebSocket.State == WebSocketState.Open)
        {
            if (_messageQueue.TryDequeue(out string message))
            {
                var bytes = MessageOld.PackData(RequestCode.None, ActionCode.None, message);
                ArraySegment<byte> sendData = new(bytes);
                await _clientWebSocket.SendAsync(sendData, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    //彻底释放资源
    private void DisposeResources()
    {
        //停止任务
        if (_cancelTokenSource != null)
        {
            _cancelTokenSource.Cancel();
            if (!_cancelTokenSource.IsCancellationRequested)
                _cancelTokenSource.Dispose();
        }


        //尝试释放接收信息的任务
        if (_receiver != null && IsCanDisposeTask(_receiver.Status))
        {
            _receiver.Dispose();
        }


        //尝试释放发送信息的任务
        if (_sender != null && IsCanDisposeTask(_sender.Status))
        {
            _sender.Dispose();
        }


        //释放socket对象
        _clientWebSocket?.Dispose();

        //释放消息队列
        _messageQueue.Clear();

        static bool IsCanDisposeTask(TaskStatus status)
        {
            return status == TaskStatus.RanToCompletion || status == TaskStatus.Faulted || status == TaskStatus.Canceled;
        }
    }

    /// <summary>
    /// 解析数据后的回调
    /// </summary>
    /// <param name="requestCode">请求代码</param>
    /// <param name="data">返回数据</param>
    private void OnProcessMessage(RequestCode requestCode, string data)
    {
        if (OnMessageReceived != null)
        {
            OnMessageReceived?.Invoke(requestCode, data);
        }
        WaitHandle.Set();
    }

}