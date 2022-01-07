using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Communiction.Client;
using Communiction.Util;

namespace PoseTeacher {

    public class ClientManager : MonoBehaviour {
        public static ClientManager Instance;
        public static DancePerformanceScriptableObject[] openPerformance;
        public static DanceData[] openDanceData;
        public static bool openLoaded = false;


        public DancePerformanceScriptableObject[] performances;
        public AvatarDisplay display;
        public ScoreDisplayJD scoreDisplay;

        public float songTime => audioSource?.time ?? 0;
        public bool paused => !audioSource.isPlaying;

        private DancePerformanceScriptableObject currentPerformance => performances[currentPerformanceNr];
        private int currentPerformanceNr = 0;
        private DanceData danceData;
        private AudioClip song;
        private AudioSource audioSource;
        private List<float> goals;
        private int currentGoal = 0;
        private int currentScore = 0;

        private int currentPoseIndex = 0;

        private int requestCounter = 0;


        private void Awake() {
            if (Instance != null) {
                Destroy(this);
            } else {
                Instance = this;
            }

            openPerformance = performances;
            openDanceData = new DanceData[performances.Length];
            for (int i = 0; i < performances.Length; i++) {
                openDanceData[i] = performances[i].danceData.LoadDanceDataFromScriptableObject();
            }


            audioSource = GetComponent<AudioSource>();
            audioSource.Pause();
            audioSource.loop = false;
            LoadSong(0, false);

            openLoaded = true;
        }

        // Update is called once per frame
        void Update() {
            if (!paused) {
                float timeOffset = audioSource.time - danceData.poses[currentPoseIndex].timestamp;
                DancePose interpolatedPose = danceData.GetInterpolatedPose(currentPoseIndex, out currentPoseIndex, timeOffset);
                display.SetPose(interpolatedPose.toPoseData());

                // if next goal is within scoring distance
                // check if end is reached
                if (currentGoal < goals.Count) {
                    float timeToNextGoal = goals[currentGoal] - songTime;
                    GoalType type = currentPerformance.goalDuration[currentGoal] > 0f ? GoalType.MOTION : GoalType.POSE;
                    if (type == GoalType.MOTION && timeToNextGoal < ServerManager.MotionWindow ||
                        type == GoalType.POSE && timeToNextGoal < ServerManager.poseSurrounding) {
                        Debug.Log("Send Request");
                        sendGoalRequest(currentGoal++);
                    }
                }
            }
        }

        public void LoadSong(int performanceNr, bool playAfter) {
            currentPerformanceNr = performanceNr;
            audioSource.clip = currentPerformance.SongObject.SongClip;
            audioSource.Pause();
            danceData = openDanceData[currentPerformanceNr];
            goals = currentPerformance.goalStartTimestamps;
            if (playAfter) {
                RestartSong();
            }
        }

        public void RestartSong() {
            audioSource.time = 0;
            audioSource.PlayDelayed(0.5f);
            currentGoal = 0;
            currentScore = 0;
        }

        public void ScoreResponse(int requestId, float score) {
            Debug.Log("Got Answer " + requestId + " with score " + score);
            int newScore = Mathf.RoundToInt(score * 1000);
            scoreDisplay.addScore(newScore);
            scoreDisplay.showScore(score > 0.7 ? Scores.GREAT : score > 0.5 ? Scores.GOOD : Scores.BAD);
            currentScore += newScore;
        }

        private void sendGoalRequest(int goalNr) {
            if (Client.Instance.fake) {
                ScoreResponse(requestCounter++, 1f);
            } else {
                EvaluatePoseRequestPacket pkt = new EvaluatePoseRequestPacket() {
                    trackNr = currentPerformanceNr,
                    PoseIndex = goalNr,
                    RequestId = requestCounter++
                };

                Client.Instance.SendPacket(pkt, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
        }
    }
}