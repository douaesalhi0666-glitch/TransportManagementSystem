using Microsoft.ML;
using Microsoft.ML.Trainers;
using TransportManagementSystem.Models;
using System;
using System.Collections.Generic;

namespace TransportManagementSystem.Services
{
    public class ETAPredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;

        public ETAPredictionService()
        {
            _mlContext = new MLContext();
            TrainModel();
        }

        private void TrainModel()
        {
            var trainingData = new List<ETAData>
            {
                new ETAData { DistanceKm = 5f, Hour = 8f, DayOfWeek = 1f, IsPeakHour = 1f, TrafficLevel = 1f, ActualMinutes = 25f },
                new ETAData { DistanceKm = 5f, Hour = 14f, DayOfWeek = 1f, IsPeakHour = 0f, TrafficLevel = 0f, ActualMinutes = 18f },
                new ETAData { DistanceKm = 10f, Hour = 8f, DayOfWeek = 1f, IsPeakHour = 1f, TrafficLevel = 1f, ActualMinutes = 48f },
                new ETAData { DistanceKm = 10f, Hour = 21f, DayOfWeek = 5f, IsPeakHour = 0f, TrafficLevel = 0f, ActualMinutes = 35f },
                new ETAData { DistanceKm = 2f, Hour = 17f, DayOfWeek = 3f, IsPeakHour = 1f, TrafficLevel = 1f, ActualMinutes = 12f },
                new ETAData { DistanceKm = 15f, Hour = 10f, DayOfWeek = 2f, IsPeakHour = 0f, TrafficLevel = 0f, ActualMinutes = 52f },
                new ETAData { DistanceKm = 8f, Hour = 13f, DayOfWeek = 4f, IsPeakHour = 0f, TrafficLevel = 1f, ActualMinutes = 32f },
                new ETAData { DistanceKm = 25f, Hour = 18f, DayOfWeek = 5f, IsPeakHour = 1f, TrafficLevel = 2f, ActualMinutes = 90f },
            };

            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            var pipeline = _mlContext.Transforms.Concatenate("Features", nameof(ETAData.DistanceKm), nameof(ETAData.Hour), nameof(ETAData.DayOfWeek), nameof(ETAData.IsPeakHour), nameof(ETAData.TrafficLevel))
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: nameof(ETAData.ActualMinutes), maximumNumberOfIterations: 100));

            _model = pipeline.Fit(dataView);
        }

        public float PredictETA(float distanceKm, DateTime currentTime, int trafficLevel = 0)
        {
            if (_model == null) return 30; // fallback

            float hour = currentTime.Hour;
            float dayOfWeek = (int)currentTime.DayOfWeek;
            float isPeakHour = (hour >= 7 && hour <= 9) || (hour >= 17 && hour <= 19) ? 1 : 0;

            var input = new ETAData
            {
                DistanceKm = distanceKm,
                Hour = hour,
                DayOfWeek = dayOfWeek,
                IsPeakHour = isPeakHour,
                TrafficLevel = trafficLevel
            };
            var predEngine = _mlContext.Model.CreatePredictionEngine<ETAData, ETAPrediction>(_model);
            var prediction = predEngine.Predict(input);
            return Math.Max(1, prediction.EstimatedMinutes);
        }

        private class ETAData
        {
            public float DistanceKm { get; set; }
            public float Hour { get; set; }
            public float DayOfWeek { get; set; }
            public float IsPeakHour { get; set; }
            public float TrafficLevel { get; set; }
            public float ActualMinutes { get; set; }
        }
    }
}