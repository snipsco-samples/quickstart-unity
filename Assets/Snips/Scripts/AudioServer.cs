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

public class AudioServer : MonoBehaviour
{
  private MqttClient client;
  AudioSource audioClip;
  AudioSource audio;
  AudioClip audioClipToPlay;
  public static int bufferSize = 3200;
  const int sampleRate = 16000;

  private float nextActionTime = 0.0f;
  float period = 1.0f;
  byte[] soundToPlaybyte;
  bool soundloaded = false;
  // Use this for initialization
  void Start()
  {
    client = new MqttClient(IPAddress.Parse(gameObject.GetComponent<SnipsConfig>().ipAddress), gameObject.GetComponent<SnipsConfig>().mqttPort, false, null);
    string clientId = Guid.NewGuid().ToString();
    client.Connect(clientId);
    audioClip = GetComponent<AudioSource>();
    audioClip.clip = Microphone.Start(null, true, 1, sampleRate);
  }

  void Update()
  {
  }

  void FixedUpdate()
  {
    if (Time.time > nextActionTime)
    {
      nextActionTime += period;

      byte[] rawdata = ConvertAndWriteBytes(audioClip.clip);

      if (SnipsConfig.RECORDStatic)
      {
        byte[] header = WriteHeaderbytes(rawdata);
        byte[] combined2 = new byte[rawdata.Length + header.Length];
        combined2 = header.Concat(rawdata).ToArray();
        File.WriteAllBytes(Application.dataPath + "/testfile.wav", combined2);
      }
      else
      {
        foreach (byte[] copySlice in rawdata.Slices(bufferSize))
        {
          byte[] rawdatahead = WriteHeaderbytes(copySlice);
          // do something with each slice
          byte[] combined = new byte[rawdatahead.Length + copySlice.Length];
          combined = rawdatahead.Concat(copySlice).ToArray();
          string satName = gameObject.GetComponent<SnipsConfig>().satName;
          client.Publish("hermes/audioServer/" + satName + "/audioFrame", combined, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }
      }
    }
  }

  void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
  {
    Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
  }

  static byte[] ConvertAndWriteBytes(AudioClip clip)
  {
    MemoryStream fileStream = new MemoryStream();
    var samples = new float[clip.samples];
    clip.GetData(samples, 0);
    Int16[] intData = new Int16[samples.Length];
    // Converting in 2 float[] steps to Int16[],
    // then Int16[] to Byte[]

    Byte[] bytesData = new Byte[samples.Length * 2];
    // bytesData array is twice the size of
    // dataSource array because a float converted in Int16 is 2 bytes.

    const float rescaleFactor = 32767; // to convert float to Int16
    for (int i = 0; i < samples.Length; i++)
    {
      intData[i] = (short)(samples[i] * rescaleFactor);
      Byte[] byteArr = new Byte[2];
      byteArr = BitConverter.GetBytes(intData[i]);
      byteArr.CopyTo(bytesData, i * 2);
    }

    fileStream.Write(bytesData, 0, bytesData.Length);
    byte[] rawdata = new byte[fileStream.Length];
    rawdata = fileStream.ToArray();
    return rawdata;
  }

  static byte[] WriteHeaderbytes(byte[] datas)
  {
    MemoryStream fileStream = new MemoryStream();
    int hz = 16000;
    var channels = 1;

    fileStream.Seek(0, SeekOrigin.Begin);

    Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
    fileStream.Write(riff, 0, 4);

    Byte[] chunkSize = BitConverter.GetBytes(datas.Length + 36);
    fileStream.Write(chunkSize, 0, 4);

    Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
    fileStream.Write(wave, 0, 4);

    Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
    fileStream.Write(fmt, 0, 4);

    Byte[] subChunk1 = BitConverter.GetBytes(16);
    fileStream.Write(subChunk1, 0, 4);

    UInt16 two = 2;
    UInt16 one = 1;

    Byte[] audioFormat = BitConverter.GetBytes(one);
    fileStream.Write(audioFormat, 0, 2);

    Byte[] numChannels = BitConverter.GetBytes(channels);
    fileStream.Write(numChannels, 0, 2);

    Byte[] sampleRate = BitConverter.GetBytes(hz);
    fileStream.Write(sampleRate, 0, 4);

    Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 16000*1*2
    fileStream.Write(byteRate, 0, 4);

    UInt16 blockAlign = (ushort)(channels * 2);
    fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

    UInt16 bps = 16;
    Byte[] bitsPerSample = BitConverter.GetBytes(bps);
    fileStream.Write(bitsPerSample, 0, 2);

    Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
    fileStream.Write(datastring, 0, 4);

    Byte[] subChunk2 = BitConverter.GetBytes(datas.Length);
    fileStream.Write(subChunk2, 0, 4);

    byte[] rawdata = new byte[44];
    rawdata = fileStream.ToArray();
    return rawdata;
  }
}
