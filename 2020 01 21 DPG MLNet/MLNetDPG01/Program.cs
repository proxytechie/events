﻿using System;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace MLNetDPG01
{
    class Program
    {
        static void Main(string[] args)
        {
            var mlContext = new MLContext(seed: 1);

            // 01 Data
            var dataView = mlContext.Data.LoadFromTextFile<SentimentIssue>("wikiDetoxAnnotated40kRowsVacuum.tsv",
                hasHeader: true);
            var trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var trainingData = trainTestSplit.TrainSet;
            var testData = trainTestSplit.TestSet;

            var dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentIssue.Text));

            // 02 Build
            var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
            var trainingPipeline = dataProcessPipeline.Append(trainer);

            // Train
            var trainedModel = trainingPipeline.Fit(trainingData);

            // Evaluate
            var predictions = trainedModel.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");
            //ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);

            // Save
            //string modelPath = @"model.zip";
            //mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
            //Console.WriteLine("The model is saved to {0}", modelPath);

            // 03 Predict
            var predEngine = mlContext.Model.CreatePredictionEngine<SentimentIssue, SentimentPrediction>(trainedModel);

            Console.WriteLine($"=============== Single Prediction  ===============");
            Predict("I love this movie!", predEngine);
            Predict("This movie is stupid", predEngine);
            Predict("The vacuum that I got from BestBuy is amazing!", predEngine);
            Predict("This vacuum sucks so much dirt", predEngine);
            Console.WriteLine($"================End of Process.Hit any key to exit==================================");

            Console.ReadLine();
        }

        public static void Predict(string text, PredictionEngine<SentimentIssue, SentimentPrediction> predEngine)
        {
            // prediction
            var testSentiment = new SentimentIssue { Text = text };
            var prediction = predEngine.Predict(testSentiment);

            // Console Write
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Text: {testSentiment.Text}");
            Console.ForegroundColor = prediction.Prediction ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"    Prediction: {(Convert.ToBoolean(prediction.Prediction) ? "Toxic" : "Good")} | Probability of being toxic: {prediction.Probability} ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
        }

    }

    public class SentimentIssue
    {
        [LoadColumn(0)]
        public bool Label { get; set; }
        [LoadColumn(2)]
        public string Text { get; set; }
    }
    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }

}