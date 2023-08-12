using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SimpleTest : MonoBehaviour
{
    public InputField Input;
    public Text BytesText;
    public Text MessageText;

    public Button ConnectBtn;
    public Button SendBtn;
    public Button CloseBtn;


    UnityWebSocketClient webSocketClient = new();

    void Start()
    {
        Input.text = "Hello World!!  你好 世界！！ こんにちは";
        ConnectBtn.onClick.AddListener(OnWebConnectClick);
        SendBtn.onClick.AddListener(OnWebSendClick);
        CloseBtn.onClick.AddListener(OnCloseConnectClick);
    }

    private void OnWebConnectClick()
    {
        Debug.Log("开始连接服务器。");

        webSocketClient.ConnectToServer();

        webSocketClient.OnMessageReceived += OnReceiveData;

    }

    private void OnWebSendClick()
    {
        // for (int i = 0; i < 1000; i++)
        // {
        //     webSocketClient.SendMessage(i.ToString() + "aa");
        // }
        webSocketClient.SendMessage(Input.text);
    }

    private void OnCloseConnectClick()
    {
        Debug.Log("关闭服务器连接。");
        webSocketClient.ConnectionClose();
    }

    private void OnReceiveData(byte[] data)
    {
        var receiveStr = Encoding.UTF8.GetString(data);
        string str = string.Format("接收到的消息:{0}", receiveStr);
        BytesText.text = GetBytesOriginal(data);
        MessageText.text = str;

        string GetBytesOriginal(byte[] data)
        {
            StringBuilder stringBuilder = new StringBuilder(data.Length * 2);
            for (int i = 0; i < data.Length; i++)
            {
                stringBuilder.Append(data[i]);
                stringBuilder.Append(",");
            }
            return stringBuilder.ToString();
        }
    }
}
