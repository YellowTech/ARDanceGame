using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PoseTeacher;

public class PreviewManager : MonoBehaviour {
    public int nrTracks = 2;
    public AvatarDisplay[] displays;
    public Transform[] hips;

    private float[] timers;
    private float[] danceEnds;
    private int[] danceIndex;
    private AudioSource audioSource;

    private bool doneSetup = false;

    void Start() {
        Setup();
    }

    // Update is called once per frame
    void Update() {
        if (!doneSetup) {
            Setup();
        } else {
            for (int i = 0; i < nrTracks; i++) {
                float currentTime = Time.time - timers[i];

                // if dance should begin anew
                if(currentTime > danceEnds[i]) {
                    timers[i] = Time.time;
                    danceIndex[i] = 0;
                } else {
                    float offset = currentTime - ClientManager.openDanceData[i].poses[danceIndex[i]].timestamp;
                    displays[i].SetPose(ClientManager.openDanceData[i].GetInterpolatedPose(danceIndex[i], out danceIndex[i], offset).toPoseData());
                    hips[i].localPosition = new Vector3(0, 87.62761f, 0);
                }
            }
        }
    }

    private void OnEnable() {
        Setup();
    }

    private void Setup() {
        if (ClientManager.openLoaded) {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.Pause();
            timers = new float[nrTracks];
            danceEnds = new float[nrTracks];
            danceIndex = new int[nrTracks];

            for (int i = 0; i < nrTracks; i++) {
                timers[i] = Time.time;
                int count = ClientManager.openDanceData[i].poses.Count;
                danceEnds[i] = ClientManager.openDanceData[i].poses[count - 1].timestamp;
                danceIndex[i] = 0;
            }

            doneSetup = true;
        }
    }

    public void Preview(int nr) {
        audioSource.clip = ClientManager.openPerformance[nr].SongObject.SongClip;
        audioSource.Play();
        danceIndex[nr] = 0;
        timers[nr] = Time.time;
    }

    public void Play(int nr) {
        audioSource.Pause();
        gameObject.SetActive(false);
        ClientManager.Instance.LoadSong(nr, true);
    }
}
