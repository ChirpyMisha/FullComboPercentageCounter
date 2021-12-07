﻿using FullComboPercentageCounter.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace FullComboPercentageCounter
{
	public class ScoreManager : IInitializable, IDisposable
	{
		public event EventHandler OnScoreUpdate;

		public double Percentage => Math.Round(((double)ScoreTotal / (double)MaxScoreTotal) * 100, PluginConfig.Instance.DecimalPrecision);
		public string PercentageStr => Percentage.ToString(percentageStringFormat);
		public int ScoreTotal => ScoreA + ScoreB;
		public int ScoreA { get; private set; }
		public int ScoreB { get; private set; }
		public int MaxScoreTotal => MaxScoreA + MaxScoreB;
		public int MaxScoreA { get; private set; }
		public int MaxScoreB { get; private set; }
		public int MaxMissedScoreA { get; private set; }
		public int MaxMissedScoreB { get; private set; }
		public int MaxMissedScoreTotal => MaxMissedScoreA + MaxMissedScoreB;
		public int MissedScoreTotal => CalculateMissedScore(ScoreTotal, MaxScoreTotal, MaxMissedScoreTotal);
		public int ScoreTotalIncMissed => ScoreTotal + MissedScoreTotal;

		private string percentageStringFormat;

		public void Initialize()
		{
			ResetScore();

			percentageStringFormat = "0";
			int decimalPrecision = PluginConfig.Instance.DecimalPrecision;
			if (decimalPrecision > 0)
				percentageStringFormat += "." + new string('0', decimalPrecision);
		}

		public void Dispose()
		{
			return;
		}

		public void ResetScore()
		{
			ScoreA = 0;
			ScoreB = 0;
			MaxScoreA = 0;
			MaxScoreB = 0;
			MaxMissedScoreA = 0;
			MaxMissedScoreB = 0;
		}

		public void AddScore(ColorType colorType, int score, int multiplier)
		{
			// Update score for left or right saber
			if (colorType == ColorType.ColorA)
			{
				ScoreA += score * multiplier;
				MaxScoreA += ScoreModel.kMaxCutRawScore * multiplier;
			}
			else if (colorType == ColorType.ColorB)
			{
				ScoreB += score * multiplier;
				MaxScoreB += ScoreModel.kMaxCutRawScore * multiplier;
			}
			else
			{
				Plugin.Log.Warn($"scoreManager, AddScore: Failed to add score of [score={score}, multiplier={multiplier}]. Reason: colorType is invalid [colorType={colorType}].");
			}

			// Inform listeners that the score has updated
			InvokeScoreUpdate();
		}

		public void SubtractScore(ColorType colorType, int score, int multiplier, bool subtractFromMaxScore = false)
		{
			// Update score for left or right saber
			if (colorType == ColorType.ColorA)
			{
				ScoreA -= score * multiplier;
				if (subtractFromMaxScore) MaxScoreA -= ScoreModel.kMaxCutRawScore * multiplier;
			}
			else if (colorType == ColorType.ColorB)
			{
				ScoreB -= score * multiplier;
				if (subtractFromMaxScore) MaxScoreB -= ScoreModel.kMaxCutRawScore * multiplier;
			}
			else
			{
				Plugin.Log.Warn($"scoreManager, SubtractScore: Failed to subtract score of [score={score}, multiplier={multiplier}]. Reason: colorType is invalid [colorType={colorType}].");
			}

			// Inform listeners that the score has updated
			InvokeScoreUpdate();
		}

		internal void AddMissedScore(ColorType colorType, int maxMissedScore, int multiplier)
		{
			// Update max missed score for left or right saber
			if (colorType == ColorType.ColorA)
			{
				MaxMissedScoreA += maxMissedScore * multiplier;
			}
			else if (colorType == ColorType.ColorB)
			{
				MaxMissedScoreB += maxMissedScore * multiplier;
			}
			else
			{
				Plugin.Log.Warn($"scoreManager, AddMissedScore: Failed to subtract score of [score={maxMissedScore}, multiplier={multiplier}]. Reason: colorType is invalid [colorType={colorType}].");
			}
		}

		private int CalculateMissedScore(int score, int maxScore, int missedMaxScore)
		{
			double decPercent = ((double)score / (double)maxScore);
			int missedScore = (int)Math.Round(decPercent * missedMaxScore);
			return missedScore;
		}

		protected virtual void InvokeScoreUpdate()
		{
			Plugin.Log.Notice($"Score Has Updated - currentScore = {ScoreTotal}, currentMaxScore = {MaxScoreTotal}");

			// Create event handler
			EventHandler handler = OnScoreUpdate;
			if (handler != null)
			{
				// Invoke event
				handler(this, EventArgs.Empty);
			}
		}
	}
}
