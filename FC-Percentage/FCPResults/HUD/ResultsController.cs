﻿#nullable enable

using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using FCPercentage.FCPCore;
using FCPercentage.FCPResults.CalculationModels;
using FCPercentage.FCPResults.Configuration;
using HMUI;
using SiraUtil.Logging;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using Zenject;

namespace FCPercentage.FCPResults.HUD
{
	abstract class ResultsController : IInitializable, IDisposable
	{
		internal abstract string ResourceNameFCPercentage { get; }
		internal abstract string ResourceNameFCScore { get; }

		// Text fields in the bsml
		[UIComponent("fcScoreText")]
		public TextMeshProUGUI? fcScoreText = null!;
		[UIComponent("fcScoreDiffText")]
		public TextMeshProUGUI? fcScoreDiffText = null!;
		[UIComponent("fcPercentText")]
		public TextMeshProUGUI? fcPercentText = null!;
		[UIComponent("fcPercentDiffText")]
		public TextMeshProUGUI? fcPercentDiffText = null!;

		private readonly SiraLog logger;
		internal abstract ResultsSettings config { get; set; }
		internal readonly ScoreManager scoreManager;
		internal abstract ResultsTextFormattingModel textModel { get; set; }
		internal LevelCompletionResults levelCompletionResults = null!;
		internal ViewController resultsViewController;

		// Checks if the result should be shown
		private bool IsActiveOnResultsView(ResultsViewModes mode) => mode == ResultsViewModes.On ||
																	(mode == ResultsViewModes.OffWhenFC && !IsFullCombo);
		private bool IsLabelEnabled(ResultsViewLabelOptions labelOption) => (labelOption == ResultsViewLabelOptions.BothOn || labelOption == ResultsViewLabelOptions.PercentageOn);
		private bool IsFullCombo => levelCompletionResults != null && levelCompletionResults.fullCombo;

		public ResultsController(SiraLog logger, ScoreManager scoreManager, ViewController resultsViewController)
		{
			this.logger = logger;
			this.scoreManager = scoreManager;
			this.resultsViewController = resultsViewController;
		}

		internal abstract LevelCompletionResults? GetLevelCompletionResults();
		internal abstract GameObject GetViewControllerGameObject();

		public void Initialize()
		{
			if (resultsViewController != null)
				resultsViewController.didActivateEvent += ResultsViewController_OnActivateEvent;
		}

		public void Dispose()
		{
			if (resultsViewController != null)
				resultsViewController.didActivateEvent -= ResultsViewController_OnActivateEvent;
		}

		internal void ResultsViewController_OnActivateEvent(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			LevelCompletionResults? levelCompletionResults = GetLevelCompletionResults();

			if (levelCompletionResults != null)
			{
				scoreManager.NotifyOfSongEnded(levelCompletionResults.modifiedScore);
				ParseAllBSML();

				if (levelCompletionResults.levelEndStateType == global::LevelCompletionResults.LevelEndStateType.Cleared)
					SetResultsViewText();
				else
					EmptyResultsViewText();
			}
			else
			{
				ParseAllBSML();
				EmptyResultsViewText();
			}
		}

		private void ParseAllBSML()
		{
			if (fcScoreText == null)
			{
				ParseBSML(ResourceNameFCScore, GetViewControllerGameObject());

				if (fcScoreDiffText != null)
					fcScoreDiffText.fontSize *= 0.85f;
				else
					logger.Error($"Parsing BSML ({ResourceNameFCScore}) has failed.");
			}
			if (fcPercentText == null)
			{
				ParseBSML(ResourceNameFCPercentage, GetViewControllerGameObject());

				if (fcPercentDiffText != null)
					fcPercentDiffText.fontSize *= 0.85f;
				else
					logger.Error($"Parsing BSML ({ResourceNameFCPercentage}) has failed.");
			}
		}

		private void ParseBSML(string bsmlPath, GameObject parentGameObject)
		{
			BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), bsmlPath), parentGameObject, this);
		}

		internal void SetResultsViewText()
		{
			// Empty the text fields so they can be filled with new information
			EmptyResultsViewText();
			textModel.RefreshPercentageTextFormatting();

			SetPercentageText();
			SetScoreText();
		}

#pragma warning disable CS8602 // Null references will be fixed in ParseAllBSML() before this method is called.
		internal void SetPercentageText()
		{
			bool isPercentageAdded = false;
			// Add total percentage if enabled.
			if (IsActiveOnResultsView(config.PercentageTotalMode))
			{
				isPercentageAdded = true;
				fcPercentText.text += textModel.GetTotalPercentageText();

				// Add the total percentage difference if enabled.
				if (config.EnableScorePercentageDifference && textModel.HasValidDifferenceResult())
					fcPercentDiffText.text += textModel.GetTotalPercentageDiffText();
			}
			// Add split percentage if enabled.
			if (IsActiveOnResultsView(config.PercentageSplitMode))
			{
				isPercentageAdded = true;
				fcPercentText.text += textModel.GetSplitPercentageText();

				// Add the split percentage difference if enabled.
				if (config.EnableScorePercentageDifference && textModel.HasValidDifferenceResult())
					fcPercentDiffText.text += textModel.GetSplitPercentageDiffText();
			}

			// Set prefix label if enabled.
			if (isPercentageAdded && IsLabelEnabled(config.EnableLabel))
				fcPercentText.text = config.Advanced.PercentagePrefixText + fcPercentText.text;

			fcPercentText.text = fcPercentText.text.TrimEnd();
			fcPercentDiffText.text = fcPercentDiffText.text.TrimEnd();
		}

		internal void SetScoreText()
		{
			bool isScoreAdded = false;
			// Add total score if it's enabled.
			if (IsActiveOnResultsView(config.ScoreTotalMode))
			{
				isScoreAdded = true;
				fcScoreText.text += textModel.GetScoreText();

				// Add the score difference if it's enabled.
				if (config.EnableScorePercentageDifference && textModel.HasValidDifferenceResult())
					fcScoreDiffText.text += textModel.GetScoreDiffText();
			}

			// Set prefix label if enabled.
			if (isScoreAdded && (config.EnableLabel == ResultsViewLabelOptions.BothOn || config.EnableLabel == ResultsViewLabelOptions.ScoreOn))
			{
				fcScoreText.text = config.Advanced.ScorePrefixText + fcScoreText.text;
			}
		}

		private void EmptyResultsViewText()
		{
			fcScoreText.text = "";
			fcScoreDiffText.text = "";
			fcPercentText.text = "";
			fcPercentDiffText.text = "";
		}
#pragma warning restore CS8602 // Restore Dereference of a possibly null reference.


		internal DiffCalculationModel GetDiffCalculationModel()
		{
			switch (config.ScorePercentageDiffModel)
			{
				case ResultsViewDiffModels.CurrentResultDiff:
					return new CurrentResultDiffCalculationModel(scoreManager, config, levelCompletionResults);
				case ResultsViewDiffModels.OldHighscoreDiff:
					return new OldHighscoreDiffCalculationModel(scoreManager, config, levelCompletionResults);
				case ResultsViewDiffModels.UpdatedHighscoreDiff:
					return new UpdatedHighscoreDiffCalculationModel(scoreManager, config, levelCompletionResults);
			}

			logger.Error("GetDiffcalculationModel: Unable to get DiffCalculationModel. Value: " + config.ScorePercentageDiffModel);
			return GetDefaultDiffCalculationModel();
		}

		internal DiffCalculationModel GetDefaultDiffCalculationModel() => new OldHighscoreDiffCalculationModel(scoreManager, config, levelCompletionResults);

	}
}
