using Communiction.Server;
using System.Collections.Generic;
using UnityEngine;

namespace PoseTeacher {

    public class ServerManager : MonoBehaviour {
        public static ServerManager Instance;
        // Scoring windows around the goals
        public static float poseSurrounding = 0.75f;
        public static float MotionWindow = 0.5f;

        PoseGetter selfPoseInputGetter;

        public DancePerformanceScriptableObject[] DancePerformances;
        public AvatarDisplay defaultTeacher;
        public InputSource selfPoseInputSource = InputSource.KINECT;

        public PoseData currentSelfPose;
        public PoseData goal;
        public AvatarDisplay goalTeacher;

        public bool debug = false;
        public int[] debugValues = { 0, 0, 0 };

        private DanceData[] danceData;
        private bool assigned;

        private List<(int trackId, int goalId, GoalType type, float startTime, int requestId, List<float> scores)> goalList = 
            new List<(int trackId, int goalId, GoalType type, float startTime, int requestId, List<float> scores)>();

        public void Awake() {
            if (Instance != null) {
                Destroy(this);
            } else {
                Instance = this;
            }
        }

        public void Start() {
            danceData = new DanceData[DancePerformances.Length];
            for (int i = 0; i < DancePerformances.Length; i++) {
                danceData[i] = DancePerformances[i].danceData.LoadDanceDataFromScriptableObject();
            }

            selfPoseInputGetter = new KinectPoseGetter();
        }

        public void Update() {
            if (debug) {
                debug = false;
                AddGoal(debugValues[0], debugValues[1], debugValues[2]);
            }

            currentSelfPose = selfPoseInputGetter.GetNextPose();

            defaultTeacher.SetPose(currentSelfPose);
            if(!assigned && Time.time > 3f) {
                //Debug.Log("Assigned Goal");
                assigned = true;
                goal = currentSelfPose;
                if (goalTeacher.gameObject.activeInHierarchy) {
                    goalTeacher.SetPose(goal);
                }
            }

            if(assigned) {
                //Debug.Log("Score = " + quaternionDistanceScore(DancePose.fromPoseData(goal), currentSelfPose));
            }

            for (int i = 0; i < goalList.Count; i++) {
                var curGoal = goalList[i];
                var curDD = danceData[curGoal.trackId];
                var curPf = DancePerformances[curGoal.trackId];
                var goalStartTime = curPf.goalStartTimestamps[curGoal.goalId];
                if (curGoal.type == GoalType.POSE) {
                    // if after starttime-window
                    if (Time.time > curGoal.startTime - poseSurrounding) {
                        // for -windows to +window, check if pose is held anytime between
                        DancePose actual = curDD.poses[getClosestId(curDD, goalStartTime)];
                        curGoal.scores.Add(quaternionDistanceScore(actual, currentSelfPose));
                    }
                } else {
                    // if after starttime
                    if (Time.time > curGoal.startTime) {
                        // for start to end, check if motion within +- window is held
                        float startingTime = goalStartTime - MotionWindow + (Time.time - curGoal.startTime);
                        float minWindowScore = 1000f;
                        int index = getClosestId(curDD, startingTime);
                        while(index < curDD.poses.Count) {
                            if(curDD.poses[index].timestamp > startingTime + 2 * MotionWindow) {
                                break;
                            }
                            // take min of iterated poses
                            minWindowScore = Mathf.Min(minWindowScore, quaternionDistanceScore(curDD.poses[index], currentSelfPose));
                            index++;
                        }
                        curGoal.scores.Add(minWindowScore);
                    }
                }
            }

            // loop through goals to see which should be finished
            for (int i = 0; i < goalList.Count; i++) {
                var curGoal = goalList[i];
                var curPf = DancePerformances[curGoal.trackId];
                if (curGoal.type == GoalType.POSE) {
                    // if after starttime + window
                    if (Time.time > curGoal.startTime + poseSurrounding) {
                        // calculate score and send
                        // take min of scores for pose
                        float minScore = 1000f;
                        for (int j = 0; j < curGoal.scores.Count; j++) {
                            minScore = Mathf.Min(minScore, curGoal.scores[j]);
                        }
                        returnGoal(curGoal.requestId, minScore);
                        // remove goal from list
                        goalList.RemoveAt(i);
                        // reset loop
                        i = -1;
                    }
                } else {
                    // if after end time
                    if (Time.time > curGoal.startTime + curPf.goalDuration[curGoal.goalId]) {
                        // calculate score and send
                        // take avg of scores for motion
                        float score = 0f;
                        for (int j = 0; j < curGoal.scores.Count; j++) {
                            score += curGoal.scores[j];
                        }
                        returnGoal(curGoal.requestId, score / curGoal.scores.Count);
                        // remove goal from list
                        goalList.RemoveAt(i);
                        // reset loop
                        i = -1;
                    }
                }
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

        public void AddGoal(int trackId, int goalId, int requestNr) {
            GoalType type = DancePerformances[trackId].goalDuration[goalId] > 0f ? GoalType.MOTION : GoalType.POSE;
            // the start time is measured without the windows
            float startTime = Time.time + (type == GoalType.POSE ? poseSurrounding : MotionWindow);
            var entry = (trackId, goalId, type, startTime, requestNr, new List<float>());
            goalList.Add(entry);
            Debug.Log("Added new goal to track: trackid: " + trackId + ", goalId: " + goalId);
        }

        private void returnGoal(int requestNr, float score) {
            Server.Instance.SendResultToAll(requestNr, Mathf.Max(0, 1-score));
        }

        private int getClosestId(DanceData danceData, float goalTime) {
            for (int i = 0; i < danceData.poses.Count; i++) {
                if(danceData.poses[i].timestamp > goalTime) {
                    return i;
                }
            }
            return danceData.poses.Count - 1;
        }
    }
}
