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
using WWUtils;
using WWUtils.Audio;

[RequireComponent(typeof(AudioSource))]

public class TTSHandler : MonoBehaviour
{
  private MqttClient client;

  AudioSource audio;
  AudioClip audioClipToPlay;
  byte[] soundToPlaybyte;
  bool soundloaded = false;
  string topicID = "";
  SnipsConfig config;

  void Start()
  {
    config = gameObject.transform.parent.gameObject.GetComponent<SnipsConfig>();
    client = new MqttClient(IPAddress.Parse(config.ipAddress), config.mqttPort, false, null);
    client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
    string clientId = Guid.NewGuid().ToString();
    client.Connect(clientId);
    client.Subscribe(new string[] { "hermes/audioServer/" + config.satName + "/playBytes/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

    audio = GetComponent<AudioSource>();
  }

  void Update()
  {
    if (soundloaded)
    {
      WAV wav = new WAV(soundToPlaybyte);
      audioClipToPlay = AudioClip.Create("testSound", wav.SampleCount, 1, wav.Frequency, false, false);
      audioClipToPlay.SetData(wav.LeftChannel, 0);

      audio.clip = audioClipToPlay;
      audio.Play();
      string dataToSendString = "{\"id\":\"" + topicID + "\",\"siteId\":\"" + config.satName + "\"}";
      byte[] dataToSend = new byte[dataToSendString.Length];
      dataToSend = System.Text.Encoding.UTF8.GetBytes(dataToSendString);
      client.Publish("hermes/audioServer/" + config.satName + "/playFinished", dataToSend, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);

      soundloaded = false;
    }
  }
  void FixedUpdate()
  {
  }

  void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
  {
    Debug.Log(e.Topic);
    string topic = e.Topic;
    topicID = topic.Split('/').Last();
    soundToPlaybyte = new byte[e.Message.Length];
    Array.Copy(e.Message, 0, soundToPlaybyte, 0, e.Message.Length - 4000);
    soundloaded = true;
  }
}
