﻿using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using System;
using System.Collections.Generic;

public class Client : MonoBehaviour
{
	public string clientName;
	public bool isHost;

	private bool socketReady;
	private TcpClient socket;
	private NetworkStream stream;
	private StreamWriter writer;
	private StreamReader reader;

	private List<GameClient> players = new List<GameClient>();

	private void Start()
	{
		DontDestroyOnLoad(gameObject);
	}

	public bool ConnectToServer(string host, int port)
	{
		if (socketReady)
		{
			return false;
		}
		try
		{
			socket = new TcpClient(host, port);
			stream = socket.GetStream();
			writer = new StreamWriter(stream);
			reader = new StreamReader(stream);

			socketReady = true;
		}
		catch (Exception e)
		{

		}

		return socketReady;
	}

	private void Update()
	{
		if (socketReady)
		{
			if (stream.DataAvailable)
			{
				string data = reader.ReadLine();
				if (data != null)
				{
					OnIncomingData(data);
				}
			}
		}
	}

	public void Send(string data)
	{
		if (!socketReady)
		{
			return;
		}

		writer.WriteLine(data);
		writer.Flush();
	}

	private void OnIncomingData(string data)
	{
		string[] aData = data.Split('|');

		switch (aData[0])
		{
			case "SWHO":
				for (int i = 1; i < aData.Length - 1; i++)
				{
					UerConnected(aData[i], false);
				}
				Send("CWHO|" + clientName + "|" + ((isHost) ? 1 : 0).ToString());
				break;
			case "SCNN":
                UerConnected(aData[1], false);
                break;
            case "SMOV":
                CheckerBaord.Instance.TryMove(int.Parse(aData[1]), int.Parse(aData[2]), int.Parse(aData[3]), int.Parse(aData[4]));
                break;
        }
	}

	private void UerConnected(string dataName, bool host)
	{
		GameClient c = new GameClient();
        c.name = dataName;
		players.Add(c);

		if (players.Count == 2)
		{
			GameManager.Instance.StartGame();
		}


			
	}

	private void OnApplicationQuit()
	{
		CloseSocket();
	}

	private void OnDisable()
	{
		CloseSocket();
	}

	private void CloseSocket()
	{
		if (!socketReady)
		{
			return;
		}

		writer.Close();
		reader.Close();
		socket.Close();
		socketReady = false;
	}
}

public class GameClient{
    public string name;
    public bool isHost;
}