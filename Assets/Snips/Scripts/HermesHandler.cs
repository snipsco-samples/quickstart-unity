using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Linq;
using System.IO;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using SimpleJSON;
using TMPro;
[RequireComponent(typeof(AudioSource))]

public class HermesHandler : MonoBehaviour
{
  private MqttClient client;
  string topicHotword = "hermes/hotword/default/detected";
  string topicFullASR = "hermes/asr/textCaptured";
  string topicIntents = "hermes/intent/#";
  public GameObject textHandler;
  string asr = "";
  // Use this for initialization
  void Start()
  {
    SnipsConfig config = gameObject.transform.parent.gameObject.GetComponent<SnipsConfig>();
    client = new MqttClient(IPAddress.Parse(config.ipAddress), config.mqttPort, false, null);
    client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
    string clientId = Guid.NewGuid().ToString();
    client.Connect(clientId);
    client.Subscribe(new string[] { topicHotword }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
    client.Subscribe(new string[] { topicFullASR }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
    client.Subscribe(new string[] { topicIntents }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
  }

  // Update is called once per frame
  void Update()
  {
    textHandler.GetComponent<TMP_Text>().text = asr;
  }
  void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
  {
    if (e.Topic == topicHotword)
    {
      Debug.Log("Hotword detected");
    }
    if (e.Topic == topicFullASR)
    {
      string json = System.Text.Encoding.UTF8.GetString(e.Message);
      JSONNode data = JSON.Parse(json);
      Debug.Log(data["text"].Value);
      asr = data["text"].Value;
    }

    if (e.Topic.Remove(e.Topic.LastIndexOf("/")) == topicIntents.Remove(topicIntents.LastIndexOf("/")))
    {
      string json = System.Text.Encoding.UTF8.GetString(e.Message);
      JSONNode data = JSON.Parse(json);
      Debug.Log("json : " + json);
      Debug.Log("data : " + data);
      Debug.Log("intent : " + data["intent"]["intentName"]);
      Debug.Log("slotName : " + data["slots"][0]["slotName"]);
      Debug.Log("slots : " + data["slots"][0]["value"]["value"]);
    }
  }
}
