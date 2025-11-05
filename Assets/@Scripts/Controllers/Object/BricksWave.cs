using UnityEngine;
using TMPro;


    public class BricksWave : MonoBehaviour
    {
        private int wave = 1;
        private Rigidbody2D rb;
        private TextMeshPro waveText;
        private AudioSource brickHitSound;
        [SerializeField] private Renderer brick; // Reference to the brick's renderer for color changes

        void Start()
        {
            // Cache required components
            // brickHitSound = GameObject.Find("brickHitSound").GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody2D>();
            Transform textTransform = transform.Find("brickWaveText");

            if (textTransform != null)
            {
                waveText = textTransform.GetComponent<TextMeshPro>();

                // Determine the number of hits required to break the brick based on level
                wave = CommonVars.level < 10 ? Random.Range(1, 3) : Random.Range(CommonVars.level / 5, CommonVars.level / 2);
                waveText.text = wave.ToString();
            }

            // Apply color if the GameObject name contains "brick"
            if (gameObject.name.Contains("brick"))
            {
                ColorBrick();
            }
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            // Play sound effect if not already playing
            // if (!brickHitSound.isPlaying)
            // {
            //     brickHitSound.Play();
            // }

            // Reduce wave count and update visual indicators
            wave--;
            ColorBrick();

            if (waveText != null)
            {
                waveText.text = wave.ToString();
            }

            // If wave reaches zero, handle brick destruction
            if (wave <= 0)
            {
                HandleBrickDestruction();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Handles the logic when a brick is destroyed, including tracking achievements.
        /// </summary>
        private void HandleBrickDestruction()
        {
            int bricksDestroyed = PlayerPrefs.GetInt("numberOfBricksDestroyed", 0) + 1;
            PlayerPrefs.SetInt("numberOfBricksDestroyed", bricksDestroyed);

            // Check if player has unlocked achievements
            // CheckAndUnlockAchievement(bricksDestroyed, 100, "destroy100bricks", "destroy 100 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 1000, "destroy1000bricks", "destroy 1000 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 10000, "destroy10000bricks", "destroy 10000 bricks");
        }

        /// <summary>
        /// Checks if an achievement should be unlocked based on the number of bricks destroyed.
        /// </summary>
        /// <param name="bricksDestroyed">The total number of bricks destroyed.</param>
        /// <param name="threshold">The required number of bricks to unlock the achievement.</param>
        /// <param name="achievementKey">The PlayerPrefs key for the achievement.</param>
        /// <param name="achievementName">The name of the achievement to display.</param>
        private void CheckAndUnlockAchievement(int bricksDestroyed, int threshold, string achievementKey, string achievementName)
        {
            if (bricksDestroyed >= threshold && PlayerPrefs.GetInt(achievementKey, 0) != 1)
            {
                PlayerPrefs.SetInt(achievementKey, 1);
                // AchievementUnlocked achievementUI = GameObject.Find("Canvas").GetComponent<AchievementUnlocked>();
                // achievementUI.enabled = true;
                // achievementUI.NameOfTheAchievement(achievementName);
            }
        }

        /// <summary>
        /// Adjusts the color of the brick based on the remaining wave count.
        /// </summary>
        public void ColorBrick()
        {
            if (wave <= 30)
            {
                brick.material.color = new Color(1, 1 - (wave / 30f), 0); // Transition from yellow to red
            }
            else if (wave <= 60)
            {
                brick.material.color = new Color(1, 0, (wave - 30) / 30f); // Transition from red to purple
            }
            else
            {
                float redColorValue = 1 - ((wave - 60) / 30f);
                brick.material.color = new Color(Mathf.Max(redColorValue, 0), 0, 1); // Transition from purple to blue
            }
        }
    }