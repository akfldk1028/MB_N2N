using System.Collections;
using System.Collections.Generic;
using UnityEngine;


	public class CommonVars : MonoBehaviour
	{

		public static int level = 1;
		public static int numberOfBalls = 1;
		public static int newBalls = 1;
		public static int ballHitBottom = 0;
		public static bool lastBallHitBottom = false;
		public static bool startMovingTowardsMainBall = false;
		public static int ballsReachedDistance = 0;
		public static bool firstBallHitBottomCollider = false;
		public static float firstBallHitXPos = 0;
		public static bool canContinue = true;
		public static bool newWaveOfBricks = false;
		public static float speedUpTimer = 0;

		public static void RestartAllVariables()
		{
			CommonVars.level = 1;
			CommonVars.numberOfBalls = 1;
			CommonVars.newBalls = 1;
			CommonVars.ballHitBottom = 0;
			CommonVars.lastBallHitBottom = false;
			CommonVars.startMovingTowardsMainBall = false;
			CommonVars.ballsReachedDistance = 0;
			CommonVars.firstBallHitBottomCollider = false;
			CommonVars.firstBallHitXPos = 0;
			CommonVars.canContinue = true;
			CommonVars.newWaveOfBricks = false;
			CommonVars.speedUpTimer = 0;
		}
	}
