using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class NetworkManager : MonoBehaviour {

	private string ConnectUrl = "ws://vrbridge.prod.loop.ms/api/socket/room?roomId="; // "ws://localhost:8800/api/socket/room?roomId=";
    public string RoomId = "dawn-darkness-SkCe6k3Tx"; // TODO
	public NotificationManager NotificationMananger;

    private SocketMessageProcessor processor = new SocketMessageProcessor();
    private bool connected = false;
    private WebSocket ws;
    private string selfId;

    // Use this for initialization
    void Start ()
    {
        this.processor.RegisterActionHandler<ConnectData>("connect", OnConnectionMessage);
        this.processor.RegisterActionHandler<UserConnectDisconnectData>("client_disconnect", OnUserDisconnectMessage);
        this.processor.RegisterActionHandler<UserConnectDisconnectData>("client_connect", OnUserConnectMessage);
		this.processor.RegisterActionHandler<NotificationData>("signal_notification", (m) => NotificationMananger.OnNotification(m));
		this.processor.RegisterActionHandler<CallData>("signal_call", (m) => NotificationMananger.OnCall(m));
		this.processor.RegisterActionHandler<PictureData>("signal_picture", (m) => NotificationMananger.OnPicture(m));
		this.processor.RegisterActionHandler<MessageData>("signal_message", (m) => NotificationMananger.OnRemoteMessage(m));
        Connect();
    }



    private void OnUserDisconnectMessage(MessageAction<UserConnectDisconnectData> obj)
    {
    }

    private void OnUserConnectMessage(MessageAction<UserConnectDisconnectData> obj)
    {
    }

    private void OnConnectionMessage(MessageAction<ConnectData> connection)
    {
        this.selfId = connection.data.you;
        this.connected = true;
    }

    private void Connect()
    {
        this.ws = new WebSocket(ConnectUrl + RoomId);
        ws.OnOpen += OnConnected;
        ws.OnMessage += OnMessage;
        ws.OnError += OnError;
        ws.OnClose += OnClose;
        ws.ConnectAsync();
    }

    private void OnClose(object sender, CloseEventArgs e)
    {
        Debug.LogWarning("Socket closed");
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogWarning("Socket error");
    }
    
    private void OnConnected(object sender, System.EventArgs e)
    {
        Debug.Log("Connected to socket");
    }
    private void OnMessage(object sender, MessageEventArgs e)
	{
		if (e.Data != null && e.Data.Length > 0) {
			Debug.Log (e.Data);
			processor.Process (e.Data);
		}
	}
}
