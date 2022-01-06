using Communiction.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerStatus : MonoBehaviour {
    public TextMesh text;
    private bool wasConnected = false;
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    private void FixedUpdate() {
        if (Client.Connected != wasConnected) {
            wasConnected = Client.Connected;
            text.text = wasConnected ? "Connected": "Searching";
            text.text += "\n" + Client.ServerName;
        }
    }
}
