using System.Collections.Generic;
using UnityEngine;

namespace PoseTeacher {

    public class ServerManager : MonoBehaviour {
        public static ServerManager Instance;

        PoseGetter selfPoseInputGetter;

        public DancePerformanceScriptableObject DancePerformanceObject;
        public AvatarDisplay defaultTeacher;
        public InputSource selfPoseInputSource = InputSource.KINECT;
        public PoseData currentSelfPose;
        public PoseData goal;
        public AvatarDisplay goalTeacher;


        private DanceData danceData;
        private bool assigned;

        List<(float, DanceData)> goals = new List<(float, DanceData)>();

        int currentId = 0;

        public void Awake() {
            if (Instance != null) {
                Destroy(this);
            } else {
                Instance = this;
            }
        }

        public void Start() {
            danceData = DancePerformanceObject.danceData.LoadDanceDataFromScriptableObject();
            selfPoseInputGetter = new KinectPoseGetter();
        }

        public void Update() {
            currentSelfPose = selfPoseInputGetter.GetNextPose();

            defaultTeacher.SetPose(currentSelfPose);
            if(!assigned && Time.time > 3f) {
                Debug.Log("Assigned Goal");
                Debug.Log(currentSelfPose);
                assigned = true;
                goal = currentSelfPose;
                goalTeacher.SetPose(goal);
            }

            if(assigned) {
                //Debug.Log("Score = " + quaternionDistanceScore(DancePose.fromPoseData(goal), currentSelfPose));
            }
        }

        private float quaternionDistanceScore(DancePose goalPose, PoseData currentSelfPose) {
            float distanceTotal = 0.0f;
            List<Quaternion> selfList = QuaternionUtils.PoseDataToOrientation(currentSelfPose);
            List<Quaternion> goalList = QuaternionUtils.DancePoseToOrientation(goalPose);

            for (int i = 0; i < 8; i++) {
                float distance = QuaternionUtils.quaternionDistance(selfList[i], goalList[i]);
                distanceTotal += Mathf.Pow(distance, 2) * QuaternionUtils.quaternionWeightsPrioritizeArms[i];
            }
            return Mathf.Sqrt(distanceTotal / ScoringUtils.TotalWeights(QuaternionUtils.quaternionWeightsPrioritizeArms));
        }

        public void OnApplicationQuit() {
            selfPoseInputGetter.Dispose();
        }
    }
}
