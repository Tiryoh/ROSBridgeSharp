﻿using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using WebSocketSharp;
using ROSBridgeSharp.Messages;

namespace ROSBridgeSharp
{
    public class RBSocket : MonoBehaviour
    {
        internal string IPAddress = "192.168.3.28"; // IPアドレス
        internal string Port = "9000"; //ポート番号

        private WebSocket ws; // WebSocketSharp
        private static RBSocket instance; // インスタンスの実体
        private readonly Queue<string> WaitingSendOperationQueue = new Queue<string>();
        private readonly Queue<string> CompletedSendOperationQueue = new Queue<string>();

        private List<UnAdvertise> UnAdvertises = new List<UnAdvertise>();
        private List<SubscribeManager> Subscribers = new List<SubscribeManager>();
        private bool isConnected = false;

        // インスタンスのプロパティー
        public static RBSocket Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<RBSocket>();
                    if (instance != null)
                    {
                        Debug.Log("RBScoket Instance OK.");
                    }
                    else
                    {
                        Debug.LogError("RBSocket Instance Error.");
                    }
                }
                return instance;
            }
        }

        // 接続状態のプロパティー
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
        }

        // 現在設定されているホストとの接続可能か判定
        public bool IsConnectable
        {
            get
            {
                if (PingCheck())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        // 外部から接続要求をする関数
        public void Connect()
        {
            if (!isConnected)
            {
                ws = new WebSocket("ws://" + IPAddress + ":" + Port + "/");
                ws.OnOpen += (sender, e) =>
                {
                    Debug.Log("WebSocket Opened.");
                    isConnected = true;
                };

                ws.OnMessage += (sender, e) =>
                {
                    var msg = JsonUtility.FromJson<QueueSubscribeMessage>(e.Data);
                    foreach (var sm in Subscribers)
                    {
                        if (msg.topic == sm.Topic)
                        {
                            sm.HandlerFunction(e.Data);
                        }
                    }
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.Log("WebSocket Error Message: " + e.Message);
                };

                ws.OnClose += (sender, e) =>
                {
                    Debug.Log("WebSocket Closed.");
                    isConnected = false;
                };

                if (PingCheck())
                {
                    ws.Connect();
                }
            }
        }

        // Pingを飛ばして、サーバに接続できるか確認
        private bool PingCheck()
        {
            Ping WSPing = new Ping(IPAddress);
            while (!WSPing.isDone) { }
            if (WSPing.time >= 0)
            {
                Debug.Log("Ping Check OK.");
                return true;
            }
            else
            {
                Debug.LogError("Ping Check Fault.");
                return false;
            }
        }

        // 接続を切るときの関数
        public void Disconnect()
        {
            if (isConnected)
            {
                // 終了時に、送信されていないデータは棄却
                WaitingSendOperationQueue.Clear();
                // サブスクライバの停止申請
                AllUnSubscribe();
                // OperationSend()経由で送られたデータを保持
                AgainSendingData();

                ws.Close();
                isConnected = false;
            }
        }

        // 更新処理
        private void Update()
        {
            if (isConnected && WaitingSendOperationQueue.Count > 0)
            {
                string message = WaitingSendOperationQueue.Dequeue();
                Debug.Log(message);
                ws.Send(message);
                // 送信済みQueueに追加
                CompletedSendOperationQueue.Enqueue(message);
            }
        }

        // サブスクライバをセットする関数
        public void SetSubscriber(SubscribeManager sm)
        {
            Subscribers.Add(sm);
        }

        // オペレーションに関する送信用(Queueが再接続時に再送信される)
        public void OperationSend(string m)
        {
            WaitingSendOperationQueue.Enqueue(m);
        }

        // トピックなどの送信用
        public void MessageSend(string m)
        {
            ws.Send(m);
        }

        // パブリッシュの申請済みかどうか。
        public bool IsAdvertise(string t)
        {
            foreach (var ua in UnAdvertises)
            {
                if (ua.topic == t)
                {
                    return true;
                }
            }
            return false;
        }

        // 指定のトピックのサブスクライバを停止する
        public void UnSubscribe(string t)
        {
            OperationMessage unsubscribe = new OperationMessage();
            unsubscribe.op = "unsubscribe";
            unsubscribe.topic = t;

            string data = JsonUtility.ToJson(unsubscribe);
            ws.Send(data);
        }

        // パブリッシュ終了時に送信するUnAdvertiseを追加
        public void AddUnAdvertise(UnAdvertise a)
        {
            UnAdvertises.Add(a);
        }

        // すべてのサブスクライブを停止
        private void AllUnSubscribe()
        {
            foreach (var s in Subscribers)
            {
                UnSubscribe(s.Topic);
            }
        }

        // UnAdvertiseを送信
        public void UnAdvertise(UnAdvertise ua)
        {
            string data = JsonUtility.ToJson(ua);
            ws.Send(data);
        }

        // すべてのUnAdvertiseを送信
        private void AllUnAdvertise()
        {
            foreach (var ua in UnAdvertises)
            {
                ws.Send(JsonUtility.ToJson(ua));
            }
        }

        // 再接続時に、オペレーション系を再送信
        private void AgainSendingData()
        {
            if(CompletedSendOperationQueue.Count > 0) {
                string data = CompletedSendOperationQueue.Dequeue();
                Debug.Log(data);
                WaitingSendOperationQueue.Enqueue(data);
                if (CompletedSendOperationQueue.Count > 0)
                {
                    AgainSendingData();
                }
            }
        }
    }
}