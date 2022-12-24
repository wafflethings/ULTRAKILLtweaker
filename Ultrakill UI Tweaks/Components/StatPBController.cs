using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Components
{
    public class StatPBController : MonoBehaviour
    {

        public void Start()
        {
            name = "PB Panel (UKt)";
            Destroy(GetComponent<LevelStats>());
            Destroy(gameObject.ChildByName("Secrets Title"));
            Destroy(gameObject.ChildByName("Challenge Title"));
            Destroy(gameObject.ChildByName("Assists Title"));
            GetComponent<RectTransform>().sizeDelta = new Vector2(transform.parent.GetComponent<RectTransform>().sizeDelta.x, transform.parent.GetComponent<RectTransform>().sizeDelta.y * (float)(220f / 320f));

            string TheFunny = SceneManager.GetActiveScene().name;
            StatsManager sman = MainClass.Instance.statman;

            if (Times.SceneToTime.ContainsKey(TheFunny))
            {
                string joe = TimeSpan.FromSeconds(Times.SceneToTime[TheFunny]).ToString();
                if (joe.StartsWith("00:"))
                    joe = joe.Substring(3, joe.Length - 7);
                else
                    joe = joe.Substring(0, joe.Length - 4);

                Value("Time").text = joe;
                Value("Kills").text = Times.SceneToKills[TheFunny].ToString();
                Value("Style").text = Times.SceneToStyle[TheFunny].ToString();

                Rank("Time").text = sman.GetRanks(sman.timeRanks, Times.SceneToTime[TheFunny], true, false);
                Rank("Kills").text = sman.GetRanks(sman.killRanks, Times.SceneToKills[TheFunny], false, false);
                Rank("Style").text = sman.GetRanks(sman.styleRanks, Times.SceneToStyle[TheFunny], false, false);
            }
            else
            {
                Rank("Time").text = sman.GetRanks(sman.timeRanks, 999999999999, true, false);
                Rank("Kills").text = sman.GetRanks(sman.killRanks, 0, false, false);
                Rank("Style").text = sman.GetRanks(sman.styleRanks, 0, false, false);

                Value("Time").text = "N/A";
                Value("Kills").text = "N/A";
                Value("Style").text = "N/A";
            }

            gameObject.ChildByName("Title").GetComponent<Text>().text += " PBs";
        }

        public Text Rank(string name)
        {
            return gameObject.ChildByName($"{name} Title").ChildByName($"{name} Rank").GetComponent<Text>();
        }

        public Text Value(string name)
        {
            return gameObject.ChildByName($"{name} Title").ChildByName($"{name}").GetComponent<Text>();
        }
    }
}
