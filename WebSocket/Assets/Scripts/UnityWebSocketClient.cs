using System;
using UnityWebSocket;
using UnityEngine;
using System.Text;

public class UnityWebSocketClient
{
    private const string ADDRESS = "ws://127.0.0.1:6688";
    //建立连接和发送数据
    private WebSocket _socket;

    //Log
    private bool _logMessage = true;

    //解析数据给外界广播
    public Action<byte[]> OnMessageReceived;

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public void ConnectToServer()
    {
        try
        {
            this._socket = new WebSocket(ADDRESS);
            this._socket.OnOpen += Socket_OnOpen;
            this._socket.OnMessage += Socket_OnMessage;
            this._socket.OnClose += Socket_OnClose;
            this._socket.OnError += Socket_OnError;
            this._socket.ConnectAsync();
        }
        catch (Exception e)
        {
            string errorData = string.Format("Error: 无法连接到服务器，请检查您的网络连接！！ 错误信息：{0}", e);
            Debug.LogError(errorData);
            throw;
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void ConnectionClose()
    {
        try
        {
            this._socket.CloseAsync();
        }
        catch (Exception e)
        {
            string warningData = string.Format("Error: 无法关闭跟服务器的连接！！ 错误信息：{0}", e);
            Debug.LogWarning(warningData);
        }
    }

    /// <summary>
    /// 给服务器发送消息
    /// </summary>
    /// <param name="message">文本数据内容</param>
    public void SendMessage(string message)
    {
        //未连接，不能发送消息
        if (this._socket?.ReadyState != WebSocketState.Open) return;

        byte[] data = Encoding.UTF8.GetBytes(message);
        this._socket.SendAsync(data);
    }
    #region PrivateTools

    /// <summary>
    /// 这个脚本中统一的Log输出
    /// </summary>
    /// <param name="str">Log内容</param>
    private void AddLog(string str)
    {
        if (!_logMessage) return;
        Debug.Log(str);
    }

    /// <summary>
    /// 解析数据后的回调
    /// </summary>
    /// <param name="data">从服务端收到的数据</param>
    private void OnProcessMessage(byte[] data)
    {
        OnMessageReceived?.Invoke(data);
    }

    #endregion
    #region Callback
    private void Socket_OnOpen(object sender, OpenEventArgs e)
    {
        AddLog(string.Format("连接服务器成功!!!  {0}", e));
    }

    private void Socket_OnMessage(object sender, MessageEventArgs e)
    {
        if (e.IsBinary)
        {
            OnProcessMessage(e.RawData);
            AddLog(string.Format("接收到byte数组消息, Bytes ({1}): {0}", e.Data, e.RawData.Length));
        }
        else if (e.IsText)
        {
            AddLog(string.Format("接收到字符串消息: {0}", e.Data));
        }
    }

    private void Socket_OnClose(object sender, CloseEventArgs e)
    {
        AddLog(string.Format("关闭连接: StatusCode: {0}, Reason: {1}", e.StatusCode, e.Reason));
    }

    private void Socket_OnError(object sender, ErrorEventArgs e)
    {
        AddLog(string.Format("Socket连接错误 Error: {0}", e.Message));
    }
    #endregion
}