using PoseTeacher;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DanceEditor : MonoBehaviour {
#if UNITY_EDITOR

    public PinchSlider slider;
    public ProgressIndicatorLoadingBar indicatorBar;
    public Task indicatorTask;

    public DancePerformanceScriptableObject DancePerformanceObject;
    public GameObject AvatarContainerObject;
    private AvatarContainer avatar;

    // Editor
    public int totalFrames;
    public float progress;
    public List<int> selectedFrames;
    public int activeSelector;
    public List<int> activeFrames;
    public List<int> goalFrames;

    public float framesPerMeter; 
    public float frameSize; 
    public float frameSpacing; 
    public float borderSpace;

    // visuals
    float startPos, endPos;
    public GameObject backgroundBar;
    public GameObject frameDisplay;
    public GameObject frame;
    public GameObject frameIndicator;
    public bool grabbed;
    Renderer[] frameRenderes;
    // Select frames by activating button and hovering over them.
    // Change colour to indicate selected frames.
    public PressableButton selectorToggle;
    public PressableButton deselectorToggle;
    public bool selecting;
    public bool deselecting;

    public PressableButtonHoloLens2 pauseToggle;
    public bool playing;

    // Data
    // todo: organize better for input
    // select type of dance
    private PoseGetter poseGetter;
    private string dance_path = "jsondata/2021_12_13-15_52_07.txt"; // impossible  // "jsondata/2021_12_13-15_27_58.txt"; // dancemonkey   // 
    private float timestep;
    public DanceDataScriptableObject danceObject;
    private DanceData danceData;

    private AudioClip song;
    private AudioSource audioSource;

    private int currentId = 0;
    private float startTime;

    // Start is called before the first frame update
    void Start() {
        Debug.Log("Starting the dance editor.");
        audioSource = GetComponent<AudioSource>();
        song = DancePerformanceObject.SongObject.SongClip;
        audioSource.clip = song;
        //song = audioSource.clip;

        this.playing = true;

        this.framesPerMeter = 500;
        this.frameSize = 0.0015f;
        this.frameSpacing = (1f / framesPerMeter) - frameSize;
        this.borderSpace = (frameSpacing + frameSize) * 10;

        // set frames accordingly: impossible 0.068f; dance monkey 0.052f;
        this.timestep = 0.068f;

        Debug.Log("Reading DanceData.");

        // JSON File to DanceData
        //LoadDancDataFromJSONFile();

        // Performance DanceData 
        this.danceData = DancePerformanceObject.danceData.LoadDanceDataFromScriptableObject(); // danceObject.LoadDanceDataFromScriptableObject();

        this.totalFrames = danceData.poses.Count;
        Debug.Log("danceData count: " + danceData.poses.Count);

        // set backgroundBar to the correct size and position
        this.backgroundBar.transform.localScale = Vector3.Scale(backgroundBar.transform.localScale,
            new Vector3(2 * borderSpace + frameSpacing + totalFrames * (frameSize + frameSpacing), 1, 1));

        float backgroundScaleX = backgroundBar.transform.localScale.x;
        float moveToNegOhFive = 0.5f - backgroundScaleX / 2f;
        this.backgroundBar.transform.localPosition -= new Vector3(moveToNegOhFive, 0, 0);

        // slider...

        SliderMinMax();
        Debug.Log(String.Format("Start end positions: {0}  {1}", startPos, endPos));

        // create frames and set active
        this.activeFrames = new List<int>();
        this.goalFrames = new List<int>();

        Debug.Log(String.Format("Number of poses: {0}", danceData.poses.Count));

        //impossible edit list
        //List<int> goalList = new List<int> { 15, 128, 129, 130, 131, 127, 126, 125, 124, 123, 122, 121, 120, 119, 118, 117, 116, 115, 114, 113, 112, 111, 110, 108, 107, 105, 103, 102, 101, 99, 98, 97, 96, 95, 94, 93, 92, 91, 90, 89, 88, 87, 100, 104, 106, 109, 85, 81, 77, 75, 72, 70, 69, 68, 67, 66, 65, 64, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 63, 71, 73, 74, 76, 78, 79, 80, 82, 83, 84, 86, 150, 149, 151, 152, 153, 154, 158, 157, 156, 155, 159, 160, 161, 162, 163, 168, 169, 170, 171, 172, 173, 167, 166, 165, 164, 179, 189, 274, 273, 272, 271, 270, 269, 268, 267, 266, 265, 264, 263, 262, 261, 260, 259, 258, 257, 256, 255, 254, 253, 252, 251, 250, 249, 248, 247, 246, 245, 244, 243, 242, 241, 240, 239, 238, 237, 236, 235, 234, 233, 232, 231, 230, 229, 228, 227, 226, 225, 224, 223, 222, 221, 220, 219, 218, 217, 216, 215, 214, 213, 212, 211, 210, 209, 208, 207, 206, 205, 204, 203, 202, 201, 200, 199, 198, 197, 196, 195, 194, 193, 192, 191, 190, 188, 187, 186, 185, 184, 183, 182, 181, 180, 178, 177, 176, 175, 174, 301, 300, 299, 298, 297, 296, 295, 294, 368, 369, 370, 367, 366, 365, 364, 363, 362, 361, 360, 359, 358, 357, 356, 355, 354, 353, 352, 351, 350, 349, 348, 347, 346, 345, 344, 343, 342, 341, 340, 339, 338, 337, 336, 335, 334, 333, 332, 331, 330, 329, 328, 327, 326, 325, 324, 323, 322, 321, 320, 319, 318, 317, 316, 315, 314, 313, 312, 311, 310, 309, 308, 307, 306, 305, 304, 303, 302, 427, 426, 425, 424, 423, 422, 421, 420, 419, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 442, 444, 447, 450, 454, 457, 460, 462, 464, 465, 466, 467, 463, 461, 459, 458, 456, 455, 453, 452, 451, 449, 448, 446, 445, 443, 441, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 378, 377, 376, 375, 374, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 408, 407, 406, 405, 415, 416, 417, 418, 524, 541, 540, 539, 538, 537, 536, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 638, 639, 640, 647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 683, 684, 685, 686, 687, 688, 689, 690, 691, 692, 693, 694, 695, 696, 697, 698, 699, 700, 701, 702, 703, 704, 705, 706, 707, 708, 709, 710, 711, 712, 713, 714, 715, 716, 717, 718, 719, 720, 721, 722, 723, 724, 725, 726, 727, 728, 729, 730, 733, 735, 738, 740, 742, 743, 745, 746, 747, 748, 749, 750, 751, 752, 753, 754, 755, 757, 758, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 770, 771, 772, 773, 774, 775, 776, 777, 778, 779, 780, 781, 782, 783, 784, 785, 786, 744, 741, 739, 736, 731, 732, 734, 737, 756, 759, 800, 799, 798, 801, 802, 803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 817, 818, 819, 820, 821, 822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 832, 833, 834, 835, 836, 837, 838, 839, 840, 841, 842, 843, 844, 845, 846, 847, 848, 849, 850, 851, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 862, 863, 864, 865, 866, 867, 868, 869, 871, 872, 873, 875, 876, 877, 879, 880, 881, 882, 883, 885, 886, 888, 884, 878, 874, 870, 887, 889, 906, 905, 904, 903, 902, 901, 900, 907, 908, 909, 910, 911, 912, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 925, 926, 927, 928, 929, 930, 931, 932, 933, 934, 935, 936, 937, 938, 939, 940, 941, 942, 943, 944, 945, 946, 947, 948, 949, 950, 951, 952, 953, 954, 955, 956, 957, 958, 959, 960, 961, 962, 963, 964, 965, 966, 967, 968, 969, 970, 971, 972, 975, 977, 980, 985, 989, 992, 987, 984, 983, 982, 981, 979, 978, 976, 974, 973, 986, 988, 990, 991, 1010, 1009, 1008, 1007, 1011, 1012, 1013, 1014, 1015, 1016, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 1030, 1031, 1032, 1033, 1034, 1035, 1036, 1037, 1038, 1039, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055, 1056, 1057, 1058, 1059, 1060, 1061, 1062, 1063, 1064, 1065, 1066, 1067, 1068, 1069, 1018, 1017, 1081, 1082, 1083, 1084, 1085, 1086, 1087, 1088, 1089, 1090, 1091, 1092, 1080, 1079, 1078, 1077, 1076, 1075, 1074, 1073, 1072, 1071, 1070, 1139, 1138, 1137, 1136, 1140, 1141, 1142, 1143, 1144, 1145, 1146, 1147, 1148, 1149, 1150, 1151, 1152, 1153, 1154, 1155, 1156, 1157, 1158, 1159, 1160, 1161, 1162, 1163, 1164, 1165, 1166, 1167, 1168, 1169, 1170, 1171, 1172, 1173, 1174, 1175, 1176, 1177, 1178, 1179, 1180, 1181, 1182, 1183, 1184, 1185, 1186, 1187, 1188, 1189, 1190, 1191, 1192, 1193, 1194, 1195, 1196, 1197, 1198, 1199, 1200, 1201, 1202, 1203, 1204, 1205, 1206, 1207, 1208, 1209, 1210, 1211, 1212, 1213, 1214, 1215, 1216, 1217, 1218, 1219, 1220, 1221, 1222, 1223, 1231, 1232, 1233, 1234, 1235, 1236, 1237, 1238, 1239, 1240, 1241, 1242, 1243, 1244, 1245, 1246, 1247, 1248, 1249, 1250, 1251, 1252, 1253, 1282, 1281, 1283, 1284, 1285, 1286, 1287, 1288, 1289, 1290, 1291, 1292, 1293, 1294, 1295, 1296, 1297, 1298, 1299, 1300, 1301, 1302, 1303, 1304, 1305, 1306, 1307, 1308, 1309, 1310, 1311, 1312, 1313, 1338, 1339, 1340, 1341, 1342, 1343, 1344, 1345, 1346, 1347, 1348, 1349, 1350, 1351, 1352, 1353, 1354, 1355, 1356, 1357, 1358, 1359, 1360, 1361, 1362, 1363, 1364, 1365, 1366, 1093, 1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1256, 1257, 1255, 1254, 1258, 1259, 1260, 1261, 1262, 1263, 1264, 1265, 1266, 1267, 1268, 1269, 1270, 1271, 1272, 1273, 1274, 1275, 1276, 1277, 1278, 1279, 1280 };
        //List<int> activeList = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 424, 425, 426, 427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 683, 684, 685, 686, 687, 688, 689, 690, 691, 692, 693, 694, 695, 696, 697, 698, 699, 700, 701, 702, 703, 704, 705, 706, 707, 708, 709, 710, 711, 712, 713, 714, 715, 716, 717, 718, 719, 720, 721, 722, 723, 724, 725, 726, 727, 728, 729, 730, 731, 732, 733, 734, 735, 736, 737, 738, 739, 740, 741, 742, 743, 744, 745, 746, 747, 748, 749, 750, 751, 752, 753, 754, 755, 756, 757, 758, 759, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 770, 771, 772, 773, 774, 775, 776, 777, 778, 779, 780, 781, 782, 783, 784, 785, 786, 787, 788, 789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 817, 818, 819, 820, 821, 822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 832, 833, 834, 835, 836, 837, 838, 839, 840, 841, 842, 843, 844, 845, 846, 847, 848, 849, 850, 851, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 862, 863, 864, 865, 866, 867, 868, 869, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 880, 881, 882, 883, 884, 885, 886, 887, 888, 889, 890, 891, 892, 893, 894, 895, 896, 897, 898, 899, 900, 901, 902, 903, 904, 905, 906, 907, 908, 909, 910, 911, 912, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 925, 926, 927, 928, 929, 930, 931, 932, 933, 934, 935, 936, 937, 938, 939, 940, 941, 942, 943, 944, 945, 946, 947, 948, 949, 950, 951, 952, 953, 954, 955, 956, 957, 958, 959, 960, 961, 962, 963, 964, 965, 966, 967, 968, 969, 970, 971, 972, 973, 974, 975, 976, 977, 978, 979, 980, 981, 982, 983, 984, 985, 986, 987, 988, 989, 990, 991, 992, 993, 994, 995, 996, 997, 998, 999, 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 1030, 1031, 1032, 1033, 1034, 1035, 1036, 1037, 1038, 1039, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055, 1056, 1057, 1058, 1059, 1060, 1061, 1062, 1063, 1064, 1065, 1066, 1067, 1068, 1069, 1070, 1071, 1072, 1073, 1074, 1075, 1076, 1077, 1078, 1079, 1080, 1081, 1082, 1083, 1084, 1085, 1086, 1087, 1088, 1089, 1090, 1091, 1092, 1093, 1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1102, 1103, 1104, 1105, 1106, 1107, 1108, 1109, 1110, 1111, 1112, 1113, 1114, 1115, 1116, 1117, 1118, 1119, 1120, 1121, 1122, 1123, 1124, 1125, 1126, 1127, 1128, 1129, 1130, 1131, 1132, 1133, 1134, 1135, 1136, 1137, 1138, 1139, 1140, 1141, 1142, 1143, 1144, 1145, 1146, 1147, 1148, 1149, 1150, 1151, 1152, 1153, 1154, 1155, 1156, 1157, 1158, 1159, 1160, 1161, 1162, 1163, 1164, 1165, 1166, 1167, 1168, 1169, 1170, 1171, 1172, 1173, 1174, 1175, 1176, 1177, 1178, 1179, 1180, 1181, 1182, 1183, 1184, 1185, 1186, 1187, 1188, 1189, 1190, 1191, 1192, 1193, 1194, 1195, 1196, 1197, 1198, 1199, 1200, 1201, 1202, 1203, 1204, 1205, 1206, 1207, 1208, 1209, 1210, 1211, 1212, 1213, 1214, 1215, 1216, 1217, 1218, 1219, 1220, 1221, 1222, 1223, 1224, 1225, 1226, 1227, 1228, 1229, 1230, 1231, 1232, 1233, 1234, 1235, 1236, 1237, 1238, 1239, 1240, 1241, 1242, 1243, 1244, 1245, 1246, 1247, 1248, 1249, 1250, 1251, 1252, 1253, 1254, 1255, 1256, 1257, 1258, 1259, 1260, 1261, 1262, 1263, 1264, 1265, 1266, 1267, 1268, 1269, 1270, 1271, 1272, 1273, 1274, 1275, 1276, 1277, 1278, 1279, 1280, 1281, 1282, 1283, 1284, 1285, 1286, 1287, 1288, 1289, 1290, 1291, 1292, 1293, 1294, 1295, 1296, 1297, 1298, 1299, 1300, 1301, 1302, 1303, 1304, 1305, 1306, 1307, 1308, 1309, 1310, 1311, 1312, 1313, 1314, 1315, 1316, 1317, 1318, 1319, 1320, 1321, 1322, 1323, 1324, 1325, 1326, 1327, 1328, 1329, 1330, 1331, 1332, 1333, 1334, 1335, 1336, 1337, 1338, 1339, 1340, 1341, 1342, 1343, 1344, 1345, 1346, 1347, 1348, 1349, 1350, 1351, 1352, 1353, 1354, 1355, 1356, 1357, 1358, 1359, 1360, 1361, 1362, 1363, 1364, 1365, 1366 };

        //dance monkey edit list
        //List<int> goalList = new List<int> { 212, 256, 290, 317, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 424, 425, 426, 427, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 683, 684, 685, 686, 687, 688, 689, 690, 691, 692, 693, 694, 695, 696, 697, 698, 699, 700, 701, 702, 703, 704, 705, 706, 707, 708, 709, 710, 711, 712, 713, 714, 715, 716, 717, 718, 719, 720, 721, 722, 723, 724, 725, 726, 727, 728, 729, 730, 731, 732, 733, 734, 735, 736, 779, 780, 781, 782, 783, 784, 785, 786, 787, 788, 789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 817, 818, 819, 820, 821, 822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 832, 833, 834, 835, 836, 837, 838, 839, 840, 841, 842, 843, 844, 845, 846, 847, 848, 849, 850, 851, 852, 853, 854, 855, 861, 862, 863, 864, 865, 866, 867, 868, 869, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 880, 881, 882, 883, 884, 885, 886, 887, 888, 889, 890, 891, 892, 893, 894, 895, 896, 897, 898, 899, 900, 901, 902, 903, 904, 905, 906, 907, 908, 909, 910, 911, 912, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 925, 926, 927, 928, 929, 930, 931, 932, 933, 934, 935, 936, 937, 938, 939, 940, 941, 942, 949, 950, 951, 952, 953, 954, 955, 956, 957, 958, 959, 960, 961, 962, 963, 964, 965, 966, 967, 968, 969, 970, 971, 972, 973, 974, 975, 976, 977, 978, 979, 980, 981, 982, 983, 984, 985, 986, 987, 988, 989, 990, 991, 992, 993, 994, 995, 996, 997, 998, 999, 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1033, 1034, 1035, 1036, 1037, 1038, 1039, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055, 1056, 1057, 1058, 1059, 1060, 1061, 1062, 1063, 1064, 1065, 1066, 1067, 1068, 1069, 1070, 1071, 1072, 1073, 1074, 1075, 1076, 1077, 1078, 1079, 1080, 1081, 1082, 1083, 1084, 1085, 1086, 1087, 1088, 1089, 1090, 1091, 1092, 1093, 1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1102, 1103, 1104, 1105, 1106, 1107, 1108, 1109, 1110, 1111, 1112, 1113, 1114, 1115, 1116, 1117, 1118, 1119, 1120, 1121, 1122, 1125, 1126, 1127, 1128, 1129, 1130, 1131, 1132, 1133, 1134, 1135, 1136, 1137, 1138, 1139, 1140, 1141, 1142, 1143, 1144, 1145, 1146, 1147, 1148, 1149, 1150, 1151, 1152, 1153, 1154, 1155, 1156, 1157, 1158, 1159, 1160, 1161, 1162, 1163, 1164, 1165, 1166, 1167, 1168, 1169, 1170, 1171, 1172, 1173, 1174, 1175, 1176, 1177, 1178, 1179, 1180, 1181, 1182, 1183, 1184, 1185, 1186, 1187, 1188, 1203, 1204, 1205, 1206, 1207, 1208, 1209, 1210, 1211, 1212, 1213, 1214, 1215, 1216, 1217, 1218, 1219, 1220, 1221, 1222, 1223, 1224, 1225, 1226, 1227, 1228, 1229, 1230, 1231, 1232, 1233, 1234, 1235, 1236, 1237, 1238, 1239, 1240, 1241, 1242, 1243, 1244, 1245, 1246, 1247, 1248, 1249, 1250, 1251, 1252, 1253, 1254, 1255, 1277, 1278, 1279, 1280, 1281, 1282, 1283, 1284, 1285, 1286, 1287, 1288, 1289, 1290, 1291, 1292, 1293, 1294, 1295, 1296, 1297, 1303, 1304, 1305, 1306, 1307, 1308, 1309, 1310, 1311, 1312, 1313, 1314, 1315, 1316, 1317, 1318, 1319, 1320, 1321, 1322, 1323, 1324, 1325, 1326, 1327, 1328, 1329, 1330, 1331, 1332, 1333, 1334, 1335, 1336, 1337, 1338, 1339, 1340, 1341, 1342, 1343, 1344, 1345, 1346, 1347, 1348, 1349, 1350, 1351, 1352, 1353, 1354, 1355, 1356, 1357, 1358, 1359, 1360, 1361, 1362, 1363, 1364, 1365, 1366, 1367, 1368, 1369, 1381, 1382, 1383, 1384, 1385, 1386, 1387, 1388, 1389, 1390, 1391, 1392, 1393, 1394, 1395, 1396, 1397, 1398, 1399, 1400, 1401, 1402, 1403, 1404, 1405, 1406, 1407, 1408, 1409, 1410, 1411, 1412, 1413, 1414, 1415, 1416, 1417, 1418, 1419, 1420, 1421, 1422, 1423, 1424, 1425, 1426, 1427, 1428, 1429, 1430, 1431, 1432, 1433, 1434, 1435, 1436, 1437, 1455, 1456, 1457, 1458, 1459, 1460, 1461, 1462, 1463, 1464, 1465, 1466, 1467, 1468, 1469, 1470, 1471, 1472, 1473, 1474, 1475, 1476, 1477, 1478, 1479, 1480, 1481, 1482, 1483, 1484, 1485, 1486, 1487, 1488, 1489, 1490, 1491, 1492, 1493, 1494, 1495, 1496, 1497, 1498, 1499, 1500, 1501, 1502, 1503, 1518 };
        //List<int> activeList = new List<int> { 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 531, 532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 737, 738, 739, 740, 741, 742, 743, 744, 745, 746, 747, 748, 749, 750, 751, 752, 753, 754, 755, 756, 757, 758, 759, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 770, 771, 772, 773, 774, 775, 776, 777, 778, 856, 857, 858, 859, 860, 943, 944, 945, 946, 947, 948, 1028, 1029, 1030, 1031, 1032, 1123, 1124, 1189, 1190, 1191, 1192, 1193, 1194, 1195, 1196, 1197, 1198, 1199, 1200, 1201, 1202, 1256, 1257, 1258, 1259, 1260, 1261, 1262, 1263, 1264, 1265, 1266, 1267, 1268, 1269, 1270, 1271, 1272, 1273, 1274, 1275, 1276, 1298, 1299, 1300, 1301, 1302, 1370, 1371, 1372, 1373, 1374, 1375, 1376, 1377, 1378, 1379, 1380, 1438, 1439, 1440, 1441, 1442, 1443, 1444, 1445, 1446, 1447, 1448, 1449, 1450, 1451, 1452, 1453, 1454, 1504, 1505, 1506, 1507, 1508, 1509, 1510, 1511, 1512, 1513, 1514, 1515, 1516, 1517, 1519, 1520, 1521, 1522, 1523, 1524, 1525, 1526, 1527, 1528, 1529, 1530, 1531, 1532, 1533, 1534, 1535, 1536, 1537, 1538, 1539, 1540, 1541, 1542, 1543, 1544, 1545, 1546, 1547 };

        for (int i = 0; i < totalFrames; ++i)
        {
            // create each frame
            float x_pos = frameSpacing / 2 + i * (frameSize + frameSpacing);
            GameObject indicator = Instantiate(frame, frameDisplay.transform);
            indicator.transform.localPosition += new Vector3(x_pos, 0, 0);
            indicator.transform.localScale = Vector3.Scale(indicator.transform.localScale, new Vector3(frameSize, 1, 1));
            // set all frames as desired
            activeFrames.Add(i);
            /*
            if (goalList.Contains(i))
            {
                goalFrames.Add(i);
            } 
            else if (activeList.Contains(i))
            {
                activeFrames.Add(i);
            }
            */
        }
        Debug.Log(String.Format("Actove {1}, Goals {2}.  Total Used frames: {0}.", activeFrames.Count + goalFrames.Count, activeFrames.Count, goalFrames.Count));


        this.frameRenderes = frameDisplay.GetComponentsInChildren<Renderer>();
        Debug.Log("Renderers: " + frameRenderes.Length);

        avatar = new AvatarContainer(AvatarContainerObject);
        avatar.ChangeActiveType(AvatarType.ROBOT);

        if (playing)
        {
            audioSource.Play();
        }

        startTime = DateTime.Now.Millisecond / 1000f;

        Debug.Log("finished editor initialization.");
    }
    
    // Update is called once per frame
    void Update() {
        //Debug.Log("In update 1");
        //PrintFrames();

        if (playing)
        {
            float timeOffset = audioSource.time - danceData.poses[currentId].timestamp; //  currentId * timestep;//
            //Debug.Log(String.Format("time: {0}    stamp: {1}    offset: {2}", audioSource.time, danceData.poses[currentId].timestamp, timeOffset));
            avatar.MovePerson(danceData.GetInterpolatedPose(currentId, out currentId, timeOffset).toPoseData());
            SetSliderPosition(currentId, timeOffset);
        }

        // show the current active frame: now for 3D boxes..
        //Debug.Log("In update 2");
        RenderFrames();
        //PrintFrames();
    }

    public void SliderChanged() {
        // also gets called when chaning via script, sight, so only do something if
        float time = slider.SliderValue;
        float songTime = audioSource.time / song.length;
        if (time < songTime + 0.01 && time > songTime - 0.01) {
            return;
        }
        Debug.Log("Reset");
        audioSource.time = time * song.length;
        currentId = 0;
    }

    public void ChangePitch(float pitch) {
        audioSource.pitch = pitch;
    }

    public void LoadDancDataFromJSONFile()
    {
        IEnumerator<string> SequenceEnum = System.IO.File.ReadLines(dance_path).GetEnumerator();
        int moveCounter = 0;
        DancePose CurrentDancePose;// = new DancePose();
        this.danceData = new DanceData();
        while (SequenceEnum.MoveNext())
        {
            //Debug.Log("-------  " + moveCounter);
            string frame_json = SequenceEnum.Current;
            //Debug.Log("json string: " + frame_json);
            PoseData CurrentPose = PoseDataUtils.JSONstring2PoseData(frame_json);
            //Debug.Log("Current pose has data of length " + CurrentPose.data.Length);
            CurrentDancePose = DancePose.fromPoseData(CurrentPose, timestep * moveCounter);
            //Debug.Log("Created dancepose w ts: " + CurrentDancePose.timestamp);
            this.danceData.poses.Add(CurrentDancePose);
            //Debug.Log("added pose to danceData");
            moveCounter++;
        }
    }

    public DancePerformanceScriptableObject DancePerformanceFromFrames(string songTitle, string songID)
    {
        DancePerformanceScriptableObject performance = new DancePerformanceScriptableObject();

        performance.songTitle = songTitle;
        performance.songId = songID;
        performance.dynamicGoals = true;
        performance.Difficulty = DanceDifficulty.Medium;
        Debug.Log("song title: " + performance.songTitle + ", id: " + performance.songId);

        DanceData danceData = DanceDataFromActiveFrames();
        Debug.Log("danceData count: " + danceData.poses.Count);
        DanceDataScriptableObject dance = DanceDataScriptableObject.DanceDataToScriptableObject(danceData, songTitle + "_" + songID, true);
        Debug.Log("scriptable dance bytelength: " + dance.DanceDataCompressed.Length);
        performance.danceData = dance;
        Debug.Log("scriptable dance bytelength in performance obj: " + performance.danceData.DanceDataCompressed.Length);


        (List<float>, List<float>) goalFrames = SaveGoalFrames();
        List<float> timestamps = goalFrames.Item1;
        List<float> durations = goalFrames.Item2;
        performance.goalStartTimestamps = timestamps;
        performance.goalDuration = durations;

        MusicDataScriptableObject music = MusicDataScriptableObject.CreateInstance<MusicDataScriptableObject>();
        music.SongName = songTitle;
        music.SongClip = song;
        music.Volume = 1;
        music.Attribution = "artist";
        performance.SongObject = music;

        return performance;
    }

    public void SaveDancePerformance()
    {
        string songTitle = "Impossible";
        string songID = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        DancePerformanceScriptableObject performance = DancePerformanceFromFrames(songTitle, songID);

        Debug.Log("byte len: " + performance.danceData.DanceDataCompressed.Length);
        DanceDataScriptableObject performanceDance = performance.danceData;

        MusicDataScriptableObject performanceMusic = performance.SongObject;

        // mark dirty to flush to disk
        EditorUtility.SetDirty(performance);
        EditorUtility.SetDirty(performanceDance);
        EditorUtility.SetDirty(performanceMusic);
        AssetDatabase.CreateAsset(performance, "Assets/Dances/Performance/" + songTitle + "_" + songID + ".asset");
        AssetDatabase.CreateAsset(performanceDance, "Assets/Dances/Performance/" + songTitle + "_" + songID + "-danceData" + ".asset");
        AssetDatabase.CreateAsset(performanceMusic, "Assets/Dances/Performance/" + songTitle + "_" + songID + "-songData" + ".asset");
        //AssetDatabase.AddObjectToAsset(performanceDance, performance);
        //AssetDatabase.AddObjectToAsset(performanceMusic, performance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Saved Dance to " + "Assets/Dances/Performance/" + songTitle + "_" + songID + ".asset");
    }

    public void SaveActiveFrames(float td = 0)
    {
        Debug.Log(String.Format("Saving all {0} active frames", activeFrames.Count));
        // TODO: change timestamps to be in order
        // save active frames into dance data
        DanceData activeData = new DanceData();
        activeData.poses = new List<DancePose>();
        DancePose copyPose = new DancePose();

        // if unsure what the timestamp difference was in the old dance
        // calculate it in average 
        if(td == 0)
        {
            float stampSum = 0;
            float lastTs = 0;
            foreach(DancePose p in danceData.poses)
            {
                float currentTs = p.timestamp;
                stampSum += (currentTs - lastTs);
                lastTs = currentTs;
            }
            td = stampSum / totalFrames;
        }
        //Debug.Log("Time difference: " + td);

        float initialTs = 0;
        for (int i = 0; i < totalFrames; ++i)
        {
            if (activeFrames.Contains(i) || goalFrames.Contains(i))
            {
                copyPose = danceData.poses[i];
                copyPose.timestamp = initialTs;
                initialTs += td;
                activeData.poses.Add(copyPose);
            }
        }

        DanceDataScriptableObject.SaveDanceDataToScriptableObject(activeData, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), true);

        activeData.SaveToJSON();

        Debug.Log("Saved all active Frames.");
    }

    public DanceData DanceDataFromActiveFrames(float td = 0)
    {
        Debug.Log(String.Format("Saving all {0} active frames", activeFrames.Count));
        // TODO: change timestamps to be in order
        // save active frames into dance data
        DanceData activeData = new DanceData();
        activeData.poses = new List<DancePose>();
        DancePose copyPose = new DancePose();

        // if unsure what the timestamp difference was in the old dance
        // calculate it in average 
        if (td == 0)
        {
            float stampSum = 0;
            float lastTs = 0;
            foreach (DancePose p in danceData.poses)
            {
                float currentTs = p.timestamp;
                stampSum += (currentTs - lastTs);
                lastTs = currentTs;
            }
            td = stampSum / totalFrames;
        }
        //Debug.Log("Time difference: " + td);

        float initialTs = 0;
        for (int i = 0; i < totalFrames; ++i)
        {
            if (activeFrames.Contains(i) || goalFrames.Contains(i))
            {
                copyPose = danceData.poses[i];
                copyPose.timestamp = initialTs;
                initialTs += td;
                activeData.poses.Add(copyPose);
            }
        }

        return activeData;
    }

    public (List<float>, List<float>) SaveGoalFrames(float td = 0)
    {
        List<float> goalStartTimestamps = new List<float>();
        List<float> goalDurations = new List<float>();

        DancePose copyPose = new DancePose();

        if (td == 0)
        {
            float stampSum = 0;
            float lastTs = 0;
            foreach (DancePose p in danceData.poses)
            {
                float currentTs = p.timestamp;
                stampSum += (currentTs - lastTs);
                lastTs = currentTs;
            }
            td = stampSum / totalFrames;
        }

        float initialTs = 0;
        bool firstGoalFrame = true;
        float goalDuration = 0;
        for (int i = 0; i < totalFrames + 1; ++i)
        {
            if (activeFrames.Contains(i) || goalFrames.Contains(i))
            {
                copyPose.timestamp = initialTs;

                if (goalFrames.Contains(i))
                {
                    if (firstGoalFrame)
                    {
                        goalStartTimestamps.Add(initialTs);
                        firstGoalFrame = false;
                    }
                    else
                    {
                        goalDuration += td;
                    }
                }
                else
                {
                    if(firstGoalFrame == false)
                    {
                        goalDurations.Add(goalDuration);
                        firstGoalFrame = true;
                        goalDuration = 0;
                    }
                }

                initialTs += td;

            }

            // check if a goal last until the very end
            if(i == totalFrames)
            {
                if (firstGoalFrame == false)
                {
                    goalDurations.Add(goalDuration);
                    firstGoalFrame = true;
                }
            }

        }

        return (goalStartTimestamps, goalDurations);
    }

    public (float, float) SliderMinMax()
    {
        // Call this function to get the min and max positions of the Slider
        // Important if Editor was moved
        Transform barT = backgroundBar.transform;
        float sPos = barT.position.x - (barT.localScale.x / 2) + borderSpace;
        float ePos = barT.position.x + (barT.localScale.x / 2) - borderSpace;
        startPos = sPos;
        endPos = ePos;
        return (startPos, endPos);
    }

    public float SliderValue()
    {
        SliderMinMax();
        Transform indicatorT = frameIndicator.transform;
        if(indicatorT.position.x < startPos)
        {
            indicatorT.position = new Vector3(startPos, indicatorT.position.y, indicatorT.position.z);
        }
        if (indicatorT.position.x > endPos)
        {
            indicatorT.position = new Vector3(endPos, indicatorT.position.y, indicatorT.position.z);
        }
        progress = (indicatorT.position.x - startPos) / (endPos - startPos);
        //Debug.Log(String.Format("start: {0}    pos: {1}    end: {2}    -> progress: {3}", startPos, indicatorT.position.x, endPos, progress));
        return progress;
    }

    public void UpdateSlider()
    {
        // On movement set take the new position and change the currently active frame
        float progress = SliderValue();
        audioSource.time = song.length * progress;
        currentId = (int)(totalFrames * progress);
        RenderFrames();
    }

    public void SetSliderPosition(int frame, float offset)
    {
        SliderMinMax();
        float barDist = (float)frame / (float)totalFrames;
        float frameDist = offset;

        float posx = startPos + (endPos - startPos) * barDist + (frameSpacing + frameSize) * frameDist;
        //Debug.Log(String.Format("start: {0}, end - start: {1}, bardist: {2}, first term: {3},  spacing + size: {4}, framedist: {5}, second term: {6}",
          //  startPos, (endPos - startPos), barDist, startPos + (endPos - startPos) * barDist, (frameSpacing + frameSize), frameDist, (frameSpacing + frameSize) * frameDist));
        frameIndicator.transform.position = new Vector3(posx, frameIndicator.transform.position.y, frameIndicator.transform.position.z);
        SliderValue();
    }

    public void RenderFrames()
    {
        //Debug.Log(String.Format("Rendering {0} Frames:", totalFrames));
        for (int i = 0; i < totalFrames; ++i)
        {
            //Debug.Log(String.Format("Frame {0} has {1}", i, frameRenderes[i].material.GetColor("_Color").ToString()));
            if (i == this.currentId)
            {
                //Debug.Log("current");
                //Call SetColor using the shader property name "_Color" and setting the color to red
                frameRenderes[i].material.SetColor("_BaseColor", Color.blue);
            }
            else if (selectedFrames.Contains(i))
            {
                //Debug.Log("selected");
                frameRenderes[i].material.color = Color.yellow; //SetColor("_BaseColor", Color.yellow);
            }
            else if (goalFrames.Contains(i))
            {
                //Debug.Log("goal");
                frameRenderes[i].material.SetColor("_BaseColor", Color.green);
            }
            else if (activeFrames.Contains(i))
            {
                //Debug.Log("active");
                frameRenderes[i].material.SetColor("_BaseColor", Color.red);
            }
            else
            {
                //Debug.Log("off");
                frameRenderes[i].material.SetColor("_BaseColor", Color.gray);
            }
        }
    }

    public void PrintFrames()
    {
        Debug.Log("total frames: " + totalFrames);
        string s = "goals: ";
        foreach(int i in goalFrames)
        {
            s += i.ToString() + ", ";
        }
        Debug.Log(s);
        
        s = "active: ";
        foreach (int i in activeFrames)
        {
            s += i.ToString() + ", ";
        }
        Debug.Log(s);
    }

    public void ButtonPauseToggle()
    {
        if(pauseToggle.GetComponent<Interactable>().IsToggled)
        {
            playing = false;
            audioSource.Pause();
        }
        else
        {
            playing = true;
            audioSource.Play();
        }
    }

    public void ButtonSelector(int type)
    {
        // select
        if(type == 0)
        {
            Debug.Log("selecting.");
            selecting = true;
            deselecting = false;
        }
        // deselect
        else if(type == 1)
        {
            Debug.Log("de-selecting.");
            selecting = false;
            deselecting = true;
        }
        else // nothing
        {
            Debug.Log("nothing");
            selecting = false;
            deselecting = false;
        }
    }

    public void HoverSelect(GameObject selfObj)
    {
        int frameIndex = GetFrameIndex(selfObj.transform.position.x);
        //Debug.Log("hovered over frame " + frameIndex.ToString());
        if (selecting)
        {
            //Debug.Log("hover and select");
            if (!selectedFrames.Contains(frameIndex))
            {
                //Debug.Log("Adding to selected frames.");
                selectedFrames.Add(frameIndex);
            }
        }
        if (deselecting)
        {
            if (selectedFrames.Contains(frameIndex))
            {
                selectedFrames.Remove(frameIndex);
            }
        }
    }

    public void ChangeFrameType(int type)
    {
        switch (type)
        {
            // in-active
            case 0:
                foreach(int f in selectedFrames)
                {
                    if (activeFrames.Contains(f))
                    {
                        activeFrames.Remove(f);
                    }
                    if (goalFrames.Contains(f))
                    {
                        goalFrames.Remove(f);
                    }
                }
                selectedFrames.Clear();
                break;

            // active
            case 1:
                foreach (int f in selectedFrames)
                {
                    if (!activeFrames.Contains(f))
                    {
                        activeFrames.Add(f);
                    }
                    if (goalFrames.Contains(f))
                    {
                        goalFrames.Remove(f);
                    }
                }
                selectedFrames.Clear();
                break;

            // goal
            case 2:
                foreach (int f in selectedFrames)
                {
                    if (!activeFrames.Contains(f))
                    {
                        activeFrames.Add(f);
                    }
                    if (!goalFrames.Contains(f))
                    {
                        goalFrames.Add(f);
                    }
                }
                selectedFrames.Clear();
                break;
                        
            // default
            default:
                Debug.Log("Changing to unexpected frame type.");
                break;

        }

    }

    public int GetFrameIndex(float posx)
    {
        SliderMinMax();
        // calculate the frame index given through parameters set in start.
        float fIdx = (posx - startPos + borderSpace/2 - (frameSpacing+frameSize)) / (frameSize + frameSpacing);
        //Debug.Log("Frame idx: " + fIdx);
        return (int)Math.Round(fIdx);
        
    }

#endif
}
