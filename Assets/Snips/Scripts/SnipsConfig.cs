using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnipsConfig : MonoBehaviour
{
  [System.Serializable]
  public enum PieceType
  {
    Hero,
    Moveable,
    Solid,
    Empty
  }

  // IP address of the device running the Snips Platform
  public string ipAddress = "127.0.0.1";
  // MQTT server port
  public int mqttPort = 1883;
  // This instance is considered as a satellite
  public string satName = "sat_unity";
  // Record 1 chunk of 1s of microphone to WAV file for testing purposes
  public static bool RECORDStatic = false;
  public bool RECORD = false;
}
