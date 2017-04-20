using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


[Serializable]
class MessageActionBase
{
    public string action;
}

[Serializable]
class UserConnectDisconnectData
{
    public string client;
}

[Serializable]
class BaseSignalData { 
	public string type;
	public string id; 
	public string notificationImage;
}

[Serializable]
class CallData : BaseSignalData { 
	public long startTime;
	public long endTime; 
	public string callType; 
	public string contact;
	public string phone; 
}

[Serializable]
class MessageData : BaseSignalData { 
	public long time;
	public string messageType;
	public string contact;
	public string phone;
	public string content;
	public long threadId; 
}

[Serializable]
class NotificationData : BaseSignalData { 
	public string iconImage;
	public long time; 
	public string title;
	public string content;
	public string packageName;
	public string applicationName; 
}

[Serializable]
class PictureData : BaseSignalData { 
	public long time;
}
	
[Serializable]
class MessageAction<T> : MessageActionBase where T : class
{
    public string from;
    public T data;
}

[Serializable]
class ConnectData
{
    public string you;
    public string[] clients;
    public RoomData room;
}

[Serializable]
class RoomData
{
    public string caption;
    public string wallColor;
    public string name;
}